using System.Linq;
using BepInEx.Configuration;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Beast
{
    /// <summary>
    /// Gives the Beast crest (Warrior) a blue tool slot. ToolCrest.slots is replaced by a modified copy;
    /// InventoryToolCrest spawns its slot objects from that array (per type, positioned by SlotInfo.Position, linked
    /// by the Nav indices).
    /// - Replace yellow slot: the left yellow slot (index 3) becomes blue and keeps its vanilla lock.
    /// - Above Up slot: an unlocked slot is appended (index 5, so existing slot indices and their save data stay the
    ///   same) directly above the Up red slot, which navigates up to it.
    /// A saved tool that no longer fits its slot, or whose slot is gone, would stay equipped
    /// (GetEquippedToolsForCrest reads every saved slot), so it is unequipped.
    /// </summary>
    internal sealed class ExtraBlueSlotTweak : Tweak
    {
        private const string CrestId = "Warrior";
        private const int UpSlot = 1;
        private const int NeutralSlot = 0;
        private const int LeftYellowSlot = 3;

        internal enum SlotPlacement
        {
            ReplaceYellowSlot,
            AboveUpSlot,
        }

        internal ExtraBlueSlotTweak(ConfigFile config)
            : base(config, Crest.Beast, "Tool slots - Extra blue slot",
                "Gives the Beast crest a blue tool slot (see 'Tool slots - Extra blue slot placement'). "
                + "Vanilla: no blue slot.")
        {
            Placement = config.Bind(Crest.Name, "Tool slots - Extra blue slot placement", SlotPlacement.ReplaceYellowSlot,
                "ReplaceYellowSlot: the left yellow slot becomes blue (it still has to be unlocked). "
                + "AboveUpSlot: a new, unlocked slot above the Up tool slot.");
        }

        private ConfigEntry<SlotPlacement> Placement { get; }

        internal override void Apply(HeroController hero)
        {
            var crest = ToolItemManager.GetCrestByName(CrestId);
            if (!crest)
            {
                return;
            }

            var slots = Override(crest, nameof(crest.slots), () => crest.slots, value => crest.slots = value);
            slots.Apply(IsEnabled, Placement.Value == SlotPlacement.AboveUpSlot
                ? WithExtraSlot(slots.Vanilla)
                : WithYellowReplaced(slots.Vanilla));
            UnequipMismatchedTools(crest.slots);
        }

        private static ToolCrest.SlotInfo[] WithYellowReplaced(ToolCrest.SlotInfo[] vanilla)
        {
            var slots = vanilla.ToArray();
            slots[LeftYellowSlot].Type = ToolItemType.Blue;
            return slots;
        }

        private static ToolCrest.SlotInfo[] WithExtraSlot(ToolCrest.SlotInfo[] vanilla)
        {
            var slots = vanilla.ToArray();
            var up = slots[UpSlot];
            var extraIndex = slots.Length;
            var spacing = up.Position.y - slots[NeutralSlot].Position.y;
            var extra = new ToolCrest.SlotInfo
            {
                Position = new Vector2(up.Position.x, up.Position.y + spacing),
                Type = ToolItemType.Blue,
                AttackBinding = AttackToolBinding.Down,
                NavUpIndex = -1,
                NavDownIndex = UpSlot,
                NavLeftIndex = up.NavLeftIndex,
                NavRightIndex = up.NavRightIndex,
                NavUpFallbackIndex = -1,
                NavDownFallbackIndex = -1,
                NavLeftFallbackIndex = -1,
                NavRightFallbackIndex = -1,
            };
            slots[UpSlot].NavUpIndex = extraIndex;
            return slots.Append(extra).ToArray();
        }

        private void UnequipMismatchedTools(ToolCrest.SlotInfo[] slots)
        {
            var equips = PlayerData.instance?.ToolEquips.GetData(CrestId).Slots;
            if (equips == null)
            {
                return;
            }

            var names = equips.Select(slot => slot.EquippedTool).ToList();
            var changed = false;
            for (var i = 0; i < names.Count; i++)
            {
                if (string.IsNullOrEmpty(names[i]))
                {
                    continue;
                }

                var tool = ToolItemManager.GetToolByName(names[i]);
                if (tool && i < slots.Length && tool.Type == slots[i].Type)
                {
                    continue;
                }

                DebugLog.Tweak(this, $"Unequipped {names[i]} from slot {i}");
                names[i] = null;
                changed = true;
            }

            if (changed)
            {
                ToolItemManager.SetEquippedTools(CrestId, names);
                ToolItemManager.SendEquippedChangedEvent();
            }
        }
    }
}
