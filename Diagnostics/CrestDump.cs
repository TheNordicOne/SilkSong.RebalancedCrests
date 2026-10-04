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
using Object = UnityEngine.Object;

namespace RebalancedCrests.Diagnostics
{
    /// <summary>
    /// Research tool: once a save is loaded, writes everything about the hero, its crests, attacks, FSMs,
    /// animations and every object/asset they reference to BepInEx/RebalancedCrests-dump.
    /// </summary>
    [HarmonyPatch]
    internal static class CrestDump
    {
        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const int MaxReferencedObjects = 2000;
        private const int MaxReferencedTransforms = 200;
        private const int MaxFormatDepth = 4;
        private const float SampleStep = 1f / 60f;

        // irrelevant to gameplay mechanics; they used up the reference cap
        private static readonly HashSet<string> IgnoredAssetTypes = new HashSet<string>
        {
            "VibrationDataAsset", "PS5VibrationData", "GamepadVibration", "RandomAudioClipTable", "MusicCue",
            "AchievementsList", "Quest", "QuestBoardList", "QuestTargetPlayerDataBools", "ShopItemList",
            "CollectableItemBasic", "CollectableItemMementoList",
        };

        private static ManualLogSource _log;
        private static ConfigEntry<bool> _enabled;
        private static bool _dumped;

        private static Transform _heroRoot;
        private static readonly HashSet<Object> Tracked = new HashSet<Object>();
        private static readonly Queue<Object> Pending = new Queue<Object>();
        private static readonly HashSet<tk2dSpriteAnimation> Libraries = new HashSet<tk2dSpriteAnimation>();
        private static readonly HashSet<Animator> Animators = new HashSet<Animator>();
        private static readonly HashSet<Type> ComponentTypes = new HashSet<Type>();
        private static readonly HashSet<PlayMakerFSM> Fsms = new HashSet<PlayMakerFSM>();

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
            _heroRoot = hero.transform;
            if (Directory.Exists(DumpDir))
            {
                Directory.Delete(DumpDir, true);
            }

            Directory.CreateDirectory(DumpDir);
            Step("crests.txt", DumpCrests);
            Step("hero-configs.txt", () => DumpHeroConfigs(hero));
            Step("hero-hierarchy.txt", () => DumpHierarchy(hero.transform));
            Step("hero-controller.txt", () => DumpHeroController(hero));
            Step("input-handler.txt", DumpInputHandler);
            Step("global-settings.txt", DumpGlobalSettings);
            Step("tools.txt", DumpTools);
            Step("engine-settings.txt", DumpEngineSettings);
            foreach (var fsm in hero.GetComponentsInChildren<PlayMakerFSM>(true))
            {
                Fsms.Add(fsm);
            }

            Step("referenced-objects.txt", DumpReferencedObjects);
            Step("fsm-files", DumpFsms);
            Step("statics.txt", DumpStatics);
            Step("animations-tk2d.txt", DumpLibraries);
            Step("animator-timelines.txt", DumpAnimatorTimelines);
            _log.LogInfo($"Crest data dumped to {DumpDir}");
        }

        private static void Step(string name, Func<string> dump)
        {
            try
            {
                var content = dump();
                if (content != null)
                {
                    File.WriteAllText(Path.Combine(DumpDir, name), content);
                }
            }
            catch (Exception e)
            {
                _log.LogError($"Crest dump step {name} failed: {e}");
            }
        }

        // ---------------------------------------------------------------- crests and configs

