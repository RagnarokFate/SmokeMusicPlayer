using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmokeMusicPlayer.Data
{
    [Serializable]
    public class VisualizerProfile
    {
        public string profileName = "Default";
        public int gridResolution = 512;
        public float viscosity = 0.0001f;
        public float diffusion = 0.0001f;
        public float vorticity = 8.0f;
        public float fadeRate = 0.002f; // Slower fade for smoke trails
        public float velocityDissipation = 0.005f; // Slower velocity fade so it swirls longer
        
        // Full color spectrum from low to high frequencies
        public List<Color> colorPalette = new List<Color> { 
            Color.red,
            new Color(1f, 0.5f, 0f), // Orange
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            new Color(0.5f, 0f, 1f)  // Violet
        };
        
        public float bassMultiplier = 2.0f;
        public float trebleMultiplier = 2.0f;
        public float simulationSpeed = 1.0f;
        public float stereoBalance = 0.0f; // -1.0 (Left) to 1.0 (Right)
    }
}
