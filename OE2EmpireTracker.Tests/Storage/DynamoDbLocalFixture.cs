// -----------------------------------------------------------------------
// <copyright file="DynamoDbLocalFixture.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// NUnit SetUpFixture that manages the DynamoDB Local process lifecycle.
    /// Starts DynamoDB Local before all tests in this namespace run and
    /// kills it after all tests complete. Uses -inMemory flag so data
    /// does not persist between test runs.
    /// Satisfies: Req 5, Criteria 1, 4; design.md (DynamoDB Local test infrastructure).
    /// </summary>
    [SetUpFixture]
    public class DynamoDbLocalFixture
    {
        private const string JavaExePath = @"D:\tools\jdk25.0.3_9\bin\java.exe";
        private const string JarPath = @"D:\tools\dynamodb-local\DynamoDBLocal.jar";
        private const string LibraryPath = @"D:\tools\dynamodb-local\DynamoDBLocal_lib";
        private const int Port = 8111;
        private const int MaxReadyWaitMs = 10000;
        private const int PollIntervalMs = 500;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static Process _dynamoProcess;
        private static bool _available;

        /// <summary>
        /// Gets the service URL for DynamoDB Local.
        /// </summary>
        public static string ServiceUrl => $"http://localhost:{Port}";

        /// <summary>
        /// Gets a value indicating whether DynamoDB Local is available
        /// (JDK and JAR exist and the process started successfully).
        /// </summary>
        public static bool IsAvailable => _available;

        /// <summary>
        /// Starts DynamoDB Local before all tests in the Storage namespace.
        /// If the JDK or JAR is not found at the expected path, logs a warning
        /// and marks the fixture as unavailable (tests should skip gracefully).
        /// </summary>
        [OneTimeSetUp]
        public void StartDynamoDbLocal()
        {
            if (!File.Exists(JavaExePath))
            {
                Log.Warn(
                    "DynamoDB Local JDK not found at {0}. DynamoDB tests will be skipped.",
                    JavaExePath);
                _available = false;
                return;
            }

            if (!File.Exists(JarPath))
            {
                Log.Warn(
                    "DynamoDB Local JAR not found at {0}. DynamoDB tests will be skipped.",
                    JarPath);
                _available = false;
                return;
            }

            var arguments = string.Format(
                "-Djava.library.path=\"{0}\" -jar \"{1}\" -inMemory -port {2}",
                LibraryPath,
                JarPath,
                Port);

            var psi = new ProcessStartInfo
            {
                FileName = JavaExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            try
            {
                _dynamoProcess = Process.Start(psi);
                Log.Info("DynamoDB Local process started (PID {0})", _dynamoProcess?.Id);
                WaitForReady();
                _available = true;
                Log.Info("DynamoDB Local is ready on {0}", ServiceUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start DynamoDB Local");
                _available = false;
                CleanupProcess();
            }
        }

        /// <summary>
        /// Stops DynamoDB Local after all tests in the Storage namespace complete.
        /// </summary>
        [OneTimeTearDown]
        public void StopDynamoDbLocal()
        {
            CleanupProcess();
            Log.Info("DynamoDB Local stopped");
        }

        /// <summary>
        /// Waits for DynamoDB Local to become ready by polling the HTTP endpoint.
        /// Attempts to reach the service for up to <see cref="MaxReadyWaitMs"/> milliseconds.
        /// </summary>
        private static void WaitForReady()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                int elapsed = 0;

                while (elapsed < MaxReadyWaitMs)
                {
                    try
                    {
                        // A simple GET to the root returns an error page but proves the server is listening
                        var response = httpClient.GetAsync(ServiceUrl).GetAwaiter().GetResult();
                        return;
                    }
                    catch (HttpRequestException)
                    {
                        // Server not ready yet — connection refused or timeout
                    }
                    catch (TaskCanceledException)
                    {
                        // HTTP client timeout — server not ready yet
                    }

                    Thread.Sleep(PollIntervalMs);
                    elapsed += PollIntervalMs;
                }
            }

            throw new InvalidOperationException(
                $"DynamoDB Local failed to start within {MaxReadyWaitMs / 1000} seconds on {ServiceUrl}");
        }

        /// <summary>
        /// Kills and disposes the DynamoDB Local process if it is running.
        /// </summary>
        private static void CleanupProcess()
        {
            if (_dynamoProcess == null)
            {
                return;
            }

            try
            {
                if (!_dynamoProcess.HasExited)
                {
                    _dynamoProcess.Kill();
                    _dynamoProcess.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
            finally
            {
                _dynamoProcess.Dispose();
                _dynamoProcess = null;
            }
        }
    }
}
