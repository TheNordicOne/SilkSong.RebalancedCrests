# Hunter Crest (Hunter, Hunter_v2, Hunter_v3)

The Hunter crest exists as three separate `ToolCrest` assets that form an upgrade chain. All three use the same moveset. The only difference between them is the combo meter that `Hunter_v2` and `Hunter_v3` have (section 3). General crest mechanics are covered in [../crest-system.md](../crest-system.md).

---

## 1. Assets, slots, config

| Crest ID | `isHidden` | `previousVersion` | `heroConfig` | Gameplay field |
|---|---|---|---|---|
| `Hunter` | False | null | null | `Gameplay.hunterCrest` |
| `Hunter_v2` | False | `Hunter` | null | `Gameplay.hunterCrest2` |
| `Hunter_v3` | False | `Hunter_v2` | null | `Gameplay.hunterCrest3` |

- New game: `PlayerData.CurrentCrestID` defaults to `"Hunter"` and `ToolEquips` starts with `"Hunter"` unlocked.
- Unlocking `Hunter_v2`/`Hunter_v3` copies the previous version's slot save data (equipped tools and unlocked flags). If the previous version is equipped, the game switches to the new version automatically (`ToolCrest.Unlock`). Once an upgrade is unlocked, the lower version is no longer visible in the inventory.
- `HeroController.IsHunterCrestEquipped()` returns true for any of the three IDs.
- `GameManager.StartAct3()` equips the highest unlocked Hunter version.

### 1.1 Slots (identical for all three versions)

| Slot | Type | Locked | AttackBinding |
|---|---|---|---|
| 0 | Red | False | Up |
| 1 | Blue | False | Neutral (unused for Blue) |
| 2 | Yellow | False | Neutral (unused for Yellow) |
| 3 | Skill | False | Neutral |
| 4 | Red | True | Down |
| 5 | Blue | True | Neutral (unused for Blue) |
| 6 | Yellow | True | Neutral (unused for Yellow) |

Totals: 7 slots: Red 2 (1 locked), Blue 2 (1 locked), Yellow 2 (1 locked), Skill 1 (0 locked). 4 are open from the start and 3 are locked.

Attack bindings: Neutral → the Skill slot (3); Up → Red slot 0; Down → Red slot 4 (after it is unlocked). If the Up or Down binding has no tool, that input uses the Neutral (Skill) tool (`GetBoundAttackTool`, `ToolEquippedReadSource.Active`).

### 1.2 Config: `null` → `Default`

All three crests have `heroConfig = null`. In `HeroController.ResetAllCrestState` this sets `crestConfig = null`, and `UpdateConfig()` then calls `SetConfigGroup(configs[0], null)`. As a result:

- `HeroController.Config` is the `Default` asset (instanceId 73424). The dump confirms `crestConfig = null` with `CurrentCrestID = "Hunter_v3"`.
- The active attack objects are those under the `Default` ActiveRoot. No `specialConfigs` override applies.
- `Default.heroAnimOverrideLib = null`, so every animation clip comes from the base `Knight` library (or the windy library in wind/updraft).
- All three versions share the same values, attack objects and animations. Nothing in `HeroControllerConfig` tells them apart.

`Default` config values:

| Field | Value |
|---|---|
| canPlayNeedolin / canBrolly / canDoubleJump / canNailCharge / canBind / canHarpoonDash | True / True / True / True / True / True |
| forceBareInventory | False |
| downSlashType | DownSpike |
| downSlashEvent | "" |
| downspikeAnticTime | 0.1 |
| downspikeTime | 0.1 |
| downspikeSpeed | 26 |
| downspikeRecoveryTime | 0.25 |
| downspikeBurstEffect | True |
| downspikeThrusts | True |
| dashStabSpeed | -50 |
| dashStabTime | 0.12 |
| forceShortDashStabBounce | False |
| dashStabBounceJumpSpeed | 18.6 |
| dashStabSteps | 1 |
| attackDuration | 0.35 |
| quickAttackSpeedMult | 1.5 |
| attackRecoveryTime | 0.15 |
| attackCooldownTime | 0.41 |
| quickAttackCooldownTime | 0.205 |
| canTurnWhileSlashing | False |
| chargeSlashRecoils | True |
| chargeSlashLungeSpeed | 0 |
| chargeSlashLungeDeceleration | 1 |
| chargeSlashChain | 0 |
| wallSlashSlowdown | True |

`Default` attack objects (configs[0], `hero-configs.txt`):

