using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ThePrimeGame
{
    /// <summary>Loads display text from data so source code and object names remain in English.</summary>
    public static class GameText
    {
        private static Dictionary<string, string> translations;

        [Serializable]
        private sealed class LocalizationData
        {
            public Entry[] entries;
        }

        [Serializable]
        private sealed class Entry
        {
            public string key;
            public string value;
        }

        /// <summary>Formats a translated message and reports missing keys instead of displaying silent blanks.</summary>
        public static string Get(string key, params object[] arguments)
        {
            if (translations == null) Load();
            if (!translations.TryGetValue(key, out string value))
                throw new KeyNotFoundException("Missing game text key: " + key);
            return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
        }

        /// <summary>Converts Unity's serializable entry array into a lookup shared by all interface components.</summary>
        private static void Load()
        {
            var asset = Resources.Load<TextAsset>("Localization/Spanish");
            if (asset == null) throw new InvalidOperationException("The game localization resource is missing.");
            var data = JsonUtility.FromJson<LocalizationData>(asset.text);
            translations = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in data.entries)
            {
                // Duplicate or empty entries are authoring errors and must not hide required instructions.
                if (string.IsNullOrWhiteSpace(entry.key) || string.IsNullOrWhiteSpace(entry.value))
                    throw new InvalidOperationException("A game text entry is empty.");
                translations.Add(entry.key, entry.value);
            }
        }
    }
}
