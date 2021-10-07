﻿using NWaves.Filters.Base;
using NWaves.Signals;
using NWaves.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NWaves.Benchmarks
{
    public class IirFilterV2 : LtiFilter
    {
        /// <summary>
        /// Denominator part coefficients in filter's transfer function 
        /// (recursive part in difference equations).
        /// 
        /// NOTE.
        /// These coefficients have single precision since they are used for filtering!
        /// For filter design & analysis specify transfer function (Tf property).
        /// 
        /// </summary>
        protected readonly float[] _a;

        /// <summary>
        /// Numerator part coefficients in filter's transfer function 
        /// (non-recursive part in difference equations)
        /// 
        /// NOTE.
        /// These coefficients have single precision since they are used for filtering!
        /// For filter design & analysis specify transfer function (Tf property).
        /// 
        /// </summary>
        protected readonly float[] _b;

        /// <summary>
        /// Transfer function (created lazily or set specifically if needed)
        /// </summary>
        protected TransferFunction _tf;
        public override TransferFunction Tf
        {
            get => _tf ?? new TransferFunction(_b.ToDoubles(), _a.ToDoubles());
            protected set => _tf = value;
        }

        /// <summary>
        /// Default length of truncated impulse response
        /// </summary>
        public const int DefaultImpulseResponseLength = 512;

        /// <summary>
        /// Internal buffers for delay lines
        /// </summary>
        protected float[] _delayLineA;
        protected float[] _delayLineB;

        /// <summary>
        /// Current offsets in delay lines
        /// </summary>
        protected int _delayLineOffsetA;
        protected int _delayLineOffsetB;

        /// <summary>
        /// Parameterized constructor (from arrays of 32-bit coefficients)
        /// </summary>
        /// <param name="b">TF numerator coefficients</param>
        /// <param name="a">TF denominator coefficients</param>
        public IirFilterV2(IEnumerable<float> b, IEnumerable<float> a)
        {
            _b = b.ToArray();
            _a = a.ToArray();
            ResetInternals();
        }

        /// <summary>
        /// Parameterized constructor (from arrays of 64-bit coefficients)
        /// 
        /// NOTE.
        /// It will simply cast values to floats!
        /// If you need to preserve precision for filter design & analysis, use constructor with TransferFunction!
        /// 
        /// </summary>
        /// <param name="b">TF numerator coefficients</param>
        /// <param name="a">TF denominator coefficients</param>
        public IirFilterV2(IEnumerable<double> b, IEnumerable<double> a) : this(b.ToFloats(), a.ToFloats())
        {
        }

        /// <summary>
        /// Parameterized constructor (from transfer function).
        /// 
        /// Coefficients (used for filtering) will be cast to floats anyway,
        /// but filter will store the reference to TransferFunction object for FDA.
        /// 
        /// </summary>
        /// <param name="tf">Transfer function</param>
        public IirFilterV2(TransferFunction tf) : this(tf.Numerator, tf.Denominator)
        {
            Tf = tf;
        }

        /// <summary>
        /// Apply filter to entire signal (offline)
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public override DiscreteSignal ApplyTo(DiscreteSignal signal,
                                               FilteringMethod method = FilteringMethod.Auto)
        {
            switch (method)
            {
                //case FilteringMethod.OverlapAdd:       // are you sure you wanna do this? It's IIR filter!
                //case FilteringMethod.OverlapSave:
                //    {
                //        var length = Math.Max(DefaultImpulseResponseLength, _a.Length + _b.Length);
                //        var fftSize = MathUtils.NextPowerOfTwo(4 * length);
                //        var ir = new DiscreteSignal(signal.SamplingRate, Tf.ImpulseResponse(length).ToFloats());
                //        return Operation.BlockConvolve(signal, ir, fftSize, method);
                //    }
                default:
                    {
                        return ApplyFilterDirectly(signal);
                    }
            }
        }

        /// <summary>
        /// IIR online filtering (sample-by-sample)
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public override float Process(float sample)
        {
            var output = 0.0f;

            _delayLineB[_delayLineOffsetB] = sample;

            var pos = 0;
            for (var k = _delayLineOffsetB; k < _b.Length; k++)
            {
                output += _b[pos++] * _delayLineB[k];
            }
            for (var k = 0; k < _delayLineOffsetB; k++)
            {
                output += _b[pos++] * _delayLineB[k];
            }

            pos = 1;
            for (var p = _delayLineOffsetA + 1; p < _a.Length; p++)
            {
                output -= _a[pos++] * _delayLineA[p];
            }
            for (var p = 0; p < _delayLineOffsetA; p++)
            {
                output -= _a[pos++] * _delayLineA[p];
            }

            _delayLineA[_delayLineOffsetA] = output;

            if (--_delayLineOffsetB < 0)
            {
                _delayLineOffsetB = _delayLineB.Length - 1;
            }

            if (--_delayLineOffsetA < 0)
            {
                _delayLineOffsetA = _delayLineA.Length - 1;
            }

            return output;
        }

        /// <summary>
        /// The most straightforward implementation of the difference equation:
        /// code the difference equation as it is
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        public DiscreteSignal ApplyFilterDirectly(DiscreteSignal signal)
        {
            var input = signal.Samples;

            var output = new float[input.Length];

            for (var n = 0; n < input.Length; n++)
            {
                for (var k = 0; k < _b.Length; k++)
                {
                    if (n >= k) output[n] += _b[k] * input[n - k];
                }
                for (var m = 1; m < _a.Length; m++)
                {
                    if (n >= m) output[n] -= _a[m] * output[n - m];
                }
            }

            return new DiscreteSignal(signal.SamplingRate, output);
        }

        /// <summary>
        /// Change filter coefficients online (numerator part)
        /// </summary>
        /// <param name="b">New coefficients</param>
        public void ChangeNumeratorCoeffs(float[] b)
        {
            if (b.Length == _b.Length)
            {
                for (var i = 0; i < _b.Length; _b[i] = b[i], i++) { }
            }
        }

        /// <summary>
        /// Change filter coefficients online (denominator / recursive part)
        /// </summary>
        /// <param name="a">New coefficients</param>
        public void ChangeDenominatorCoeffs(float[] a)
        {
            if (a.Length == _a.Length)
            {
                for (var i = 0; i < _a.Length; _a[i] = a[i], i++) { }
            }
        }

        /// <summary>
        /// Reset internal buffers
        /// </summary>
        protected void ResetInternals()
        {
            if (_delayLineB == null || _delayLineA == null)
            {
                _delayLineB = new float[_b.Length];
                _delayLineA = new float[_a.Length];
            }
            else
            {
                for (var i = 0; i < _delayLineB.Length; i++)
                {
                    _delayLineB[i] = 0;
                }
                for (var i = 0; i < _delayLineA.Length; i++)
                {
                    _delayLineA[i] = 0;
                }
            }

            _delayLineOffsetB = _delayLineB.Length - 1;
            _delayLineOffsetA = _delayLineA.Length - 1;
        }

        /// <summary>
        /// Reset filter
        /// </summary>
        public override void Reset() => ResetInternals();

        /// <summary>
        /// Divide all filter coefficients by Tf.Denominator[0] if Tf is specified (double precision)
        /// or by _a[0] otherwise (single precision)
        /// </summary>
        public void Normalize()
        {
            var a0 = _a[0];

            if (Math.Abs(a0 - 1.0) < 1e-10)
            {
                return;
            }

            if (Math.Abs(a0) < 1e-10)
            {
                throw new ArgumentException("The first A coefficient can not be zero!");
            }

            for (var i = 0; i < _a.Length; i++)
            {
                _a[i] /= a0;
            }
            for (var i = 0; i < _b.Length; i++)
            {
                _b[i] /= a0;
            }

            _tf?.Normalize();
        }
    }
}