| Slot in ConfigGroup | Object | Components of note | DamageEnemies: attackType / useNailDamage / nailDamageMultiplier / silkGeneration |
|---|---|---|---|
| NormalSlash | `Slash` | NailSlash, DamageEnemies, NailSlashRecoil | Nail / True / 1 / Full |
| AlternateSlash | `AltSlash` | NailSlash, DamageEnemies, NailSlashRecoil | Nail / True / 1 / Full |
| UpSlash | `UpSlash` | NailSlash, DamageEnemies, NailSlashRecoil | Nail / True / 1 / Full |
| AltUpSlash | null | | |
| DownSlash | `DownSlash` | Downspike, DamageEnemies, HeroDownAttack | Nail / True / 1 / Full |
| AltDownSlash | null | | |
| WallSlash | `WallSlash` | NailSlash, DamageEnemies, NailSlashRecoil | Nail / True / 1 / Full |
| DashStab | `Dash Stab` | DamageEnemies, DashStabNailAttack | Nail / True / 1 / Full |
| DashStabAlt | null | | |
| ChargeSlash | `Charge Slash Basic` | HeroExtraNailSlash; children `slash 01`, `slash 02` with DamageEnemies | Heavy / True / 1.4 / Full (both children) |
| TauntSlash | `Taunt Slash` | DamageEnemies | Nail / True / 0.5 / Full |

All listed damagers have `damageDealt = 5` (not used when `useNailDamage = True`), `useHeroDamageAffectors = False`, `damageMultPerHit` empty and `multiHitter = False`.

---

## 2. Moveset summary

The Hunter crests use the `Default` moveset. The specific mechanics are covered in:

- Ground/air/up slashes, the alternating slash, wall slash and the `DownSpike`-type down attack (pogo): [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md)
- Sprint and the dash attack (`Dash Stab`, `dashStabSpeed` -50, `dashStabTime` 0.12). The `Sprint` FSM also activates a `Hunter SprintAttackBurst` effect object: [../mechanics/sprint.md](../mechanics/sprint.md)
- Needle Strike / charged attack (`Charge Slash Basic`, `chargeSlashRecoils` True, `chargeSlashChain` 0): [../mechanics/needle-strike.md](../mechanics/needle-strike.md)

The base `Hunter` crest has no crest-specific runtime state or effect. None of the dumped FSMs check for any Hunter crest asset.

---

## 3. Hunter_v2 / Hunter_v3 combo meter

### 3.1 State

```csharp
public struct HunterUpgCrestStateInfo
{
    public int CurrentMeterHits;
    public bool IsComboMeterAboveExtra => CurrentMeterHits >= Gameplay.HunterCombo2Hits + Gameplay.HunterCombo2ExtraHits;
}
```

- The state is stored in `HeroController.hunterUpgState` and exposed read-only as `HeroController.HunterUpgState`. It is runtime-only and is not saved in `PlayerData`.
- `ResetHunterUpgCrestState()` sets it to `default` (`CurrentMeterHits = 0`).
- `CurrentMeterHits` has no upper cap. It keeps counting past every threshold.

### 3.2 Settings (`GlobalSettings.Gameplay`)

| Setting | Value | Used by |
|---|---|---|
| `hunterComboHits` | 6 | Hunter_v2 threshold (damage) and v2 HUD bar size |
| `hunterComboDamageMult` | 1.3 | Hunter_v2 multiplier |
| `hunterCombo2Hits` | 6 | Hunter_v3 first threshold and v3 HUD bar A size |
| `hunterCombo2DamageMult` | 1.3 | Hunter_v3 first-stage multiplier |
| `hunterCombo2ExtraHits` | 6 | Hunter_v3 additional hits for the second stage and v3 HUD bar B size |
| `hunterCombo2ExtraDamageMult` | 1.5 | Hunter_v3 second-stage multiplier |
| `hunterCombo2DamageExtraScale` | (1.30, 1.30) | Scale of the combo hit effect in the v3 second stage |
| `hunterComboDamageEffect` | `hunter_combo_damage_effect` (GameObject) | Effect spawned on boosted hits |

The thresholds are cumulative. The v3 second stage needs `CurrentMeterHits >= hunterCombo2Hits + hunterCombo2ExtraHits` = 6 + 6 = **12**.

### 3.3 What increments the meter

The meter is increased in `HeroController.NailHitEnemy(HealthManager enemyHealth, HitInstance hitInstance)`:

