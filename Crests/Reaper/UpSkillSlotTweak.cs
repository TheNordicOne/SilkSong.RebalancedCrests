using System.Collections.Generic;
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
    /// With 'Up skill slot needs a Memory Locket', SlotInfo.IsLocked is set. A slot is locked while IsLocked is set
    /// and its save data (ToolCrestsData.SlotData.IsUnlocked) is not; saves store the vanilla-unlocked slot as
    /// unlocked, so its save flag is driven by a marker: an extra save entry after the crest's slots, whose IsUnlocked
    /// records a Memory Locket unlock of this slot (the InventoryToolCrestSlot.SaveData setter). The game reads save
    /// entries only up to the crest's slot count or treats them as empty slots. Turning the lock off restores the
    /// vanilla unlocked flag; a tool in the slot while it is locked is unequipped.
    /// </summary>
    [HarmonyPatch]
    internal sealed class UpSkillSlotTweak : Tweak
    {
        private const string CrestId = "Reaper";
        private const int UpSlot = 1;

        private static UpSkillSlotTweak _instance;

        internal UpSkillSlotTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Tool slots - Up slot holds a silk skill",
                "Reaper's Up tool slot holds a silk skill instead of a red tool; Up + skill uses it. "
                + "Vanilla: a red tool slot.")
        {
            NeedsLocket = config.Bind(Crest.Name, "Tool slots - Up skill slot needs a Memory Locket", true,
                "The silk skill slot on Up has to be unlocked with a Memory Locket first. Vanilla: the Up slot is "
                + "unlocked from the start.");
            _instance = this;
        }

        private ConfigEntry<bool> NeedsLocket { get; }

        private bool Locks => Enabled.Value && NeedsLocket.Value;

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
            var locked = Override(crest, "UpSlotLocked", () => slots[UpSlot].IsLocked, value => slots[UpSlot].IsLocked = value);
            locked.Apply(Locks, true);
            var unlocked = SyncUnlockFlag(slots.Length);
            UnequipMismatchedTool(slots[UpSlot].Type, unlocked);
        }

        /// <summary>
        /// Sets the save flag of the Up slot from the marker (or back to vanilla) and returns whether it is unlocked.
        /// </summary>
        private bool SyncUnlockFlag(int markerIndex)
        {
            var equips = PlayerData.instance?.ToolEquips.GetData(CrestId).Slots;
            if (equips == null || equips.Count <= UpSlot)
            {
                return true;
            }

            var isUnlocked = true;
            if (Locks)
            {
                while (equips.Count <= markerIndex)
                {
                    equips.Add(default);
                }

                isUnlocked = equips[markerIndex].IsUnlocked;
            }

            var slot = equips[UpSlot];
            slot.IsUnlocked = isUnlocked;
            equips[UpSlot] = slot;
            return isUnlocked;
        }

        [HarmonyPatch(typeof(InventoryToolCrestSlot), nameof(InventoryToolCrestSlot.SaveData), MethodType.Setter)]
        [HarmonyPostfix]
        private static void RecordLocketUnlock(InventoryToolCrestSlot __instance, ToolCrestsData.SlotData value)
        {
            if (!_instance.Locks || !value.IsUnlocked || __instance.SlotIndex != UpSlot
                || !__instance.Crest || __instance.Crest.name != CrestId)
            {
                return;
            }

            var crest = ToolItemManager.GetCrestByName(CrestId);
            var equips = PlayerData.instance.ToolEquips.GetData(CrestId).Slots;
            if (!crest || equips == null || equips.Count <= crest.slots.Length)
            {
                return;
            }

            var marker = equips[crest.slots.Length];
            marker.IsUnlocked = true;
            equips[crest.slots.Length] = marker;
            DebugLog.Tweak(_instance, "Up skill slot unlocked with a Memory Locket");
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
