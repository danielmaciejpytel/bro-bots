using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum TutorialState { Kamikaze, Play, Fail }

public class TutorialCore : MonoBehaviour
{
    public event System.Action<TutorialState> StateChanged;

    public TutorialState tutorialState;

    [SerializeField] private TextMeshProUGUI popUp;
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private string[] texts;

    [SerializeField] private GameObject enemy;
    [SerializeField] private GameObject[] spawners;
    [SerializeField] private ScrapManager scrapManager;
    [SerializeField] private Transform player;

    private TutorialState lastState;

    void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("NewPlayerBody");
            if (p != null) player = p.transform;
        }

        if (scrapManager == null)
        {
            scrapManager = Object.FindFirstObjectByType<ScrapManager>();
        }

        if (spawners != null)
        {
            foreach (var item in spawners)
            {
                if (item != null) item.SetActive(false);
            }
        }

        if (popupPanel != null) popupPanel.SetActive(false);
        SetTutorialSate(TutorialState.Kamikaze);
    }

    public void SetTutorialSate(TutorialState state)
    {
        lastState = tutorialState;
        tutorialState = state;

        switch (state)
        {
            case TutorialState.Kamikaze:
                SpawnEnemy();
                ShowPopUp(0);
                break;
            case TutorialState.Play:
                if (spawners != null)
                {
                    foreach (var item in spawners)
                    {
                        if (item != null) item.SetActive(true);
                    }
                }
                ShowPopUp(texts != null && texts.Length > 2 ? 2 : 1);
                break;
            case TutorialState.Fail:
                SpawnEnemy();
                ShowPopUp(texts != null && texts.Length > 3 ? 3 : 0);
                tutorialState = lastState;
                break;
            default:
                break;
        }

        StateChanged?.Invoke(tutorialState);
    }

    private void SpawnEnemy()
    {
        if (enemy == null) return;

        var newEnemy = Instantiate(enemy);

        Vector3 newPos = newEnemy.transform.position;
        newPos.x = transform.position.x;
        newPos.z = transform.position.z;
        newEnemy.transform.position = newPos;

        var movement = newEnemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.scrapManager = scrapManager;
            movement.player = player;
        }

        var manager = newEnemy.GetComponent<EnemyManager>();
        if (manager != null)
        {
            manager.tutorialCore = this;
            if (manager.GFX != null && manager.GFX.Length > 0)
            {
                manager.GFX[Random.Range(0, manager.GFX.Length)].SetActive(true);
            }
        }
    }

    private void ShowPopUp(int indexText)
    {
        Time.timeScale = 0;
        if (popupPanel != null) popupPanel.SetActive(true);
        if (popUp != null && texts != null && indexText >= 0 && indexText < texts.Length)
        {
            popUp.SetText(texts[indexText]);
        }
    }

    public void OK()
    {
        Time.timeScale = 1;
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}
