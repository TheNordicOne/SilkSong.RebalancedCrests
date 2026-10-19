using BepInEx.Configuration;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Reaper
{
    /// <summary>
    /// Harvest Bind: Up + Bind with Reaper is a quick bind that starts Silk Steal (reaper mode) without healing.
    /// It runs through Architect's Craft Bind states of the Bind FSM ("Quick Craft"): the input, Silk check, start
    /// chain, interruption and end sequence stay vanilla. Actions added to those states only act during a Harvest
    /// Bind, so Architect's Craft Bind is unchanged (the two crests cannot be equipped together):
    /// - Can Bind?: the Silk cost is lowered when Up is held, before the Silk check
    /// - Quick Craft?: Reaper + Up goes to Quick Craft Start, and the heal amount is set to 0
    /// - Quick Craft Start: Warding Bell is shown, as Bind Bell? does for a normal bind
    /// - QC Start Ground/Air: Injector Band speeds up the animation by the bind time factor of Quick Bind?
    /// - Crafting Started / Quick Craft Burst: the Architect HUD event and the tool refill are skipped, and
    ///   BindCompleted() starts Silk Steal at the moment tools would be refilled
    /// </summary>
    internal sealed class HarvestBindTweak : Tweak
    {
        private bool _harvesting;

        internal HarvestBindTweak(ConfigFile config)
            : base(config, Crest.Reaper, "Bind - Harvest Bind on Up",
                "Up + Bind does a Harvest Bind: a quick bind that starts Silk Steal without healing. "
                + "It works like Architect's Craft Bind. Warding Bell and Injector Band apply. Vanilla: Up + Bind is a normal bind.")
        {
            SilkCost = config.Bind(Crest.Name, "Bind - Harvest Bind Silk cost", 4,
                new ConfigDescription("Silk spent on a Harvest Bind. Vanilla bind: 9.",
                    new AcceptableValueRange<int>(0, 9)));
        }

        private ConfigEntry<int> SilkCost { get; }

        internal override void Apply(HeroController hero)
        {
            var fsm = hero.GetFsm("Bind");
            var canBind = fsm.GetState("Can Bind?");
            if (canBind.HasAction<CallbackAction>(action => action.Tweak == this))
            {
                return;
            }

            var variables = fsm.FsmVariables;
            var bellCheck = fsm.GetState("Bind Bell?").GetAction<CheckIfToolEquipped>();
            var quickBind = fsm.GetState("Quick Bind?");
            var quickBindCheck = quickBind.GetAction<CheckIfToolEquipped>();
            var quickBindFactor = quickBind.GetAction<FloatMultiply>().multiplyBy.Value;

            canBind.InsertActionBefore(canBind.GetAction<PlayerdataIntCompare>(), new CallbackAction(this, _ =>
            {
                if (IsActive && hero.inputHandler.inputActions.Up.IsPressed && !ToolItemManager.IsInCutscene)
                {
                    variables.GetFsmInt("Current Silk Cost").Value = SilkCost.Value;
                }
            }));

            var quickCraft = fsm.GetState("Quick Craft?");
            quickCraft.InsertActionBefore(quickCraft.GetAction<CheckIfCrestEquipped>(), new CallbackAction(this, action =>
            {
                _harvesting = IsActive && variables.GetFsmBool("Was Up Pressed").Value;
                if (_harvesting)
                {
                    variables.GetFsmInt("Heal Amount").Value = 0;
                    Core.DebugLog.Tweak(this, $"Harvest Bind started, Silk cost {variables.GetFsmInt("Current Silk Cost").Value}");
                    action.Fsm.Event(FsmEvent.GetFsmEvent("TRUE"));
                }
            }));

            fsm.GetState("Quick Craft Start").AddAction(new CallbackAction(this, _ =>
            {
                if (_harvesting && bellCheck.IsTrue)
                {
                    var bell = variables.GetFsmGameObject("Bind Bell").Value;
                    bell.SetActive(false);
                    bell.SetActive(true);
                }
            }));

            foreach (var stateName in new[] { "QC Start Ground", "QC Start Air" })
            {
                fsm.GetState(stateName).AddAction(new CallbackAction(this, action =>
                {
                    if (_harvesting && quickBindCheck.IsTrue)
                    {
                        var animator = action.Owner.GetComponent<tk2dSpriteAnimator>();
                        animator.ClipFps = animator.CurrentClip.fps / quickBindFactor;
                    }
                }));
            }

            var craftingStarted = fsm.GetState("Crafting Started");
            var quickCraftingEvent = craftingStarted.GetAction<SendEventToRegister>();
            craftingStarted.InsertActionBefore(craftingStarted.Actions[0],
                new CallbackAction(this, _ => quickCraftingEvent.Enabled = !_harvesting));

            var burst = fsm.GetState("Quick Craft Burst");
            var replenish = burst.GetAction<TryReplenishTools>();
            burst.InsertActionBefore(replenish, new CallbackAction(this, _ =>
            {
                replenish.Enabled = !_harvesting;
                if (_harvesting)
                {
                    hero.BindCompleted();
                    Core.DebugLog.Tweak(this, "Harvest Bind completed, Silk Steal started");
                }
            }));
        }
    }
}