        private static string DumpCrests()
        {
            var sb = new StringBuilder();
            foreach (var crest in ToolItemManager.GetAllCrests())
            {
                sb.AppendLine($"== Crest {crest.name} (hidden={crest.IsHidden}, previous={Name(crest.previousVersion)}) ==");
                for (var i = 0; i < crest.Slots.Length; i++)
                {
                    var slot = crest.Slots[i];
                    sb.AppendLine($"  slot[{i}] type={slot.Type} locked={slot.IsLocked} binding={slot.AttackBinding} pos={slot.Position} nav(u/d/l/r)={slot.NavUpIndex}/{slot.NavDownIndex}/{slot.NavLeftIndex}/{slot.NavRightIndex}");
                }

                sb.AppendLine("  fields:");
                AppendFields(sb, crest, "    ");
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
                sb.AppendLine($"== {label}[{i}] config={Name(group.Config)} (instanceId={group.Config?.GetInstanceID()}) activeRoot={PathOf(group.ActiveRoot)} ==");
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
                    sb.AppendLine($"  {slotName}: {PathOf(go)}");
                    if (go != null)
                    {
                        AppendHierarchy(sb, go.transform, "    ", int.MaxValue);
                    }
                }

                sb.AppendLine();
            }
        }

        // ---------------------------------------------------------------- hierarchy and components

        private static string DumpHierarchy(Transform root)
        {
            var sb = new StringBuilder();
            AppendHierarchy(sb, root, "", int.MaxValue);
            return sb.ToString();
        }

        private static int AppendHierarchy(StringBuilder sb, Transform t, string indent, int budget)
        {
            var go = t.gameObject;
            var components = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).ToArray();
            sb.AppendLine($"{indent}{t.name} (path={PathOf(go)}, activeSelf={go.activeSelf}, tag={go.tag}, layer={LayerMask.LayerToName(go.layer)}, localPos={V(t.localPosition)}, localScale={V(t.localScale)}, localRot={V(t.localEulerAngles)}) [{string.Join(", ", components.Select(c => c.GetType().Name))}]");
            foreach (var component in components)
            {
                ComponentTypes.Add(component.GetType());
                AppendComponent(sb, component, indent + "  | ");
            }

            var used = 1;
            foreach (Transform child in t)
            {
                if (used >= budget)
                {
                    sb.AppendLine($"{indent}  ... (children truncated)");
                    break;
                }

                used += AppendHierarchy(sb, child, indent + "  ", budget - used);
            }

            return used;
        }

        private static void AppendComponent(StringBuilder sb, Component component, string indent)
        {
            switch (component)
            {
                case MeshFilter _:
                case ParticleSystemRenderer _:
                    return;
                case PlayMakerFSM fsm:
                    Fsms.Add(fsm);
                    sb.AppendLine($"{indent}PlayMakerFSM enabled={fsm.enabled} name='{fsm.FsmName}' (see fsm file)");
                    return;
                case Renderer renderer:
                    sb.AppendLine($"{indent}{renderer.GetType().Name} enabled={renderer.enabled} sortingLayer={renderer.sortingLayerName} order={renderer.sortingOrder}");
                    return;
                case ParticleSystem ps:
                    sb.AppendLine($"{indent}ParticleSystem duration={ps.main.duration} loop={ps.main.loop} playOnAwake={ps.main.playOnAwake}");
                    return;
                case PolygonCollider2D poly:
                    sb.AppendLine($"{indent}PolygonCollider2D enabled={poly.enabled} trigger={poly.isTrigger} offset={V(poly.offset)} paths={poly.pathCount} points=[{string.Join(" ", poly.points.Select(p => $"({p.x:0.###},{p.y:0.###})"))}]");
                    return;
                case BoxCollider2D box:
                    sb.AppendLine($"{indent}BoxCollider2D enabled={box.enabled} trigger={box.isTrigger} offset={V(box.offset)} size={V(box.size)} edgeRadius={box.edgeRadius}");
                    return;
                case CircleCollider2D circle:
                    sb.AppendLine($"{indent}CircleCollider2D enabled={circle.enabled} trigger={circle.isTrigger} offset={V(circle.offset)} radius={circle.radius}");
                    return;
                case CapsuleCollider2D capsule:
                    sb.AppendLine($"{indent}CapsuleCollider2D enabled={capsule.enabled} trigger={capsule.isTrigger} offset={V(capsule.offset)} size={V(capsule.size)} direction={capsule.direction}");
                    return;
                case EdgeCollider2D edge:
                    sb.AppendLine($"{indent}EdgeCollider2D enabled={edge.enabled} trigger={edge.isTrigger} offset={V(edge.offset)} points=[{string.Join(" ", edge.points.Select(p => $"({p.x:0.###},{p.y:0.###})"))}]");
                    return;
                case Rigidbody2D body:
                    sb.AppendLine($"{indent}Rigidbody2D type={body.bodyType} simulated={body.simulated} gravityScale={body.gravityScale} mass={body.mass} linearDamping={body.linearDamping} angularDamping={body.angularDamping} interpolation={body.interpolation} collision={body.collisionDetectionMode} constraints={body.constraints} material={Name(body.sharedMaterial)}");
                    return;
                case Animator animator:
                    Animators.Add(animator);
                    sb.AppendLine($"{indent}Animator enabled={animator.enabled} speed={animator.speed} updateMode={animator.updateMode} cullingMode={animator.cullingMode} controller={Name(animator.runtimeAnimatorController)} (timeline in animator-timelines.txt)");
                    if (animator.runtimeAnimatorController != null)
                    {
                        foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        {
                            var events = string.Join(" ", clip.events.Select(e =>
                                $"{e.time:0.####}s:{e.functionName}(f={e.floatParameter},i={e.intParameter},s='{e.stringParameter}',o={Format(e.objectReferenceParameter)})"));
                            sb.AppendLine($"{indent}  clip {clip.name}: length={clip.length:0.####}s fps={clip.frameRate} loop={clip.isLooping} legacy={clip.legacy} wrap={clip.wrapMode} events=[{events}]");
                        }
                    }

                    return;
                case Behaviour behaviour:
                    sb.AppendLine($"{indent}{component.GetType().Name} enabled={behaviour.enabled}");
                    AppendFields(sb, component, indent + "  ", typeof(MonoBehaviour));
                    return;
                default:
                    sb.AppendLine($"{indent}{component.GetType().Name}");
                    AppendFields(sb, component, indent + "  ", typeof(Component));
                    return;
            }
        }

        // ---------------------------------------------------------------- hero, input, settings, tools

        private static string DumpHeroController(HeroController hero)
        {
            var sb = new StringBuilder();
            sb.AppendLine("== HeroController fields ==");
            AppendFields(sb, hero, "  ", typeof(MonoBehaviour));
            sb.AppendLine();
            sb.AppendLine("== HeroController constants and statics ==");
            AppendStatics(sb, typeof(HeroController), "  ");

            var animationController = hero.GetComponent<HeroAnimationController>();
            if (animationController != null)
            {
                sb.AppendLine();
                sb.AppendLine("== HeroAnimationController fields ==");
                AppendFields(sb, animationController, "  ", typeof(MonoBehaviour));
                AppendStatics(sb, typeof(HeroAnimationController), "  ");
            }

            sb.AppendLine();
            sb.AppendLine("== PlayerData ==");
            AppendFields(sb, PlayerData.instance, "  ", typeof(object));
            return sb.ToString();
        }

        private static string DumpInputHandler()
        {
            var sb = new StringBuilder();
            var handler = Object.FindObjectOfType<InputHandler>();
            sb.AppendLine($"== InputHandler {Name(handler)} ==");
            if (handler != null)
            {
                AppendFields(sb, handler, "  ", typeof(MonoBehaviour));
            }

            AppendStatics(sb, typeof(InputHandler), "  ");
            return sb.ToString();
        }

        private static string DumpGlobalSettings()
        {
            var sb = new StringBuilder();
            var types = typeof(Gameplay).Assembly.GetTypes()
                .Where(t => t.Namespace == "GlobalSettings" && !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t));
            foreach (var type in types)
            {
                Object instance = null;
                try
                {
                    instance = AccessTools.Method(type, "Get")?.Invoke(null, null) as Object;
                }
                catch (Exception e)
                {
                    sb.AppendLine($"== {type.Name}: Get() failed: {e.GetBaseException().Message} ==");
                }

                instance ??= Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
                sb.AppendLine($"== {type.FullName} ({Name(instance)}) ==");
                if (instance != null)
                {
                    AppendFields(sb, instance, "  ");
                }

                sb.AppendLine();
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

        private static string DumpEngineSettings()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Time.fixedDeltaTime = {Time.fixedDeltaTime}");
            sb.AppendLine($"Time.maximumDeltaTime = {Time.maximumDeltaTime}");
            sb.AppendLine($"Time.timeScale = {Time.timeScale}");
            sb.AppendLine($"Application.targetFrameRate = {Application.targetFrameRate}");
            sb.AppendLine($"QualitySettings.vSyncCount = {QualitySettings.vSyncCount}");
            sb.AppendLine($"Screen.currentResolution = {Screen.currentResolution}");
            sb.AppendLine($"Physics2D.gravity = {V(Physics2D.gravity)}");
            sb.AppendLine($"Physics2D.velocityIterations = {Physics2D.velocityIterations}");
            sb.AppendLine($"Physics2D.positionIterations = {Physics2D.positionIterations}");
            sb.AppendLine($"Physics2D.defaultContactOffset = {Physics2D.defaultContactOffset}");
            sb.AppendLine($"Physics2D.queriesHitTriggers = {Physics2D.queriesHitTriggers}");
            sb.AppendLine($"Physics2D.simulationMode = {Physics2D.simulationMode}");
            sb.AppendLine();
            sb.AppendLine("Layers:");
            for (var i = 0; i < 32; i++)
            {
                var layer = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layer))
                {
                    sb.AppendLine($"  {i} = {layer}");
                }
            }

            return sb.ToString();
        }

        // ---------------------------------------------------------------- referenced objects

        private static void Track(Object obj)
        {
            switch (obj)
            {
                case tk2dSpriteAnimation library:
                    Libraries.Add(library);
                    return;
                case tk2dSpriteCollectionData _:
                case ToolBase _:
                    return;
                case ScriptableObject so when IgnoredAssetTypes.Contains(so.GetType().Name):
                    return;
                case GameObject _:
                case Component _:
                case ScriptableObject _:
                    break;
                default:
                    return;
            }

            var go = obj is Component c ? c.gameObject : obj as GameObject;
            if (go != null)
            {
                if (_heroRoot != null && go.transform.IsChildOf(_heroRoot))
                {
                    return;
                }

                obj = go;
            }

            if (Tracked.Count < MaxReferencedObjects && Tracked.Add(obj))
            {
                Pending.Enqueue(obj);
            }
        }

        private static string DumpReferencedObjects()
        {
            var sb = new StringBuilder();
            while (Pending.Count > 0)
            {
                var obj = Pending.Dequeue();
                if (obj == null)
                {
                    continue;
                }

                try
                {
                    switch (obj)
                    {
                        case GameObject go:
                            sb.AppendLine($"== GameObject {PathOf(go)} (scene={(go.scene.IsValid() ? go.scene.name : "<asset/prefab>")}) ==");
                            AppendHierarchy(sb, go.transform, "  ", MaxReferencedTransforms);
                            foreach (var fsm in go.GetComponentsInChildren<PlayMakerFSM>(true))
                            {
                                Fsms.Add(fsm);
                            }

                            break;
                        case FsmTemplate template:
                            sb.AppendLine($"== FsmTemplate {template.name} (see fsm file) ==");
                            WriteFsmFile($"fsmtemplate_{template.name}", DumpFsm(template.fsm, $"template {template.name}"));
                            break;
                        default:
                            sb.AppendLine($"== {obj.GetType().Name} {obj.name} ==");
                            AppendFields(sb, obj, "  ");
                            break;
                    }
                }
                catch (Exception e)
                {
                    sb.AppendLine($"  <dump failed: {e.GetType().Name}: {e.Message}>");
                }

                sb.AppendLine();
            }

            sb.AppendLine($"(tracked {Tracked.Count} objects, cap {MaxReferencedObjects})");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- FSMs

        private static string DumpFsms()
        {
            var done = new HashSet<PlayMakerFSM>();
            // dumping FSMs can discover more referenced objects with their own FSMs
            while (true)
            {
                var todo = Fsms.Where(f => f != null && !done.Contains(f)).ToList();
                if (todo.Count == 0)
                {
                    break;
                }

                foreach (var fsm in todo)
                {
                    done.Add(fsm);
                    try
                    {
                        WriteFsmFile($"fsm_{PathOf(fsm.gameObject)}_{fsm.FsmName}", DumpFsm(fsm.Fsm, $"'{fsm.FsmName}' on {PathOf(fsm.gameObject)}"));
                    }
                    catch (Exception e)
                    {
                        _log.LogError($"FSM dump failed for {PathOf(fsm.gameObject)}/{fsm.FsmName}: {e}");
                    }
                }

                if (Pending.Count > 0)
                {
                    File.AppendAllText(Path.Combine(DumpDir, "referenced-objects.txt"), DumpReferencedObjects());
                }
            }

            return null;
        }

        private static void WriteFsmFile(string baseName, string content)
        {
            var fileName = baseName.Replace('/', '~');
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            var path = Path.Combine(DumpDir, fileName + ".txt");
            for (var i = 2; File.Exists(path); i++)
            {
                path = Path.Combine(DumpDir, $"{fileName} ({i}).txt");
            }

            File.WriteAllText(path, content);
        }

        private static string DumpFsm(Fsm fsm, string label)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"FSM {label}, start state '{fsm.StartState}'");
            sb.AppendLine("Variables:");
            foreach (var variable in fsm.Variables.GetAllNamedVariablesSorted())
            {
                sb.AppendLine($"  {variable.VariableType} {variable.Name} = {Format(variable.RawValue)}");
            }

            sb.AppendLine("Global transitions:");
            foreach (var transition in fsm.GlobalTransitions)
            {
                sb.AppendLine($"  {transition.EventName} -> {transition.ToState}");
            }

            foreach (var state in fsm.States)
            {
                sb.AppendLine();
                sb.AppendLine($"State '{state.Name}'");
                foreach (var transition in state.Transitions)
                {
                    sb.AppendLine($"  on {transition.EventName} -> {transition.ToState}");
                }

                FsmStateAction[] actions;
                try
                {
                    actions = state.Actions;
                }
                catch (Exception e)
                {
                    sb.AppendLine($"  <actions failed to load: {e.Message}>");
                    continue;
                }

                for (var i = 0; i < actions.Length; i++)
                {
                    var action = actions[i];
                    var name = string.IsNullOrEmpty(action.Name) ? "" : $" \"{action.Name}\"";
                    sb.AppendLine($"  [{i}] {action.GetType().Name}{name}{(action.Enabled ? "" : " (disabled)")}");
                    foreach (var field in action.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                    {
                        sb.AppendLine($"      {field.Name} = {Format(field.GetValue(action))}");
                    }
                }
            }

            return sb.ToString();
        }

        // ---------------------------------------------------------------- statics, animations

        private static string DumpStatics()
        {
            var sb = new StringBuilder();
            foreach (var type in ComponentTypes.Where(t => t.Assembly == typeof(HeroController).Assembly).OrderBy(t => t.FullName))
            {
                var inner = new StringBuilder();
                AppendStatics(inner, type, "  ");
                if (inner.Length > 0)
                {
                    sb.AppendLine($"== {type.FullName} ==");
                    sb.Append(inner);
                }
            }

            return sb.ToString();
        }

        private static string DumpLibraries()
        {
            var sb = new StringBuilder();
            foreach (var library in Libraries.Where(l => l != null).OrderBy(l => l.name))
            {
                sb.Append(DumpClips(library));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string DumpClips(tk2dSpriteAnimation library, string indent = "")
        {
            var sb = new StringBuilder();
            if (library?.clips == null)
            {
                return sb.ToString();
            }

            Libraries.Add(library);
            sb.AppendLine($"{indent}animation library {library.name}:");
            foreach (var clip in library.clips.Where(c => c != null && !string.IsNullOrEmpty(c.name)))
            {
                var triggers = clip.frames == null
                    ? ""
                    : string.Join(" ", clip.frames
                        .Select((f, idx) => (f, idx))
                        .Where(x => x.f.triggerEvent)
                        .Select(x => $"{x.idx}:'{x.f.eventInfo}'/{x.f.eventInt}/{x.f.eventFloat}"));
                sb.AppendLine($"{indent}  {clip.name}: fps={clip.fps} frames={clip.frames?.Length} wrap={clip.wrapMode} loopStart={clip.loopStart} triggers=[{triggers}]");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Samples every Animator clip on an inactive clone and records which objects, colliders and
        /// behaviours switch on/off (and transform changes) over time. Unity has no runtime API for curves.
        /// </summary>
        private static string DumpAnimatorTimelines()
        {
            var sb = new StringBuilder();
            var holder = new GameObject("RebalancedCrests dump sampler");
            holder.SetActive(false);
            try
            {
                foreach (var animator in Animators.Where(a => a != null && a.runtimeAnimatorController != null))
                {
                    sb.AppendLine($"== Animator on {PathOf(animator.gameObject)} controller={Name(animator.runtimeAnimatorController)} ==");
                    foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                    {
                        sb.AppendLine($"  clip {clip.name}: length={clip.length:0.####}s fps={clip.frameRate}");
                        var clone = Object.Instantiate(animator.gameObject, holder.transform);
                        try
                        {
                            Dictionary<string, string> previous = null;
                            for (var time = 0f; ; time += SampleStep)
                            {
                                var t = Mathf.Min(time, clip.length);
                                clip.SampleAnimation(clone, t);
                                var snapshot = Snapshot(clone.transform);
                                foreach (var entry in snapshot)
                                {
                                    if (previous == null || !previous.TryGetValue(entry.Key, out var old) || old != entry.Value)
                                    {
                                        sb.AppendLine($"    t={t:0.####}s ({t * 60f:0.#}f@60) {entry.Key}: {entry.Value}");
                                    }
                                }

                                previous = snapshot;
                                if (t >= clip.length)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            sb.AppendLine($"    <sampling failed: {e.GetType().Name}: {e.Message}>");
                        }
                        finally
                        {
                            Object.Destroy(clone);
                        }
                    }

                    sb.AppendLine();
                }
            }
            finally
            {
                Object.Destroy(holder);
            }

            return sb.ToString();
        }

        private static Dictionary<string, string> Snapshot(Transform root)
        {
            var result = new Dictionary<string, string>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var path = t == root ? "." : RelativePath(root, t);
                result[path + " active"] = t.gameObject.activeSelf.ToString();
                result[path + " transform"] = $"pos={V(t.localPosition)} scale={V(t.localScale)} rot={V(t.localEulerAngles)}";
                foreach (var component in t.GetComponents<Component>())
                {
                    switch (component)
                    {
                        case Collider2D collider:
                            result[$"{path} {collider.GetType().Name}.enabled"] = collider.enabled.ToString();
                            break;
                        case Renderer renderer:
                            result[$"{path} {renderer.GetType().Name}.enabled"] = renderer.enabled.ToString();
                            if (renderer is SpriteRenderer sprite)
                            {
                                result[$"{path} SpriteRenderer.sprite/color"] = $"{Name(sprite.sprite)} {sprite.color}";
                            }

                            break;
                        case Behaviour behaviour when !(behaviour is Animator):
                            result[$"{path} {behaviour.GetType().Name}.enabled"] = behaviour.enabled.ToString();
                            break;
                    }
                }
            }

            return result;
        }

        // ---------------------------------------------------------------- formatting

        private static void AppendFields(StringBuilder sb, object obj, string indent, Type stopAt = null, int depth = 0)
        {
            if (obj == null)
            {
                return;
            }

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

        private static void AppendStatics(StringBuilder sb, Type type, string indent)
        {
            foreach (var field in type.GetFields(StaticFields | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType.IsGenericType && field.Name.Contains("<"))
                {
                    continue;
                }

                try
                {
                    sb.AppendLine($"{indent}{(field.IsLiteral ? "const " : "static ")}{field.Name} = {Format(field.GetValue(null))}");
                }
                catch (Exception e)
                {
                    sb.AppendLine($"{indent}{field.Name} = <error {e.GetType().Name}>");
                }
            }
        }

        private static bool IsNestedData(object value)
        {
            if (value == null || value is Object || value is string || value is Array || value is ICollection
                || value is NamedVariable || value is AnimationCurve)
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

        private static string Format(object value) => Format(value, 0);

        private static string Format(object value, int depth)
        {
            switch (value)
            {
                case null:
                    return "null";
                case Object unityObject:
                    if (!unityObject)
                    {
                        return "null";
                    }

                    Track(unityObject);
                    return unityObject is GameObject || unityObject is Component
                        ? $"{PathOf(unityObject)} ({unityObject.GetType().Name})"
                        : $"{unityObject.name} ({unityObject.GetType().Name})";
                case string s:
                    return $"\"{s}\"";
                case NamedVariable variable:
                    return variable.UseVariable
                        ? $"<var {variable.Name}>"
                        : variable.IsNone ? "<none>" : Format(variable.RawValue, depth + 1);
                case FsmEvent fsmEvent:
                    return $"<event {fsmEvent.Name}>";
                case FsmOwnerDefault owner:
                    return owner.OwnerOption == OwnerDefaultOption.UseOwner ? "<owner>" : Format(owner.GameObject, depth + 1);
                case AnimationCurve curve:
                    return $"curve(pre={curve.preWrapMode}, post={curve.postWrapMode}, keys=[{string.Join(" ", curve.keys.Select(k => $"(t={k.time:0.####}, v={k.value:0.####}, in={k.inTangent:0.###}, out={k.outTangent:0.###})"))}])";
                case Vector2 v2:
                    return V(v2);
                case Vector3 v3:
                    return V(v3);
                case Array array:
                    return $"({array.Length})[" + string.Join(", ", array.Cast<object>().Take(64).Select(o => Format(o, depth + 1))) + (array.Length > 64 ? ", ..." : "") + "]";
                case IList list:
                    return $"({list.Count})[" + string.Join(", ", list.Cast<object>().Take(64).Select(o => Format(o, depth + 1))) + (list.Count > 64 ? ", ..." : "") + "]";
            }

            var type = value.GetType();
            if (depth < MaxFormatDepth && type.Namespace != null && type.Namespace.StartsWith("HutongGames") && !type.IsEnum)
            {
                // PlayMaker helper types (FunctionCall, FsmVar, FsmProperty, FsmEventTarget, FsmAnimationCurve, ...)
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                return $"{type.Name}{{{string.Join(", ", fields.Select(f => $"{f.Name}={SafeGet(f, value, depth)}"))}}}";
            }

            try
            {
                return value.ToString();
            }
            catch (Exception e)
            {
                return $"<error {e.GetType().Name}>";
            }
        }

        private static string SafeGet(FieldInfo field, object obj, int depth)
        {
            try
            {
                return Format(field.GetValue(obj), depth + 1);
            }
            catch (Exception e)
            {
                return $"<error {e.GetType().Name}>";
            }
        }

        private static string Name(Object obj) => obj ? obj.name : "null";

        private static string PathOf(Object obj)
        {
            var go = obj is Component c ? c.gameObject : obj as GameObject;
            if (!go)
            {
                return Name(obj);
            }

            var path = go.name;
            for (var t = go.transform.parent; t != null; t = t.parent)
            {
                path = t.name + "/" + path;
            }

            return path;
        }

        private static string RelativePath(Transform root, Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent)
            {
                path = p.name + "/" + path;
            }

            return path;
        }

        private static string V(Vector2 v) => $"({v.x:0.####}, {v.y:0.####})";

        private static string V(Vector3 v) => $"({v.x:0.####}, {v.y:0.####}, {v.z:0.####})";
    }
}
