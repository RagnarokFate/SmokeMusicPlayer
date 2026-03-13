using System;
using System.IO;
using UnityEngine;

namespace SmokeMusicPlayer.Data
{
    public static class PresetManager
    {
        private static string PresetDirectory
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "SmokeMusicPlayer", "presets");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static void SaveProfile(VisualizerProfile profile)
        {
            string json = JsonUtility.ToJson(profile, true);
            string filePath = Path.Combine(PresetDirectory, $"{profile.profileName}.json");
            File.WriteAllText(filePath, json);
            Debug.Log($"Profile saved to: {filePath}");
        }

        public static VisualizerProfile LoadProfile(string profileName)
        {
            string filePath = Path.Combine(PresetDirectory, $"{profileName}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<VisualizerProfile>(json);
            }
            
            Debug.LogWarning($"Profile {profileName} not found, returning default.");
            return new VisualizerProfile { profileName = profileName };
        }
        
        public static string[] GetAvailableProfiles()
        {
            if (!Directory.Exists(PresetDirectory)) return new string[0];
            
            string[] files = Directory.GetFiles(PresetDirectory, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }
    }
}
