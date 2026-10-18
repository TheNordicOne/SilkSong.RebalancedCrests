using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Turns Reaper's Up-bound red tool slot (slot 1) into a silk skill slot by changing ToolCrest.SlotInfo.Type on the
    /// crest asset. The game picks attack tools and silk skills alike by the slot's AttackBinding
    /// (ToolItemManager.GetBoundAttackTool), as with Shaman's Up/Down skill slots, so Up + skill uses the skill there.
    /// A tool in that slot that no longer fits its type is unequipped (ToolItemManager.SetEquippedTools).
    /// With 'Swap locket to the Up skill slot', the Memory Locket lock moves from the Down red slot (slot 2) to the Up
    /// slot (SlotInfo.IsLocked), so the number of lockets stays the same. A slot is locked while IsLocked is set and
    /// its save flag (ToolCrestsData.SlotData.IsUnlocked) is not:
    /// - The Down slot's save flag records the locket, as in vanilla: a locket used on the Down slot carries over to
    ///   the Up slot, and an unlock of the Up slot (InventoryToolCrestSlot.SaveData setter) sets it.
    /// - The Up slot's save flag, true in vanilla saves, is set from it on every apply (and back to true when off).
    /// - ToolCrest.Unlock sets the save flags from SlotInfo.IsLocked; for a Reaper crest obtained with the swap, the
    ///   Down slot's flag is cleared afterwards, since no locket was used.
    /// A tool in the Up slot while it is locked is unequipped.
    /// </summary>
    [HarmonyPatch]
    internal sealed class UpSkillSlotTweak : Tweak
    {
        private const string CrestId = "Reaper";
        private const int UpSlot = 1;
        private const int DownSlot = 2;

        private static UpSkillSlotTweak _instance;

        internal UpSkillSlotTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Tool slots - Up slot holds a silk skill",
                "Reaper's Up tool slot holds a silk skill instead of a red tool; Up + skill uses it. "
                + "Vanilla: a red tool slot.")
        {
            SwapLocket = config.Bind(Crest.Name, "Tool slots - Swap locket to the Up skill slot", true,
                "The Up silk skill slot needs the Memory Locket and the Down red tool slot is free instead. A locket "
                + "already used on the Down slot unlocks the Up slot. Vanilla: the Down slot needs the locket.");
            _instance = this;
        }

        private ConfigEntry<bool> SwapLocket { get; }

        private bool Swaps => Enabled.Value && SwapLocket.Value;

        internal override void Apply(HeroController hero)
        {
            var crest = ToolItemManager.GetCrestByName(CrestId);
            if (!crest)
            {
                return;
            }

            var slots = crest.slots;
            var type = Override(crest, "UpSlotType", () => slots[UpSlot].Type, value => slots[UpSlot].Type = value);
            type.Apply(Enabled.Value, ToolItemType.Skill);
            Override(crest, "UpSlotLocked", () => slots[UpSlot].IsLocked, value => slots[UpSlot].IsLocked = value)
                .Apply(Swaps, true);
            Override(crest, "DownSlotLocked", () => slots[DownSlot].IsLocked, value => slots[DownSlot].IsLocked = value)
                .Apply(Swaps, false);
            var upUnlocked = SyncUpUnlocked();
            UnequipMismatchedTool(slots[UpSlot].Type, upUnlocked);
        }

        /// <summary>
        /// Sets the Up slot's save flag (locket used, or vanilla true) and returns it.
        /// </summary>
        private bool SyncUpUnlocked()
        {
            var equips = PlayerData.instance?.ToolEquips.GetData(CrestId).Slots;
            if (equips == null || equips.Count <= DownSlot)
            {
                return true;
            }

            var up = equips[UpSlot];
            up.IsUnlocked = !Swaps || equips[DownSlot].IsUnlocked;
            equips[UpSlot] = up;
            return up.IsUnlocked;
        }

        private static void SetDownUnlocked(bool value)
        {
            var equips = PlayerData.instance.ToolEquips.GetData(CrestId).Slots;
            if (equips == null || equips.Count <= DownSlot)
            {
                return;
            }

            var down = equips[DownSlot];
            down.IsUnlocked = value;
            equips[DownSlot] = down;
        }

        [HarmonyPatch(typeof(InventoryToolCrestSlot), nameof(InventoryToolCrestSlot.SaveData), MethodType.Setter)]
        [HarmonyPostfix]
        private static void RecordLocketUnlock(InventoryToolCrestSlot __instance, ToolCrestsData.SlotData value)
        {
            if (_instance.Swaps && value.IsUnlocked && __instance.SlotIndex == UpSlot && __instance.Crest
                && __instance.Crest.name == CrestId)
            {
                SetDownUnlocked(true);
                DebugLog.Tweak(_instance, "Up skill slot unlocked with a Memory Locket");
            }
        }

        [HarmonyPatch(typeof(ToolCrest), nameof(ToolCrest.Unlock))]
        [HarmonyPrefix]
        private static void BeforeCrestUnlock(ToolCrest __instance, out bool __state)
        {
            __state = __instance.IsUnlocked;
        }

        [HarmonyPatch(typeof(ToolCrest), nameof(ToolCrest.Unlock))]
        [HarmonyPostfix]
        private static void AfterCrestUnlock(ToolCrest __instance, bool __state)
        {
            if (!__state && _instance.Swaps && __instance.name == CrestId)
            {
                SetDownUnlocked(false);
            }
        }

        private void UnequipMismatchedTool(ToolItemType slotType, bool slotUnlocked)
        {
            var equips = PlayerData.instance?.ToolEquips.GetData(CrestId).Slots;
            if (equips == null || equips.Count <= UpSlot)
            {
                return;
            }

            var toolName = equips[UpSlot].EquippedTool;
            var tool = string.IsNullOrEmpty(toolName) ? null : ToolItemManager.GetToolByName(toolName);
            if (!tool || (tool.Type == slotType && slotUnlocked))
            {
                return;
            }

            var names = equips.Select(slot => slot.EquippedTool).ToList();
            names[UpSlot] = null;
            ToolItemManager.SetEquippedTools(CrestId, names);
            ToolItemManager.SendEquippedChangedEvent();
            DebugLog.Tweak(this, $"Unequipped {tool.name} from the Up slot");
        }
    }
}
