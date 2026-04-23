using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Tests.Persistence
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

            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("{\"key\":\"value\"}"));
        }

        [Test]
        public void WriteAllText_NewFile_NoBackupCreated()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, "content");

            Assert.That(File.Exists(path + ".bak"), Is.False);
        }

        [Test]
        public void WriteAllText_NewFile_TempFileCleanedUp()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, "content");

            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void WriteAllText_ExistingFile_UpdatesContent()
        {
            var path = TestFile();
            File.WriteAllText(path, "old content");

            SafeFileWriter.WriteAllText(path, "new content");

            Assert.That(File.ReadAllText(path), Is.EqualTo("new content"));
        }

        [Test]
        public void WriteAllText_ExistingFile_CreatesBackup()
        {
            var path = TestFile();
            File.WriteAllText(path, "old content");

            SafeFileWriter.WriteAllText(path, "new content");

            Assert.That(File.Exists(path + ".bak"), Is.True);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo("old content"));
        }

        [Test]
        public void WriteAllText_ExistingFile_TempFileCleanedUp()
        {
            var path = TestFile();
            File.WriteAllText(path, "old content");

            SafeFileWriter.WriteAllText(path, "new content");

            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void WriteAllText_MultipleSaves_BackupIsOnePreviousVersion()
        {
            var path = TestFile();
            File.WriteAllText(path, "v1");

            SafeFileWriter.WriteAllText(path, "v2");
            SafeFileWriter.WriteAllText(path, "v3");

            Assert.That(File.ReadAllText(path), Is.EqualTo("v3"));
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo("v2"));
        }

        [Test]
        public void WriteAllText_EmptyContent_WritesEmptyFile()
        {
            var path = TestFile();
            SafeFileWriter.WriteAllText(path, string.Empty);

            Assert.That(File.ReadAllText(path), Is.EqualTo(string.Empty));
        }

        [Test]
        public void WriteAllText_LargeContent_WritesCorrectly()
        {
            var path = TestFile();
            var content = new string('x', 1_000_000);

            SafeFileWriter.WriteAllText(path, content);

            Assert.That(File.ReadAllText(path).Length, Is.EqualTo(content.Length));
        }
    }
}
