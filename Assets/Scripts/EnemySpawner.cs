using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    [Header("Settings")]
    [FormerlySerializedAs("secondsToSpownEnemy")]
    [SerializeField] private float secondsToSpawnEnemy = 2f;
    [SerializeField] private int maxSpawnedEnemies = 3;
    [SerializeField] private float spawnRadius = 2f; // Promień wokół spawnera
    [SerializeField] private int enemiesPerSpawn = 1; // Liczba przeciwników do zespawnowania podczas jednej iteracji

    [Header("Objects")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private ScrapManager scrapManager;

    [FormerlySerializedAs("numberOfSpownedEnemies")]
    [HideInInspector] public int numberOfSpawnedEnemies;

    [Header("Wave")]
    [SerializeField] private bool finiteWave;
    [SerializeField, Min(0)]
    [Tooltip("Total enemies spawned by this spawner during a finite room wave. Level_01 uses this value.")]
    private int totalEnemiesToSpawn = 3;

    private int totalSpawnedEnemies;
    private bool waveClearedNotified;
    private Coroutine spawnRoutine;
    private bool initialized;

    public event System.Action<EnemySpawner> WaveCleared;

    public int MaxSpawnedEnemies => maxSpawnedEnemies;
    public int ActiveEnemies => numberOfSpawnedEnemies;
    public int TotalSpawnedEnemies => totalSpawnedEnemies;
    public int ConfiguredWaveSize => Mathf.Max(1, totalEnemiesToSpawn);
    public bool IsFiniteWave => finiteWave;
    public bool IsWaveCleared =>
        finiteWave &&
        totalSpawnedEnemies >= totalEnemiesToSpawn &&
        numberOfSpawnedEnemies == 0;

    private void Start()
    {
        if (!IsSceneObject(player))
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("NewPlayerBody");
            if (p != null) player = p.transform;
        }

        if (scrapManager == null)
        {
            scrapManager = Object.FindFirstObjectByType<ScrapManager>();
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("enemyPrefab nie jest przypisany w EnemySpawner.");
            return;
        }
        if (player == null)
        {
            Debug.LogError("Player nie jest przypisany w EnemySpawner.");
            return;
        }
        if (scrapManager == null)
        {
            Debug.LogError("ScrapManager nie jest przypisany w EnemySpawner.");
            return;
        }
        initialized = true;
        BeginSpawning();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            BeginSpawning();
        }
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void BeginSpawning()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLogicCoroutine());
        }
    }

    private static bool IsSceneObject(Transform target)
    {
        return target != null &&
               target.gameObject.scene.IsValid() &&
               target.gameObject.scene.isLoaded;
    }

    private void SpawnEnemy()
    {
        if (finiteWave && totalSpawnedEnemies >= totalEnemiesToSpawn)
        {
            TryNotifyWaveCleared();
            return;
        }

        // Sprawdzenie, czy osiągnięto maksymalną liczbę przeciwników
        if (numberOfSpawnedEnemies >= maxSpawnedEnemies)
        {
            return;
        }

        if (!TryGetSpawnPosition(out Vector3 randomSpawnPosition))
        {
            Debug.LogWarning($"Nie znaleziono NavMesh w pobliżu spawnera '{name}'. Pomijam spawn.", this);
            return;
        }

        var newEnemy = Instantiate(enemyPrefab, randomSpawnPosition, Quaternion.identity);
        var enemyMovement = newEnemy.GetComponent<EnemyMovement>();
        var enemyManager = newEnemy.GetComponent<EnemyManager>();

        if (enemyMovement != null)
        {
            enemyMovement.scrapManager = scrapManager;
            enemyMovement.player = player;
        }
        else
        {
            Debug.LogWarning("Brak komponentu EnemyMovement na prefabrykacie wroga.");
        }

        if (enemyManager != null)
        {
            enemyManager.SetSpawner(this); // Przypisuje EnemySpawner do spawnowanego przeciwnika

            if (enemyManager.GFX != null && enemyManager.GFX.Length > 0)
            {
                // Dezaktywuj wszystkie GFX na początku
                foreach (var gfx in enemyManager.GFX)
                {
                    if (gfx != null)
                    {
                        gfx.SetActive(false);
                    }
                }

                // Wybierz losowy GFX i aktywuj go
                int randomIndex = Random.Range(0, enemyManager.GFX.Length);
                if (enemyManager.GFX[randomIndex] != null)
                {
                    enemyManager.GFX[randomIndex].SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("GFX nie są przypisane w EnemyManager lub tablica jest pusta.");
            }
        }
        else
        {
            Debug.LogWarning("Brak komponentu EnemyManager na prefabrykacie wroga.");
        }

        // Zwiększ licznik zespawnowanych przeciwników
        numberOfSpawnedEnemies++;
        totalSpawnedEnemies++;
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        const int maxAttempts = 8;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                spawnPosition = navHit.position;
                return true;
            }
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit fallbackHit, spawnRadius + 2f, NavMesh.AllAreas))
        {
            spawnPosition = fallbackHit.position;
            return true;
        }

        spawnPosition = transform.position;
        return false;
    }

    private IEnumerator SpawnLogicCoroutine()
    {
        while (true)
        {
            // Czekaj przez określoną liczbę sekund przed kolejnym spawnem
            yield return new WaitForSeconds(secondsToSpawnEnemy);

            // Sprawdź, ilu przeciwników można jeszcze zespawnować
            int enemiesToSpawn = Mathf.Min(enemiesPerSpawn, maxSpawnedEnemies - numberOfSpawnedEnemies);
            if (finiteWave)
            {
                enemiesToSpawn = Mathf.Min(
                    enemiesToSpawn,
                    totalEnemiesToSpawn - totalSpawnedEnemies);
            }

            // Spawnowanie kilku przeciwników na raz (w zależności od enemiesPerSpawn)
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                if (numberOfSpawnedEnemies < maxSpawnedEnemies)
                {
                    SpawnEnemy();
                }
            }

            TryNotifyWaveCleared();
        }
    }

    // Metoda do zmniejszenia liczby wrogów, gdy zostaną zniszczeni
    public void EnemyDestroyed()
    {
        numberOfSpawnedEnemies = Mathf.Max(0, numberOfSpawnedEnemies - 1); // Zapewnia, że liczba wrogów nie będzie mniejsza niż 0
        TryNotifyWaveCleared();
    }

    public void ConfigureFiniteWave(int enemyCount)
    {
        finiteWave = true;
        totalEnemiesToSpawn = Mathf.Max(0, enemyCount);
        totalSpawnedEnemies = 0;
        numberOfSpawnedEnemies = 0;
        waveClearedNotified = false;
    }

    private void TryNotifyWaveCleared()
    {
        if (!IsWaveCleared || waveClearedNotified)
        {
            return;
        }

        waveClearedNotified = true;
        WaveCleared?.Invoke(this);
    }
}
