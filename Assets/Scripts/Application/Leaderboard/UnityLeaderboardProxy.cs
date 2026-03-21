using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task SubmitScoreAsync(string leaderboardId, int score, string playerName)
        {
            var metadata = new PlayerMetadata { playerName = playerName };
            var options = new AddPlayerScoreOptions
            {
                Metadata = metadata
            };
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
        }

        public async Task<List<LeaderboardEntry>> GetTopScoresAsync(string leaderboardId, int count)
        {
            var options = new GetScoresOptions
            {
                Offset = 0,
                Limit = count,
                IncludeMetadata = true
            };
            var response = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);

            var entries = new List<LeaderboardEntry>();
            foreach (var entry in response.Results)
            {
                var name = ParsePlayerName(entry.Metadata);
                entries.Add(new LeaderboardEntry(entry.Rank + 1, entry.PlayerId, name, (int)entry.Score));
            }
            return entries;
        }

        public async Task<LeaderboardEntry?> GetPlayerScoreAsync(string leaderboardId)
        {
            try
            {
                var options = new GetPlayerScoreOptions { IncludeMetadata = true };
                var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options);
                var name = ParsePlayerName(entry.Metadata);
                return new LeaderboardEntry(entry.Rank + 1, entry.PlayerId, name, (int)entry.Score);
            }
            catch
            {
                return null;
            }
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
