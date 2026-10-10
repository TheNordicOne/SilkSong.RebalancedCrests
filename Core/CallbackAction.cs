using System;
using HutongGames.PlayMaker;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// FSM action that runs a callback when its state is entered. <see cref="Tweak"/> identifies the owner, so a
    /// tweak can check whether it already added its actions.
    /// </summary>
    internal sealed class CallbackAction : FsmStateAction
    {
        private readonly Action<FsmStateAction> _onEnter;

        internal CallbackAction(Tweak tweak, Action<FsmStateAction> onEnter)
        {
            Tweak = tweak;
            _onEnter = onEnter;
        }

        internal Tweak Tweak { get; }

        public override void OnEnter()
        {
            _onEnter(this);
            Finish();
        }
    }
}
