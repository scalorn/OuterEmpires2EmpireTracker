// <copyright file="GameApiConnectionMonitorTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Reflection;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based and unit tests for <see cref="GameApiConnectionMonitor"/> state machine.
    /// **Validates: Requirements 4.4, 4.5, 4.6**
    /// Correctness Property 5: State Machine Validity.
    /// </summary>
    [TestFixture]
    public class GameApiConnectionMonitorTests
    {
        private string _tempSecretsPath;
        private GameApiCredentialManager _credentialManager;
        private GameApiClient _client;
        private GameApiConnectionMonitor _monitor;

        [SetUp]
        public void SetUp()
        {
            // Register DPAPI protection functions for the credential manager
            GameApiCredentialManager.RegisterProtectionFunctions(
                OE2EmpireTracker.Client.CredentialStore.Protect,
                OE2EmpireTracker.Client.CredentialStore.Unprotect);

            _tempSecretsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-secrets.dat");
            _credentialManager = new GameApiCredentialManager(_tempSecretsPath);
            _credentialManager.StoreKey("test-uuid", "test-api-key-12345");
            _client = new GameApiClient("http://localhost:59999");
            _monitor = new GameApiConnectionMonitor(_client, _credentialManager, "test-uuid", "test-app-id", "test-client-id");
        }

        [TearDown]
        public void TearDown()
        {
            _monitor?.Dispose();
            _client?.Dispose();

            if (File.Exists(_tempSecretsPath))
            {
                File.Delete(_tempSecretsPath);
            }
        }

        // -------------------------------------------------------------------
        // Property 1: No same-state transition
        // TransitionTo with the same state as current does nothing.
        // **Validates: Requirements 4.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SameStateTransition_DoesNotRaiseEvent()
        {
            var stateGen = Gen.Elements(
                GameApiConnectionMonitor.ConnectionState.NotConfigured,
                GameApiConnectionMonitor.ConnectionState.Connected,
                GameApiConnectionMonitor.ConnectionState.Disconnected,
                GameApiConnectionMonitor.ConnectionState.DisconnectedCircuitOpen,
                GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                GameApiConnectionMonitor.ConnectionState.Syncing,
                GameApiConnectionMonitor.ConnectionState.RateLimited);

            return Prop.ForAll(Arb.From(stateGen), targetState =>
            {
                // Arrange: create a fresh monitor and transition to the target state
                using (var client = new GameApiClient("http://localhost:59999"))
                using (var monitor = new GameApiConnectionMonitor(client, _credentialManager, "test-uuid", "test-app-id", "test-client-id"))
                {
                    // First transition to the target state (from NotConfigured)
                    if (targetState != GameApiConnectionMonitor.ConnectionState.NotConfigured)
                    {
                        monitor.TransitionTo(targetState, "setup");
                    }

                    // Now try to transition to the same state again
                    int eventCount = 0;
                    monitor.StatusChanged += (s, e) => eventCount++;

                    monitor.TransitionTo(targetState, "duplicate");

                    // Assert: no event raised, state unchanged
                    bool noEvent = eventCount == 0;
                    bool stateUnchanged = monitor.CurrentState == targetState;

                    return (noEvent && stateUnchanged).Label(
                        string.Format(
                            "eventCount={0}, state={1}, expected={2}",
                            eventCount,
                            monitor.CurrentState,
                            targetState));
                }
            });
        }

        // -------------------------------------------------------------------
        // Property 2: Event raised on every valid transition
        // For any valid state change, StatusChanged fires with correct args.
        // **Validates: Requirements 4.5**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ValidTransition_RaisesEventWithCorrectArgs()
        {
            var transitionGen =
                from source in Gen.Elements(
                    GameApiConnectionMonitor.ConnectionState.NotConfigured,
                    GameApiConnectionMonitor.ConnectionState.Connected,
                    GameApiConnectionMonitor.ConnectionState.Disconnected,
                    GameApiConnectionMonitor.ConnectionState.DisconnectedCircuitOpen,
                    GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                    GameApiConnectionMonitor.ConnectionState.Syncing,
                    GameApiConnectionMonitor.ConnectionState.RateLimited)
                from target in Gen.Elements(
                    GameApiConnectionMonitor.ConnectionState.NotConfigured,
                    GameApiConnectionMonitor.ConnectionState.Connected,
                    GameApiConnectionMonitor.ConnectionState.Disconnected,
                    GameApiConnectionMonitor.ConnectionState.DisconnectedCircuitOpen,
                    GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                    GameApiConnectionMonitor.ConnectionState.Syncing,
                    GameApiConnectionMonitor.ConnectionState.RateLimited)
                where source != target
                select new { Source = source, Target = target };

            return Prop.ForAll(Arb.From(transitionGen), transition =>
            {
                using (var client = new GameApiClient("http://localhost:59999"))
                using (var monitor = new GameApiConnectionMonitor(client, _credentialManager, "test-uuid", "test-app-id", "test-client-id"))
                {
                    // Set up source state
                    if (transition.Source != GameApiConnectionMonitor.ConnectionState.NotConfigured)
                    {
                        monitor.TransitionTo(transition.Source, "setup");
                    }

                    // Subscribe to event
                    GameApiConnectionStatusChangedEventArgs receivedArgs = null;
                    monitor.StatusChanged += (s, e) => receivedArgs = e;

                    // Act: transition to target
                    monitor.TransitionTo(transition.Target, "test message");

                    // Assert
                    bool eventFired = receivedArgs != null;
                    bool oldStateCorrect = eventFired && receivedArgs.OldState == transition.Source;
                    bool newStateCorrect = eventFired && receivedArgs.NewState == transition.Target;
                    bool stateUpdated = monitor.CurrentState == transition.Target;

                    return (eventFired && oldStateCorrect && newStateCorrect && stateUpdated).Label(
                        string.Format(
                            "fired={0}, oldState={1}(expected {2}), newState={3}(expected {4}), current={5}",
                            eventFired,
                            eventFired ? receivedArgs.OldState.ToString() : "null",
                            transition.Source,
                            eventFired ? receivedArgs.NewState.ToString() : "null",
                            transition.Target,
                            monitor.CurrentState));
                }
            });
        }

        // -------------------------------------------------------------------
        // Property 3: Exception in handler does not prevent transition
        // If StatusChanged handler throws, state still changes.
        // **Validates: Requirements 4.5**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ExceptionInHandler_DoesNotPreventTransition()
        {
            var targetGen = Gen.Elements(
                GameApiConnectionMonitor.ConnectionState.Connected,
                GameApiConnectionMonitor.ConnectionState.Disconnected,
                GameApiConnectionMonitor.ConnectionState.DisconnectedCircuitOpen,
                GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                GameApiConnectionMonitor.ConnectionState.Syncing,
                GameApiConnectionMonitor.ConnectionState.RateLimited);

            return Prop.ForAll(Arb.From(targetGen), targetState =>
            {
                using (var client = new GameApiClient("http://localhost:59999"))
                using (var monitor = new GameApiConnectionMonitor(client, _credentialManager, "test-uuid", "test-app-id", "test-client-id"))
                {
                    // Subscribe a handler that throws
                    monitor.StatusChanged += (s, e) =>
                    {
                        throw new InvalidOperationException("Handler failure");
                    };

                    // Act: transition should succeed despite handler throwing
                    monitor.TransitionTo(targetState, "test");

                    // Assert: state changed successfully
                    bool stateChanged = monitor.CurrentState == targetState;

                    return stateChanged.Label(
                        string.Format(
                            "state={0}, expected={1}",
                            monitor.CurrentState,
                            targetState));
                }
            });
        }

        // -------------------------------------------------------------------
        // Property 4: Backoff doubling
        // Starting from 1000ms, each failure doubles up to 60000ms cap.
        // **Validates: Requirements 4.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property BackoffDoubling_CapsAt60000ms()
        {
            var failureCountGen = Gen.Choose(1, 20);

            return Prop.ForAll(Arb.From(failureCountGen), failureCount =>
            {
                using (var client = new GameApiClient("http://localhost:59999"))
                using (var monitor = new GameApiConnectionMonitor(client, _credentialManager, "test-uuid", "test-app-id", "test-client-id"))
                {
                    // First transition to Connected so HandleHealthCheckFailure
                    // can transition to Disconnected
                    monitor.TransitionTo(
                        GameApiConnectionMonitor.ConnectionState.Connected,
                        "initial");

                    // Use reflection to invoke private HandleConnectivityFailure
                    var method = typeof(GameApiConnectionMonitor).GetMethod(
                        "HandleConnectivityFailure",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    // Use reflection to read _currentBackoffMs
                    var backoffField = typeof(GameApiConnectionMonitor).GetField(
                        "_currentBackoffMs",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    // Call HandleHealthCheckFailure multiple times
                    for (int i = 0; i < failureCount; i++)
                    {
                        // Reset to Connected before each failure so the transition
                        // actually fires (same-state is a no-op)
                        monitor.TransitionTo(
                            GameApiConnectionMonitor.ConnectionState.Connected,
                            "reset");
                        method.Invoke(monitor, new object[] { "test failure" });
                    }

                    int actualBackoff = (int)backoffField.GetValue(monitor);

                    // Expected: 1000 * 2^failureCount, capped at 60000
                    // After each HandleHealthCheckFailure, backoff doubles AFTER the call
                    // Initial is 1000, after 1 failure: 2000, after 2: 4000, etc.
                    int expectedBackoff = 1000;
                    for (int i = 0; i < failureCount; i++)
                    {
                        expectedBackoff = Math.Min(expectedBackoff * 2, 60000);
                    }

                    bool backoffCorrect = actualBackoff == expectedBackoff;
                    bool cappedCorrectly = actualBackoff <= 60000;

                    return (backoffCorrect && cappedCorrectly).Label(
                        string.Format(
                            "failures={0}, actual={1}, expected={2}, capped={3}",
                            failureCount,
                            actualBackoff,
                            expectedBackoff,
                            cappedCorrectly));
                }
            });
        }
    }
}
