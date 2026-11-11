using System;
using UnityEngine;

public static class LocalLeaderboard
{
    public const int Capacity = 3;
    private const string KeyPrefix = "GhostBonk.Leaderboard.v1.";

    public static int[] GetScores()
    {
        var scores = new int[Capacity];
        for (int i = 0; i < Capacity; i++)
            scores[i] = Math.Max(0, PlayerPrefs.GetInt(KeyPrefix + i, 0));

        Array.Sort(scores);
        Array.Reverse(scores);
        return scores;
    }

    public static void SubmitScore(int score)
    {
        int[] scores = GetScores();
        for (int rank = 0; rank < Capacity; rank++)
        {
            if (score <= scores[rank])
                continue;

            for (int i = Capacity - 1; i > rank; i--)
                scores[i] = scores[i - 1];

            scores[rank] = score;
            for (int i = 0; i < Capacity; i++)
                PlayerPrefs.SetInt(KeyPrefix + i, scores[i]);

            PlayerPrefs.Save();
            return;
        }
    }
}
