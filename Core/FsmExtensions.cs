using System;
using System.Linq;
using HutongGames.PlayMaker;

namespace RebalancedCrests.Core
{
    internal static class FsmExtensions
    {
        internal static FsmState GetState(this PlayMakerFSM fsm, string stateName)
        {
            return fsm.Fsm.States.FirstOrDefault(state => state.Name == stateName)
                   ?? throw new InvalidOperationException($"FSM '{fsm.FsmName}' has no state '{stateName}'");
        }

        internal static bool HasAction<T>(this FsmState state) where T : FsmStateAction
        {
            return state.Actions.OfType<T>().Any();
        }

        /// <summary>
        /// Appends an action to the state. The FSM must already be initialised (true from HeroController.Start on).
        /// </summary>
        internal static void AddAction(this FsmState state, FsmStateAction action)
        {
            action.Init(state);
            state.Actions = state.Actions.Append(action).ToArray();
        }
    }
}
