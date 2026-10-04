using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RebalancedCrests.Crests.Hunter
{
    /// <summary>
    /// Keeps the Hunter_v2/v3 focus HUD correct when the meter drops without reaching 0 (see <see cref="FocusLossTweak"/>).
    /// Vanilla only ever drops to 0, so BindOrbHudFrame.HunterCrestUpgradedRoutine empties the bar on any drop and never
    /// leaves the second-stage "full" look. This prefix handles partial drops itself and updates the routine's
    /// tracking fields, so the vanilla loop sees no change in that frame.
    /// </summary>
    [HarmonyPatch]
    internal static class FocusHudPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BindOrbHudFrame), "HunterCrestUpgradedRoutine"));
        }

        private static void Prefix(object __instance)
        {
            var routine = Traverse.Create(__instance);
            var previousHits = Hoisted(routine, "previousHits");
            var hero = HeroController.instance;
            if (!previousHits.FieldExists() || hero == null || hero.IsPaused())
            {
                return;
            }

            var hits = hero.HunterUpgState.CurrentMeterHits;
            var previous = previousHits.GetValue<int>();
            if (previous < 0 || hits == 0 || hits >= previous)
            {
                return;
            }

            var frame = routine.Field("<>4__this").GetValue<BindOrbHudFrame>();
            var maxHits = routine.Field("maxHits").GetValue<int>();
            var extraMaxHits = routine.Field("extraMaxHits").GetValue<int>();
            var bar = routine.Field("bar").GetValue<UiProgressBar>();
            var extraBar = routine.Field("extraBar").GetValue<UiProgressBar>();
            var wasFull = Hoisted(routine, "wasFull");
            var wasFullExtra = Hoisted(routine, "wasFullExtra");

            var full = hits >= maxHits;
            var extraHits = hits - maxHits;
            var fullExtra = extraMaxHits > 0 && extraHits >= extraMaxHits;
            if (!full)
            {
                if (wasFull.GetValue<bool>())
                {
                    frame.PlayFrameAnim(routine.Field("idleAnim").GetValue<string>());
                    if (extraBar != null)
                    {
                        extraBar.SetValueInstant(0f);
                    }

                    bar.gameObject.SetActive(true);
                }

                bar.SetValueInstant((float)hits / maxHits);
            }
            else if (!fullExtra && extraBar != null)
            {
                if (wasFullExtra.GetValue<bool>())
                {
                    frame.PlayFrameAnim(routine.Field("fullAnimA").GetValue<string>());
                }

                extraBar.gameObject.SetActive(true);
                extraBar.SetValueInstant((float)extraHits / extraMaxHits);
            }

            previousHits.SetValue(hits);
            wasFull.SetValue(full);
            wasFullExtra.SetValue(fullExtra);
        }

        /// <summary>
        /// Locals of an iterator are hoisted into fields named "&lt;name&gt;5__N".
        /// </summary>
        private static Traverse Hoisted(Traverse routine, string local)
        {
            var name = routine.Fields().FirstOrDefault(field => field.StartsWith($"<{local}>"));
            return name == null ? Traverse.Create(null) : routine.Field(name);
        }
    }
}