```csharp
if (cState.isMaggoted) return;
if (hitInstance.RageHit) { ... Warrior ... }
else if ((Gameplay.HunterCrest2.IsEquipped || Gameplay.HunterCrest3.IsEquipped)
         && hitInstance.SilkGeneration != HitSilkGeneration.None
         && !enemyHealth.DoNotGiveSilk)
{
    hunterUpgState.CurrentMeterHits++;
}
```

`NailHitEnemy` is called from `HealthManager.TakeDamage` (switch on `hitInstance.AttackType`) for:
- `AttackTypes.Nail`
- `AttackTypes.NailBeam`
- `AttackTypes.Heavy` when `DamageEnemies.IsNailAttackObject(hitInstance.Source)` is true, meaning the source is tagged `"Nail Attack"` or has a `HeroExtraNailSlash` parent. `Charge Slash Basic` has `HeroExtraNailSlash`, so its `Heavy` hits count.

Every call adds exactly +1. All conditions together:

| Condition | Effect |
|---|---|
| Equipped crest is `Hunter_v2` or `Hunter_v3` | Required. `Hunter` (v1) never increments. |
| Hero is maggoted (`cState.isMaggoted`) | No increment. |
| `hitInstance.RageHit` (Warrior rage) | Takes the Warrior branch instead (not reachable with a Hunter crest). |
| `hitInstance.SilkGeneration == None` | No increment. `Full` and `FirstHit` both count. With `FirstHit`, non-first hits also count: the meter does not check `IsFirstHit`. |
| `enemyHealth.DoNotGiveSilk` | No increment. |
| Enemy type `Shade` / `Armoured` | Still increments. That check applies only to silk gain, not to the meter. |
| Hit dealt 0 damage (weak hit) | Still increments if the hit reached `TakeDamage`. `HealthManager.Hit` rejects hits with `DamageDealt <= 0` unless `CanWeakHit` or `LagHit`. |
| Hit blocked by direction (`IsBlockingByDirection` → `Invincible`) | No increment (`TakeDamage` not called). |
| Enemy immune (`IsImmuneTo`), dead, or evading (`evasionByHitRemaining > 0`) | No increment. |
| Amount of damage dealt | Irrelevant. Each counted hit is +1 no matter how much damage it dealt. |
| One swing hitting several enemies | +1 per enemy `HealthManager` that runs `TakeDamage`. Hits redirected through `SendDamageTo` count on the target `HealthManager`. |
| Repeated hits from one attack on one enemy | +1 per `TakeDamage` call. How often an attack can hit the same target depends on the attack's `DamageEnemies` (not specific to Hunter). |
| Tool / skill attacks (`Spell`, `Generic`, `Explosion`, etc.) | No increment (not routed to `NailHitEnemy`). |

### 3.4 Damage multiplier

Applied in `DamageEnemies.DoDamage` while the hit is being built, before `HealthManager.TakeDamage`. The hit is therefore evaluated with the meter value from before it adds its own +1.

```csharp
if (useNailDamage || useHeroDamageAffectors)
{
    ...
    if (attackType == AttackTypes.Nail || attackType == AttackTypes.NailBeam || isNailAttack)
    {
        if (hunterUpgState.CurrentMeterHits > 0)
        {
            if (Gameplay.HunterCrest2.IsEquipped)
            {
                if (CurrentMeterHits >= Gameplay.HunterComboHits)        // >= 6
                    { AddMultiplier(HunterComboDamageMult); hunterCombo = true; }      // 1.3
            }
            else if (Gameplay.HunterCrest3.IsEquipped)
            {
                if (IsComboMeterAboveExtra)                               // >= 12
                    { AddMultiplier(HunterCombo2ExtraDamageMult); hunterCombo = true; } // 1.5
                else if (CurrentMeterHits >= Gameplay.HunterCombo2Hits)  // >= 6
                    { AddMultiplier(HunterCombo2DamageMult); hunterCombo = true; }      // 1.3
            }
        }
        // silk taunt (x1.5), Barbed Wire (x1.25) follow
    }
}
```

Resulting values:

| Crest | Meter before the hit | Multiplier added | First boosted hit |
|---|---|---|---|
| Hunter | – | none | – |
| Hunter_v2 | 0–5 | none | |
| Hunter_v2 | ≥ 6 | 1.3 | 7th counted hit (meter already 6) |
| Hunter_v3 | 0–5 | none | |
| Hunter_v3 | 6–11 | 1.3 | 7th counted hit |
| Hunter_v3 | ≥ 12 | 1.5 (instead of 1.3, not stacked) | 13th counted hit |

