using HarmonyLib;
using UnityEngine;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Shows the progress of the Critical Strike harp on the HUD frame (BindOrbHudFrame) with the game's own frames:
    /// stage 0 holds frame 0 of the Wanderer "Full" clip (harp opened, no strings), stage 1 holds frame 1 (strings
    /// appearing). An empty harp shows the vanilla idle frame, and a full harp plays the vanilla Full animation from
    /// WandererCrestRoutine (IsWandererLucky). A stage is only shown while the frame rests on its idle clip or after a
    /// clip has finished, so the appear, change and Full / Full End animations are not interrupted.
    /// </summary>
    [HarmonyPatch]
    internal sealed class HarpHudDisplay : MonoBehaviour
    {
        private BindOrbHudFrame _frame;
        private int _shownStage = -1;

        [HarmonyPatch(typeof(BindOrbHudFrame), "Awake")]
        [HarmonyPostfix]
        private static void Attach(BindOrbHudFrame __instance)
        {
            __instance.gameObject.AddComponent<HarpHudDisplay>()._frame = __instance;
        }

        private void LateUpdate()
        {
            var tweak = CriticalStrikeTweak.Instance;
            var frame = _frame;
            var animator = frame.animator;
            var isWandererFrame = frame.isActive && frame.currentFrameAnims == frame.wandererFrameAnims;
            var stage = tweak != null && tweak.IsActive && isWandererFrame ? tweak.FillingStage : -1;
            var fullClip = frame.animProxy.GetClip(frame.wandererFullAnim);
            if (_shownStage >= 0 && animator.CurrentClip != fullClip)
            {
                // the HUD replaced the shown stage (e.g. it played its appear animation again)
                _shownStage = -1;
            }

            if (stage == _shownStage)
            {
                return;
            }

            var idleClip = frame.animProxy.GetClip(frame.wandererFrameAnims.Idle);
            if (stage >= 0)
            {
                if (fullClip == null || (animator.Playing && animator.CurrentClip != idleClip))
                {
                    return;
                }

                animator.PlayFromFrame(fullClip, stage);
                animator.Stop();
            }
            else if (isWandererFrame && tweak != null && !tweak.HarpFull && animator.CurrentClip == fullClip && !animator.Playing)
            {
                // harp emptied without being full (e.g. setting turned off): back to the vanilla idle frame
                animator.Play(idleClip);
            }

            _shownStage = stage;
        }
    }
}
