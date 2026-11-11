using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const int PointsPerHit = 10;

    [Header("Configuração da partida")]
    [SerializeField] private int maxErrors = 3;

    [Header("Interface")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Graphic[] lifeHearts;

    [Header("Referências")]
    [SerializeField] private HitManager hitManager;
    [SerializeField] private MoleRandomizer moleRandomizer;

    private float elapsedTime;
    private bool gameOver;
    private int displayedHits = -1;
    private int displayedLives = -1;

    private void Start()
    {
        elapsedTime = 0f;
        UpdateTimerDisplay();
        UpdateLivesDisplay(0);
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

            UpdateLivesDisplay(totalErrors);

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

        if (hitManager != null)
            LocalLeaderboard.SubmitScore(hitManager.GetSuccessfulHits() * PointsPerHit);

        Debug.Log("GAME OVER: " + reason);

        if (moleRandomizer != null)
            moleRandomizer.StopSpawning();

        SceneManager.LoadScene("GameOver");
    }

    private void UpdateLivesDisplay(int totalErrors)
    {
        int lives = Mathf.Clamp(maxErrors - totalErrors, 0, maxErrors);
        if (lives == displayedLives || lifeHearts == null)
            return;

        for (int i = 0; i < lifeHearts.Length; i++)
        {
            if (lifeHearts[i] != null)
            {
                lifeHearts[i].color = i < lives
                    ? new Color(1f, 0.32f, 0.40f, 1f)
                    : new Color(0.30f, 0.23f, 0.34f, 1f);
            }
        }

        displayedLives = lives;
    }
}
