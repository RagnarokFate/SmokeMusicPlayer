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
        private AudioSource audioSource;
        private AudioSpectrumData spectrumData;
        private const int SAMPLE_SIZE = 1024;
        
        private string activeMicDevice = null;
        private bool isMicrophoneMode = false;
        private bool isDesktopMode = false;
        
        [SerializeField] private WasapiAudioCapture wasapiCapture;

        public AudioTrackMetadata CurrentTrack { get; private set; }
        public bool IsPlaying => (isMicrophoneMode && Microphone.IsRecording(activeMicDevice)) || 
                                (isDesktopMode && wasapiCapture != null && wasapiCapture.IsCapturing) ||
                                (audioSource != null && audioSource.isPlaying);

        public event Action<AudioTrackMetadata> OnTrackLoaded;
        public event Action<string> OnError;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            spectrumData = new AudioSpectrumData(SAMPLE_SIZE);
            audioSource.loop = true; // Loop for mic buffer
            
            if (wasapiCapture == null) wasapiCapture = GetComponent<WasapiAudioCapture>();
        }

        public string[] GetMicrophoneDevices()
        {
            return Microphone.devices;
        }

        public void StartMicrophone(string deviceName)
        {
            StopAllModes();
            isMicrophoneMode = true;
            activeMicDevice = deviceName;

            // Start recording: 1 sec buffer, looping, at default frequency
            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
            int freq = maxFreq > 0 ? maxFreq : 44100;

            audioSource.clip = Microphone.Start(deviceName, true, 1, freq);
            audioSource.mute = true; // Analyzed silently

            // Wait for mic to start properly before playing
            while (!(Microphone.GetPosition(deviceName) > 0)) { }
            audioSource.Play();
            
            Debug.Log($"Started Microphone: {deviceName} at {freq}Hz");
        }

        public void StartDesktopAudio()
        {
            StopAllModes();
            isDesktopMode = true;
            if (wasapiCapture != null) wasapiCapture.StartCapture();
        }

        public void StopAllModes()
        {
            if (isMicrophoneMode)
            {
                Microphone.End(activeMicDevice);
                audioSource.Stop();
                audioSource.mute = false;
                isMicrophoneMode = false;
            }
            if (isDesktopMode)
            {
                if (wasapiCapture != null) wasapiCapture.StopCapture();
                isDesktopMode = false;
            }
        }

        public void LoadAndPlayTrack(string absolutePath)
        {
            StopAllModes();
            StartCoroutine(LoadAudioRoutine(absolutePath));
        }

        private IEnumerator LoadAudioRoutine(string path)
        {
            // Note: In Unity, local file paths need the file:// prefix
            string uri = "file://" + path;
            
            // Determine type based on extension
            AudioType type = AudioType.UNKNOWN;
            if (path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) type = AudioType.MPEG;
            else if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) type = AudioType.WAV;
            else
            {
                OnError?.Invoke($"Unsupported audio format: {path}");
                yield break;
            }
            
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, type))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    OnError?.Invoke($"Error loading audio: {www.error}");
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
                {
                    OnError?.Invoke("Failed to decode audio clip.");
                    yield break;
                }

                audioSource.clip = clip;
                
                CurrentTrack = new AudioTrackMetadata(
                    path, 
                    System.IO.Path.GetFileName(path), 
                    clip.length, 
                    clip.frequency, 
                    clip.channels
                );

                OnTrackLoaded?.Invoke(CurrentTrack);
                Play();
            }
        }

        public void Play()
        {
            if (audioSource.clip != null) audioSource.Play();
        }

        public void Pause()
        {
            audioSource.Pause();
        }

        public void SetPlaybackSpeed(float speed)
        {
            if (audioSource != null)
            {
                audioSource.pitch = Mathf.Clamp(speed, 0.5f, 2.0f);
            }
        }

        public AudioSpectrumData GetSpectrumData()
        {
            if (!IsPlaying) return spectrumData;

            if (isDesktopMode && wasapiCapture != null && wasapiCapture.LatestSamples != null)
            {
                AnalyzeExternalSamples(wasapiCapture.LatestSamples);
            }
            else
            {
                audioSource.GetSpectrumData(spectrumData.spectrum, 0, FFTWindow.BlackmanHarris);
            }

            // Calculate averages for frequency bands
            // Assuming 44100Hz sample rate, 1024 samples -> ~21.5Hz per bin
            float low = 0, mid = 0, high = 0;
            
            // Low: 0 - 250Hz (approx bins 0-11)
            for (int i = 0; i < 12; i++) low += spectrumData.spectrum[i];
            spectrumData.lowBandAvg = low / 12f;

            // Mid: 250Hz - 4000Hz (approx bins 12-186)
            for (int i = 12; i < 186; i++) mid += spectrumData.spectrum[i];
            spectrumData.midBandAvg = mid / 174f;

            // High: 4000Hz - 20000Hz (approx bins 186-930)
            for (int i = 186; i < 930; i++) high += spectrumData.spectrum[i];
            spectrumData.highBandAvg = high / 744f;

            // Overall Amplitude (simplified average)
            float total = 0;
            for (int i = 0; i < 512; i++) total += spectrumData.spectrum[i];
            spectrumData.overallAmplitude = total / 512f;

            return spectrumData;
        }

        private void AnalyzeExternalSamples(float[] samples)
        {
            // For MVP: Simple power distribution approximation from time-domain samples
            // Note: In a production visualizer, we would use a real FFT library like KissFFT or a Compute Shader
            int startIdx = Math.Max(0, samples.Length - SAMPLE_SIZE);
            for (int i = 0; i < SAMPLE_SIZE && (startIdx + i) < samples.Length; i++)
            {
                spectrumData.spectrum[i] = Mathf.Abs(samples[startIdx + i]);
            }
        }
    }
}
