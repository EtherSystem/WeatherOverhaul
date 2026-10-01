namespace WeatherOverhaul.Utilities
{
    internal sealed class DeterministicRng
    {
        private uint m_State;

        internal DeterministicRng(int seed)
        {
            m_State = (uint)(seed == 0 ? 0x6D2B79F5 : seed);
        }

        internal float NextFloat()
        {
            uint value = NextUInt();
            return (value & 0x00FFFFFF) / 16777216f;
        }

        internal bool Chance(float probability)
        {
            return NextFloat() < Math.Max(0f, Math.Min(1f, probability));
        }

        internal float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        private uint NextUInt()
        {
            uint z = m_State += 0x6D2B79F5;
            z = (z ^ z >> 15) * (z | 1u);
            z ^= z + (z ^ z >> 7) * (z | 61u);
            return z ^ z >> 14;
        }
    }
}