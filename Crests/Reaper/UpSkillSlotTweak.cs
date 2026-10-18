using System.Linq;
using BepInEx.Configuration;
using RebalancedCrests.Core;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Turns Reaper's Up-bound red tool slot (slot 1) into a silk skill slot by changing ToolCrest.SlotInfo.Type on the
    /// crest asset. The game picks attack tools and silk skills alike by the slot's AttackBinding
    /// (ToolItemManager.GetBoundAttackTool), as with Shaman's Up/Down skill slots, so Up + skill uses the skill there.
    /// A tool in that slot that no longer fits its type is unequipped (ToolItemManager.SetEquippedTools).
    /// </summary>
    internal sealed class UpSkillSlotTweak : Tweak
    {
        private const string CrestId = "Reaper";
        private const int UpSlot = 1;

        internal UpSkillSlotTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Tool slots - Up slot holds a silk skill",
                "Reaper's Up tool slot holds a silk skill instead of a red tool; Up + skill uses it. "
                + "Vanilla: a red tool slot.")
        {
        }

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
            UnequipMismatchedTool(slots[UpSlot].Type);
        }

        private void UnequipMismatchedTool(ToolItemType slotType)
        {
            var equips = PlayerData.instance?.ToolEquips.GetData(CrestId).Slots;
            if (equips == null || equips.Count <= UpSlot)
            {
                return;
            }

            var toolName = equips[UpSlot].EquippedTool;
            var tool = string.IsNullOrEmpty(toolName) ? null : ToolItemManager.GetToolByName(toolName);
            if (!tool || tool.Type == slotType)
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
