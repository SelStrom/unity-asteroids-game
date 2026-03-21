namespace SelStrom.Asteroids
{
    public readonly struct LeaderboardEntry
    {
        public readonly int Rank;
        public readonly string PlayerId;
        public readonly string PlayerName;
        public readonly int Score;
        public readonly bool IsCurrentPlayer;

        public LeaderboardEntry(int rank, string playerId, string playerName, int score,
            bool isCurrentPlayer = false)
        {
            Rank = rank;
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
            IsCurrentPlayer = isCurrentPlayer;
        }
    }
}
