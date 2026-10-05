using NUnit.Framework;

namespace RollerSplat.Tests
{
    public class SaveDataTests
    {
        string savedPrefix;

        [SetUp]
        public void SetUp()
        {
            // Keep the real player's data untouched.
            savedPrefix = SaveKeys.Prefix;
            SaveKeys.Prefix = "rollersplat.test.";
            GameProgress.Reset();
            GameSettings.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            GameProgress.Reset();
            GameSettings.ResetToDefaults();
            SaveKeys.Prefix = savedPrefix;
        }

        [Test]
        public void Progress_DefaultsToFirstLevel()
        {
            Assert.AreEqual(0, GameProgress.CurrentLevel);
        }

        [Test]
        public void Progress_RemembersLevel_AndResets()
        {
            GameProgress.CurrentLevel = 4;
            Assert.AreEqual(4, GameProgress.CurrentLevel);
            GameProgress.Reset();
            Assert.AreEqual(0, GameProgress.CurrentLevel);
        }

        [Test]
        public void Progress_NegativeClampsToZero()
        {
            GameProgress.CurrentLevel = -3;
            Assert.AreEqual(0, GameProgress.CurrentLevel);
        }

        [Test]
        public void Settings_DefaultVolumes()
        {
            Assert.AreEqual(GameSettings.DefaultVolume, GameSettings.MusicVolume, 1e-4f);
            Assert.AreEqual(GameSettings.DefaultVolume, GameSettings.SfxVolume, 1e-4f);
        }

        [Test]
        public void Settings_ClampsVolume()
        {
            GameSettings.MusicVolume = 2f;
            GameSettings.SfxVolume = -1f;
            Assert.AreEqual(1f, GameSettings.MusicVolume, 1e-4f);
            Assert.AreEqual(0f, GameSettings.SfxVolume, 1e-4f);
        }

        [Test]
        public void Settings_RaisesChanged()
        {
            int calls = 0;
            void Count() => calls++;
            GameSettings.Changed += Count;
            try
            {
                GameSettings.MusicVolume = 0.3f;
                GameSettings.SfxVolume = 0.4f;
            }
            finally
            {
                GameSettings.Changed -= Count;
            }
            Assert.AreEqual(2, calls);
            Assert.AreEqual(0.3f, GameSettings.MusicVolume, 1e-4f);
        }
    }
}
