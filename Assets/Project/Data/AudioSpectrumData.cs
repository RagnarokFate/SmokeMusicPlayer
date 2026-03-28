using UnityEngine;

namespace SmokeMusicPlayer.Data
{
    public struct AudioSpectrumData
    {
        public float[] spectrum;
        public float lowBandAvg;
        public float midBandAvg;
        public float highBandAvg;
        public float overallAmplitude;
        public float rmsValue;
        public float currentDB;
        public float peakDB;

        public AudioSpectrumData(int sampleSize)
        {
            spectrum = new float[sampleSize];
            lowBandAvg = 0f;
            midBandAvg = 0f;
            highBandAvg = 0f;
            overallAmplitude = 0f;
            rmsValue = 0f;
            currentDB = -70f;
            peakDB = -70f;
        }
    }
}
