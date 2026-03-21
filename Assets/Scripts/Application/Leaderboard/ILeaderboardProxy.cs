using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelStrom.Asteroids
{
    public interface ILeaderboardProxy
    {
        Task SubmitScoreAsync(string leaderboardId, int score, string playerName);
        Task<List<LeaderboardEntry>> GetTopScoresAsync(string leaderboardId, int count);
        Task<LeaderboardEntry?> GetPlayerScoreAsync(string leaderboardId);
    }
}
