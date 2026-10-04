using BepInEx.Configuration;
using HutongGames.PlayMaker;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Hunter
{
    /// <summary>
    /// Shortens the windup of Hunter's sprint slash. Vanilla, the Sprint FSM state "Attack Antic" waits for the
    /// "Dash Attack Antic" clip to complete (0.1667 s) before the hitbox is enabled in "Attack Dash Start".
    /// The state is shared with Witch, Cursed and Cloakless, so the injected action only acts for Hunter.
    /// </summary>
    internal sealed class SprintSlashWindupTweak : Tweak
    {
        private const string SprintAnticState = "Attack Antic";

        internal SprintSlashWindupTweak(ConfigFile config)
            : base(config, HunterCrest.Section, "SprintSlashWindup",
                "Change the windup of the sprint slash before its hitbox becomes active. Vanilla: 0.1667 seconds.")
        {
            WindupSeconds = config.Bind(HunterCrest.Section, "SprintSlashWindupSeconds", 0f,
                new ConfigDescription(
                    "Windup of the sprint slash in seconds. 0 = the hitbox is active in the frame the attack is pressed. "
                    + "Values of 0.1667 or more behave like vanilla.",
                    new AcceptableValueRange<float>(0f, 0.1667f)));
        }

        internal ConfigEntry<float> WindupSeconds { get; }

        internal override void Apply(HeroController hero)
        {
            var state = hero.sprintFSM.GetState(SprintAnticState);
            if (!state.HasAction<WindupAction>())
            {
                state.AddAction(new WindupAction(this));
            }
        }

        /// <summary>
        /// Runs after the state's own actions (antic clip, attack speed, deceleration) and leaves the state early.
        /// </summary>
        private sealed class WindupAction : FsmStateAction
        {
            private readonly SprintSlashWindupTweak _tweak;
            private float _elapsed;

            internal WindupAction(SprintSlashWindupTweak tweak)
            {
                _tweak = tweak;
            }

            public override void OnEnter()
            {
                if (!_tweak.Enabled.Value || !HeroController.instance.IsHunterCrestEquipped())
                {
                    Finish();
                    return;
                }

                _elapsed = 0f;
                CheckWindup();
            }

            public override void OnUpdate()
            {
                _elapsed += Time.deltaTime;
                CheckWindup();
            }

            private void CheckWindup()
            {
                if (_elapsed >= _tweak.WindupSeconds.Value)
                {
                    Fsm.Event(FsmEvent.Finished);
                }
            }
        }
    }
}
