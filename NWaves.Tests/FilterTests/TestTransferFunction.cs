﻿using NUnit.Framework;
using NWaves.Filters;
using NWaves.Filters.Base;
using NWaves.Filters.Fda;
using System.Numerics;

namespace NWaves.Tests.FilterTests
{
    [TestFixture]
    public class TestTransferFunction
    {
        [Test]
        public void TestParallelIirIir()
        {
            var f1 = new IirFilter(new[] { 1, -0.1 }, new[] { 1, 0.2 });
            var f2 = new IirFilter(new[] { 1, 0.4 }, new[] { 1, -0.6 });

            var f = f1 + f2;

            Assert.Multiple(() =>
            {
                Assert.That(f.Tf.Numerator, Is.EqualTo(new[] { 2, -0.1, 0.14 }).Within(1e-7));
                Assert.That(f.Tf.Denominator, Is.EqualTo(new[] { 1, -0.4, -0.12 }).Within(1e-7));
            });
        }

        [Test]
        public void TestParallelIirFir()
        {
            var f1 = new IirFilter(new[] { 1,  0.4 }, new[] { 1, -0.6 });
            var f2 = new FirFilter(new[] { 1, -0.1 });

            var f = f1 + f2;

            Assert.Multiple(() =>
            {
                Assert.That(f.Tf.Numerator, Is.EqualTo(new[] { 2, -0.3, 0.06 }).Within(1e-7));
                Assert.That(f.Tf.Denominator, Is.EqualTo(new[] { 1, -0.6 }).Within(1e-7));
                Assert.That(f, Is.TypeOf<IirFilter>());
            });
        }

        [Test]
        public void TestParallelFirIir()
        {
            var f1 = new FirFilter(new[] { 1, -0.1 });
            var f2 = new IirFilter(new[] { 1, 0.4 }, new[] { 1, -0.6 });

            var f = f1 + f2;

            Assert.Multiple(() =>
            {
                Assert.That(f.Tf.Numerator, Is.EqualTo(new[] { 2, -0.3, 0.06 }).Within(1e-7));
                Assert.That(f.Tf.Denominator, Is.EqualTo(new[] { 1, -0.6 }).Within(1e-7));
                Assert.That(f, Is.TypeOf<IirFilter>());
            });
        }

        [Test]
        public void TestParallelFirFir()
        {
            var f1 = new FirFilter(new[] { 1, -0.1 });
            var f2 = new FirFilter(new[] { 1, -0.6 });

            var f = f1 + f2;

            Assert.Multiple(() =>
            {
                Assert.That(f.Tf.Numerator, Is.EqualTo(new[] { 2, -0.7 }).Within(1e-7));
                Assert.That(f, Is.TypeOf<FirFilter>());
            });
        }

        [Test]
        public void TestSequentialIirFir()
        {
            var f1 = new IirFilter(new[] { 1, -0.1 }, new[] { 1, 0.2 });
            var f2 = new FirFilter(new[] { 1, 0.4 });

            var f = f1 * f2;

            Assert.Multiple(() =>
            {
                Assert.That(f.Tf.Numerator, Is.EqualTo(new[] { 1, 0.3, -0.04 }).Within(1e-7));
                Assert.That(f.Tf.Denominator, Is.EqualTo(new[] { 1, 0.2 }).Within(1e-7));
                Assert.That(f, Is.TypeOf<IirFilter>());
            });
        }

        [Test]
        public void TestSequentialFirIir()
        {
            var f1 = new FirFilter(new[] { 1, 0.4 });
            var f2 = new IirFilter(new[] { 1, -0.1 }, new[] { 1, 0.2 });

            var f = f1 * f2;

            Assert.Multiple(() =>
            {
                Assert.That(f.Tf.Numerator, Is.EqualTo(new[] { 1, 0.3, -0.04 }).Within(1e-7));
                Assert.That(f.Tf.Denominator, Is.EqualTo(new[] { 1, 0.2 }).Within(1e-7));
                Assert.That(f, Is.TypeOf<IirFilter>());
            });
        }

        [Test]
        public void TestGroupDelay()
        {
            var f = new MovingAverageFilter(5);
            
            Assert.That(f.Tf.GroupDelay(), Is.All.EqualTo(2.0).Within(1e-10));
        }

