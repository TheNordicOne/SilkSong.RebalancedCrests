using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Resets the selected ModMenu setting row to its default: R or Backspace on the keyboard, or the game's
    /// "Menu Extra" action on a controller (HeroActions.MenuExtra, bound to InputControlType.Action3: X / Square).
    /// The selected row is EventSystem.current.currentSelectedGameObject, which is the row element's Selectable.
    /// </summary>
    internal sealed class SettingResetKey : MonoBehaviour
    {
        private readonly Dictionary<GameObject, ConfigEntryBase> _rows = new Dictionary<GameObject, ConfigEntryBase>();

        internal const string Hint = "R / Backspace or X / Square resets the selected setting.";

        internal void Register(GameObject row, ConfigEntryBase entry)
        {
            _rows[row] = entry;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.R) && !Input.GetKeyDown(KeyCode.Backspace) && !MenuExtraPressed())
            {
                return;
            }

            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected && _rows.TryGetValue(selected, out var entry))
            {
                entry.BoxedValue = entry.DefaultValue;
            }
        }

        private static bool MenuExtraPressed()
        {
            var input = InputHandler.UnsafeInstance;
            return input && input.inputActions != null && input.inputActions.MenuExtra.WasPressed;
        }
    }
}
