using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for PlayerRank. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyPlayerRank
    {
        private readonly PlayerRank _entity;

        public ReadOnlyPlayerRank(PlayerRank entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public int Rank => _entity.Rank;
        public long CurrentXp => _entity.CurrentXp;
        public long XpToNextLevel => _entity.XpToNextLevel;
        public string RankName => _entity.RankName;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPlayerRank other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.RankName;
    }
}
