using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class UnityLeaderboardProxy : ILeaderboardProxy
    {
        [Serializable]
        private struct PlayerMetadata
        {
            public string playerName;
        }

        public IEnumerator SubmitScore(string leaderboardId, int score, string playerName, CoroutineResult result)
        {
            var metadata = new PlayerMetadata { playerName = playerName };
            var options = new AddPlayerScoreOptions { Metadata = metadata };
            var task = LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                result.Error = task.Exception;
            }
        }

        public IEnumerator GetTopScores(string leaderboardId, int count,
            CoroutineResult<List<LeaderboardEntry>> result)
        {
            var options = new GetScoresOptions
            {
                Offset = 0,
                Limit = count,
                IncludeMetadata = true
            };
            var task = LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                result.Error = task.Exception;
                yield break;
            }

            var response = task.Result;
            var entries = new List<LeaderboardEntry>();
            foreach (var entry in response.Results)
            {
                var name = ParsePlayerName(entry.Metadata);
                entries.Add(new LeaderboardEntry(entry.Rank + 1, entry.PlayerId, name, (int)entry.Score));
            }
            result.Value = entries;
        }

        public IEnumerator GetPlayerScore(string leaderboardId, CoroutineResult<LeaderboardEntry?> result)
        {
            var options = new GetPlayerScoreOptions { IncludeMetadata = true };
            var task = LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                result.Value = null;
                yield break;
            }

            var entry = task.Result;
            var name = ParsePlayerName(entry.Metadata);
            result.Value = new LeaderboardEntry(entry.Rank + 1, entry.PlayerId, name, (int)entry.Score);
        }

        private static string ParsePlayerName(string metadataJson)
        {
            if (string.IsNullOrEmpty(metadataJson))
            {
                return "???";
            }

            try
            {
                var metadata = JsonUtility.FromJson<PlayerMetadata>(metadataJson);
                return string.IsNullOrEmpty(metadata.playerName) ? "???" : metadata.playerName;
            }
            catch
            {
                return "???";
            }
        }
    }
}
