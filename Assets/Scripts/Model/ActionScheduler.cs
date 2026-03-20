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

        private readonly List<ScheduledAction> _scheduledEntries = new();
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
            //TODO theoretically it can be added during update. So it should be add new entries collection
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
            
            for (var i = _scheduledEntries.Count - 1; i >= 0; i--) {
                var entry = _scheduledEntries[i];
                entry.Duration -= _secondsSinceLastUpdate;
                if (entry.Duration > 0) {
                    _nextUpdateDuration = Math.Min(_nextUpdateDuration, (float)entry.Duration);
                    continue;
                }
                
                var lastIndex = _scheduledEntries.Count - 1;
                var lastEntry = _scheduledEntries[lastIndex];
                _scheduledEntries[i] = lastEntry;
                _scheduledEntries.RemoveAt(lastIndex);
                entry.Action?.Invoke();
            }
            
            _secondsSinceLastUpdate = 0;
        }

        public void ResetSchedule()
        {
            _nextUpdateDuration = float.MaxValue;
            _scheduledEntries.Clear();
        }
    }
}