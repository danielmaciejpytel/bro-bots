using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private bool tutorial;
    public GameObject[] GFX;
    [HideInInspector] public EnemySpawner enemySpawner;
    [HideInInspector] public ScrapExplosion scrapExplosion;
    [HideInInspector] public TutorialCore tutorialCore;
    private AudioManager audioManager;

    private void Awake()
    {
        // Sprawdź, czy component ScrapExplosion istnieje
        scrapExplosion = GetComponent<ScrapExplosion>();
        if (scrapExplosion == null)
        {
            Debug.LogError("Brak przypisanego komponentu ScrapExplosion na obiekcie: " + gameObject.name);
        }

        // Znajdź EnemySpawner w scenie
        enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogWarning("EnemySpawner nie został przypisany do EnemyManager.");
        }

    }

    private void Start()
    {
        // Znajdź AudioManager, upewnij się, że nie jest null
        audioManager = AudioManager.AM != null ? AudioManager.AM : Object.FindAnyObjectByType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("Nie znaleziono AudioManager w scenie.");
        }

        // TutorialCore przypisuje tę referencję bezpośrednio po Instantiate().
        // Sprawdzanie jej w Awake() dawało fałszywy warning, zanim caller zdążył ją ustawić.
        if (tutorial && tutorialCore == null)
        {
            Debug.LogWarning("TutorialCore nie został przypisany do EnemyManager podczas trybu tutorialu.");
        }
    }

    private void OnDestroy()
    {
        // Zmniejsz licznik przeciwników, jeśli enemySpawner został przypisany
        if (!tutorial && enemySpawner != null)
        {
            enemySpawner.EnemyDestroyed();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Użyj CompareTag, ponieważ jest bardziej wydajny niż == przy porównywaniu tagów
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            DestroyEnemy("Obstacle");
        }
    }

    public void SetSpawner(EnemySpawner spawner)
    {
        enemySpawner = spawner;
    }

    public void DestroyEnemy(string destroyIndicator)
    {
        // Upewnij się, że scrapExplosion nie jest null
        if (scrapExplosion != null)
        {
            scrapExplosion.DropScrap();
        }
        else
        {
            Debug.LogWarning("ScrapExplosion nie jest przypisany na obiekcie: " + gameObject.name);
        }

        // Zniszcz obiekt
        Destroy(gameObject);

        // Odtwórz dźwięk, jeśli AudioManager nie jest null
        if (audioManager != null)
        {
            audioManager.Play("EnemiesDeath");
        }
        else
        {
            Debug.LogWarning("AudioManager nie jest przypisany lub nie znaleziono AudioManager.");
        }

        // Logika tutorialu, upewnij się, że tutorialCore nie jest null
        if (tutorial && tutorialCore != null)
        {
            if (destroyIndicator == "Obstacle" && tutorialCore.tutorialState == TutorialState.Kamikaze)
            {
                tutorialCore.SetTutorialSate(TutorialState.Play);
            }
            else if (destroyIndicator != "Obstacle" && tutorialCore.tutorialState == TutorialState.Kamikaze)
            {
                tutorialCore.SetTutorialSate(TutorialState.Fail);
            }
        }
    }
}
