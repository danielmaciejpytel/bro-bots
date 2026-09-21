using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScrapManager : MonoBehaviour
{
    public static ScrapManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scrapCounter;
    [Space(10)]
    [SerializeField] private Slider lvlUpSlider;
    [SerializeField] private TextMeshProUGUI sliderValuesText;
    [SerializeField]
    [Tooltip("To tak będzie wyglądać: textInSlider 5/10")]
    private string textInSlider;
    [Space(10)]
    [SerializeField] private int[] valuesToLvlUp;
    [SerializeField] private LosePanel losePanel;
    [SerializeField] private ScrapExplosion scrapExplosion; // Zmieniona nazwa
    public int scrapNumber = 0;
    private int points = 0;
    private int actualAttackState = 0; // Poprawiona literówka
    private AudioManager audioManager;
    private bool isPlayerDead = false; // Flaga sprawdzająca, czy gracz już zginął
    private PlayerController playerController;

    public bool IsPlayerDead => isPlayerDead;

    private void Awake()
    {
        Instance = this;

        if (scrapExplosion == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                scrapExplosion = playerController.GetComponent<ScrapExplosion>();
            }
        }
        else
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        SetScrapValueText();
        if (HasLevelThreshold(actualAttackState))
        {
            SetSlider(actualAttackState, 0);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        audioManager = AudioManager.AM != null ? AudioManager.AM : Object.FindAnyObjectByType<AudioManager>();
    }

    private void Update()
    {
        SetScrapValueText();
        if (lvlUpSlider != null)
        {
            lvlUpSlider.value = scrapNumber;
        }
    }

    private void SetScrapValueText()
    {
        if (scrapCounter != null)
        {
            scrapCounter.SetText(points.ToString());
        }

        if (sliderValuesText != null && lvlUpSlider != null && HasLevelThreshold(actualAttackState))
        {
            sliderValuesText.SetText(
                $"{textInSlider} {lvlUpSlider.value}/{valuesToLvlUp[actualAttackState]}");
        }
    }

    private void SetSlider(int indexOfLvl, int scraps)
    {
        scrapNumber = scraps;
        if (lvlUpSlider != null && HasLevelThreshold(indexOfLvl))
        {
            lvlUpSlider.maxValue = valuesToLvlUp[indexOfLvl];
        }
    }

    private bool HasLevelThreshold(int index)
    {
        return valuesToLvlUp != null && index >= 0 && index < valuesToLvlUp.Length;
    }

    private IEnumerator LoseCutdownCoroutine() // Poprawiona literówka
    {
        while (points > 0)
        {
            yield return new WaitForSeconds(0.4f);
            points--;
            if (scrapExplosion != null)
            {
                scrapExplosion.DropScrap();
            }
        }
        yield return new WaitForSeconds(0.2f);
        if (losePanel != null)
        {
            losePanel.OpenPanel(false);
        }
    }

    public void EndGame(bool win)
    {
        if (win)
        {
            if (losePanel != null)
            {
                losePanel.OpenPanel(true);
            }
        }
        else if (!isPlayerDead) // Sprawdź, czy gracz już zginął
        {
            if (playerController == null)
            {
                playerController = Object.FindFirstObjectByType<PlayerController>();
            }

            if (playerController != null && playerController.IsGodMode)
            {
                return;
            }

            isPlayerDead = true; // Ustaw flagę na true
            if (playerController != null)
            {
                playerController.Die();
            }
            StartCoroutine(LoseCutdownCoroutine());
        }
    }

    public void AddScraps(int value)
    {
        points += value;

        if (!HasLevelThreshold(actualAttackState))
        {
            scrapNumber += value;
            return;
        }

        if (scrapNumber + value > valuesToLvlUp[actualAttackState])
        {
            if (actualAttackState + 1 >= valuesToLvlUp.Length) // Sprawdzenie, aby nie wykraczać poza tablicę
            {
                SetSlider(actualAttackState, valuesToLvlUp[actualAttackState]);
            }
            else
            {
                actualAttackState++;
                SetSlider(actualAttackState, scrapNumber + value - valuesToLvlUp[actualAttackState - 1]);
                if (audioManager != null)
                {
                    audioManager.Play("LevelUp");
                }
            }
        }
        else
        {
            scrapNumber += value;
        }
    }

    public void SubtractScraps(int value)
    {
        if (points - value < 0)
        {
            points = 0;
            EndGame(false);
        }
        else
        {
            points -= value;
        }
        if (!HasLevelThreshold(actualAttackState))
        {
            scrapNumber = Mathf.Max(0, scrapNumber - value);
            return;
        }

        if (scrapNumber - value < 0)
        {
            if (actualAttackState - 1 < 0)
            {
                SetSlider(actualAttackState, 0);
            }
            else
            {
                actualAttackState--;
                SetSlider(actualAttackState, valuesToLvlUp[actualAttackState]);
                if (audioManager != null)
                {
                    audioManager.Play("LevelDown");
                }
            }
        }
        else
        {
            scrapNumber -= value;
        }
    }
}
