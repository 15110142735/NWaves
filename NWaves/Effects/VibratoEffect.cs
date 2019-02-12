﻿using NWaves.Signals.Builders;
using System;

namespace NWaves.Effects
{
    /// <summary>
    /// Vibrato effect
    /// </summary>
    public class VibratoEffect : AudioEffect
    {
        /// <summary>
        /// Max delay (in seconds)
        /// </summary>
        public float MaxDelay { get; }

        /// <summary>
        /// LFO frequency
        /// </summary>
        public float LfoFrequency { get; }

        /// <summary>
        /// Sampling rate
        /// </summary>
        private int _fs;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="maxDelay"></param>
        /// <param name="lfoFrequency"></param>
        public VibratoEffect(int samplingRate, float maxDelay = 0.003f/*sec*/, float lfoFrequency = 1/*Hz*/)
        {
            _fs = samplingRate;
            MaxDelay = maxDelay;

            _lfo = new SineBuilder()
                            .SetParameter("freq", lfoFrequency)
                            .SampledAt(samplingRate);

            _maxDelayPos = (int)(Math.Ceiling(samplingRate * maxDelay));
            _delayLine = new float[_maxDelayPos];
        }

        /// <summary>
        /// Simple flanger effect
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public override float Process(float sample)
        {
            var delay = (int)Math.Ceiling((_lfo.NextSample() + 1) / 2 * _maxDelayPos);

            if (_n == _delayLine.Length)
            {
                _n = 0;
            }

            _delayLine[_n] = sample;

            var delayedSample = _n >= delay ? _delayLine[_n - delay] : _delayLine[_n + _maxDelayPos - delay];

            _n++;

            return delayedSample;
            
            // instead of:
            // return Dry * sample + Wet * delayedSample;
        }

        public override void Reset()
        {
            _n = 0;

            for (var i = 0; i < _delayLine.Length; i++)
            {
                _delayLine[i] = 0;
            }
        }

        private SignalBuilder _lfo;

        private float[] _delayLine;
        private int _maxDelayPos;

        private int _n;
    }
}