Which attacks are boosted:
- Only damagers with `useNailDamage` or `useHeroDamageAffectors` set.
- Of those, only when `attackType` is `Nail` or `NailBeam`, or `isNailAttack` is true (`isNailAttack` = the GameObject has tag `"Nail Attack"`, set in `DamageEnemies.Start`).
- `Default` objects `Slash`, `AltSlash`, `UpSlash`, `DownSlash`, `WallSlash`, `Dash Stab`, `Taunt Slash` are `Nail` + `useNailDamage`, so they are boosted.
- `Charge Slash Basic` children (`slash 01`, `slash 02`) are `Heavy`. They are boosted only if they carry the `"Nail Attack"` tag. The dump does not include GameObject tags, and its `isNailAttack = False` was captured on inactive objects before `Start`, so this **cannot be determined from the dumped data**. Their hits do increment the meter (section 3.3).
- Tools and silk skills are not boosted (they use the `representingTool` branch).

How the multiplier combines (`DamageStack.PopDamage`):
- Multipliers are additive on the base value. Each multiplier `m` adds `base × (m − 1)`, then the offset is added and the result is rounded (`Mathf.RoundToInt`).
- Example: v3 at ≥ 12 with Barbed Wire equipped: `base × (1 + 0.5 + 0.25)` = `base × 1.75` (not `1.5 × 1.25`).
- `base` = `RoundToInt(PlayerData.nailDamage × nailDamageMultiplier)`. NailImbuement, Warrior rage, silk taunt (1.5), Barbed Wire (`barbedWireDamageDealtMultiplier` 1.25) and the damager's `DamageMultiplier` are added to the same stack.
- `damageMultPerHit` and `HealthManager.ApplyDamageScaling` (needle upgrade level) are applied multiplicatively afterwards.

### 3.5 Combo hit effect

`hitInstance.HunterCombo = hunterCombo` is set on every boosted hit. In `HealthManager.TakeDamage`, if `hitInstance.HunterCombo`:
- `Gameplay.HunterComboDamageEffect` (`hunter_combo_damage_effect`) is spawned at `TransformPoint(effectOrigin)`.
- Rotation depends on the cardinal hit direction: 2 → 180°, 0 → 0°, 1/3 → 90°.
- Scale = the prefab's `localScale`. If `Hunter_v3` is equipped and `IsComboMeterAboveExtra` (≥ 12), x and y are multiplied by `hunterCombo2DamageExtraScale` (1.30, 1.30).

The effect is visual only. The damage change happens in `DamageEnemies`.

### 3.6 Where the meter is reset

`ResetHunterUpgCrestState()` is called directly in two places and indirectly through `ResetAllCrestStateMinimal(clearHunterState: true)`.

`ResetAllCrestState()` (no argument) passes `clearHunterState = !ToolItemManager.IsChangingActiveState`. It therefore clears the meter except during `ToolItemManager.SetActiveState` (cutscene ↔ active switches), which raises `TOOL EQUIPS CHANGED` while `IsChangingActiveState = true`.

