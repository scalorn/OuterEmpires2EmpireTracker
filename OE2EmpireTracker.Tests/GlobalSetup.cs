using NUnit.Framework;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests
{
    /// <summary>
    /// Runs once before any test in the assembly.
    /// Suppresses MessageBox dialogs from MigrationRunner during tests.
    /// </summary>
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void AssemblyInit()
        {
            MigrationRunner.SuppressUI = true;
        }
    }
}
