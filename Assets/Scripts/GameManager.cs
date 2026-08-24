using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Gameplay References")]
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private GameGridLayout gridLayout;

    [Header("Settings")]
    [SerializeField] private float roundTimeSeconds = 60f;

    private float timeRemaining;
    private bool isGameActive = false;

    void Start()
    {
        ShowMainMenu();
    }

    // --- Panel Navigation ---

    public void ShowMainMenu()
    {
        SetPanelState(mainMenu: true, dialogue: false, gameplay: false, win: false, lose: false);
    }

    public void OnStartButtonPressed()
    {
        AudioManager.Instance.PlayButtonClick();
        SetPanelState(mainMenu: false, dialogue: true, gameplay: false, win: false, lose: false);
    }

    public void OnDialogueContinuePressed()
    {
        AudioManager.Instance.PlayButtonClick();
        StartGame();
    }

    public void StartGame()
    {
        timeRemaining = roundTimeSeconds;
        isGameActive = true;

        SetPanelState(mainMenu: false, dialogue: false, gameplay: true, win: false, lose: false);

        itemSpawner.StartNewPuzzle();
    }

    public void OnPlayAgainPressed()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        StartGame();
    }

    public void OnBackToMainMenuPressed()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        ShowMainMenu();
    }

    private void SetPanelState(bool mainMenu, bool dialogue, bool gameplay, bool win, bool lose)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenu);
        if (dialoguePanel != null) dialoguePanel.SetActive(dialogue);
        if (gameplayPanel != null) gameplayPanel.SetActive(gameplay);
        if (winPanel != null) winPanel.SetActive(win);
        if (gameOverPanel != null) gameOverPanel.SetActive(lose);
    }

    // --- Game Loop ---

    void Update()
    {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0, timeRemaining));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = $"Leaving in: {minutes}:{seconds:D2}";
        }

        if (timeRemaining <= 0f)
        {
            GameOver();
        }
    }

    public void CheckBagState()
    {
        if (!isGameActive) return;

        if (gridLayout.IsBagFull())
        {
            isGameActive = false;
            winPanel.SetActive(true);
        }
    }

    private void GameOver()
    {
        isGameActive = false;
        gameOverPanel.SetActive(true);
    }
}
