using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for PropertyBag. Exposes only query methods.
    /// Does NOT expose: SetProperty, Remove, Clear, Properties dictionary.
    /// </summary>
    public class ReadOnlyPropertyBag
    {
        private readonly PropertyBag _entity;

        public ReadOnlyPropertyBag(PropertyBag entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public int Count => _entity.Count;

        public bool ContainsKey(string name) => _entity.ContainsKey(name);

        public bool GetDecimal(string name, decimal defaultValue, out decimal value)
            => _entity.GetDecimal(name, defaultValue, out value);

        public bool GetLong(string name, long defaultValue, out long value)
            => _entity.GetLong(name, defaultValue, out value);

        public bool GetBoolean(string name, bool defaultValue, out bool value)
            => _entity.GetBoolean(name, defaultValue, out value);

        public bool GetString(string name, string defaultValue, out string value)
            => _entity.GetString(name, defaultValue, out value);

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPropertyBag other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ToString();
    }
}
