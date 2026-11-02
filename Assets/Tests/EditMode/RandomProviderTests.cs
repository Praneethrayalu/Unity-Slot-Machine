using System;
using NUnit.Framework;
using SlotGame.Core.Random;

namespace SlotGame.Tests
{
    public sealed class RandomProviderTests
    {
        [Test]
        public void SeededProvider_IsDeterministic()
        {
            var a = new SeededRandomProvider(42);
            var b = new SeededRandomProvider(42);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(a.Next(0, 20), b.Next(0, 20));
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var a = new SeededRandomProvider(1);
            var b = new SeededRandomProvider(2);
            int same = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (a.Next(0, 1000) == b.Next(0, 1000))
                {
                    same++;
                }
            }

            Assert.Less(same, 20);
        }

        [Test]
        public void Providers_StayInRange([Values(1, 2, 3, 7, 20, 1000)] int max)
        {
            using (var crypto = new CryptoRandomProvider())
            {
                var seeded = new SeededRandomProvider(7);
                for (int i = 0; i < 5000; i++)
                {
                    int c = crypto.Next(-5, max);
                    int s = seeded.Next(-5, max);
                    Assert.That(c, Is.InRange(-5, max - 1));
                    Assert.That(s, Is.InRange(-5, max - 1));
                }
            }
        }

        [Test]
        public void InvalidRange_Throws()
        {
            using (var crypto = new CryptoRandomProvider())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => crypto.Next(5, 5));
                Assert.Throws<ArgumentOutOfRangeException>(() => new SeededRandomProvider(1).Next(3, 1));
            }
        }

        /// <summary>
        /// Chi-square goodness-of-fit over 20 buckets (one per reel stop). With 19 degrees of
        /// freedom the 99.9% critical value is 43.82, so a fair generator fails this test about
        /// once in a thousand runs; a biased one (e.g. naive modulo on a small range) fails always.
        /// </summary>
        [Test]
        public void CryptoProvider_IsUniform_ChiSquare()
        {
            using (var crypto = new CryptoRandomProvider())
            {
                AssertUniform(crypto, buckets: 20, samples: 200_000);
            }
        }

        [Test]
        public void SeededProvider_IsUniform_ChiSquare()
        {
            AssertUniform(new SeededRandomProvider(12345), buckets: 20, samples: 200_000);
        }

        private static void AssertUniform(IRandomProvider random, int buckets, int samples)
        {
            var counts = new int[buckets];
            for (int i = 0; i < samples; i++)
            {
                counts[random.Next(0, buckets)]++;
            }

            double expected = (double)samples / buckets;
            double chiSquare = 0;
            foreach (int count in counts)
            {
                double diff = count - expected;
                chiSquare += diff * diff / expected;
            }

            const double criticalValue = 43.82; // df = 19, p = 0.001
            Assert.Less(chiSquare, criticalValue, $"Distribution looks biased (chi^2 = {chiSquare:F2}).");
        }
    }
}
