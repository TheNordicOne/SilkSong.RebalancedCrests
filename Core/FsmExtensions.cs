using System;
using System.Linq;
using HutongGames.PlayMaker;

namespace RebalancedCrests.Core
{
    internal static class FsmExtensions
    {
        internal static PlayMakerFSM GetFsm(this UnityEngine.Component owner, string fsmName)
        {
            return owner.GetComponents<PlayMakerFSM>().FirstOrDefault(fsm => fsm.FsmName == fsmName)
                   ?? throw new InvalidOperationException($"'{owner.name}' has no FSM '{fsmName}'");
        }

        internal static T GetAction<T>(this FsmState state, Func<T, bool> predicate = null) where T : FsmStateAction
        {
            return state.Actions.OfType<T>().FirstOrDefault(predicate ?? (_ => true))
                   ?? throw new InvalidOperationException($"State '{state.Name}' has no matching {typeof(T).Name}");
        }

        internal static FsmState GetState(this PlayMakerFSM fsm, string stateName)
        {
            return fsm.Fsm.States.FirstOrDefault(state => state.Name == stateName)
                   ?? throw new InvalidOperationException($"FSM '{fsm.FsmName}' has no state '{stateName}'");
        }

        internal static bool HasAction<T>(this FsmState state, Func<T, bool> predicate) where T : FsmStateAction
        {
            return state.Actions.OfType<T>().Any(predicate);
        }

        /// <summary>
        /// Appends an action to the state. The FSM must already be initialised (true from HeroController.Start on).
        /// </summary>
        internal static void AddAction(this FsmState state, FsmStateAction action)
        {
            action.Init(state);
            state.Actions = state.Actions.Append(action).ToArray();
        }

        /// <summary>
        /// Inserts an action in front of the given action of the state.
        /// </summary>
        internal static void InsertActionBefore(this FsmState state, FsmStateAction before, FsmStateAction action)
        {
            var actions = state.Actions.ToList();
            var index = actions.IndexOf(before);
            if (index < 0)
            {
                throw new InvalidOperationException($"State '{state.Name}' does not contain {before.GetType().Name}");
            }

            action.Init(state);
            actions.Insert(index, action);
            state.Actions = actions.ToArray();
        }
    }
}