        [Test]
        public void TestTfToSos()
        {
            var zeros = new Complex[6]
            {
                new Complex(1,    0),
                new Complex(0.5,  0.2),
                new Complex(-0.3, 0),
                new Complex(0.2, -0.9),
                new Complex(0.5, -0.2),
                new Complex(0.2,  0.9)
            };
            var poles = new Complex[7]
            {
                new Complex(1,    0),
                new Complex(0.2,  0),
                new Complex(0.5,  0),
                new Complex(-0.9, 0.2),
                new Complex(0.6,  0),
                new Complex(0.1,  0),
                new Complex(-0.9, -0.2)
            };

            var sos = DesignFilter.TfToSos(new TransferFunction(zeros, poles));

            Assert.Multiple(() =>
            {
                Assert.That(sos[0].Numerator,   Is.EqualTo(new[] { 1, -0.4, 0.85 }).Within(1e-10));
                Assert.That(sos[0].Denominator, Is.EqualTo(new[] { 1, -0.1,    0 }).Within(1e-10));
                Assert.That(sos[1].Numerator,   Is.EqualTo(new[] { 1,   -1, 0.29 }).Within(1e-10));
                Assert.That(sos[1].Denominator, Is.EqualTo(new[] { 1, -0.7,  0.1 }).Within(1e-10));
                Assert.That(sos[2].Numerator,   Is.EqualTo(new[] { 1,  0.3,    0 }).Within(1e-10));
                Assert.That(sos[2].Denominator, Is.EqualTo(new[] { 1,  1.8, 0.85 }).Within(1e-10));
                Assert.That(sos[3].Numerator,   Is.EqualTo(new[] { 1,   -1,    0 }).Within(1e-10));
                Assert.That(sos[3].Denominator, Is.EqualTo(new[] { 1, -1.6,  0.6 }).Within(1e-10));
                Assert.That(sos.Length, Is.EqualTo(4));
            });
        }

        [Test]
        public void TestSosToTf()
        {
            var sos = new TransferFunction[4]
            {
                new TransferFunction(new[] { 1, -0.4, 0.85 }, new[] { 1, -0.1,    0 }),
                new TransferFunction(new[] { 1, -1,   0.29 }, new[] { 1, -0.7,  0.1 }),
                new TransferFunction(new[] { 1,  0.3,    0 }, new[] { 1,  1.8, 0.85 }),
                new TransferFunction(new[] { 1,   -1,  0.0 }, new[] { 1, -1.6,  0.6 })
            };

            var tf = DesignFilter.SosToTf(sos);

            Assert.Multiple(() =>
            {
                Assert.That(tf.Numerator, Is.EqualTo(new[]   { 1, -2.1, 2.22, -1.624, 0.4607, 0.11725, -0.07395, 0, 0 }).Within(1e-10));
                Assert.That(tf.Denominator, Is.EqualTo(new[] { 1, -0.6, -1.42, 0.888, 0.4889, -0.4413, 0.0895, -0.0051, 0}).Within(1e-10));
            });
        }

        [Test]
        public void TestTfToStateSpace()
        {
            var tf = new TransferFunction(new[] { 2, 0.7, -0.2 }, new[] { 1, 0.5, 0.9, 0.6 });
            var ss = tf.StateSpace;

            Assert.Multiple(() =>
            {
                Assert.That(ss.A[0], Is.EqualTo(new[] { -0.5, -0.9, -0.6 }).Within(1e-10));
                Assert.That(ss.A[1], Is.EqualTo(new[] { 1.0, 0.0, 0.0 }).Within(1e-10));
                Assert.That(ss.A[2], Is.EqualTo(new[] { 0.0, 1.0, 0.0 }).Within(1e-10));
                Assert.That(ss.B, Is.EqualTo(new[] { 1.0, 0.0, 0.0 }).Within(1e-10));
                Assert.That(ss.C, Is.EqualTo(new[] { 2.0, 0.7, -0.2 }).Within(1e-10));
                Assert.That(ss.D, Is.EqualTo(new[] { 0.0 }).Within(1e-10));
            });
        }

        [Test]
        public void TestStateSpaceToTf()
        {
            var tf = new TransferFunction(new[] { 1, 0.7, -0.2, 0.1 }, new[] { 1, 0.5, 0.9, 0.6 });
            var ss = tf.StateSpace;

            var tfs = new TransferFunction(ss);

            Assert.Multiple(() =>
            {
                Assert.That(tf.Numerator, Is.EqualTo(tf.Numerator).Within(1e-10));
                Assert.That(tf.Denominator, Is.EqualTo(tf.Denominator).Within(1e-10));
            });
        }
    }
}
