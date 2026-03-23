using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class LeaderboardService
    {
        private readonly IAuthProxy _auth;
        private readonly ILeaderboardProxy _leaderboard;
        private readonly string _leaderboardId;
        private readonly MonoBehaviour _coroutineHost;

        private bool _initialized;
        private bool _initializing;
        private Exception _initError;

        public LeaderboardService(IAuthProxy auth, ILeaderboardProxy leaderboard, string leaderboardId,
            MonoBehaviour coroutineHost)
        {
            _auth = auth;
            _leaderboard = leaderboard;
            _leaderboardId = leaderboardId;
            _coroutineHost = coroutineHost;
        }

        public void Initialize()
        {
            if (!_initialized && !_initializing)
            {
                _coroutineHost.StartCoroutine(RunInitialize());
            }
        }

        private IEnumerator RunInitialize()
        {
            var result = new CoroutineResult();
            yield return EnsureInitialized(result);
            if (!result.IsSuccess)
            {
                Debug.LogError($"[LeaderboardService] Initialization failed: {result.Error}");
            }
        }

        private IEnumerator EnsureInitialized(CoroutineResult result)
        {
            if (_initialized)
            {
                yield break;
            }

            if (_initializing)
            {
                yield return new WaitUntil(() => !_initializing);
                if (!_initialized)
                {
                    result.Error = _initError ?? new Exception("Initialization failed");
                }
                yield break;
            }

            _initializing = true;

            var authResult = new CoroutineResult();
            yield return _auth.Initialize(authResult);
            if (!authResult.IsSuccess)
            {
                _initError = authResult.Error;
                _initializing = false;
                result.Error = authResult.Error;
                yield break;
            }

            if (!_auth.IsSignedIn)
            {
                var signInResult = new CoroutineResult();
                yield return _auth.SignInAnonymously(signInResult);
                if (!signInResult.IsSuccess)
                {
                    _initError = signInResult.Error;
                    _initializing = false;
                    result.Error = signInResult.Error;
                    yield break;
                }
            }

            _initialized = true;
            _initializing = false;
            Debug.Log($"[LeaderboardService] Signed in. PlayerId: {_auth.PlayerId}");
        }

        public IEnumerator SubmitScore(string playerName, int score, CoroutineResult result)
        {
            var initResult = new CoroutineResult();
            yield return EnsureInitialized(initResult);
            if (!initResult.IsSuccess)
            {
                result.Error = initResult.Error;
                yield break;
            }

            yield return _leaderboard.SubmitScore(_leaderboardId, score, playerName, result);
        }

        public IEnumerator GetTopScores(CoroutineResult<List<LeaderboardEntry>> result, int count = 10)
        {
            var initResult = new CoroutineResult();
            yield return EnsureInitialized(initResult);
            if (!initResult.IsSuccess)
            {
                result.Error = initResult.Error;
                yield break;
            }

            var rawResult = new CoroutineResult<List<LeaderboardEntry>>();
            yield return _leaderboard.GetTopScores(_leaderboardId, count, rawResult);
            if (!rawResult.IsSuccess)
            {
                result.Error = rawResult.Error;
                yield break;
            }

            var currentPlayerId = _auth.PlayerId;
            var entries = new List<LeaderboardEntry>(rawResult.Value.Count);
            foreach (var entry in rawResult.Value)
            {
                entries.Add(new LeaderboardEntry(
                    entry.Rank, entry.PlayerId, entry.PlayerName, entry.Score,
                    entry.PlayerId == currentPlayerId
                ));
            }
            result.Value = entries;
        }

        public IEnumerator GetPlayerScore(CoroutineResult<LeaderboardEntry?> result)
        {
            var initResult = new CoroutineResult();
            yield return EnsureInitialized(initResult);
            if (!initResult.IsSuccess)
            {
                result.Error = initResult.Error;
                yield break;
            }

            var rawResult = new CoroutineResult<LeaderboardEntry?>();
            yield return _leaderboard.GetPlayerScore(_leaderboardId, rawResult);
            if (!rawResult.IsSuccess)
            {
                result.Error = rawResult.Error;
                yield break;
            }

            if (!rawResult.Value.HasValue)
            {
                result.Value = null;
                yield break;
            }

            var e = rawResult.Value.Value;
            result.Value = new LeaderboardEntry(e.Rank, e.PlayerId, e.PlayerName, e.Score, true);
        }
    }
}
