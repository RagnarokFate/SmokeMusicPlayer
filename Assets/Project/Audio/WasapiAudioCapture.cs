using UnityEngine;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;

namespace SmokeMusicPlayer.Audio
{
    public class WasapiAudioCapture : MonoBehaviour
    {
        private WasapiLoopbackCapture capture;
        private ConcurrentQueue<float[]> bufferQueue = new ConcurrentQueue<float[]>();
        
        public float[] LatestSamples { get; private set; }
        public bool IsCapturing { get; private set; }

        public void StartCapture()
        {
            if (IsCapturing) return;

            try
            {
                capture = new WasapiLoopbackCapture();
                capture.DataAvailable += OnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
                capture.StartRecording();
                IsCapturing = true;
                Debug.Log("WASAPI Loopback Capture started.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start WASAPI capture: {ex.Message}");
            }
        }

        public void StopCapture()
        {
            if (capture != null)
            {
                capture.StopRecording();
                IsCapturing = false;
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            // WASAPI loopback is typically 32-bit float
            int sampleCount = e.BytesRecorded / 4;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = BitConverter.ToSingle(e.Buffer, i * 4);
            }

            bufferQueue.Enqueue(samples);
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            IsCapturing = false;
            if (e.Exception != null)
            {
                Debug.LogError($"WASAPI capture stopped unexpectedly: {e.Exception.Message}");
            }
        }

        void Update()
        {
            // Dequeue all pending buffers and keep the latest one for visualization
            while (bufferQueue.TryDequeue(out float[] samples))
            {
                LatestSamples = samples;
            }
        }

        void OnDisable()
        {
            StopCapture();
        }

        void OnDestroy()
        {
            if (capture != null)
            {
                capture.Dispose();
            }
        }
    }
}