# Crest System

How crests are represented and applied in the current game code. All values are taken from the runtime dump (`raw-dump/`) and the decompiled game assembly.

---

## 1. What a crest is in code

### 1.1 `ToolCrest` (ScriptableObject)

`ToolCrest : ToolBase : QuestTargetCounter` (asset menu `Hornet/Tool Crest`). Serialized fields:

| Field | Type | Meaning |
|---|---|---|
| `displayName` | `LocalisedString` | Name shown in the inventory. |
| `description` | `LocalisedString` | Inventory description. |
| `itemNamePrefix` | `LocalisedString` | Prefix used in item-get prompts. |
| `getPromptDesc` | `LocalisedString` (not required) | Description on the item-get prompt. |
| `equipText` | `LocalisedString` | Text shown when equipping. |
| `crestSprite`, `crestSilhouette`, `crestGlow` | `Sprite` | Inventory art. `crestSprite` is also the popup icon (`GetPopupIcon`). |
| `isHidden` | `bool` | Hidden crests are only visible in the inventory while equipped (see `IsVisible`). |
| `displayPrefab` | `GameObject` | Inventory display prefab. |
| `slots` | `SlotInfo[]` | Tool slots (section 2). |
| `hasCustomAction`, `customButtonCombo` | `bool`, `InventoryItemComboButtonPromptDisplay.Display` | Inventory button prompt for a crest-specific action. |
| `heroConfig` | `HeroControllerConfig` | Movement/attack configuration applied while equipped (section 3). `null` means the `Default` config is used. |
| `previousVersion` | `ToolCrest` | The crest this one upgrades from (section 4). |

Non-serialized: `oldPreviousVersion`, `upgradedVersion` (filled by `OnValidate`/`OnEnable`: `previousVersion.upgradedVersion = this`), a cached `name`.

Derived properties:

