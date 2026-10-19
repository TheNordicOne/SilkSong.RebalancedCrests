using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using UnityEngine.UI;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Builds the ModMenu pages from the config: the mod page lists the sections (crests), a crest page lists its
    /// moves (the "&lt;Move&gt;" of a "&lt;Move&gt; - &lt;Change&gt;" key), and a move page holds its settings, labelled
    /// only with the "&lt;Change&gt;". A crest with a single move shows that move's settings directly; keys without a
    /// move sit on the section page. Descriptions show only their first sentence.
    /// ModMenu labels elements with the key; ChoiceElement and TextInput expose the label and description as public
    /// LabelText and DescriptionText fields. Every setting row is registered with <see cref="SettingResetKey"/>.
    /// </summary>
    internal sealed class ModMenuConfigFactory : ConfigEntryFactory
    {
        private const string MoveSeparator = " - ";
        private const string SentenceEnd = ". ";
        private const int MaxFloatSteps = 30;
        private const string ResetLabel = "Reset to default";

        private readonly SettingResetKey _resetKey;

        internal ModMenuConfigFactory(SettingResetKey resetKey)
        {
            _resetKey = resetKey;
            Generators.Insert(0, GenerateIntStepper);
            Generators.Insert(0, GenerateFloatStepper);
        }

        internal AbstractMenuScreen BuildMenu(string title, ConfigFile config)
        {
            var root = new PaginatedMenuScreenBuilder(title);
            var entries = config.Select(pair => pair.Value).ToList();
            foreach (var section in entries.GroupBy(entry => entry.Definition.Section))
            {
                var screen = BuildSectionScreen(section.Key, section.ToList());
                root.Add(new TextButton(section.Key) { OnSubmit = () => MenuScreenNavigation.Show(screen) });
            }

            return root.Build();
        }

        private AbstractMenuScreen BuildSectionScreen(string section, List<ConfigEntryBase> entries)
        {
            var builder = new PaginatedMenuScreenBuilder(section);
            var moves = entries.GroupBy(entry => Move(entry.Definition.Key)).ToList();
            var singleMove = moves.Count(move => move.Key != null) == 1;
            var direct = new List<ConfigEntryBase>();
            foreach (var move in moves)
            {
                if (move.Key == null || singleMove)
                {
                    builder.AddRange(BuildElements(move));
                    direct.AddRange(move);
                    continue;
                }

                var moveScreen = new PaginatedMenuScreenBuilder(move.Key);
                moveScreen.AddRange(BuildElements(move));
                moveScreen.Add(ResetButton(move.ToList()));
                var screen = moveScreen.Build();
                builder.Add(new TextButton(move.Key) { OnSubmit = () => MenuScreenNavigation.Show(screen) });
            }

            if (direct.Count > 0)
            {
                builder.Add(ResetButton(direct));
            }

            return builder.Build();
        }

        /// <summary>
        /// Resets the settings shown on a page to their defaults; pages that only list sub-pages have none. The
        /// preset is skipped, because resetting it would apply the Default preset to every setting. Saving is
        /// paused so the file is written once; the elements follow through SettingChanged.
        /// </summary>
        private static TextButton ResetButton(List<ConfigEntryBase> entries)
        {
            var config = entries[0].ConfigFile;
            return new TextButton(ResetLabel, SettingResetKey.Hint)
            {
                OnSubmit = () =>
                {
                    var save = config.SaveOnConfigSet;
                    config.SaveOnConfigSet = false;
                    foreach (var entry in entries.Where(entry => entry.SettingType != typeof(Preset)))
                    {
                        entry.BoxedValue = entry.DefaultValue;
                    }

                    config.SaveOnConfigSet = save;
                    config.Save();
                },
            };
        }

        private IEnumerable<MenuElement> BuildElements(IEnumerable<ConfigEntryBase> entries)
        {
            foreach (var entry in entries)
            {
                if (!GenerateMenuElement(entry, out var element))
                {
                    continue;
                }

                SetText(element, "LabelText", Change(entry.Definition.Key));
                SetText(element, "DescriptionText",
                    $"{FirstSentence(entry.Description.Description)} Default: {Display(entry.DefaultValue)}.");
                if (element is SelectableElement selectable)
                {
                    _resetKey.Register(selectable.SelectableComponent.gameObject, entry);
                }

                yield return element;
            }
        }

        private static void SetText(MenuElement element, string field, string text)
        {
            if (element.GetType().GetField(field)?.GetValue(element) is Text label)
            {
                label.text = text;
            }
        }

        /// <summary>
        /// Ranged numbers get the game's left/right option control instead of ModMenu's text field.
        /// </summary>
        private static bool GenerateIntStepper(ConfigEntryBase entry, out MenuElement menuElement)
        {
            menuElement = null;
            if (!(entry is ConfigEntry<int> typed) || !(typed.Description.AcceptableValues is AcceptableValueRange<int> range))
            {
                return false;
            }

            var element = new ChoiceElement<int>(entry.LabelName(),
                new IntRangeChoiceModel(range.MinValue, range.MaxValue, typed.Value), entry.DescriptionLine());
            element.SynchronizeWith(typed);
            menuElement = element;
            return true;
        }

        private static bool GenerateFloatStepper(ConfigEntryBase entry, out MenuElement menuElement)
        {
            menuElement = null;
            if (!(entry is ConfigEntry<float> typed) || !(typed.Description.AcceptableValues is AcceptableValueRange<float> range))
            {
                return false;
            }

            var element = new ChoiceElement<float>(entry.LabelName(),
                new FloatStepChoiceModel(range.MinValue, range.MaxValue, MaxFloatSteps), entry.DescriptionLine());
            element.SynchronizeWith(typed);
            menuElement = element;
            return true;
        }

        private static string Move(string key)
        {
            var separator = key.IndexOf(MoveSeparator, StringComparison.Ordinal);
            return separator > 0 ? key.Substring(0, separator) : null;
        }

        private static string Change(string key)
        {
            var separator = key.IndexOf(MoveSeparator, StringComparison.Ordinal);
            return separator > 0 ? key.Substring(separator + MoveSeparator.Length) : key;
        }

        private static string Display(object value)
        {
            switch (value)
            {
                case bool on:
                    return on ? "On" : "Off";
                case float number:
                    return number.ToString("0.###", CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private static string FirstSentence(string description)
        {
            var end = description.IndexOf(SentenceEnd, StringComparison.Ordinal);
            return end > 0 ? description.Substring(0, end + 1) : description;
        }
    }
}
