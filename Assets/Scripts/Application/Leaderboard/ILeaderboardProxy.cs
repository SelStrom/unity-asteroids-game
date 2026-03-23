using System.Collections;
using System.Collections.Generic;

namespace SelStrom.Asteroids
{
    public interface ILeaderboardProxy
    {
        IEnumerator SubmitScore(string leaderboardId, int score, string playerName, CoroutineResult result);
        IEnumerator GetTopScores(string leaderboardId, int count, CoroutineResult<List<LeaderboardEntry>> result);
        IEnumerator GetPlayerScore(string leaderboardId, CoroutineResult<LeaderboardEntry?> result);
    }
}
