using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyManager))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float timeToLocatePlayer = 2;
    [SerializeField] private float pushPower = 5;
    [SerializeField] private float pushSpeed = 10;
    [SerializeField] private float atackDistance = 2;
    [SerializeField] private float atackDelay = 1;
    [SerializeField] private int atackValue = 20;

    [HideInInspector] public Transform player;
    [HideInInspector] public ScrapManager scrapManager;

    private ScrapExplosion playerScrap;
    private NavMeshAgent meshAgent;
    private Vector3 currentPath;
    private bool hit;
    private Vector3 targetPos;
    private EnemyManager enemyManager;
    private bool atack;
    private bool atackIsActive;
    private AudioManager audioManager;
    private Rigidbody body;

    private void Start()
    {
        // Znajdź AudioManager i EnemyManager, upewnij się, że nie są null
        audioManager = AudioManager.AM != null ? AudioManager.AM : Object.FindAnyObjectByType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogWarning("Nie znaleziono AudioManager w scenie.");
        }

        enemyManager = GetComponent<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogError("Brak komponentu EnemyManager na obiekcie: " + gameObject.name);
        }

        // Znajdź komponent NavMeshAgent
        meshAgent = GetComponent<NavMeshAgent>();
        if (meshAgent == null)
        {
            Debug.LogError("Brak komponentu NavMeshAgent na obiekcie: " + gameObject.name);
        }

        body = GetComponent<Rigidbody>();

        if (meshAgent != null && meshAgent.enabled && !meshAgent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, 3f, meshAgent.areaMask))
        {
            transform.position = startHit.position;
            meshAgent.Warp(startHit.position);
        }

        if (!IsSceneObject(player))
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("NewPlayerBody");
            if (p != null) player = p.transform;
        }

        // Sprawdź, czy player został przypisany
        if (player == null)
        {
            Debug.LogError("Player nie został przypisany do EnemyMovement.");
        }
        else
        {
            playerScrap = player.GetComponent<ScrapExplosion>();
            if (playerScrap == null)
            {
                Debug.LogWarning("Nie znaleziono ScrapExplosion na graczu: " + player.name);
            }
        }

        if (scrapManager == null)
        {
            scrapManager = Object.FindFirstObjectByType<ScrapManager>();
        }

        // Sprawdź ScrapManager
        if (scrapManager == null)
        {
            Debug.LogWarning("ScrapManager nie został przypisany do EnemyMovement.");
        }

        // Wybierz pierwszą ścieżkę
        SelectPath();

        // Rozpocznij coroutine do lokalizowania gracza
        StartCoroutine(FindPlayerCoroutine());
    }

    private static bool IsSceneObject(Transform target)
    {
        return target != null &&
               target.gameObject.scene.IsValid() &&
               target.gameObject.scene.isLoaded;
    }

    private void Update()
    {
        if (player == null || hit)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool canUseAgent = meshAgent != null && meshAgent.enabled && meshAgent.isOnNavMesh;

        if (distance > atackDistance && canUseAgent)
        {
            meshAgent.SetDestination(currentPath);
            atack = false;
            atackIsActive = false;
        }
        else if (distance <= atackDistance && canUseAgent)
        {
            atack = true;
            if (!atackIsActive)
            {
                atackIsActive = true;
                StartCoroutine(AtackPlayerCourutine());
            }
        }
    }

    private void FixedUpdate()
    {
        if (!hit)
        {
            return;
        }

        Vector3 currentPosition = body != null ? body.position : transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPos,
            pushSpeed * Time.fixedDeltaTime);

        if (body != null)
        {
            body.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        if ((nextPosition - targetPos).sqrMagnitude <= 0.01f)
        {
            FinishPush();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Użyj CompareTag, aby porównać tagi
        if (hit && collision.gameObject.CompareTag("Wall"))
        {
            FinishPush();
        }
    }

    private void SelectPath()
    {
        if (player != null)
        {
            currentPath = player.position;
        }
        else
        {
            Debug.LogWarning("Nie można ustawić ścieżki, ponieważ player jest null.");
        }
    }

    private IEnumerator FindPlayerCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeToLocatePlayer);
            SelectPath();
        }
    }

    public void Push()
    {
        if (player == null || meshAgent == null)
        {
            return;
        }

        Vector3 pushDirection = transform.position - player.position;
        pushDirection.y = 0f;
        if (pushDirection.sqrMagnitude <= 0.001f)
        {
            pushDirection = -transform.forward;
        }

        atack = false;
        atackIsActive = false;

        if (meshAgent.enabled)
        {
            if (meshAgent.isOnNavMesh)
            {
                meshAgent.isStopped = true;
                meshAgent.ResetPath();
            }

            meshAgent.enabled = false;
        }

        targetPos = transform.position + pushDirection.normalized * pushPower;
        hit = true;

        if (audioManager != null)
        {
            audioManager.Play("EnemiesPush");
        }
    }

    private void FinishPush()
    {
        hit = false;

        if (meshAgent == null)
        {
            return;
        }

        float sampleDistance = Mathf.Max(2f, pushPower);
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, sampleDistance, meshAgent.areaMask))
        {
            if (body != null)
            {
                body.position = navHit.position;
            }
            else
            {
                transform.position = navHit.position;
            }

            meshAgent.enabled = true;
            meshAgent.Warp(navHit.position);
            meshAgent.isStopped = false;
        }
        else
        {
            Debug.LogWarning($"Nie znaleziono NavMesh po odepchnięciu przeciwnika '{name}'.", this);
        }
    }

    private IEnumerator AtackPlayerCourutine()
    {
        while (atack && playerScrap != null && scrapManager != null)
        {
            // Wyrzuć scrap i odejmij wartość ataku
            playerScrap.DropScrap();
            scrapManager.SubtractScraps(atackValue);

            if (audioManager != null)
            {
                audioManager.Play("EnemyAttack");
            }

            yield return new WaitForSeconds(atackDelay);
        }
    }
}
