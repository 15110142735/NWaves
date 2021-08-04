﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NWaves.Audio;
using NWaves.Effects;
using NWaves.Filters.Base;
using NWaves.Operations;
using NWaves.Operations.Tsm;
using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Transforms;

namespace NWaves.DemoForms
{
    public partial class EffectsForm : Form
    {
        private DiscreteSignal _signal;
        private DiscreteSignal _filteredSignal;

        private readonly Stft _stft = new Stft(256, fftSize: 256);

        private string _waveFileName;
        private short _bitDepth;

        private readonly MemoryStreamPlayer _player = new MemoryStreamPlayer();


        public EffectsForm()
        {
            InitializeComponent();

            signalBeforeFilteringPanel.Gain = 80;
            signalAfterFilteringPanel.Gain = 80;
        }
        
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            _waveFileName = ofd.FileName;

            using (var stream = new FileStream(_waveFileName, FileMode.Open))
            {
                var waveFile = new WaveFile(stream);
                _bitDepth = waveFile.WaveFmt.BitsPerSample;
                _signal = waveFile[Channels.Average];
            }

            signalBeforeFilteringPanel.Signal = _signal;
            spectrogramBeforeFilteringPanel.Spectrogram = _stft.Spectrogram(_signal);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            using (var stream = new FileStream(sfd.FileName, FileMode.Create))
            {
                var waveFile = new WaveFile(_filteredSignal, _bitDepth);
                waveFile.SaveTo(stream);
            }
        }

