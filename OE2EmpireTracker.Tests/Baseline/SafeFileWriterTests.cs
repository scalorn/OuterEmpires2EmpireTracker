using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using System.IO;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class SafeFileWriterTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "SafeFileWriterTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        private string TestFile(string name = "test.json") => Path.Combine(_testDir, name);

        [Test]
        public void WriteAllText_NewFile_CreatesFile()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, "{\"key\":\"value\"}");

            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual("{\"key\":\"value\"}", File.ReadAllText(path));
        }

        [Test]
        public void WriteAllText_NewFile_NoBackupCreated()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, "content");

            Assert.IsFalse(File.Exists(path + ".bak"));
        }

        [Test]
        public void WriteAllText_NewFile_TempFileCleanedUp()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, "content");

            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void WriteAllText_ExistingFile_UpdatesContent()
        {
            var path = TestFile();
            File.WriteAllText(path, "old content");

            SafeFileWriter.WriteAllText(path, "new content");

            Assert.AreEqual("new content", File.ReadAllText(path));
        }

        [Test]
        public void WriteAllText_ExistingFile_CreatesBackup()
        {
            var path = TestFile();
            File.WriteAllText(path, "old content");

            SafeFileWriter.WriteAllText(path, "new content");

            Assert.IsTrue(File.Exists(path + ".bak"));
            Assert.AreEqual("old content", File.ReadAllText(path + ".bak"));
        }

        [Test]
        public void WriteAllText_ExistingFile_TempFileCleanedUp()
        {
            var path = TestFile();
            File.WriteAllText(path, "old content");

            SafeFileWriter.WriteAllText(path, "new content");

            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void WriteAllText_MultipleSaves_BackupIsOnePreviousVersion()
        {
            var path = TestFile();
            File.WriteAllText(path, "v1");

            SafeFileWriter.WriteAllText(path, "v2");
            SafeFileWriter.WriteAllText(path, "v3");

            Assert.AreEqual("v3", File.ReadAllText(path));
            Assert.AreEqual("v2", File.ReadAllText(path + ".bak"));
        }

        [Test]
        public void WriteAllText_EmptyContent_WritesEmptyFile()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, "");

            Assert.AreEqual("", File.ReadAllText(path));
        }

        [Test]
        public void WriteAllText_LargeContent_WritesCorrectly()
        {
            var path = TestFile();
            var content = new string('x', 1_000_000);

            SafeFileWriter.WriteAllText(path, content);

            Assert.AreEqual(content.Length, File.ReadAllText(path).Length);
        }
    }
}
