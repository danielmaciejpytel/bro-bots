using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LosePanel : MonoBehaviour
{
    [SerializeField] private GameObject reasumeButton; // przycisk wznowienia gry
    [SerializeField] private TextMeshProUGUI text; // tekst wygranej/przegranej
    [SerializeField] private Animator animator; // animator do zarządzania animacjami

    private AudioManager audioManager;
    private bool isGameOver = false; // zmienna śledząca stan gry

    private void Start()
    {
        // Znajdź AudioManager w scenie
        audioManager = Object.FindFirstObjectByType<AudioManager>();

        // Sprawdź, czy AudioManager został znaleziony
        if (audioManager != null)
        {
            audioManager.Play("BGMgame");
        }
        else
        {
            Debug.LogError("AudioManager nie został znaleziony w scenie!");
        }

        // Ukryj przycisk wznowienia gry na starcie
        reasumeButton.SetActive(false);

        // Upewnij się, że animator jest przypisany
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator nie został przypisany do LosePanel.");
            }
        }

        // Na starcie ukryj panel końcowy (EndPanel)
        animator.SetBool("isGameOver", false);
    }

    public void OpenPanel(bool win)
    {
        // Sprawdź, czy gra już jest zakończona, aby uniknąć wielokrotnego otwierania panelu
        if (isGameOver)
            return;

        // Ustaw stan gry na "game over"
        isGameOver = true;

        // Uaktywnij animację otwierania panelu
        animator.SetBool("isGameOver", true);

        // Ustawianie panelu w zależności od wyniku
        if (!win)
        {
            reasumeButton.SetActive(true);
            //text.SetText("Lose"); //Tymczasowe wyłączenie napisu. Zastąpione tekstem z Canvas/Animator
            // Sprawdź, czy AudioManager istnieje przed odtwarzaniem dźwięku
            if (audioManager != null)
            {
                audioManager.Play("PlayersDeath");
            }
        }
        else
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            text.SetText("Win");
            Time.timeScale = 0f;
        }
    }

    public void LoadScene(int sceneIndex)
    {
        Time.timeScale = 1;

        // Sprawdź, czy AudioManager istnieje przed odtwarzaniem dźwięku
        if (audioManager != null)
        {
            audioManager.Play("Click");

            if (sceneIndex == 0)
            {
                audioManager.Stop("BGMgame");
            }
        }

        // Załaduj scenę o podanym indeksie
        SceneManager.LoadScene(sceneIndex);
    }

    public void ClosePanel()
    {
        // Ukryj panel końcowy, ustawiając stan gry na "nie game over"
        isGameOver = false;
        animator.SetBool("isGameOver", false);
    }
}
