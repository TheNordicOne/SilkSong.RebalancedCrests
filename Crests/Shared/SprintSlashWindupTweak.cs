using BepInEx.Configuration;
using HutongGames.PlayMaker;
using RebalancedCrests.Core;
using UnityEngine;

namespace RebalancedCrests.Crests.Shared
{
    /// <summary>
    /// Shortens the windup of a crest's sprint slash. In the Sprint FSM each crest has antic states that wait for an
    /// animation clip (complete or trigger frame) before the hitbox is enabled. An action appended to those states
    /// leaves them early. Some states are shared between crests (e.g. "Attack Antic" for Hunter, Witch and
    /// Cloakless), so the action only acts while this tweak's crest is equipped.
    /// </summary>
    internal sealed class SprintSlashWindupTweak : Tweak
    {
        private readonly string[] _anticStates;
        private readonly int _strikeFrame;

        /// <param name="vanillaSeconds">Vanilla windup, used for the description and the upper limit.</param>
        /// <param name="strikeFrame">
        /// For crests whose following states keep playing the antic clip: the clip frame where the strike starts. The
        /// hero animation jumps there when the windup is skipped, so the animation matches the hit. -1 if the following
        /// states play their own clip.
        /// </param>
        /// <param name="anticStates">Sprint FSM states that hold the windup.</param>
        internal SprintSlashWindupTweak(ConfigFile config, Crest crest, float vanillaSeconds, int strikeFrame, params string[] anticStates)
            : base(config, crest, "Run-slash - Shorter windup",
                $"Change the windup of the Run-slash before it can hit. Vanilla: {vanillaSeconds:0.####} seconds.")
        {
            _anticStates = anticStates;
            _strikeFrame = strikeFrame;
            WindupSeconds = config.Bind(crest.Name, "Run-slash - Windup seconds", 0f,
                new ConfigDescription(
                    "Windup of the Run-slash in seconds. 0 = it hits the moment you press attack. "
                    + $"Values of {vanillaSeconds:0.####} or more behave like vanilla.",
                    new AcceptableValueRange<float>(0f, vanillaSeconds)));
        }

        private ConfigEntry<float> WindupSeconds { get; }

        internal override void Apply(HeroController hero)
        {
            foreach (var stateName in _anticStates)
            {
                var state = hero.sprintFSM.GetState(stateName);
                if (!state.HasAction<WindupAction>(action => action.Tweak == this))
                {
                    state.AddAction(new WindupAction(this));
                }
            }
        }

        /// <summary>
        /// Runs after the state's own actions (antic clip, attack speed, deceleration) and leaves the state early.
        /// </summary>
        private sealed class WindupAction : FsmStateAction
        {
            private float _elapsed;

            internal WindupAction(SprintSlashWindupTweak tweak)
            {
                Tweak = tweak;
            }

            internal SprintSlashWindupTweak Tweak { get; }

            public override void OnEnter()
            {
                if (!Tweak.IsActive)
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
                if (_elapsed < Tweak.WindupSeconds.Value)
                {
                    return;
                }

                var state = State.Name;
                Fsm.Event(FsmEvent.Finished);
                if (Tweak._strikeFrame >= 0)
                {
                    var animator = Owner.GetComponent<tk2dSpriteAnimator>();
                    if (animator.CurrentClip != null && animator.CurrentClip.frames.Length > Tweak._strikeFrame
                                                     && animator.CurrentFrame < Tweak._strikeFrame)
                    {
                        animator.PlayFromFrame(Tweak._strikeFrame);
                    }
                }

                Core.DebugLog.Tweak(Tweak, $"Sprint slash windup ended after {_elapsed:0.000}s in state '{state}'");
            }
        }
    }
}
