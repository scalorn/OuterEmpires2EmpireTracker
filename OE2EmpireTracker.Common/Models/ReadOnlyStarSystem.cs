using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for StarSystem. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyStarSystem
    {
        private readonly StarSystem _system;

        public ReadOnlyStarSystem(StarSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public int Id => _system.Id;

        public string Name => _system.Name;

        public decimal X => _system.X;

        public decimal Y => _system.Y;

        public int Quadrant => _system.Quadrant;

        public int Sector => _system.Sector;

        public int Region => _system.Region;

        public int Locality => _system.Locality;

        public string SpectralClass => _system.SpectralClass;

        public int FactionId => _system.FactionId;

        public string FactionName => _system.FactionName;

        public string FactionColor => _system.FactionColor;

        public bool HasOrbital => _system.HasOrbital;

        public bool HasSpaceport => _system.HasSpaceport;

        public bool HasStarbase => _system.HasStarbase;
    }
}
