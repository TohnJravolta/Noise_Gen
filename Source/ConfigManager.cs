using System;
using System.Collections.Generic;
using System.IO;

namespace NoiseGen
{
    public class ConfigManager
    {
        public string FilePath { get; set; }

        public Dictionary<string, string> Settings = new Dictionary<string, string>();

        public ConfigManager(string path = "profiles.ini")
        {
            FilePath = path;
        }

        public void Load()
        {
            if (!File.Exists(FilePath)) return;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                var parts = line.Split(new char[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    Settings[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        public void Save()
        {
            using (var sw = new StreamWriter(FilePath))
            {
                foreach (var kvp in Settings)
                {
                    sw.WriteLine(kvp.Key + "=" + kvp.Value);
                }
            }
        }

        public string Get(string key, string def)
        {
            return Settings.ContainsKey(key) ? Settings[key] : def;
        }

        public void Set(string key, string val)
        {
            Settings[key] = val;
        }

        public float GetFloat(string key, float def)
        {
            float result;
            if (Settings.ContainsKey(key) && float.TryParse(Settings[key], out result))
            {
                return result;
            }
            return def;
        }

        public bool GetBool(string key, bool def)
        {
            bool result;
            if (Settings.ContainsKey(key) && bool.TryParse(Settings[key], out result))
            {
                return result;
            }
            return def;
        }
    }
}
