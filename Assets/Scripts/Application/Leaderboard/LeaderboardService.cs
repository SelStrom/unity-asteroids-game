using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class LeaderboardService
    {
        private readonly IAuthProxy _auth;
        private readonly ILeaderboardProxy _leaderboard;
        private readonly string _leaderboardId;
        private Task _initTask;

        public LeaderboardService(IAuthProxy auth, ILeaderboardProxy leaderboard, string leaderboardId)
        {
            _auth = auth;
            _leaderboard = leaderboard;
            _leaderboardId = leaderboardId;
        }

        public void Initialize()
        {
            _initTask ??= InitializeInternalAsync();
        }

        private async Task InitializeInternalAsync()
        {
            try
            {
                await _auth.InitializeAsync();
                if (!_auth.IsSignedIn)
                {
                    await _auth.SignInAnonymouslyAsync();
                }
                Debug.Log($"[LeaderboardService] Signed in. PlayerId: {_auth.PlayerId}");
            }
            catch
            {
                _initTask = null;
                throw;
            }
        }

        private async Task EnsureInitializedAsync()
        {
            _initTask ??= InitializeInternalAsync();
            await _initTask;
        }

        public async Task SubmitScoreAsync(string playerName, int score)
        {
            await EnsureInitializedAsync();
            await _leaderboard.SubmitScoreAsync(_leaderboardId, score, playerName);
        }

        public async Task<List<LeaderboardEntry>> GetTopScoresAsync(int count = 10)
        {
            await EnsureInitializedAsync();
            var rawEntries = await _leaderboard.GetTopScoresAsync(_leaderboardId, count);
            var currentPlayerId = _auth.PlayerId;

            var entries = new List<LeaderboardEntry>(rawEntries.Count);
            foreach (var entry in rawEntries)
            {
                entries.Add(new LeaderboardEntry(
                    entry.Rank, entry.PlayerId, entry.PlayerName, entry.Score,
                    entry.PlayerId == currentPlayerId
                ));
            }
            return entries;
        }

        public async Task<LeaderboardEntry?> GetPlayerScoreAsync()
        {
            await EnsureInitializedAsync();
            var entry = await _leaderboard.GetPlayerScoreAsync(_leaderboardId);
            if (!entry.HasValue)
            {
                return null;
            }

            return new LeaderboardEntry(
                entry.Value.Rank, entry.Value.PlayerId, entry.Value.PlayerName,
                entry.Value.Score, true
            );
        }
    }
}