| Trigger | Code path | Clears meter? |
|---|---|---|
| Hero takes damage (enemy, explosion, hazard such as spikes/acid/lava/coal/zap/steam, etc.) | `HeroController.TakeDamage` → inside `if (damageAmount > 0)` after all reductions → `ResetHunterUpgCrestState()` | Yes, if the final `damageAmount > 0`. This happens even when health is not actually removed (`takeNoDamage`, `PreventDeath` cheat, `HeroKeepHealth`). |
| Damage reduced to 0 | Lucky Dice (Magnetite Dice) shield, Bell bind hit (`WillDoBellBindHit`), `RESPAWN_PIT` hazard (`damageAmount = 0`) | No |
| Damage avoided before the damage block | Parry, Scuttlebrace evade, whip-lash vs layer 11, downspike invulnerability, invulnerability/i-frames, `HAZARD_ONLY` mode, shadow dash, spikes while down-travelling with TinkEffect, hazard death loop (≤ 0.5 s after hazard respawn, re-routes to respawn) | No |
| Special damage | `DoSpecialDamage` (from `TakeQuickDamage`, `TakeFrostDamage`, `TakeChompDamage`, `ApplyTagDamage` for damage tags) → `ResetHunterUpgCrestState()` when not `takeNoDamage`, invincibility cheat off, tools not in `Cutscene` state, not transitioning | Yes (no `damageAmount > 0` check here). `TakeQuickDamage` is skipped within 0.5 s of a hazard respawn. |
| Any `TOOL EQUIPS CHANGED` event | `HeroController` subscribes `ResetAllCrestState` (registered in `Awake`) | Yes (unless `IsChangingActiveState`) |
| … crest change | `SetEquippedCrest` queues the event; `AutoEquip` sends it | Yes |
| … tool equip changes | `SetEquippedTools` / `SetExtraEquippedTool` / `AutoEquip(ToolItem)` / `RemoveToolFromAllCrests` / `ResetPreviousCrest` (via `SendEquippedChangedEvent`) | Yes, when the event is sent |
| … tool replenish | `ToolItemManager.TryReplenishTools(doReplenish: true, …)` → `SendEquippedChangedEvent(force: true)`. In the dumped FSMs the only `DoReplenish = True` call is in the `Bind` FSM state `Quick Craft Burst`, which is only reached through the `Quick Craft?` state (`CheckIfCrestEquipped Toolmaster`, otherwise FALSE). Other FSMs that call it (e.g. benches) are not in the dump. | Yes |
| … `PlayerData.ClearToolAmountsOverride`, `PlayerData.OnBeforeSave` (when it restores a temp crest), collecting a tool pouch (`PlayerDataCollectable`), PlayMaker `SendToolEquipChanged` | `SendEquippedChangedEvent` | Yes |
| … `ToolItemManager.SetActiveState` (entering/leaving tool cutscene state) | `SendEquippedChangedEvent(force: true)` with `IsChangingActiveState = true` | **No** |
| Death | `HeroController.Die` → `EventRegister.SendEvent("TOOL EQUIPS CHANGED")` → `ResetAllCrestState()`. The lethal hit itself has already reset the meter through `TakeDamage`. | Yes |
| Respawn after death | `HeroController.Respawn` (`wasDead`) → `MaxHealth()` → `ResetAllCrestState()` | Yes |
| `HeroController.MaxHealth()` in general | `ResetAllCrestState()`. Known callers: `Respawn`, `HeroSlabCapture.ApplyCaptured`, `BossSequenceController` (l.431). Callers through reflection-based FSM actions cannot be ruled out from the dumps. | Yes |
| `MaxHealthKeepBlue()` (memory scene entry, `HeroControllerMethods`) | does not reset crest state | No |
| Becoming maggoted | `SetIsMaggoted(true)` → `ResetAllCrestState()` | Yes |
| Clearing effects | `ClearEffects` / `ClearEffectsLite` / `ClearEffectsInstant` → `ResetAllCrestState(clearHunterState: false)` (e.g. `Respawn`, leaving a memory scene, PlayMaker `ClearHeroEffects*`) | **No** |
| Game state `CUTSCENE` | `HeroController.Update` → `ResetAllCrestStateMinimal(clearHunterState: false)` every frame | **No** |
| HeroController creation (game load) | `Awake` → `ResetAllCrestState()` | Yes (the meter also starts at 0 because the state is not saved) |
| `GameManager.FixUpSaveState` (Cloakless restore case only) | `hero_ctrl.ResetAllCrestState()` | Yes |

Things that do not reset the meter:
- Time: there is no decay or timer.
- Missing an attack, or hitting a non-enemy.
- Scene transitions: no transition code path calls these resets directly, only the triggers listed above.
- Binding (healing): the Bind FSM's only replenish/equip-change call (`Quick Craft Burst`) is gated to `Toolmaster`.

Unlocking `Hunter_v3` while `Hunter_v2` is equipped switches crests through `SetEquippedCrest`. That only queues `TOOL EQUIPS CHANGED`, so the meter is cleared when the queued event is next sent.

### 3.7 HUD (`BindOrbHudFrame`)

When the frame updates for the equipped crest: `Hunter` gets no custom routine; `Hunter_v2` runs `HunterCrestV2Routine`; `Hunter_v3` runs `HunterCrestV3Routine`. Both run `HunterCrestUpgradedRoutine(maxHits, extraMaxHits, bar, extraBar, fullAnimA, fullAnimB, idleAnim)`:

- v2: `(HunterComboHits = 6, 0, hunterV2Bar, null, hunterV2FullAnim, null, hunterV2FrameAnims.Idle)`
- v3: `(HunterCombo2Hits = 6, HunterCombo2ExtraHits = 6, hunterV3BarA, hunterV3BarB, hunterV3FullAnimA, hunterV3FullAnimB, hunterV3FrameAnims.Idle)`

