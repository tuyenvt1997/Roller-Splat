using NUnit.Framework;

namespace RollerSplat.Tests
{
    public class LevelTimerTests
    {
        [Test]
        public void Tick_CountsDown()
        {
            var t = new LevelTimer();
            t.Start(10f);
            Assert.IsFalse(t.Tick(3f));
            Assert.AreEqual(7f, t.Remaining, 1e-4f);
        }

        [Test]
        public void Tick_ReportsExpiryOnce_AndClampsAtZero()
        {
            var t = new LevelTimer();
            t.Start(2f);
            Assert.IsTrue(t.Tick(5f));
            Assert.AreEqual(0f, t.Remaining);
            Assert.IsFalse(t.Tick(1f));
        }

        [Test]
        public void NoLimit_NeverExpires()
        {
            var t = new LevelTimer();
            t.Start(0f);
            Assert.IsFalse(t.HasLimit);
            Assert.IsFalse(t.Tick(1000f));
        }

        [Test]
        public void Stop_FreezesRemaining()
        {
            var t = new LevelTimer();
            t.Start(10f);
            t.Tick(4f);
            t.Stop();
            Assert.IsFalse(t.Tick(100f));
            Assert.AreEqual(6f, t.Remaining, 1e-4f);
        }

        [TestCase(60f, 60f, 3)]
        [TestCase(40f, 60f, 3)]
        [TestCase(39.9f, 60f, 2)]
        [TestCase(20f, 60f, 2)]
        [TestCase(19.9f, 60f, 1)]
        [TestCase(0f, 60f, 1)]
        [TestCase(0f, 0f, 3)]
        public void ComputeStars_ByRemainingFraction(float remaining, float limit, int expected)
        {
            Assert.AreEqual(expected, LevelTimer.ComputeStars(remaining, limit));
        }
    }
}
