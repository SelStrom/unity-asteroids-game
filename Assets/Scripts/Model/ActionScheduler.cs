using System;
using System.Collections.Generic;
using System.Linq;

namespace SelStrom.Asteroids
{
    public class ActionScheduler
    {
        private class ScheduledAction
        {
            public float Duration;
            public Action Action;
        }

        private readonly HashSet<ScheduledAction> _scheduledEntries = new();
        private float _nextUpdateDuration = float.MaxValue;
        private float _secondsSinceLastUpdate;

        public void ScheduleAction(Action action, float durationSec)
        {
            var nextUpdate = durationSec + _secondsSinceLastUpdate;
            _nextUpdateDuration = Math.Min(_nextUpdateDuration, nextUpdate);
            _scheduledEntries.Add(new ScheduledAction
            {
                Duration = nextUpdate,
                Action = action,
            });
        }

        public void Update(float deltaTime)
        {
            if (!_scheduledEntries.Any())
            {
                return;
            }

            _secondsSinceLastUpdate += deltaTime;
            if (_secondsSinceLastUpdate < _nextUpdateDuration)
            {
                return;
            }

            foreach (var entry in _scheduledEntries.ToArray())
            {
                entry.Duration -= _secondsSinceLastUpdate;
                if (entry.Duration <= 0)
                {
                    entry.Action?.Invoke();
                }
                else
                {
                    _nextUpdateDuration = Math.Min(_nextUpdateDuration, (float)entry.Duration);
                }
            }

            _scheduledEntries.RemoveWhere(x => x.Duration <= 0);
            _secondsSinceLastUpdate = 0;
        }

        public void ResetSchedule()
        {
            _nextUpdateDuration = float.MaxValue;
            _scheduledEntries.Clear();
        }
    }
}