using System.Linq;
using BepInEx.Configuration;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Beast
{
    /// <summary>
    /// Adds an unlocked blue tool slot to the Beast crest (Warrior), directly above the Up red slot. ToolCrest.slots is
    /// replaced by a copy with the new slot appended (index 5), so existing slot indices and their save data stay the
    /// same. InventoryToolCrest spawns its slot objects from that array (per type, positioned by SlotInfo.Position,
    /// linked by the Nav indices). The Up slot navigates up to the new slot.
    /// Without the slot, a tool still saved in it would stay equipped (GetEquippedToolsForCrest reads every saved
    /// slot), so it is unequipped when the tweak is turned off.
    /// </summary>
    internal sealed class ExtraBlueSlotTweak : Tweak
    {
        private const string CrestId = "Warrior";
        private const int UpSlot = 1;
        private const int NeutralSlot = 0;

        internal ExtraBlueSlotTweak(ConfigFile config)
            : base(config, Crest.Beast, "Tool slots - Extra blue slot",
                "Adds a blue tool slot above the Up tool slot. Vanilla: no slot there.")
        {
        }

        internal override void Apply(HeroController hero)
        {
            var crest = ToolItemManager.GetCrestByName(CrestId);
            if (!crest)
            {
                return;
            }

            var slots = Override(crest, nameof(crest.slots), () => crest.slots, value => crest.slots = value);
            slots.Apply(Enabled.Value, WithExtraSlot(slots.Vanilla));
            if (!Enabled.Value)
            {
                UnequipExtraSlot(slots.Vanilla.Length);
            }
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

        private void UnequipExtraSlot(int extraIndex)
        {
            var equips = PlayerData.instance?.ToolEquips.GetData(CrestId).Slots;
            if (equips == null || equips.Count <= extraIndex || string.IsNullOrEmpty(equips[extraIndex].EquippedTool))
            {
                return;
            }

            var toolName = equips[extraIndex].EquippedTool;
            var names = equips.Select(slot => slot.EquippedTool).ToList();
            names[extraIndex] = null;
            ToolItemManager.SetEquippedTools(CrestId, names);
            ToolItemManager.SendEquippedChangedEvent();
            DebugLog.Tweak(this, $"Unequipped {toolName} from the extra blue slot");
        }
    }
}
