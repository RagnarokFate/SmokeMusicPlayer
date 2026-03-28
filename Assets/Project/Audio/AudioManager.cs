using System;
using System.Collections;
using SmokeMusicPlayer.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace SmokeMusicPlayer.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public enum SourceMode { File, Microphone }
        
        [Header("Settings")]
        public SourceMode initialMode = SourceMode.File;
        public string selectedMicName = ""; // Empty = Default
        public float liveSensitivity = 10.0f; // Multiplier for quiet mic inputs
        
        private AudioSource audioSource;
        private AudioSpectrumData spectrumData;
        private const int SAMPLE_SIZE = 1024;
        
        private string activeMicDevice = null;
        private bool isMicrophoneMode = false;

        public AudioTrackMetadata CurrentTrack { get; private set; }
        public bool IsPlaying => (isMicrophoneMode && Microphone.IsRecording(activeMicDevice)) || (audioSource != null && audioSource.isPlaying);

        public event Action<AudioTrackMetadata> OnTrackLoaded;
        public event Action<string> OnError;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            spectrumData = new AudioSpectrumData(SAMPLE_SIZE);
            audioSource.loop = true; 
        }

        private void Start()
        {
            ApplyMode(initialMode, selectedMicName);
        }

        public void ApplyMode(SourceMode mode, string micName = "")
        {
            StopAllModes();
            initialMode = mode; // Keep enum in sync
            
            if (mode == SourceMode.Microphone)
            {
                StartMicrophone(string.IsNullOrEmpty(micName) ? (Microphone.devices.Length > 0 ? Microphone.devices[0] : "") : micName);
            }
            else
            {
                // File mode: If we already have a clip, resume playing it
                if (audioSource != null && audioSource.clip != null)
                {
                    audioSource.Play();
                }
            }
        }

        public string[] GetMicrophoneDevices() => Microphone.devices;

        public void StartMicrophone(string deviceName)
        {
            StopAllModes();
            isMicrophoneMode = true;
            activeMicDevice = deviceName;

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
            int freq = maxFreq > 0 ? maxFreq : 44100;

            audioSource.clip = Microphone.Start(deviceName, true, 1, freq);
            audioSource.mute = true; 

            while (!(Microphone.GetPosition(deviceName) > 0)) { }
            audioSource.Play();
            
            Debug.Log($"Started Microphone: {deviceName}");
        }

        public void StopAllModes()
        {
            if (isMicrophoneMode)
            {
                Microphone.End(activeMicDevice);
                isMicrophoneMode = false;
            }
            
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.mute = false;
            }
        }

        public void LoadAndPlayTrack(string absolutePath)
        {
            StopAllModes();
            StartCoroutine(LoadAudioRoutine(absolutePath));
        }

        private IEnumerator LoadAudioRoutine(string path)
        {
            string uri = "file://" + path;
            AudioType type = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.WAV;
            
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, type))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    CurrentTrack = new AudioTrackMetadata(path, System.IO.Path.GetFileName(path), clip.length, clip.frequency, clip.channels);
                    OnTrackLoaded?.Invoke(CurrentTrack);
                    Play();
                }
            }
        }

        public void Play() { if (audioSource.clip != null) audioSource.Play(); }
        public void Pause() { audioSource.Pause(); }
        public void SetPlaybackSpeed(float speed) { if (audioSource != null) audioSource.pitch = Mathf.Clamp(speed, 0.5f, 2.0f); }

        private int lastUpdateFrame = -1;

        public AudioSpectrumData GetSpectrumData()
        {
            if (this == null || !IsPlaying) return spectrumData;
            if (lastUpdateFrame == Time.frameCount) return spectrumData;
            lastUpdateFrame = Time.frameCount;

            if (isMicrophoneMode && audioSource != null && audioSource.clip != null)
            {
                float[] micSamples = new float[SAMPLE_SIZE];
                int micPos = Microphone.GetPosition(activeMicDevice);
                if (micPos >= SAMPLE_SIZE)
                {
                    audioSource.clip.GetData(micSamples, micPos - SAMPLE_SIZE);
                    AnalyzeSamples(micSamples, liveSensitivity);
                }
            }
            else if (audioSource != null && audioSource.clip != null)
            {
                audioSource.GetSpectrumData(spectrumData.spectrum, 0, FFTWindow.BlackmanHarris);
            }

            CalculateBands();
            return spectrumData;
        }

        private void AnalyzeSamples(float[] samples, float multiplier)
        {
            if (this == null || samples == null || spectrumData.spectrum == null) return;
            for (int i = 0; i < SAMPLE_SIZE && i < samples.Length; i++)
            {
                spectrumData.spectrum[i] = Mathf.Abs(samples[i]) * multiplier * (1.0f - (float)i / SAMPLE_SIZE);
            }
        }

        private float[] smoothSpectrum;
        private float[] perceptualSpectrum;
        private float peakFallSpeed = 30f; // dB per second
        private float autoGain = 1.0f;

        private void CalculateBands()
        {
            if (this == null || spectrumData.spectrum == null) return;
            
            int sampleCount = spectrumData.spectrum.Length;
            if (smoothSpectrum == null || smoothSpectrum.Length != sampleCount)
                smoothSpectrum = new float[sampleCount];
            if (perceptualSpectrum == null || perceptualSpectrum.Length != 64)
                perceptualSpectrum = new float[64];

            float maxThisFrame = 0.0001f;
            float sumSquared = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float raw = spectrumData.spectrum[i];
                smoothSpectrum[i] = Mathf.Lerp(smoothSpectrum[i], raw, Time.deltaTime * 15f);
                sumSquared += raw * raw;
                if (raw > maxThisFrame) maxThisFrame = raw;
            }

            // Update Auto-Gain: slowly adjust to the peak level to keep visuals "full"
            float targetGain = 1.0f / Mathf.Max(maxThisFrame, 0.01f);
            autoGain = Mathf.Lerp(autoGain, Mathf.Clamp(targetGain, 1f, 50f), Time.deltaTime * 0.5f);

            // Calculate Perceptual Spectrum (Logarithmic grouping + dB Scaling)
            int numBands = perceptualSpectrum.Length;
            for (int i = 0; i < numBands; i++)
            {
                float normalized = (float)i / numBands;
                // Logarithmic mapping from 512 bins to 64 bands
                int startBin = Mathf.FloorToInt(Mathf.Pow(normalized, 1.5f) * (sampleCount - 1));
                int endBin = Mathf.FloorToInt(Mathf.Pow((float)(i + 1) / numBands, 1.5f) * (sampleCount - 1));
                if (endBin <= startBin) endBin = startBin + 1;

                float avg = 0;
                for (int b = startBin; b < endBin && b < sampleCount; b++) avg += spectrumData.spectrum[b];
                avg /= (endBin - startBin);

                // Perceptual Scaling: Convert to a "pseudo-dB" 0-1 range for UI
                float dbValue = 20 * Mathf.Log10(Mathf.Max(avg * autoGain, 0.0001f));
                float normalizedDb = Mathf.InverseLerp(-60f, 0f, dbValue);
                perceptualSpectrum[i] = Mathf.Lerp(perceptualSpectrum[i], normalizedDb, Time.deltaTime * 20f);
            }

            // RMS and DB
            spectrumData.rmsValue = Mathf.Sqrt(sumSquared / sampleCount);
            float db = 20 * Mathf.Log10(Mathf.Max(spectrumData.rmsValue, 0.0003f)); 
            spectrumData.currentDB = Mathf.Lerp(spectrumData.currentDB, db, Time.deltaTime * 25f);
            
            if (spectrumData.currentDB > spectrumData.peakDB)
            {
                spectrumData.peakDB = spectrumData.currentDB;
            }
            else
            {
                spectrumData.peakDB -= peakFallSpeed * Time.deltaTime;
                if (spectrumData.peakDB < -70f) spectrumData.peakDB = -70f;
            }

            float low = 0, mid = 0, high = 0;
            for (int i = 0; i < 12; i++) low += spectrumData.spectrum[i];
            spectrumData.lowBandAvg = low / 12f;
            for (int i = 12; i < 186; i++) mid += spectrumData.spectrum[i];
            spectrumData.midBandAvg = mid / 174f;
            for (int i = 186; i < 930 && i < sampleCount; i++) high += spectrumData.spectrum[i];
            spectrumData.highBandAvg = high / Mathf.Max(1, (Mathf.Min(930, sampleCount) - 186));
            
            float total = 0;
            for (int i = 0; i < Mathf.Min(512, sampleCount); i++) total += spectrumData.spectrum[i];
            spectrumData.overallAmplitude = total / Mathf.Min(512, sampleCount);
        }

        public float[] GetSmoothSpectrum() => smoothSpectrum;
        public float[] GetPerceptualSpectrum() => perceptualSpectrum;

    }
}