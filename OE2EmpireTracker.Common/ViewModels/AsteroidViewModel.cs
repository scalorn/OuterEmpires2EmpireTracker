using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    public class AsteroidViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlyAsteroid _original;
        private string _uuid;
        private string _name = string.Empty;
        private string _systemName = string.Empty;
        private List<AsteroidReserve> _reserves = new List<AsteroidReserve>();

        public string UUID => _uuid;

        public ReadOnlyAsteroid Original => _original;

        public bool IsNew => _original == null;

        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    return !string.IsNullOrEmpty(_name) || !string.IsNullOrEmpty(_systemName)
                        || _reserves.Count > 0;
                }

                if (_name != (_original.Name ?? string.Empty))
                {
                    return true;
                }

                if (_systemName != (_original.SystemName ?? string.Empty))
                {
                    return true;
                }

                var originalReserves = _original.Reserves;
                if (_reserves.Count != originalReserves.Count)
                {
                    return true;
                }

                for (int i = 0; i < _reserves.Count; i++)
                {
                    var local = _reserves[i];
                    var orig = originalReserves[i];
                    if (local.ResourceName != orig.ResourceName) return true;
                    if (local.Purity != orig.Purity) return true;
                    if (local.MaxReserve != orig.MaxReserve) return true;
                    if (local.CurrentReserve != orig.CurrentReserve) return true;
                    if (local.ResetTimestamp != orig.ResetTimestamp) return true;
                }

                return false;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string SystemName
        {
            get => _systemName;
            set => _systemName = value;
        }

        public List<AsteroidReserve> Reserves => _reserves;

        public void LoadFrom(ReadOnlyAsteroid ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _name = ro.Name ?? string.Empty;
            _systemName = ro.SystemName ?? string.Empty;
            _reserves = new List<AsteroidReserve>();
            foreach (var reserve in ro.Reserves)
            {
                _reserves.Add(DeepCopyReserve(reserve));
            }
        }

        public void Reset()
        {
            _original = null;
            _uuid = null;
            _name = string.Empty;
            _systemName = string.Empty;
            _reserves = new List<AsteroidReserve>();
        }

        public AsteroidUpdateRequest BuildUpdateRequest()
        {
            return new AsteroidUpdateRequest
            {
                Original = _original,
                Name = _name,
                SystemName = _systemName,
                Reserves = DeepCopyReserves(_reserves),
            };
        }

        public AsteroidCreateRequest BuildCreateRequest()
        {
            return new AsteroidCreateRequest
            {
                Name = _name,
                SystemName = _systemName,
                Reserves = DeepCopyReserves(_reserves),
            };
        }

        public void AddReserve(AsteroidReserve reserve)
        {
            _reserves.Add(reserve);
        }

        public void RemoveReserve(int index)
        {
            if (index >= 0 && index < _reserves.Count)
            {
                _reserves.RemoveAt(index);
            }
        }

        public void UpdateReserve(int index, AsteroidReserve reserve)
        {
            if (index >= 0 && index < _reserves.Count)
            {
                _reserves[index] = reserve;
            }
        }

        private static AsteroidReserve DeepCopyReserve(ReadOnlyAsteroidReserve source)
        {
            return new AsteroidReserve
            {
                ResourceName = source.ResourceName,
                Purity = source.Purity,
                MaxReserve = source.MaxReserve,
                CurrentReserve = source.CurrentReserve,
                ResetTimestamp = source.ResetTimestamp,
            };
        }

        private static List<AsteroidReserve> DeepCopyReserves(List<AsteroidReserve> source)
        {
            var copy = new List<AsteroidReserve>(source.Count);
            foreach (var s in source)
            {
                copy.Add(new AsteroidReserve
                {
                    ResourceName = s.ResourceName,
                    Purity = s.Purity,
                    MaxReserve = s.MaxReserve,
                    CurrentReserve = s.CurrentReserve,
                    ResetTimestamp = s.ResetTimestamp,
                });
            }

            return copy;
        }
    }
}