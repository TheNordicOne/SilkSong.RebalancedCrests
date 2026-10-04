using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace RebalancedCrests.Diagnostics
{
    /// <summary>
    /// Temporary research tool: writes the crest data, hero configs, animation clips and
    /// PlayMaker FSMs of the hero to BepInEx/RebalancedCrests-dump once a save is loaded.
    /// </summary>
    [HarmonyPatch]
    internal static class CrestDump
    {
        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static ManualLogSource _log;
        private static ConfigEntry<bool> _enabled;
        private static bool _dumped;

        private static string DumpDir => Path.Combine(Paths.BepInExRootPath, "RebalancedCrests-dump");

        internal static void Init(ConfigFile config, ManualLogSource log)
        {
            _log = log;
            _enabled = config.Bind("Debug", "DumpCrestData", false,
                "Write crest, hero config and FSM data to BepInEx/RebalancedCrests-dump when a save is loaded.");
        }

        [HarmonyPatch(typeof(HeroController), "Start")]
        [HarmonyPostfix]
        private static void AfterHeroStart(HeroController __instance)
        {
            if (!_enabled.Value || _dumped)
            {
                return;
            }

            _dumped = true;
            __instance.StartCoroutine(DumpDelayed(__instance));
        }

        private static IEnumerator DumpDelayed(HeroController hero)
        {
            // give the FSMs time to initialise
            yield return new WaitForSecondsRealtime(3f);
            try
            {
                Directory.CreateDirectory(DumpDir);
                File.WriteAllText(Path.Combine(DumpDir, "crests.txt"), DumpCrests());
                File.WriteAllText(Path.Combine(DumpDir, "hero-configs.txt"), DumpHeroConfigs(hero));
                File.WriteAllText(Path.Combine(DumpDir, "hero-animations.txt"), DumpClips(hero.GetComponent<tk2dSpriteAnimator>()?.Library));
                File.WriteAllText(Path.Combine(DumpDir, "hero-controller.txt"), DumpHeroController(hero));
                File.WriteAllText(Path.Combine(DumpDir, "gameplay-settings.txt"), DumpObject(Gameplay.Get()));
                File.WriteAllText(Path.Combine(DumpDir, "tools.txt"), DumpTools());
                DumpFsms(hero);
                _log.LogInfo($"Crest data dumped to {DumpDir}");
            }
            catch (Exception e)
            {
                _log.LogError($"Crest dump failed: {e}");
            }
        }

        private static string DumpCrests()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== Gameplay hunter settings ==");
            sb.AppendLine($"HunterComboHits={Gameplay.HunterComboHits} HunterComboDamageMult={Gameplay.HunterComboDamageMult}");
            sb.AppendLine($"HunterCombo2Hits={Gameplay.HunterCombo2Hits} HunterCombo2DamageMult={Gameplay.HunterCombo2DamageMult}");
            sb.AppendLine($"HunterCombo2ExtraHits={Gameplay.HunterCombo2ExtraHits} HunterCombo2ExtraDamageMult={Gameplay.HunterCombo2ExtraDamageMult}");
            sb.AppendLine();

            foreach (var crest in ToolItemManager.GetAllCrests())
            {
                sb.AppendLine($"== Crest {crest.name} (hidden={crest.IsHidden}, previous={Name(crest.previousVersion)}) ==");
                for (var i = 0; i < crest.Slots.Length; i++)
                {
                    var slot = crest.Slots[i];
                    sb.AppendLine($"  slot[{i}] type={slot.Type} locked={slot.IsLocked} binding={slot.AttackBinding}");
                }

                var config = crest.HeroConfig;
                sb.AppendLine($"  heroConfig={Name(config)} ({config?.GetType().Name}, instanceId={config?.GetInstanceID()})");
                if (config != null)
                {
                    AppendFields(sb, config, "    ");
                    sb.Append(DumpClips(config.heroAnimOverrideLib, "    "));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string DumpHeroConfigs(HeroController hero)
        {
            var sb = new StringBuilder();
            AppendConfigGroups(sb, "configs", hero.configs);
            AppendConfigGroups(sb, "specialConfigs", hero.specialConfigs);
            return sb.ToString();
        }

        private static void AppendConfigGroups(StringBuilder sb, string label, HeroController.ConfigGroup[] groups)
        {
            if (groups == null)
            {
                return;
            }

            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                sb.AppendLine($"== {label}[{i}] config={Name(group.Config)} (instanceId={group.Config?.GetInstanceID()}) activeRoot={Name(group.ActiveRoot)} ==");
                if (group.Config != null)
                {
                    sb.AppendLine($"  config values ({group.Config.GetType().Name}):");
                    AppendFields(sb, group.Config, "    ");
                    sb.Append(DumpClips(group.Config.heroAnimOverrideLib, "    "));
                }
                var objects = new (string, GameObject)[]
                {
                    ("NormalSlash", group.NormalSlashObject), ("AlternateSlash", group.AlternateSlashObject),
                    ("UpSlash", group.UpSlashObject), ("AltUpSlash", group.AltUpSlashObject),
                    ("DownSlash", group.DownSlashObject), ("AltDownSlash", group.AltDownSlashObject),
                    ("WallSlash", group.WallSlashObject), ("DashStab", group.DashStab),
                    ("DashStabAlt", group.DashStabAlt), ("ChargeSlash", group.ChargeSlash),
                    ("TauntSlash", group.TauntSlash),
                };
                foreach (var (slotName, go) in objects)
                {
                    sb.AppendLine($"  {slotName}: {Name(go)}");
                    if (go != null)
                    {
                        AppendHierarchy(sb, go.transform, "    ");
                    }
                }

                sb.AppendLine();
            }
        }

        private static void AppendHierarchy(StringBuilder sb, Transform t, string indent)
        {
            var components = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).ToArray();
            sb.AppendLine($"{indent}{t.name} (activeSelf={t.gameObject.activeSelf}, localPos={t.localPosition}, localScale={t.localScale}, localRot={t.localEulerAngles}) [{string.Join(", ", components.Select(c => c.GetType().Name))}]");
            foreach (var component in components)
            {
                AppendComponent(sb, component, indent + "  | ");
            }

            foreach (Transform child in t)
            {
                AppendHierarchy(sb, child, indent + "  ");
            }
        }

        private static string DumpClips(tk2dSpriteAnimation library, string indent = "")
        {
            var sb = new StringBuilder();
            if (library?.clips == null)
            {
                return sb.ToString();
            }

            sb.AppendLine($"{indent}animation library {library.name}:");
            foreach (var clip in library.clips.Where(c => c != null && !string.IsNullOrEmpty(c.name)))
            {
                var triggers = clip.frames == null
                    ? ""
                    : string.Join(" ", clip.frames
                        .Select((f, idx) => (f, idx))
                        .Where(x => x.f.triggerEvent)
                        .Select(x => $"{x.idx}:'{x.f.eventInfo}'"));
                sb.AppendLine($"{indent}  {clip.name}: fps={clip.fps} frames={clip.frames?.Length} wrap={clip.wrapMode} loopStart={clip.loopStart} triggers=[{triggers}]");
            }

            return sb.ToString();
        }

        private static void DumpFsms(HeroController hero)
        {
            var fsms = new HashSet<PlayMakerFSM>(hero.GetComponentsInChildren<PlayMakerFSM>(true));
            foreach (var extra in new[] { hero.sprintFSM, hero.crestAttacksFSM, hero.toolsFSM, hero.silkSpecialFSM, hero.harpoonDashFSM })
            {
                if (extra != null)
                {
                    fsms.Add(extra);
                }
            }

            foreach (var fsm in fsms)
            {
                var fileName = $"fsm_{fsm.gameObject.name}_{fsm.FsmName}.txt";
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                File.WriteAllText(Path.Combine(DumpDir, fileName), DumpFsm(fsm));
            }
        }

        private static string DumpFsm(PlayMakerFSM fsm)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"FSM '{fsm.FsmName}' on {fsm.gameObject.name}, start state '{fsm.Fsm.StartState}'");
            sb.AppendLine("Variables:");
            foreach (var variable in fsm.FsmVariables.GetAllNamedVariablesSorted())
            {
                sb.AppendLine($"  {variable.VariableType} {variable.Name} = {Format(variable.RawValue)}");
            }

            sb.AppendLine("Global transitions:");
            foreach (var transition in fsm.Fsm.GlobalTransitions)
            {
                sb.AppendLine($"  {transition.EventName} -> {transition.ToState}");
            }

            foreach (var state in fsm.Fsm.States)
            {
                sb.AppendLine();
                sb.AppendLine($"State '{state.Name}'");
                foreach (var transition in state.Transitions)
                {
                    sb.AppendLine($"  on {transition.EventName} -> {transition.ToState}");
                }

                var actions = state.Actions;
                for (var i = 0; i < actions.Length; i++)
                {
                    var action = actions[i];
                    sb.AppendLine($"  [{i}] {action.GetType().Name}{(action.Enabled ? "" : " (disabled)")}");
                    foreach (var field in action.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                    {
                        sb.AppendLine($"      {field.Name} = {FormatFsmValue(field.GetValue(action))}");
                    }
                }
            }

            return sb.ToString();
        }


        private static void AppendComponent(StringBuilder sb, Component component, string indent)
        {
            switch (component)
            {
                case MeshFilter _:
                case MeshRenderer _:
                case ParticleSystemRenderer _:
                case PlayMakerFSM _:
                    return;
                case ParticleSystem ps:
                    sb.AppendLine($"{indent}ParticleSystem duration={ps.main.duration}");
                    return;
                case PolygonCollider2D poly:
                    sb.AppendLine($"{indent}PolygonCollider2D enabled={poly.enabled} trigger={poly.isTrigger} offset={poly.offset} points=[{string.Join(" ", poly.points.Select(p => $"({p.x:0.###},{p.y:0.###})"))}]");
                    return;
                case BoxCollider2D box:
                    sb.AppendLine($"{indent}BoxCollider2D enabled={box.enabled} trigger={box.isTrigger} offset={box.offset} size={box.size}");
                    return;
                case Animator animator:
                    sb.AppendLine($"{indent}Animator enabled={animator.enabled} speed={animator.speed} controller={Name(animator.runtimeAnimatorController)}");
                    if (animator.runtimeAnimatorController != null)
                    {
                        foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        {
                            var events = string.Join(" ", clip.events.Select(e =>
                                $"{e.time:0.###}s:{e.functionName}(f={e.floatParameter},i={e.intParameter},s='{e.stringParameter}',o={Name(e.objectReferenceParameter)})"));
                            sb.AppendLine($"{indent}  clip {clip.name}: length={clip.length:0.###}s fps={clip.frameRate} loop={clip.isLooping} events=[{events}]");
                        }
                    }

                    return;
                case Behaviour behaviour:
                    sb.AppendLine($"{indent}{component.GetType().Name} enabled={behaviour.enabled}");
                    AppendFields(sb, component, indent + "  ", typeof(MonoBehaviour));
                    return;
                default:
                    sb.AppendLine($"{indent}{component.GetType().Name}");
                    return;
            }
        }

        private static string DumpHeroController(HeroController hero)
        {
            var sb = new StringBuilder();
            sb.AppendLine("== HeroController fields ==");
            AppendFields(sb, hero, "  ", typeof(MonoBehaviour));
            sb.AppendLine();
            sb.AppendLine("== HeroController constants and statics ==");
            foreach (var field in typeof(HeroController).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                sb.AppendLine($"  {field.Name} = {Format(field.GetValue(null))}");
            }

            var animationController = hero.GetComponent<HeroAnimationController>();
            if (animationController != null)
            {
                sb.AppendLine();
                sb.AppendLine("== HeroAnimationController fields ==");
                AppendFields(sb, animationController, "  ", typeof(MonoBehaviour));
            }

            return sb.ToString();
        }

        private static string DumpTools()
        {
            var sb = new StringBuilder();
            foreach (var tool in ManagerSingleton<ToolItemManager>.Instance.toolItems)
            {
                if (tool == null)
                {
                    continue;
                }

                sb.AppendLine($"== Tool {tool.name} ({tool.GetType().Name}) ==");
                AppendFields(sb, tool, "  ");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string DumpObject(UnityEngine.Object obj)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"== {Name(obj)} ({obj?.GetType().Name}) ==");
            if (obj != null)
            {
                AppendFields(sb, obj, "  ");
            }

            return sb.ToString();
        }

        private static void AppendFields(StringBuilder sb, object obj, string indent, Type stopAt = null, int depth = 0)
        {
            stopAt ??= typeof(ScriptableObject);
            for (var type = obj.GetType(); type != null && type != stopAt && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(InstanceFields))
                {
                    object value;
                    try
                    {
                        value = field.GetValue(obj);
                    }
                    catch (Exception e)
                    {
                        value = $"<error {e.GetType().Name}>";
                    }

                    sb.AppendLine($"{indent}{field.Name} = {Format(value)}");
                    if (depth < 2 && IsNestedData(value))
                    {
                        AppendFields(sb, value, indent + "    ", typeof(object), depth + 1);
                    }
                }
            }
        }

        private static bool IsNestedData(object value)
        {
            if (value == null || value is UnityEngine.Object || value is string || value is Array || value is System.Collections.ICollection)
            {
                return false;
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type.Namespace == "UnityEngine" || type.Namespace == "System")
            {
                return false;
            }

            return type.IsSerializable || type.IsValueType;
        }

        private static string FormatFsmValue(object value)
        {
            if (value is NamedVariable variable)
            {
                return variable.UseVariable ? $"<var {variable.Name}>" : Format(variable.RawValue);
            }

            if (value is FsmEvent fsmEvent)
            {
                return $"<event {fsmEvent.Name}>";
            }

            if (value is FsmOwnerDefault owner)
            {
                return owner.OwnerOption == OwnerDefaultOption.UseOwner ? "<owner>" : FormatFsmValue(owner.GameObject);
            }

            if (value is Array array && !(value is string))
            {
                return "[" + string.Join(", ", array.Cast<object>().Select(FormatFsmValue)) + "]";
            }

            return Format(value);
        }

        private static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case UnityEngine.Object unityObject:
                    return unityObject ? $"{unityObject.name} ({unityObject.GetType().Name})" : "null";
                case string s:
                    return $"\"{s}\"";
                case Array array:
                    return $"({array.Length})[" + string.Join(", ", array.Cast<object>().Take(64).Select(Format)) + (array.Length > 64 ? ", ..." : "") + "]";
                case System.Collections.IList list when !(value is string):
                    return $"({list.Count})[" + string.Join(", ", list.Cast<object>().Take(64).Select(Format)) + (list.Count > 64 ? ", ..." : "") + "]";
                default:
                    try
                    {
                        return value.ToString();
                    }
                    catch (Exception e)
                    {
                        return $"<error {e.GetType().Name}>";
                    }
            }
        }

        private static string Name(UnityEngine.Object obj) => obj ? obj.name : "null";
    }
}
