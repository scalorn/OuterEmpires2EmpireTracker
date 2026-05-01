using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for CountDownTime. Exposes only read-only computed properties.
    /// Does NOT expose: TimeRemaining setter, TimeRemainingString setter,
    /// ConsumeIntervals, StartRepeating, or any other mutation method.
    /// </summary>
    public class ReadOnlyCountDownTime
    {
        private readonly CountDownTime _entity;

        public ReadOnlyCountDownTime(CountDownTime entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public long TimeRemaining => _entity.TimeRemaining;

        public string TimeRemainingString => _entity.TimeRemainingString;

        public long IntervalsPassed => _entity.IntervalsPassed;

        public bool IsRepeating => _entity.IsRepeating;

        public long RepeatIntervalSeconds => _entity.RepeatIntervalSeconds;

        public DateTime StartTime => _entity.StartTime;

        public DateTime EndTime => _entity.EndTime;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyCountDownTime other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ToString();
    }
}
