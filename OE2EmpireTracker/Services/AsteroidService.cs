using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Asteroid mutation.
    /// The form and ViewModel never touch the entities directly.
    /// </summary>
    public class AsteroidService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public AsteroidService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>Applies changes from the update request to an existing asteroid.</summary>
        public ReadOnlyAsteroid Update(string uuid, AsteroidUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var asteroid = _playerContext.FindMutableAsteroid(uuid);
            if (asteroid == null)
            {
                throw new InvalidOperationException("Asteroid not found: " + uuid);
            }

            Log.Info("AsteroidService.Update: UUID={0} name='{1}' -> '{2}'", uuid, asteroid.Name, request.Name);

            asteroid.Name = request.Name ?? string.Empty;
            asteroid.SystemName = request.SystemName ?? string.Empty;
            asteroid.Reserves = DeepCopyReserves(request.Reserves);

            _playerContext.WriteContext();
            _playerContext.OnAsteroidDataChanged(uuid);
            return new ReadOnlyAsteroid(asteroid);
        }

        /// <summary>Creates a new asteroid with a generated UUID.</summary>
        public ReadOnlyAsteroid Create(AsteroidCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var asteroid = new Asteroid();
            asteroid.UUID = Guid.NewGuid().ToString();
            asteroid.Name = string.IsNullOrWhiteSpace(request.Name) ? "New Asteroid" : request.Name;
            asteroid.SystemName = request.SystemName ?? string.Empty;
            asteroid.Reserves = DeepCopyReserves(request.Reserves);

            Log.Info("AsteroidService.Create: name='{0}' UUID={1}", asteroid.Name, asteroid.UUID);

            _playerContext.AddAsteroid(asteroid);
            _playerContext.WriteContext();
            _playerContext.OnAsteroidDataChanged(asteroid.UUID);
            return new ReadOnlyAsteroid(asteroid);
        }

        /// <summary>Removes an asteroid. No-op if UUID is empty or not found.</summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var asteroid = _playerContext.FindMutableAsteroid(uuid);
            if (asteroid == null)
            {
                return;
            }

            Log.Info("AsteroidService.Delete: UUID={0} name='{1}'", uuid, asteroid.Name);

            _playerContext.RemoveAsteroid(asteroid);
            _playerContext.WriteContext();
            _playerContext.OnAsteroidDataChanged(uuid);
        }

        private static List<AsteroidReserve> DeepCopyReserves(List<AsteroidReserve> source)
        {
            if (source == null)
            {
                return new List<AsteroidReserve>();
            }

            var copy = new List<AsteroidReserve>(source.Count);
            foreach (var s in source)
            {
                copy.Add(new AsteroidReserve
                {
                    ResourceName = s.ResourceName ?? string.Empty,
                    Purity = s.Purity ?? string.Empty,
                    MaxReserve = s.MaxReserve,
                    CurrentReserve = s.CurrentReserve,
                    ResetTimestamp = s.ResetTimestamp ?? string.Empty,
                });
            }

            return copy;
        }
    }
}