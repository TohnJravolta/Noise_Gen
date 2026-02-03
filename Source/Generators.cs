using System;

namespace NoiseGen
{
    public interface IGenerator
    {
        string Name { get; }
        bool Enabled { get; set; }
        float Volume { get; set; } // 0.0 to 1.0
        // Fill interleaved stereo buffer. 
        // offset is integer index in buffer float array. 
        // count is number of float samples to fill (must be even for stereo).
        // sampleRate is needed for frequency calculations.
        void FillBuffer(float[] buffer, int offset, int count, int sampleRate);
    }

    public abstract class BaseGenerator : IGenerator
    {
        public string Name { get; protected set; }
        public bool Enabled { get; set; }
        public float Volume { get; set; }

        protected Random _rng = new Random();

        public BaseGenerator() { Volume = 0.5f; }

        public abstract void FillBuffer(float[] buffer, int offset, int count, int sampleRate);
    }

    public class WhiteNoiseGenerator : BaseGenerator
    {
        public WhiteNoiseGenerator() { Name = "White Noise"; }

        public override void FillBuffer(float[] buffer, int offset, int count, int sampleRate)
        {
            for (int i = 0; i < count; i += 2) // Stereo
            {
                float val = ((float)_rng.NextDouble() * 2.0f - 1.0f) * Volume;
                buffer[offset + i] += val;     // Left
                buffer[offset + i + 1] += val; // Right
            }
        }
    }

    public class PinkNoiseGenerator : BaseGenerator
    {
        // Simple approximation using Voss-McCartney algorithm or just a simple filter
        // Using a simple 1/f approximation for lightness: 
        // 3 poles
        private double _b0, _b1, _b2;

        public PinkNoiseGenerator() { Name = "Pink Noise"; }

        public override void FillBuffer(float[] buffer, int offset, int count, int sampleRate)
        {
            for (int i = 0; i < count; i += 2)
            {
                double white = _rng.NextDouble() * 2.0 - 1.0;
                _b0 = 0.99886 * _b0 + white * 0.0555179;
                _b1 = 0.99332 * _b1 + white * 0.0750759;
                _b2 = 0.96900 * _b2 + white * 0.1538520;
                
                float val = (float)((_b0 + _b1 + _b2) * 0.5) * Volume; // 0.5 to normalize roughly

                buffer[offset + i] += val;
                buffer[offset + i + 1] += val;
            }
        }
    }

    public class BrownNoiseGenerator : BaseGenerator
    {
        private float _lastOut = 0;
        public BrownNoiseGenerator() { Name = "Brown Noise"; }

        public override void FillBuffer(float[] buffer, int offset, int count, int sampleRate)
        {
            for (int i = 0; i < count; i += 2)
            {
                float white = (float)_rng.NextDouble() * 2.0f - 1.0f;
                _lastOut = (_lastOut + (0.02f * white)) / 1.02f;
                
                float val = _lastOut * 3.5f * Volume; // Boost due to low amplitude

                buffer[offset + i] += val;
                buffer[offset + i + 1] += val;
            }
        }
    }

    public class BinauralGenerator : BaseGenerator
    {
        private double _phaseL = 0;
        private double _phaseR = 0;
        
        // Configurable frequencies
        public float CarrierFreq { get; set; } // Base frequency
        public float BeatFreq { get; set; }     // Target beat frequency (Alpha/Beta)

        public BinauralGenerator(string name, float carrier, float beat) 
        { 
            Name = name; 
            CarrierFreq = carrier;
            BeatFreq = beat;
            Volume = 0.5f;
        }

        public override void FillBuffer(float[] buffer, int offset, int count, int sampleRate)
        {
            float freqL = CarrierFreq;
            float freqR = CarrierFreq + BeatFreq;
            
            double incL = (Math.PI * 2 * freqL) / sampleRate;
            double incR = (Math.PI * 2 * freqR) / sampleRate;

            for (int i = 0; i < count; i += 2)
            {
                float valL = (float)Math.Sin(_phaseL) * Volume;
                float valR = (float)Math.Sin(_phaseR) * Volume;

                buffer[offset + i] += valL;
                buffer[offset + i + 1] += valR;

                _phaseL += incL;
                _phaseR += incR;

                if (_phaseL > Math.PI * 2) _phaseL -= Math.PI * 2;
                if (_phaseR > Math.PI * 2) _phaseR -= Math.PI * 2;
            }
        }
    }
}
