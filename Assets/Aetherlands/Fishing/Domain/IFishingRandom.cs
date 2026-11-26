using System;

namespace Aetherlands.Fishing
{
    public interface IFishingRandom
    {
        float NextFloat();
        float Range(float minInclusive, float maxInclusive);
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// Deterministic pseudo-random generator for fishing simulation and unit tests.
    /// Uses a 32-bit xorshift state so sequences are identical across runtimes and platforms.
    /// </summary>
    public sealed class SeededFishingRandom : IFishingRandom
    {
        uint state;

        public SeededFishingRandom(int seed)
        {
            // Mix initial seed with SplitMix32 avalanche so small integer seeds start with uniform entropy.
            uint z = unchecked((uint)seed) + 0x9E3779B9u;
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            z ^= z >> 16;
            state = z == 0 ? 0x6D2B79F5u : z;
        }

        uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public float NextFloat()
        {
            // 24-bit mantissa precision in [0, 1).
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return minInclusive + (maxInclusive - minInclusive) * NextFloat();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }
    }
}
