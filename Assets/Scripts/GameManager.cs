using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    private const int PointsPerHit = 10;

    [Header("Configuração da partida")]
    [SerializeField] private int maxErrors = 10;

    [Header("Interface")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Referências")]
    [SerializeField] private HitManager hitManager;
    [SerializeField] private MoleRandomizer moleRandomizer;

    private float elapsedTime;
    private bool gameOver;
    private int displayedHits = -1;

    private void Start()
    {
        elapsedTime = 0f;
        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (gameOver)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerDisplay();

        if (hitManager != null)
        {
            int totalErrors =
                hitManager.GetMissedTargets() +
                hitManager.GetTargetsTooLate();

            if (totalErrors >= maxErrors)
            {
                EndGame("Limite de erros atingido!");
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void LateUpdate()
    {
        if (scoreText == null || hitManager == null)
            return;

        int hits = hitManager.GetSuccessfulHits();
        if (hits == displayedHits)
            return;

        scoreText.text = $"PONTOS  {hits * PointsPerHit}";
        displayedHits = hits;
    }

    private void EndGame(string reason)
    {
        if (gameOver)
            return;

        gameOver = true;

        Debug.Log("GAME OVER: " + reason);

        if (moleRandomizer != null)
            moleRandomizer.StopSpawning();

        SceneManager.LoadScene("GameOver");
    }
}
