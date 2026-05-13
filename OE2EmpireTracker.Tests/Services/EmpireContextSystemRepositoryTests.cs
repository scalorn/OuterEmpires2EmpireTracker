// <copyright file="EmpireContextSystemRepositoryTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests verifying EmpireContext correctly integrates SystemRepository.
    /// Satisfies: Req 2, Criteria 3-4; Req 3, Criterion 6.
    /// </summary>
    [TestFixture]
    public class EmpireContextSystemRepositoryTests
    {
        private string _originalFilePath;

        [SetUp]
        public void SetUp()
        {
            _originalFilePath = SystemRepository.FilePath;
            EmpireContext.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SystemRepository.FilePath = _originalFilePath;
            EmpireContext.Reset();
        }

        [Test]
        public void SystemRepository_IsAccessible_AfterEmpireContextInitialization()
        {
            // Arrange: point SystemRepository at a non-existent file
            SystemRepository.FilePath = Path.Combine(Path.GetTempPath(), "NonExistent_SystemData.json");

            // Act: create EmpireContext via cached test data
            TestHelper.ResetWithCachedData();
            var context = EmpireContext.GetInstance();

            // Assert: SystemRepository property is accessible and not null
            Assert.IsNotNull(context.SystemRepository);
        }

        [Test]
        public void SystemRepository_HasZeroCount_WhenFileDoesNotExist()
        {
            // Arrange: point SystemRepository at a non-existent file
            SystemRepository.FilePath = Path.Combine(Path.GetTempPath(), "NonExistent_SystemData.json");

            // Act: create EmpireContext via cached test data
            TestHelper.ResetWithCachedData();
            var context = EmpireContext.GetInstance();

            // Assert: graceful degradation — 0 systems loaded
            Assert.AreEqual(0, context.SystemRepository.Count);
        }

        [Test]
        public void MissingSystemDataJson_DoesNotCrash_EmpireContextStartup()
        {
            // Arrange: ensure the file truly does not exist
            string fakePath = Path.Combine(Path.GetTempPath(), "Definitely_Missing_SystemData.json");
            if (File.Exists(fakePath))
            {
                File.Delete(fakePath);
            }

            SystemRepository.FilePath = fakePath;

            // Act & Assert: no exception thrown during initialization
            Assert.DoesNotThrow(() => TestHelper.ResetWithCachedData());
        }
    }
}
