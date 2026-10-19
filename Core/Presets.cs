using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;

namespace RebalancedCrests.Core
{
    internal enum Preset
    {
        Default,
        Custom,
    }

    /// <summary>
    /// 'Mod - Preset' in General. A preset sets every tweak setting: its listed values, everything else to default.
    /// - Changing a setting by hand selects the preset whose values match, otherwise Custom.
    /// - Leaving Custom saves the current values to a separate file; selecting Custom restores them.
    /// - General and Debug settings are not part of presets.
    /// On startup, a preset whose values no longer match (e.g. the config file was edited) becomes Custom.
    /// </summary>
    internal sealed class Presets
    {
        private static readonly string[] UntrackedSections = { "General", "Debug" };

        /// <summary>
        /// Values per preset, keyed by "Section/Key"; settings that are not listed use their default.
        /// </summary>
        private static readonly Dictionary<Preset, Dictionary<string, object>> Definitions =
            new Dictionary<Preset, Dictionary<string, object>>
            {
                { Preset.Default, new Dictionary<string, object>() },
            };

        private readonly ConfigFile _config;
        private readonly ConfigEntry<Preset> _preset;
        private readonly string _customPath;
        private Preset _current;
        private bool _applying;

        internal Presets(ConfigFile config)
        {
            _config = config;
            _preset = config.Bind("General", "Mod - Preset", Preset.Default,
                "Default: the recommended values. Custom: your own values; changing any setting switches to Custom, "
                + "and switching back to Custom restores them.");
            _customPath = Path.ChangeExtension(config.ConfigFilePath, "custom.txt");
        }

        /// <summary>
        /// Starts tracking; call after every setting is bound.
        /// </summary>
        internal void Start()
        {
            _current = _preset.Value;
            if (_current != Preset.Custom && !Matches(_current))
            {
                SetPreset(Preset.Custom);
            }

            _config.SettingChanged += OnSettingChanged;
        }

        private IEnumerable<ConfigEntryBase> Tracked =>
            _config.Select(pair => pair.Value).Where(entry => !UntrackedSections.Contains(entry.Definition.Section));

        private void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (_applying)
            {
                return;
            }

            if (args.ChangedSetting == _preset)
            {
                Switch(_current, _preset.Value);
            }
            else if (!UntrackedSections.Contains(args.ChangedSetting.Definition.Section))
            {
                SetPreset(Definitions.Keys.Where(Matches).DefaultIfEmpty(Preset.Custom).First());
            }
        }

        private void Switch(Preset from, Preset to)
        {
            _current = to;
            if (from == to)
            {
                return;
            }

            if (from == Preset.Custom)
            {
                SaveCustom();
            }

            Apply(entry =>
            {
                if (to != Preset.Custom)
                {
                    entry.BoxedValue = Value(to, entry);
                }
            });
            if (to == Preset.Custom)
            {
                LoadCustom();
            }
        }

        private bool Matches(Preset preset)
        {
            return Definitions.ContainsKey(preset) && Tracked.All(entry => Equals(entry.BoxedValue, Value(preset, entry)));
        }

        private static object Value(Preset preset, ConfigEntryBase entry)
        {
            return Definitions[preset].TryGetValue(Id(entry.Definition), out var value) ? value : entry.DefaultValue;
        }

        private void SetPreset(Preset preset)
        {
            _current = preset;
            _applying = true;
            _preset.Value = preset;
            _applying = false;
        }

        /// <summary>
        /// Runs <paramref name="set"/> on every tracked setting without treating the changes as hand changes, and
        /// writes the config file once.
        /// </summary>
        private void Apply(Action<ConfigEntryBase> set)
        {
            var save = _config.SaveOnConfigSet;
            _config.SaveOnConfigSet = false;
            _applying = true;
            try
            {
                foreach (var entry in Tracked)
                {
                    set(entry);
                }
            }
            finally
            {
                _applying = false;
                _config.SaveOnConfigSet = save;
                _config.Save();
            }
        }

        private void SaveCustom()
        {
            File.WriteAllLines(_customPath,
                Tracked.Select(entry => $"{Id(entry.Definition)}={entry.GetSerializedValue()}"));
        }

        private void LoadCustom()
        {
            if (!File.Exists(_customPath))
            {
                return;
            }

            var values = File.ReadAllLines(_customPath)
                .Select(line => line.Split(new[] { '=' }, 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1]);
            Apply(entry =>
            {
                if (values.TryGetValue(Id(entry.Definition), out var value))
                {
                    entry.SetSerializedValue(value);
                }
            });
        }

        private static string Id(ConfigDefinition definition)
        {
            return definition.Section + "/" + definition.Key;
        }
    }
}