        private void applyEffectButton_Click(object sender, EventArgs e)
        {
            AudioEffect effect;

            var fs = _signal.SamplingRate;

            var winSize = int.Parse(winSizeTextBox.Text);
            var hopSize = int.Parse(hopSizeTextBox.Text);
            var tsm = (TsmAlgorithm)tsmComboBox.SelectedIndex;

            var shift = float.Parse(pitchShiftTextBox.Text);

            if (tremoloRadioButton.Checked)
            {
                var freq = float.Parse(tremoloFrequencyTextBox.Text);
                var index = float.Parse(tremoloIndexTextBox.Text);
                effect = new TremoloEffect(fs, freq, index);
            }
            else if (overdriveRadioButton.Checked)
            {
                var gain = float.Parse(distortionGainTextBox.Text);
                effect = new OverdriveEffect(gain);
            }
            else if (distortionRadioButton.Checked)
            {
                var gain = float.Parse(distortionGainTextBox.Text);
                effect = new DistortionEffect(gain);
            }
            else if (tubeDistortionRadioButton.Checked)
            {
                var gain = float.Parse(distortionGainTextBox.Text);
                var mix = float.Parse(wetTextBox.Text);
                var dist = float.Parse(distTextBox.Text);
                var q = float.Parse(qTextBox.Text);
                effect = new TubeDistortionEffect(gain, mix, q, dist);
            }
            else if (echoRadioButton.Checked)
            {
                var delay = float.Parse(echoDelayTextBox.Text);
                var decay = float.Parse(echoDecayTextBox.Text);
                effect = new EchoEffect(fs, delay, decay);
            }
            else if (delayRadioButton.Checked)
            {
                var delay = float.Parse(echoDelayTextBox.Text);
                var decay = float.Parse(echoDecayTextBox.Text);
                effect = new DelayEffect(fs, delay, decay);
            }
            else if (wahwahRadioButton.Checked)
            {
                var lfoFrequency = float.Parse(lfoFreqTextBox.Text);
                var minFrequency = float.Parse(minFreqTextBox.Text);
                var maxFrequency = float.Parse(maxFreqTextBox.Text);
                var q = float.Parse(lfoQTextBox.Text);
                effect = new WahwahEffect(fs, lfoFrequency, minFrequency, maxFrequency, q);
                //effect = new AutowahEffect(fs, minFrequency, maxFrequency, q);
            }
            else if (flangerRadioButton.Checked)
            {
                var lfoFrequency = float.Parse(lfoFreqTextBox.Text);
                var width = float.Parse(widthTextBox.Text);
                //effect = new FlangerEffect(fs, lfoFrequency, width);//, 0.7f, 0.5f);
                effect = new NewVibratoEffect(fs, lfoFrequency, width);
                //effect = new ChorusEffect(fs, new[] { 1f, 1f, 1f, 1f }, new[] { 0.004f, 0.0042f, 0.0045f, 0.0038f });
            }
            else if (pitchShiftRadioButton.Checked)
            {
                //effect = pitchShiftCheckBox.Checked ? new PitchShiftVocoderEffect(fs, shift, winSize, hopSize) : null;
                effect = pitchShiftCheckBox.Checked ? new PitchShiftEffect(shift, winSize, hopSize, tsm) : null;
                //effect = pitchShiftCheckBox.Checked ? new WhisperEffect(hopSize, winSize) : null;
                //effect = new MorphEffect(hopSize, winSize);
            }
            else
            {
                var lfoFrequency = float.Parse(lfoFreqTextBox.Text);
                var minFrequency = float.Parse(minFreqTextBox.Text);
                var maxFrequency = float.Parse(maxFreqTextBox.Text);
                var q = float.Parse(lfoQTextBox.Text);
                
                var lfo = new SawtoothBuilder()
                                    .SetParameter("freq", lfoFrequency)
                                    .SetParameter("min", minFrequency)
                                    .SetParameter("max", maxFrequency)
                                    .SampledAt(_signal.SamplingRate);

                effect = new PhaserEffect(fs, lfo, q);
            }

            if (effect != null)
            {
                effect.Wet = float.Parse(wetTextBox.Text);
                effect.Dry = float.Parse(dryTextBox.Text);

                _filteredSignal = effect.ApplyTo(_signal, FilteringMethod.Auto);

               
                
                //var ef1 = new VibratoEffect(_signal.SamplingRate);
                //var ef2 = new NewVibratoEffect(_signal.SamplingRate);

                //var res1 = ef1.ApplyTo(_signal, FilteringMethod.Auto);
                //var res2 = ef2.ApplyTo(_signal, FilteringMethod.Auto);

                //var sum = (res1 - res2).Samples.Sum();

                //MessageBox.Show(sum + "");

                //signalBeforeFilteringPanel.Signal = res1;
                //spectrogramBeforeFilteringPanel.Spectrogram = _stft.Spectrogram(res1.Samples);

                //signalAfterFilteringPanel.Signal = res2;
                //spectrogramAfterFilteringPanel.Spectrogram = _stft.Spectrogram(res2.Samples);




                //DiscreteSignal morph;
                //using (var stream = new FileStream(@"D:\Docs\Research\DATABASE\Dictor1\wav\21.wav", FileMode.Open))
                //{
                //    var waveFile = new WaveFile(stream);
                //    morph = waveFile[Channels.Average];
                //}

                //if (morph.SamplingRate != _signal.SamplingRate)
                //{
                //    morph = Operation.Resample(morph, _signal.SamplingRate);
                //}

                //var eff = new MorphEffect(hopSize /*50*/, winSize/*256*/)
                //{
                //    Wet = effect.Wet,
                //    Dry = effect.Dry
                //};

                //_filteredSignal = eff.ApplyTo(_signal, morph);
            }
            else
            {
                _filteredSignal = //Operation.TimeStretch(_signal, shift, tsm);
                                  Operation.TimeStretch(_signal, shift, winSize, hopSize, tsm);
            }

            signalAfterFilteringPanel.Signal = _filteredSignal;
            spectrogramAfterFilteringPanel.Spectrogram = _stft.Spectrogram(_filteredSignal.Samples);
        }

        #region playback

        private async void playSignalButton_Click(object sender, EventArgs e)
        {
            await _player.PlayAsync(_waveFileName);
        }

        private async void playFilteredSignalButton_Click(object sender, EventArgs e)
        {
            await _player.PlayAsync(_filteredSignal, _bitDepth);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            _player.Stop();
        }

        #endregion
    }
}