Behaviour each unpaused frame (`hits = CurrentMeterHits`):

- Below `maxHits` (0–5): `bar.Value = hits / maxHits` when hits rose; `bar.SetValueInstant(0)` when hits fell. When dropping from full: plays `idleAnim`, re-shows the bar, and sets the extra bar to 0 instantly.
- Reaching `maxHits` (6): hides `bar`, plays `fullAnimA`, plays `hunterV2ChargedAudio` (the v3 HUD also uses this sound).
- v3 only, `hits > 6`: `hunterV3ExtraHitEffect` is re-triggered on every meter change. This continues past 12.
- v3, 6 ≤ hits < 12: shows `extraBar` with `extraBar.Value = (hits − 6) / 6`.
- v3, hits ≥ 12: hides `extraBar`, plays `fullAnimB`, plays `hunterV3ChargedAudio`.
- The v2 HUD has no second stage. Hits above 6 change nothing visible.

---

## 4. Source references

Decompiled (`scratchpad/decomp/`):
- `HeroController.cs`:
  - `HunterUpgCrestStateInfo` (l.217–222), `hunterUpgState` (l.1306), `HunterUpgState` (l.1484).
  - `Awake` registering `TOOL EQUIPS CHANGED` → `ResetAllCrestState` (l.1663–1664), `UpdateConfig` (l.1801), `SetConfigGroup` (l.1834).
  - `Update` cutscene minimal reset (l.2419), `SetIsMaggoted` (l.2576, reset l.2603).
  - `IsHunterCrestEquipped` (l.4998).
  - `TakeQuickDamage` (l.5137), `DoSpecialDamage` (l.5184, reset l.5207), `TakeDamage` (l.5280, reset l.5512).
  - `NailHitEnemy` (l.5880, increment l.5914–5916), `ResetAllCrestStateMinimal` (l.5947), `ResetAllCrestState` (l.5958/5963), `ResetHunterUpgCrestState` (l.6008).
  - `MaxHealth` (l.6321, reset l.6328), `Respawn` (l.7139), `Die` (l.9325, event l.9460), `ClearEffectsLite` (l.12057, l.12061).
- `DamageEnemies.cs` – `Start` sets `isNailAttack` (l.557), `IsNailAttackObject` (l.904), `DoDamage` hunter multipliers (l.973–997), `hitInstance.HunterCombo` (l.1159).
- `DamageStack.cs` – additive multiplier math.
- `HealthManager.cs` – `Hit` (l.779), `TakeDamage` (l.943), combo effect (l.1023–1050), `NailHitEnemy` call (l.1105–1109), silk gain conditions (l.1111).
- `BindOrbHudFrame.cs` – crest frame selection (l.292–333), `HunterCrestV2Routine`/`V3Routine` (l.716–724), `HunterCrestUpgradedRoutine` (l.726–813).
- `ToolItemManager.cs` – `IsChangingActiveState` (l.167), `SetEquippedCrest` (l.461), `SendEquippedChangedEvent` (l.513), `GetBoundAttackTool` (l.768), `TryReplenishTools` (l.946, event l.1102), `SetActiveState` (l.1433–1455).
- `ToolCrest.cs` – `Unlock()`.
- `GlobalSettings/Gameplay.cs` – hunter fields (l.97–127) and getters (l.520–538).
- `PlayerData.cs` (l.2739, l.3883), `GameManager.cs` (`StartAct3` l.2183, `FixUpSaveState` l.2341–2369), `HeroSlabCapture.cs`, `BossSequenceController.cs` (l.431).
- `HitSilkGeneration.cs` (`Full`, `FirstHit`, `None`), `AttackTypes.cs`.

Dumps (`raw-dump/`):
- `crests.txt` – Hunter slots, `heroConfig=null`, Gameplay hunter combo values.
- `gameplay-settings.txt` – `hunterCrest*`, `hunterCombo*`, `hunterComboDamageEffect`, `hunterCombo2DamageExtraScale`.
- `hero-configs.txt` – `configs[0]` Default values and attack objects (l.1–3002).
- `hero-controller.txt` – `crestConfig = null`, `CurrentCrestID = "Hunter_v3"`, `hunterUpgState`.
- `hero-animations.txt` – base `Knight` library.
- `fsm_Hero_Hornet(Clone)_Bind.txt` – `TryReplenishTools` in `Quick Craft Burst`.
- `fsm_Hero_Hornet(Clone)_Sprint.txt` – `Hunter SprintAttackBurst`.