- `IsEquipped` => `PlayerData.instance.CurrentCrestID == name`.
- `IsUnlocked` => `SaveData.IsUnlocked` (save data keyed by the crest's asset name).
- `IsUpgradedVersionUnlocked` => `upgradedVersion != null && upgradedVersion.IsUnlocked`.
- `IsBaseVersion` => `previousVersion == null`.
- `IsVisible` => `false` if the upgraded version is unlocked; `false` if not unlocked; if `IsHidden` then only when equipped; otherwise `true`.

### 1.2 Crest list and lookup

- `ToolItemManager.crestList` is a `ToolCrestList` (`NamedScriptableObjectList<ToolCrest>`).
- `ToolItemManager.GetAllCrests()` returns all non-null entries of `crestList` in list order.
- `ToolItemManager.GetCrestByName(name)` returns `crestList.GetByName(name)` (asset name).
- `GlobalSettings.Gameplay` holds direct references to individual crests (`hunterCrest`, `hunterCrest2`, `hunterCrest3`, `warriorCrest`, `reaperCrest`, `wandererCrest`, `cursedCrest`, `witchCrest`, `toolmasterCrest`, `spellCrest`, `cloaklessCrest`).

### 1.3 Crest IDs (order of `GetAllCrests()`)

| # | Crest ID (asset name) | `isHidden` | `previousVersion` | `heroConfig` asset (type, instanceId) | Gameplay field |
|---|---|---|---|---|---|
| 1 | `Hunter` | False | null | null | `hunterCrest` |
| 2 | `Hunter_v2` | False | `Hunter` | null | `hunterCrest2` |
| 3 | `Hunter_v3` | False | `Hunter_v2` | null | `hunterCrest3` |
| 4 | `Reaper` | False | null | `Reaper` (HeroControllerConfig, 65784) | `reaperCrest` |
| 5 | `Wanderer` | False | null | `Wanderer` (HeroControllerConfig, 65766) | `wandererCrest` |
| 6 | `Warrior` | False | null | `Warrior` (HeroControllerConfigWarrior, 65804) | `warriorCrest` |
| 7 | `Witch` | False | null | `Whip` (HeroControllerConfig, 65800) | `witchCrest` |
| 8 | `Toolmaster` | False | null | `Toolmaster` (HeroControllerConfig, 65792) | `toolmasterCrest` |
| 9 | `Spell` | False | null | `Shaman` (HeroControllerConfig, 65818) | `spellCrest` |
| 10 | `Cursed` | True | null | `Whip` (HeroControllerConfig, 65800) | `cursedCrest` |
| 11 | `Cloakless` | True | null | `Cloakless` (HeroControllerConfig, 50250) | `cloaklessCrest` |

### 1.4 Internal ID / config / in-game name mapping

The localised display strings are not part of the dump, so the in-game names below come from code hints (method names, config names, scene logic) plus the crest's known in-game identity. Entries marked "not verified" have no direct confirmation in the dumped data or code.

| Crest ID | Config asset | Attack root object | Anim override library | In-game name | Evidence |
|---|---|---|---|---|---|
| `Hunter`, `Hunter_v2`, `Hunter_v3` | none → `Default` | `Default` | none (base `Knight` library) | Hunter (base and two upgrades) | `HeroController.IsHunterCrestEquipped()` checks these three IDs |
| `Reaper` | `Reaper` | `Scythe` | Hornet CrestWeapon Scythe Anim | Reaper | ID/config name |
| `Wanderer` | `Wanderer` | `Wanderer` | Hornet CrestWeapon Dagger Anim | Wanderer | ID/config name |
| `Warrior` | `Warrior` | `Warrior` (+ special rage group) | Hornet CrestWeapon Warrior Anim | Beast (not verified from dump) | ID is "Warrior"; rage mode logic is `WarriorState` |
| `Witch` | `Whip` | `Witch` | Hornet CrestWeapon Whip Anim | Witch | ID; config named after the whip weapon |
| `Cursed` | `Whip` (same asset as `Witch`) | `Witch` | Hornet CrestWeapon Whip Anim | Cursed crest (the pre-Witch cursed state; not verified from dump) | `PlayerData.IsAnyCursed => CurrentCrestID == Gameplay.CursedCrest.name`; `ToolItemManager.IsCursed` set from it |
| `Toolmaster` | `Toolmaster` | `Toolmaster` | Hornet CrestWeapon Drill Lance Anim | Architect | `HeroController.IsArchitectCrestEquipped()` returns `CurrentCrestID == "Toolmaster"` |
| `Spell` | `Shaman` | `Shaman` | Hornet CrestWeapon Shaman Anim | Shaman | config/root named `Shaman`; `Gameplay.spellCrestRuneDamageMult` |
| `Cloakless` | `Cloakless` | `Cloakless` | Hornet Cloakless Anim | No crest name; Hornet's cloakless state in The Slab | `HeroSlabCapture.ApplyCaptured()` auto-equips `Gameplay.CloaklessCrest`; `GameManager.FixUpSaveState()` / `PlayerData.OnBeforeSave()` restore the previous crest around `slab_cloak_battle_completed` / scene `Slab_16` |

Override library clip counts (from `crests.txt`): Scythe 29, Dagger 20, Warrior 48, Whip 22, Drill Lance 26, Shaman 14, Cloakless 174. The base hero library (`Knight`) has 501 clips (`hero-animations.txt`). `HeroAnimationController.GetClip(name)` returns the config's override clip if the override library has a clip of that name, otherwise it falls back to the windy library (in wind/updraft) and then the base library.

---

## 2. Slots

### 2.1 `ToolCrest.SlotInfo`

| Field | Type | Meaning |
|---|---|---|
| `Position` | `Vector2` | Position on the inventory crest display. |
| `Type` | `ToolItemType` | Which tool type fits. |
| `AttackBinding` | `AttackToolBinding` | Only shown/used when `Type.IsAttackType()` (Red or Skill). Selects which direction + tool input uses this slot's tool. |
| `NavUpIndex`, `NavDownIndex`, `NavLeftIndex`, `NavRightIndex` | `int` | Inventory cursor navigation. |
| `NavUpFallbackIndex` … `NavRightFallbackIndex` | `int` | Fallback navigation. |
| `IsLocked` | `bool` | Slot starts locked and has to be unlocked (2.4). |

### 2.2 `ToolItemType`

| Value | Int | `EnumOrder` | `IsAttackType()` | Tools in `tools.txt` |
|---|---|---|---|---|
| `Red` | 0 | 1 | true | 23 |
| `Blue` | 1 | 2 | false | 23 |
| `Yellow` | 2 | 3 | false | 13 |
| `Skill` | 3 | 0 | true | 6 (silk skills, `ToolItemSkill`) |

`tools.txt` lists 65 tools in total (55 `ToolItemBasic`, 6 `ToolItemSkill`, 2 `ToolItemStatesLiquid`, 1 `ToolItemLerpStates`, 1 `ToolItemToggleState`). `Gameplay.defaultSkillTool` = `Silk Spear`.

### 2.3 `AttackToolBinding`

`Neutral` (0), `Up` (1), `Down` (2).

Resolution (`ToolItemManager.GetBoundAttackTool`):
- For the current crest, every slot (index < min(crest slot count, saved slot count)) holding an attack-type tool (Red or Skill) is collected.
- For each binding, the first collected slot whose `AttackBinding` equals that binding provides the tool (`GetToolForBinding`).
- `HeroController.GetWillThrowTool`: Up held → `Up`; Down held → `Down`; otherwise → `Neutral`.
- With `ToolEquippedReadSource.Active`, if the requested Up/Down binding has no tool, the `Neutral` tool is used instead (`usedBinding = Neutral`).
- Returns `null` when tools are disabled, in `Cutscene` active state (for `Active` read source), or in hidden-item mode (`CollectableItemManager.IsInHiddenMode()`).
- `customToolOverride`, when set, replaces Neutral and clears Up/Down.

`AttackBinding` values are also serialized on Blue/Yellow slots (shown in the dump) but are not used for them (`GetAttackToolBinding` returns `null` for non-attack tools).

### 2.4 Locked slots

- `SlotInfo.IsLocked` marks the slot as initially locked.
- On first unlock of a base crest (`ToolCrest.Unlock`), save slot data is created with `IsUnlocked = !slotInfo.IsLocked` for every slot.
- `InventoryToolCrestSlot.IsLocked`: if `slotInfo.IsLocked`, the slot is locked while `SaveData.IsUnlocked` is false.
- Unlocking happens in the inventory: `InventoryItemToolManager.CanUnlockSlot` => `slotUnlockItem.CollectedAmount > 0`. Holding the confirm input on a locked slot runs `UnlockHoldRoutine` for `unlockHoldDuration`, then sets the slot's `SaveData.IsUnlocked = true` and takes 1 of `slotUnlockItem`. The asset name of `slotUnlockItem` is not in the dumps.
- An upgraded crest version copies the previous version's slot save data, including unlocked state (section 4).
- `CountCrestUnlockPoints` (PlayMaker action) counts, over non-hidden base crests whose upgraded version is not unlocked: max = total slot count; current = unlocked crests' slots that are either not `IsLocked` or saved as unlocked.

Separate from crest slots: `PlayerData.UnlockedExtraBlueSlot`, `PlayerData.UnlockedExtraYellowSlot` and `PlayerData.ExtraToolEquips` (`FloatingCrestSlotsData`) are crest-independent floating slots. `SetEquippedCrest` removes from the new crest's slots any tool that is already equipped in an extra slot.

### 2.5 Slot table (all crests)

Per-slot listing (`type / locked / binding`):

| Crest | slot[0] | slot[1] | slot[2] | slot[3] | slot[4] | slot[5] | slot[6] |
|---|---|---|---|---|---|---|---|
| Hunter / Hunter_v2 / Hunter_v3 | Red / – / Up | Blue / – / Neutral | Yellow / – / Neutral | Skill / – / Neutral | Red / locked / Down | Blue / locked / Neutral | Yellow / locked / Neutral |
| Reaper | Skill / – / Neutral | Red / – / Up | Red / locked / Down | Blue / locked / Down | Blue / – / Down | Yellow / locked / Down | Yellow / – / Down |
| Wanderer | Skill / – / Neutral | Red / – / Up | Blue / locked / Down | Blue / locked / Down | Yellow / locked / Down | Yellow / – / Down | Yellow / – / Down |
| Warrior | Skill / – / Neutral | Red / – / Up | Red / – / Down | Yellow / locked / Down | Yellow / locked / Down | | |
| Witch | Skill / – / Neutral | Red / – / Up | Red / locked / Down | Blue / locked / Down | Blue / locked / Down | Blue / – / Down | |
| Toolmaster | Red / – / Neutral | Red / – / Up | Red / – / Down | Blue / locked / Down | Yellow / locked / Down | Yellow / locked / Down | Blue / locked / Down |
| Spell | Skill / – / Neutral | Skill / – / Up | Skill / – / Down | Blue / locked / Down | Blue / locked / Down | | |
| Cursed | (no slots) | | | | | | |
| Cloakless | (no slots) | | | | | | |

Counts (total / of which locked):

| Crest | Total | Red | Blue | Yellow | Skill | Locked total | Unlocked from start | Attack bindings (unlocked from start → locked) |
|---|---|---|---|---|---|---|---|---|
| Hunter, Hunter_v2, Hunter_v3 | 7 | 2 (1) | 2 (1) | 2 (1) | 1 (0) | 3 | 4 | Skill=Neutral, Red=Up → Red=Down |
| Reaper | 7 | 2 (1) | 2 (1) | 2 (1) | 1 (0) | 3 | 4 | Skill=Neutral, Red=Up → Red=Down |
| Wanderer | 7 | 1 (0) | 2 (2) | 3 (1) | 1 (0) | 3 | 4 | Skill=Neutral, Red=Up |
| Warrior | 5 | 2 (0) | 0 | 2 (2) | 1 (0) | 2 | 3 | Skill=Neutral, Red=Up, Red=Down |
| Witch | 6 | 2 (1) | 3 (2) | 0 | 1 (0) | 3 | 3 | Skill=Neutral, Red=Up → Red=Down |
| Toolmaster | 7 | 3 (0) | 2 (2) | 2 (2) | 0 | 4 | 3 | Red=Neutral, Red=Up, Red=Down |
| Spell | 5 | 0 | 2 (2) | 0 | 3 (0) | 2 | 3 | Skill=Neutral, Skill=Up, Skill=Down |
| Cursed | 0 | 0 | 0 | 0 | 0 | 0 | 0 | – |
| Cloakless | 0 | 0 | 0 | 0 | 0 | 0 | 0 | – |

---

## 3. `HeroControllerConfig`

### 3.1 Fields

`HeroControllerConfig : ScriptableObject, IIncludeVariableExtensions` (asset menu `Hornet/HeroController Config`). FSMs read fields by name via `GetHeroConfigVariable` (`Config.GetVariable(name, type)`) and compare the active config via `CheckHeroConfig`.

| Field | Type | Consumer | What it does |
|---|---|---|---|
| `heroAnimOverrideLib` | `tk2dSpriteAnimation` | `HeroAnimationController.GetClip` | Per-clip override library; clips not present fall back to the base library. |
| `canPlayNeedolin` | bool | `HeroController.HasNeedolin` | Allows Needolin. |
| `canBrolly` | bool | `HeroController` brolly/float check | Allows the umbrella float. |
| `canDoubleJump` | bool | `HeroController.CanDoubleJump` | Allows double jump. |
| `canNailCharge` | bool | `HeroController.CanNailCharge` | Allows charging the needle (charged attack). |
| `canBind` | bool | `HeroController.CanBind` (called by the `Bind` FSM through `CallMethodProper "CanBind"`) | Allows binding (healing). |
| `canHarpoonDash` | bool | `HeroController.HasHarpoonDash` | Allows the harpoon dash (Clawline). |
| `forceBareInventory` | bool | `HeroController.SetConfigGroup`, `CollectableItemManager`, `EnemyJournalManager`, animation/audio controllers, frost logic | Bare/hidden inventory mode; switching away from such a config re-applies hidden items and rebuilds the map; with frost, doubles frost speed and disables double jump. |
| `downSlashType` | `DownSlashTypes` {`DownSpike`=0, `Slash`=1, `Custom`=2} | `HeroController.Attack`, `ConfigGroup.Setup` | `DownSpike`: down attack is a `Downspike` (HeroController-driven thrust). `Slash`: down attack is a `NailSlash`. `Custom`: down attack is delegated to the `Crest Attacks` FSM via `downSlashEvent`. In `Attack()`, a downward attack with `DownSpike` type does not set `cState.attacking`/`attackDuration`. |
| `downSlashEvent` | string | `HeroController` → `crestAttacksFSM.SendEvent(Config.DownSlashEvent)` | FSM event for `Custom` down attacks. |
| `downspikeAnticTime` | float | HeroController downspike update | Duration of the downspike anticipation. |
| `downspikeTime` | float | HeroController downspike update | Duration of the downspike thrust. |
| `downspikeSpeed` | float | HeroController downspike | Thrust speed: velocity `(±downspikeSpeed, -downspikeSpeed)` when `downspikeThrusts`. |
| `downspikeRecoveryTime` | float | HeroController downspike update | Recovery duration after the downspike. |
| `downspikeBurstEffect` | bool | HeroController downspike update | Plays the burst effect when the antic ends. |
| `downspikeThrusts` | bool | HeroController downspike | Whether the downspike moves the hero diagonally. |
| `dashStabSpeed` | float | `Sprint` FSM (`GetHeroConfigVariable "DashStabSpeed"`) | Dash-attack velocity. |
| `dashStabTime` | float | `Sprint` FSM (`"DashStabTime"`) | Dash-attack duration. |
| `forceShortDashStabBounce` | bool | `HeroController.SetStartWithDashStabBounce` | Always uses the short bounce (and sets `attack_cooldown = 0.1`). |
| `dashStabBounceJumpSpeed` | float | HeroController | Vertical velocity of the dash-attack bounce. |
| `dashStabSteps` | int (min 1) | `Sprint` FSM (`"DashStabSteps"`) | Number of dash-attack steps. |
| `attackDuration` | float (virtual) | `HeroController.Attack`, `DidAttack` | Attack state duration; divided by `quickAttackSpeedMult` while `IsUsingQuickening`. |
| `quickAttackSpeedMult` | float (virtual) | `HeroController.Attack`, `NailSlash`, `HeroAnimationController` | Attack duration divisor and slash/animation speed multiplier while `IsUsingQuickening`. |
| `attackRecoveryTime` | float (virtual) | many `HeroController` `Can…` checks; alt-attack reset | While `attack_time < attackRecoveryTime` most other actions (dash, jump, bind, harpoon, etc.) are blocked. Alternate slash resets after `attackRecoveryTime + ALT_ATTACK_RESET` (`ALT_ATTACK_RESET = 0.5`) or a direction change. |
| `attackCooldownTime` | float (virtual) | `HeroController.DidAttack` | `attack_cooldown`; clamped to at least `attackDuration`. |
| `quickAttackCooldownTime` | float (virtual) | `HeroController.DidAttack` | `attack_cooldown` while `IsUsingQuickening`; clamped to at least `attackDuration`. |
| `canTurnWhileSlashing` | bool | HeroController | Allows facing changes during a slash. |
| `chargeSlashRecoils` | bool | `HeroController.ChargeSlashRecoilLeft/Right` | Charged attack recoils the hero. |
| `chargeSlashLungeSpeed` | float | `Nail Arts` FSM (`"ChargeSlashLungeSpeed"`) | Lunge speed of the charged attack. |
| `chargeSlashLungeDeceleration` | float | `Nail Arts` FSM | Lunge deceleration factor. |
| `chargeSlashChain` | int | `Nail Arts` FSM (`"ChargeSlashChain"`) | Number of chained charged-attack hits. |
| `wallSlashSlowdown` | bool | `HeroController.Attack` | On a wall slash (non-Toolmaster) halves vertical velocity and caps fall speed at -5. |

`HeroControllerConfigWarrior : HeroControllerConfig` adds `rageAttackDuration`, `rageAttackRecoveryTime`, `rageAttackCooldownTime`, `rageQuickAttackCooldownTime`. Its `AttackDuration`, `AttackRecoveryTime`, `AttackCooldownTime` and `QuickAttackCooldownTime` getters return the rage values while `HeroController.instance.WarriorState.IsInRageMode`, otherwise the base values. `QuickAttackSpeedMult` is not overridden.

### 3.2 `ConfigGroup` and selection

`HeroController.ConfigGroup` fields: `Config`, `ActiveRoot`, `NormalSlashObject`, `AlternateSlashObject`, `UpSlashObject`, `AltUpSlashObject`, `DownSlashObject`, `AltDownSlashObject`, `WallSlashObject`, `DashStab`, `DashStabAlt`, `ChargeSlash`, `TauntSlash`. `Setup()` deactivates `ActiveRoot` and caches the `NailSlash`/`Downspike`/`DamageEnemies` components (down-attack component depends on `DownSlashType`).

`HeroController` has `configs` (8 groups) and `specialConfigs` (1 group):

| Group | Config | ActiveRoot | NormalSlash | AlternateSlash | UpSlash | AltUpSlash | DownSlash | AltDownSlash | WallSlash | DashStab | DashStabAlt | ChargeSlash | TauntSlash |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| configs[0] | Default | Default | Slash | AltSlash | UpSlash | null | DownSlash | null | WallSlash | Dash Stab | null | Charge Slash Basic | Taunt Slash |
| configs[1] | Cloakless | Cloakless | Kick | AltKick | UpKick | null | DownKick | null | WallKick | Dash Kick | null | null | null |
| configs[2] | Reaper | Scythe | Slash | AltSlash | UpSlash | null | DownSlash | null | WallSlash | Dash Stab | null | Charge Slash Scythe | Taunt Slash |
| configs[3] | Warrior | Warrior | Slash | AltSlash | UpSlash | null | DownSlash | null | WallSlash | null | null | Charge Slash Warrior Old | Taunt Slash |
| configs[4] | Wanderer | Wanderer | Slash | AltSlash | UpSlash | null | DownSlash | DownSlashAlt | WallSlash | DashStab | DashStabAlt | Charge Slash Wanderer | Taunt Slash |
| configs[5] | Toolmaster | Toolmaster | AltSlash | Slash | UpSlash | null | DownSlash | null | WallSlash | Dash Stab | null | Charge Slash Toolmaster | Taunt Slash |
| configs[6] | Whip | Witch | AltSlash | Slash | UpSlash | null | DownSlash | null | WallSlash | Dash Stab Parent | null | Charge Slash Witch | Taunt Slash |
| configs[7] | Shaman | Shaman | AltSlash | Slash | UpSlash | null | DownSlash | null | WallSlash | null | null | Charge Slash Shaman | Taunt Slash |
| specialConfigs[0] | Warrior | null | Slash Rage | AltSlash Rage | UpSlash Rage | null | null | null | WallSlash Rage | null | null | null | null |

(Object names are the GameObject names under each ActiveRoot; Toolmaster/Whip/Shaman assign the object named `AltSlash` as NormalSlash and `Slash` as AlternateSlash.)

Selection logic:

1. `crestConfig` is set in `ResetAllCrestState(bool)`: `crest = GetCrestByName(PlayerData.CurrentCrestID)`; if found, `crestConfig = crest.HeroConfig` (null for Hunter crests). If the crest is not found, `crestConfig` keeps its previous value.
2. `UpdateConfig()` (called from `Awake`, `ResetAllCrestState`, and at the start of every `Attack()`):
   - If `crestConfig` is null → `SetConfigGroup(configs[0], null)` (the `Default` group).
   - Otherwise: base group = first `configs[i]` whose `Config == crestConfig`; override group = first `specialConfigs[i]` whose `Config == crestConfig`, where the group whose config is `Gameplay.WarriorCrest.HeroConfig` only qualifies while `warriorState.IsInRageMode`.
3. `SetConfigGroup(configGroup, overrideGroup)`:
   - Sets `CurrentConfigGroup = configGroup`. If the config changed: re-applies hidden items/map when leaving a `forceBareInventory` config, calls `ToolItemManager.ReportAllBoundAttackToolsUpdated()`, gives the config to `HeroAnimationController`, deactivates the old `ActiveRoot`, activates the new one, and finally sends `HC CONFIG UPDATED` to the hero's FSMs.
   - Slash references (`normalSlash`, `alternateSlash`, `upSlash`, `altUpSlash`, `downSpike`, `downSlash`, `altDownSpike`, `altDownSlash`, `wallSlash` and their damagers) come from `overrideGroup` where it has a non-null object, otherwise from `configGroup`. `DashStab`, `DashStabAlt`, `ChargeSlash`, `TauntSlash` are always read from `CurrentConfigGroup` (FSM action `GetHeroAttackObject`), so the override group does not affect them.
   - `HeroController.Config` returns `CurrentConfigGroup.Config`.
4. Warrior rage therefore has two effects: rage timing values through `HeroControllerConfigWarrior`'s virtual getters, and rage slash objects through `specialConfigs[0]` (applied at the next `UpdateConfig`, i.e. the next attack or reset).

### 3.3 Config values (all configs)

`Default` from `hero-configs.txt` (configs[0]); the others from `crests.txt` (same asset instances as in `hero-configs.txt`).

| Field | Default (Hunter ×3) | Cloakless | Reaper | Warrior | Wanderer | Toolmaster | Whip (Witch, Cursed) | Shaman (Spell) |
|---|---|---|---|---|---|---|---|---|
| heroAnimOverrideLib | null | Hornet Cloakless Anim | Hornet CrestWeapon Scythe Anim | Hornet CrestWeapon Warrior Anim | Hornet CrestWeapon Dagger Anim | Hornet CrestWeapon Drill Lance Anim | Hornet CrestWeapon Whip Anim | Hornet CrestWeapon Shaman Anim |
| canPlayNeedolin | True | False | True | True | True | True | True | True |
| canBrolly | True | False | True | True | True | True | True | True |
| canDoubleJump | True | False | True | True | True | True | True | True |
| canNailCharge | True | False | True | True | True | True | True | True |
| canBind | True | True | True | True | True | True | True | True |
| canHarpoonDash | True | False | True | True | True | True | True | True |
| forceBareInventory | False | True | False | False | False | False | False | False |
| downSlashType | DownSpike | DownSpike | Custom | Custom | Slash | Custom | Custom | Custom |
| downSlashEvent | "" | "" | "RPR DOWNSLASH" | "WARRIOR DOWNSLASH" | "" | "TOOLMASTER DOWNSLASH" | "WITCH DOWNSLASH" | "SHAMAN DOWNSLASH" |
| downspikeAnticTime | 0.1 | 0.07 | 0.07 | 0.07 | 0.09 | 0.07 | 0.1 | 0.1 |
| downspikeTime | 0.1 | 0.04 | 0.15 | 0.08 | 0.16 | 0.16 | 0.16 | 0.1 |
| downspikeSpeed | 26 | 33 | 13 | 20 | 15 | 5 | 5 | 26 |
| downspikeRecoveryTime | 0.25 | 0.25 | 0.25 | 0.25 | 0.09 | 0.25 | 0.25 | 0.25 |
| downspikeBurstEffect | True | True | False | True | True | False | False | True |
| downspikeThrusts | True | True | False | True | True | False | True | True |
| dashStabSpeed | -50 | -50 | -30 | -35 | -30 | -50 | -30 | -50 |
| dashStabTime | 0.12 | 0.175 | 0.2 | 0.2 | 0.2 | 0.25 | 0.15 | 0.18 |
| forceShortDashStabBounce | False | False | True | False | False | False | False | False |
| dashStabBounceJumpSpeed | 18.6 | 18.6 | 12 | 18.6 | 18.6 | 12 | 18.6 | 18.6 |
| dashStabSteps | 1 | 1 | 1 | 1 | 1 | 1 | 2 | 1 |
| attackDuration | 0.35 | 0.35 | 0.35 | 0.3 | 0.25 | 0.35 | 0.4 | 0.35 |
| quickAttackSpeedMult | 1.5 | 2 | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 |
| attackRecoveryTime | 0.15 | 0.1 | 0.3 | 0.2 | 0.1 | 0.2 | 0.3 | 0.1 |
| attackCooldownTime | 0.41 | 0.41 | 0.5 | 0.39 | 0.3 | 0.45 | 0.45 | 0.5 |
| quickAttackCooldownTime | 0.205 | 0.205 | 0.25 | 0.195 | 0.15 | 0.225 | 0.2375 | 0.25 |
| canTurnWhileSlashing | False | False | False | False | False | False | False | True |
| chargeSlashRecoils | True | False | False | False | False | False | False | True |
| chargeSlashLungeSpeed | 0 | 0 | 0 | 0 | 0 | -40 | 0 | 0 |
| chargeSlashLungeDeceleration | 1 | 1 | 1 | 1 | 1 | 0.9 | 1 | 1 |
| chargeSlashChain | 0 | 0 | 0 | 0 | 0 | 0 | 3 | 0 |
| wallSlashSlowdown | True | True | True | True | False | True | False | True |

Warrior-only (`HeroControllerConfigWarrior`, used while in rage mode):

| Field | Value | Replaces (normal value) |
|---|---|---|
| rageAttackDuration | 0.3 | attackDuration (0.3) |
| rageAttackRecoveryTime | 0.2 | attackRecoveryTime (0.2) |
| rageAttackCooldownTime | 0.32 | attackCooldownTime (0.39) |
| rageQuickAttackCooldownTime | 0.195 | quickAttackCooldownTime (0.195) |

---

## 4. Upgrades, equipping, save data

### 4.1 Upgrade chain

- Only chain in the data: `Hunter` → `Hunter_v2` → `Hunter_v3` (`previousVersion` of v2 is `Hunter`, of v3 is `Hunter_v2`). `upgradedVersion` is the runtime back-link.
- `ToolCrest.Unlock()` (also `Get()`), when the crest is not yet unlocked:
  - With a `previousVersion`: unlocks the previous version first if needed, then creates save data `{ IsUnlocked = true, Slots = copy of previousVersion.SaveData.Slots, DisplayNewIndicator = true }` (equipped tools and slot-unlock flags carry over). If the previous version is currently equipped, `ToolItemManager.SetEquippedCrest(name)` switches to the new version.
  - Base version: `{ IsUnlocked = true, Slots = one SlotData per SlotInfo with IsUnlocked = !IsLocked, DisplayNewIndicator = true }`.
  - Then `ToolItemManager.ReportCrestUnlocked(IsBaseVersion)` (achievements `FIRST_CREST` and `ALL_CRESTS` progress only for base versions; the "all crests" max counts non-hidden base crests) and `InventoryPaneList.SetNextOpen("Tools")`.
- A crest whose upgraded version is unlocked is not visible (`IsVisible` false), so only the highest unlocked Hunter version appears in the inventory.
- `GameManager.StartAct3()` auto-equips `Hunter_v3` if unlocked, else `Hunter_v2` if unlocked, else `Hunter` (`AutoEquip(crest, markTemp: false, removeTools: true)`), clears `ExtraToolEquips` and auto-equips `Silk Spear`.

### 4.2 Equipping

- `ToolItemManager.SetEquippedCrest(crestId)`: empty ID → first crest in `crestList`. Clears from the target crest's saved slots any tool also equipped in an extra (floating) slot. If the ID differs from the current one: `PreviousCrestID = CurrentCrestID`, `CurrentCrestID = crestId`, `IsCurrentCrestTemp = false`, clears the tool cache, updates the tool cap, updates `IsCursed` from `cursedCrest.IsEquipped`, and queues the equips-changed event (`queueEquipsChanged = true`).
- Inventory crest selection calls `SetEquippedCrest(crest.gameObject.name)` (`InventoryToolCrestList`).
- `ToolItemManager.AutoEquip(ToolCrest crest, bool markTemp, bool removeTools)`: null crest → `PreviousCrestID` crest, else first visible crest. Records `PreviousCrestID`, equips, sets `IsCurrentCrestTemp = markTemp`, optionally transfers/removes tools, then `SendEquippedChangedEvent()`.
- `SendEquippedChangedEvent(force)` (only when queued or forced): refreshes equipped state and silk, sends `TOOL EQUIPS CHANGED` (EventRegister), invokes `OnEquippedStateChanged`, sends `POST TOOL EQUIPS CHANGED`.
- `HeroController` subscribes `ResetAllCrestState` to `TOOL EQUIPS CHANGED`; that re-reads the crest config and resets crest runtime state (Warrior, Reaper, Wanderer always; Hunter meter unless `ToolItemManager.IsChangingActiveState`).
- `ResetAllCrestStateMinimal(clearHunterState)`: `ResetWarriorCrestState()`, `ResetReaperCrestState()`, `ResetHunterUpgCrestState()` only if `clearHunterState`, `ResetWandererCrestState()`.
- Temporary crests: `PlayerData.OnBeforeSave()` restores `PreviousCrestID` when `IsCurrentCrestTemp` (and swaps back to `Cloakless` in `Slab_16` before `slab_cloak_battle_completed`), then forces the equips-changed event.
- `GameManager.FixUpSaveState()`: if `slab_cloak_battle_completed` and the current crest is `Cloakless`, restores `PreviousCrestID` (or, if empty, resets the previous crest's non-skill tools and equips the first visible crest, falling back to `"Hunter"`), then `hero_ctrl.ResetAllCrestState()`.

### 4.3 Save data

`PlayerData` fields:

| Field | Type | Notes |
|---|---|---|
| `CurrentCrestID` | string | `[DefaultValue("Hunter")]`. |
| `PreviousCrestID` | string | Set on every crest change. |
| `IsCurrentCrestTemp` | bool | `[NonSerialized]`. |
| `ToolEquips` | `ToolCrestsData` | Per-crest data keyed by crest name. New game: `ToolEquips.SetData("Hunter", { IsUnlocked = true })` (Slots null). |
| `UnlockedExtraBlueSlot`, `UnlockedExtraYellowSlot` | bool | Floating slots. |
| `ExtraToolEquips` | `FloatingCrestSlotsData` | Named list of `ToolCrestsData.SlotData`. |

`ToolCrestsData` (`SerializableNamedList<Data, NamedData>`):
- `Data { bool IsUnlocked; List<SlotData> Slots; bool DisplayNewIndicator; }`
- `SlotData { string EquippedTool; bool IsUnlocked; }`

Crest runtime state (`HeroController.warriorState`, `reaperState`, `hunterUpgState`, `WandererState`) is not stored in `PlayerData`.

---

## 5. Source references

Decompiled (`scratchpad/decomp/`):
- `ToolCrest.cs` – fields, `SlotInfo`, `IsVisible`, `Unlock()`, `OnValidate()`.
- `ToolCrestList.cs`, `ToolCrestsData.cs`, `FloatingCrestSlotsData.cs`, `ToolBase.cs`.
- `ToolItemType.cs`, `ToolItemTypeExtensions.cs`, `AttackToolBinding.cs`.
- `ToolItemManager.cs` – `GetAllCrests` (l.332), `GetCrestByName` (l.341), `SetEquippedCrest` (l.461), `SendEquippedChangedEvent` (l.513), `GetBoundAttackTool` (l.768), `GetToolForBinding` (l.873), `AutoEquip` (l.1246), `ResetPreviousCrest` (l.1378), `SetActiveState` (l.1433), `ReportCrestUnlocked` (l.1501).
- `HeroControllerConfig.cs`, `HeroControllerConfigWarrior.cs`.
- `HeroController.cs` – `ConfigGroup` (l.15), `configs`/`specialConfigs` (l.1261–1264), `crestConfig` (l.1350), `Awake` (l.1618), `UpdateConfig` (l.1801), `SetConfigGroup` (l.1834), `Attack` (l.4092), `DidAttack` (l.4324), `IsHunterCrestEquipped` (l.4998), `IsArchitectCrestEquipped` (l.5007), `ResetAllCrestStateMinimal` / `ResetAllCrestState` (l.5947–5972).
- `HeroAnimationController.cs` – `SetHeroControllerConfig` (l.245), `GetClip` (l.1518).
- `InventoryToolCrestSlot.cs` (`IsLocked` l.291, `UnlockHoldRoutine` l.986), `InventoryItemToolManager.cs` (`CanUnlockSlot` l.276), `InventoryToolCrestList.cs` (l.406).
- `HutongGames.PlayMaker.Actions/GetHeroConfigVariable.cs`, `CheckHeroConfig.cs`, `GetHeroAttackObject.cs`, `CountCrestUnlockPoints.cs`.
- `PlayerData.cs` – crest fields (l.2739–2753), new-game setup (l.3882), `OnBeforeSave` (l.3972).
- `GameManager.cs` – `StartAct3` (l.2165), `FixUpSaveState` (l.2341).
- `HeroSlabCapture.cs`.
- `GlobalSettings/Gameplay.cs` – crest references.

Dumps (`raw-dump/`):
- `crests.txt` – crest list order, slots, heroConfig values, override animation clips.
- `hero-configs.txt` – `configs[0..7]`, `specialConfigs[0]`, Default config values, attack object names.
- `hero-controller.txt` – `configs` (8), `specialConfigs` (1), `crestConfig = null` with `CurrentCrestID = "Hunter_v3"`, `ALT_ATTACK_RESET = 0.5`.
- `gameplay-settings.txt` – crest references in `Gameplay`.
- `tools.txt` – tool type counts.
- `hero-animations.txt` – base `Knight` library.
- `fsm_Hero_Hornet(Clone)_Sprint.txt`, `_Nail Arts.txt`, `_Bind.txt` – `GetHeroConfigVariable` / `CanBind` usage.
