# Reaper Crest (Reaper)

The Reaper crest is a single `ToolCrest` asset (`Reaper`) with its own `HeroControllerConfig` (`Reaper`,
`configs[2]`, ActiveRoot `Attacks/Scythe`) and animation override library `Hornet CrestWeapon Scythe Anim`. It
has a custom down attack (`RPR DOWNSLASH`, `Crest Attacks` FSM), its own sprint attack and needle strike
branches, and a crest mode ("reaper mode") that starts on a completed bind and makes nail hits drop silk
bundles. General crest mechanics are covered in [../crest-system.md](../crest-system.md).

Conventions on this page:

- Times are measured as in [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md):
  "@60" is `seconds x 60`, tk2d frame `n` is reached `n / fps` seconds after the clip starts, and one physics
  step is `Time.fixedDeltaTime = 0.01999999 s` (50 Hz).
- **Hero-local coordinates.** Attack objects are children of `Hero_Hornet(Clone)/Attacks/Scythe` (and
  `Attacks/`), both at localPos (0, 0, 0), localScale (1, 1, 1), no rotation, so their transforms map directly
  into the hero's local space. The hero faces **left** when `transform.localScale.x > 0` and **right** when
  `< 0`. In hero-local space, **negative x is in front of Hornet and positive x is behind her**, for both
  facings. Hero-local points below are `localPos + R(localRot) x (scale x point)`, using the `NailSlash.scale`
  field (written to `localScale` on every `StartSlash`) where one exists.
- For reference, the hero hurtbox (`HeroBox`, localPos (0, 0, 0)) is 0.4554 wide, centred on x = 0
  (`HeroBox.HeroBoxNormal`: offset (0, -0.3799), size (0.4554, 2.2498) on the ground; offset (0, -0.14), size
  (0.4554, 1.77) in the air).

---

## 1. Assets, slots, config

| Crest ID | `isHidden` | `previousVersion` | `upgradedVersion` | `heroConfig` | Gameplay field |
|---|---|---|---|---|---|
| `Reaper` | False | null | null | `Reaper` (HeroControllerConfig, instanceId 65000) | `Gameplay.reaperCrest` |

Localised fields (`crests.txt`):

| Field | Value |
|---|---|
| `displayName` | `Tools/CREST_REAPER_NAME` → "Reaper" |
| `description` | `Tools/CREST_REAPER_DESC` → "Attack with heavy, arcing slashes and reap additional Silk from enemies." |
| `itemNamePrefix` | `Tools/UI_MSG_TITLE_CREST` → "Bound the Crest of" |
| `getPromptDesc` | `Tools/CREST_REAPER_DESC` (same text as `description`) |
| `equipText` | `Tools/UI_MSG_EQUIP_CREST` → "To change Crest, open the Crest screen of the Menu while resting at a bench." |
| `crestSprite` / `crestSilhouette` / `crestGlow` | Crest__0004_reaper / Crest__0000_reaper_sil / Crest__0001_reaper_equip_glow |
| `hasCustomAction` | False |

### 1.1 Slots

| Slot | Type | Locked | AttackBinding | Position |
|---|---|---|---|---|
| 0 | Skill | False | Neutral | (0.00, 0.02) |
| 1 | Red | False | Up | (0.00, 1.96) |
| 2 | Red | True | Down | (0.00, -1.96) |
| 3 | Blue | True | Down (unused for Blue) | (-1.91, 1.00) |
| 4 | Blue | False | Down (unused for Blue) | (-1.91, -0.99) |
| 5 | Yellow | True | Down (unused for Yellow) | (1.92, 1.00) |
| 6 | Yellow | False | Down (unused for Yellow) | (1.92, -0.99) |

Totals: 7 slots: Red 2 (1 locked), Blue 2 (1 locked), Yellow 2 (1 locked), Skill 1 (0 locked). 4 are open from
the start and 3 are locked. Attack bindings: Neutral → Skill slot 0; Up → Red slot 1; Down → Red slot 2 (after
it is unlocked). Unbound directions fall back to the Neutral tool (see the Hunter page / `GetBoundAttackTool`).

### 1.2 Config `Reaper` (`crests.txt`, `hero-configs.txt` configs[2])

| Field | Reaper | Default (Hunter), for comparison |
|---|---|---|
| heroAnimOverrideLib | Hornet CrestWeapon Scythe Anim | null |
| canPlayNeedolin / canBrolly / canDoubleJump / canNailCharge / canBind / canHarpoonDash | all True | all True |
| forceBareInventory | False | False |
| downSlashType | **Custom** | DownSpike |
| downSlashEvent | **"RPR DOWNSLASH"** | "" |
| downspikeAnticTime / downspikeTime / downspikeSpeed | 0.07 / 0.15 / 13 (not read on the Custom path) | 0.1 / 0.1 / 26 |
| downspikeRecoveryTime | 0.25 | 0.25 |
| downspikeBurstEffect / downspikeThrusts | False / False | True / True |
| dashStabSpeed / dashStabTime | -30 / 0.2 (not read by the Reaper sprint branch) | -50 / 0.12 |
| forceShortDashStabBounce / dashStabBounceJumpSpeed / dashStabSteps | True / 12 / 1 | False / 18.6 / 1 |
| attackDuration | 0.35 | 0.35 |
| quickAttackSpeedMult | 1.5 | 1.5 |
| attackRecoveryTime | **0.3** | 0.15 |
| attackCooldownTime | **0.5** | 0.41 |
| quickAttackCooldownTime | 0.25 (effective 0.35, floored at `attackDuration`) | 0.205 (effective 0.35) |
| canTurnWhileSlashing | False | False |
| chargeSlashRecoils | **False** | True |
| chargeSlashLungeSpeed / chargeSlashLungeDeceleration / chargeSlashChain | 0 / 1 / 0 | 0 / 1 / 0 |
| wallSlashSlowdown | True | True |

How `attackRecoveryTime`, `attackCooldownTime` and quickening apply is described in
[../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) section 1.5–1.6.

### 1.3 Attack objects (`hero-configs.txt` configs[2], `hero-hierarchy.txt`)

All paths are under `Hero_Hornet(Clone)/`. All objects below are tagged `Nail Attack` on layer `Attack`, have
`useNailDamage = True`, `damageDealt = 5` (replaced by nail damage), `useHeroDamageAffectors = False`,
`multiHitter = False`, `hitOnceUntilEnd = False`, `silkGeneration = Full`, `firstHitFreeze = None`,
`canTriggerBouncePod = False`.

| ConfigGroup slot | Object | Components of note | attackType / nailDamageMultiplier / stunDamage / direction (dump) / magnitudeMult / corpseMagnitudeMult override |
|---|---|---|---|
| NormalSlash | `Attacks/Scythe/Slash` | NailSlash (`SlashEffect`), DamageEnemies | Nail / 1 / 1 / 0 / **1.5** / 1.15 |
| AlternateSlash | `Attacks/Scythe/AltSlash` | NailSlash (`SlashEffectAlt`), DamageEnemies | Nail / 1 / 1 / 0 / **1.5** / 1.15 |
| UpSlash | `Attacks/Scythe/UpSlash` | NailSlash (`UpSlashEffect`), DamageEnemies | Nail / 1 / 1 / 0 / **1.25** / 1.15 |
| AltUpSlash | null | | |
| DownSlash | `Attacks/Scythe/DownSlash` | Downspike, DamageEnemies, HeroDownAttack. **Not used**: the Custom down attack uses `DownSlash New` (section 5) | Nail / 1 / 1 / 270 / 1 / – |
| AltDownSlash | null | | |
| WallSlash | `Attacks/Scythe/WallSlash` | NailSlash (`SlashEffect`), DamageEnemies | Nail / 1 / 1 / 0 / **1.5** / 1.15 |
| DashStab | `Attacks/Scythe/Dash Stab` | DashStabNailAttack (`waitForBounceTrigger = True`, `doEnemyHitRecoil = True`). **Not used** by the Reaper sprint branch (section 3) | Nail / 1 / 1 / 0 / 1 / 1.5 |
| DashStabAlt | null | | |
| ChargeSlash | `Attacks/Charge Slash Scythe` | HeroExtraNailSlash, EnemyDamagerGroup, KeepWorldPosition, DisableAfterTime 0.5 | – (children below) |
| ↳ | `Charge Slash Scythe/slash 01` | DamageEnemies, NailSlashTerrainThunk (`doRecoil = False`) | **Heavy / 2.5 / 2** / 180 / **2** / 1.5 |
| ↳ | `Charge Slash Scythe/slash 02` | same | **Heavy / 2.5 / 2** / 180 / **2** / 1.5 |
| ↳ | `Charge Slash Scythe/slash behind` | same | **Heavy / 2.5 / 2** / 0 / 1 / – |
| TauntSlash | `Taunt Slash` (shared) | DamageEnemies | Nail / 0.5 / 0.5 / 180 / 0 |

Objects used only by FSMs (not in the ConfigGroup):

| Object | Used by | attackType / nailDamageMultiplier / stunDamage / direction / magnitudeMult / corpse overrides |
|---|---|---|
| `Attacks/Scythe/DownSlash New` (NailSlash `v3 Down Slash Effect`, `dealtDamageFSM` = own `Dealt Damage Response`, event `"DEALT DAMAGE"`) | `Crest Attacks` FSM variable `Rpr DownSlash` | Nail / 1 / 1 / 270 / **1.3** / corpseDirection 270 |
| `Attacks/Scythe/DashUpper Slash` (NailSlash `Dash Upper Effect`) | `Sprint` FSM variable `Reaper Slash` | Nail / 1 / 1 / 180 / 1 / corpseDirection 90, corpseMagnitudeMult 1.25 |
| `Attacks/Scythe/DashUpper Slash/Extra Damager` (FSM `Collider Timing Control`) | activated by the NailSlash first trigger | Nail / 1 / 1 / 180 / 1 / corpseDirection 90, corpseMagnitudeMult 1.25 |

- Every Scythe `NailSlash` has `dontAddRecoiler = False`. `NailSlash.Start` therefore adds a `NailSlashRecoil`
  component at runtime (`NailSlashRecoil.Add`). The dump does not list it because `Attacks/Scythe` was inactive
  (`activeSelf = False`) when dumped. Hornet's own recoil on hit is the same as for the Default slashes
  ([../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) section 5.4).
- `isNailAttack` reads False in the dump for the same reason; all these objects are tagged `Nail Attack`, so it
  becomes True in `DamageEnemies.Start`.

---

## 2. Neutral, alternate, up and wall slash

The attack flow (`DoAttack` → `Attack(dir)` → `NailSlash.StartSlash`) is the generic one described in
[../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) section 1. The Reaper differences
are the config values (section 1.2), the clips, the hitboxes and the damager values.

### 2.1 Clips and hitbox timing

Effect clips (`Hornet CrestWeapon Scythe Anim`). The 1st animation trigger enables the polygon and the
`Clash Tink` polygon, the 2nd disables them and calls `EndDamage()`, clip completion calls `CancelAttack()`.
Quickening plays the effect clip at `fps x 1.5`.

| Object | Clip | fps / frames / triggers | Collider on | Collider off | Clip end | Quickened on / off |
|---|---|---|---|---|---|---|
| Slash, WallSlash | SlashEffect | 24 / 6 / [2, 5] | 0.0833 s (5 @60) | 0.2083 s (12.5 @60) | 0.25 s | 0.0556 / 0.1389 s |
| AltSlash | SlashEffectAlt | 24 / 6 / [2, 5] | 0.0833 s (5 @60) | 0.2083 s (12.5 @60) | 0.25 s | 0.0556 / 0.1389 s |
| UpSlash | UpSlashEffect | 24 / 7 / [2, 6] | 0.0833 s (5 @60) | 0.25 s (15 @60) | 0.2917 s | 0.0556 / 0.1667 s |

Default for comparison: Slash/AltSlash on 0 s, off 0.1 s; UpSlash on 0.0417 s, off 0.125 s.

Hero body clips (Scythe library): `Slash` fps 20, 7 frames (0.35 s); `SlashAlt` fps 20, 7 frames (0.35 s);
`UpSlash` fps 24, 8 frames (0.3333 s). The Scythe library has no `Wall Slash` clip, so the base `Knight` clip
is used.

### 2.2 Hitbox geometry (PolygonCollider2D, trigger, offset (0, 0), disabled at rest)

Raw values:

| Object | localPos | localRot | `NailSlash.scale` | Polygon points (object space) |
|---|---|---|---|---|
| Slash | (-0.4, 0, -0.001) | 0 | (1, 1, 1) | (0.654,2.173) (-0.682,1.962) (-2.132,1.304) (-3.126,0.074) (-3.196,-0.932) (0.434,-0.748) |
| AltSlash | (-0.8, 0.08, -0.001) | 0 | (1, 1, 1) | (0.398,0.297) (-0.514,0.431) (-2.869,0.388) (-2.77,-0.269) (-1.832,-1.134) (-0.485,-1.574) (0.694,-1.589) |
| UpSlash | (-0.02, 0.18, -0.001) | **270** | (0.92, 1, 1) | (1.439,2.14) (-0.623,1.933) (-1.899,1.318) (-2.988,0.254) (-3.342,-0.791) (-2.164,-0.471) (-0.344,-0.243) |
| WallSlash | (0.61, 0, -0.001) | 0 | (-1, 1, 1) | (0.456,2.219) (-1.337,1.702) (-2.66,0.696) (-3.321,-0.474) (-3.333,-0.831) (-0.166,-0.411) |

`Clash Tink` children (trigger = False, layer Tinker, `NailSlashTerrainThunk`):

| Parent | Clash Tink localScale | `doRecoil` | Points |
|---|---|---|---|
| Slash | (0.95, 0.9, 1) | True | (0.985,2.187) (-0.678,1.615) (-1.97,1.233) (-2.985,0.148) (-3.409,-0.723) (-3.342,-1.497) (0.876,-0.73) |
| AltSlash | (0.95, 0.9, 1) | True | (-0.904,0.995) (-3.357,1.41) (-3.191,0.415) (-1.961,-0.904) (-1.299,-1.269) (-0.474,-1.438) (0.949,-1.4) |
| UpSlash | (0.93, 0.95, 1) | True | (1.346,1.972) (-0.491,1.765) (-1.727,1.15) (-2.782,0.262) (-3.502,-1.114) (-3.192,-1.393) (-2.41,-1.109) (-0.284,-0.68) |
| WallSlash | (1, 1, 1) | False | (-0.22,1.678) (-2.687,1.561) (-3.741,0.541) (-4.274,-1.316) (-0.247,-1.242) |

Hero-local extents (negative x = in front, positive x = behind; no Long Needle):

| Hitbox | x range | y range | Area | Centroid (x, y) |
|---|---|---|---|---|
| Reaper Slash | -3.596 … +0.254 | -0.932 … +2.173 | 8.677 | (-1.476, 0.427) |
| Reaper AltSlash | -3.669 … -0.106 | -1.509 … +0.511 | 5.446 | (-1.726, -0.401) |
| **Reaper UpSlash** | **-0.811 … +2.120** | -1.144 … +3.255 | 6.198 | **(+0.695, 1.128)** |
| Reaper UpSlash `Clash Tink` | -1.343 … +1.853 | -0.972 … +3.176 | | |
| Default Slash | -3.170 … +0.424 | -1.429 … +1.231 | 7.018 | (-1.314, -0.196) |
| Default AltSlash | -3.167 … +0.158 | -1.329 … +0.957 | 5.903 | (-1.386, -0.200) |
| Default UpSlash | -1.063 … +1.476 | -0.939 … +3.188 | 8.657 | (+0.214, 0.958) |

Hero-local UpSlash polygon (Reaper): (2.12,-1.144) (1.913,0.753) (1.298,1.927) (0.234,2.929) (-0.811,3.255)
(-0.491,2.171) (-0.263,0.496).

**Up slash position relative to Hornet.** The Reaper up slash object is rotated 270°, which maps the
forward-pointing arc of the polygon onto an arc that starts behind and below Hornet and ends above and slightly
in front of her:

- It reaches 2.12 units behind the hero origin (at y = -1.144, below the origin) and 1.913 behind at y = 0.753.
- It reaches only 0.811 units in front, and only near its top (y = 3.255). At y ≈ 0.5 (the point (-0.263,
  0.496)) the front edge is 0.263 in front of the origin; the hurtbox front edge is at 0.2277.
- 79.7 % of its area (4.94 of 6.198) lies behind the hero origin (x > 0); the centroid is 0.695 behind and
  1.128 above the origin.
- Default UpSlash: 59.6 % of its area (5.163 of 8.657) is behind the origin, front reach 1.063, back reach
  1.476, centroid 0.214 behind.

Long Needle (`longNeedleMultiplier = (1.2, 1.075)`, swapped to (1.075, 1.2) while `cState.upAttacking`): no
Reaper slash overrides it (`overrideLongNeedleScale = False`). With Long Needle the Reaper UpSlash spans x
-0.969 … +2.548, y -1.243 … +3.485.

### 2.3 Knockback

The enemy recoil speed is `Recoil.recoilSpeedBase x HitInstance.MagnitudeMultiplier x RecoilMultiplier`
(`Recoil.RecoilByDirection`; `recoilSpeedBase` defaults to 15 and `recoilDuration` to 0.5 in `Recoil.Reset`,
both serialized per enemy). `MagnitudeMultiplier` is the damager's `magnitudeMult`, multiplied by
`NailImbuement.NailDamageMultiplier` when imbued, by `warriorDamageMultiplier` on rage hits and by
`flippedDirMagnitude` when the direction is flipped (`DamageEnemies.DoDamage`).

| Attack | Reaper `magnitudeMult` | Default / Hunter `magnitudeMult` |
|---|---|---|
| Slash / AltSlash | **1.5** | 1 |
| UpSlash | **1.25** | 1 |
| WallSlash | **1.5** | 1 |
| Down attack | **1.3** (`DownSlash New`) | 1 (`DownSlash`) |
| Sprint attack | 1 (`DashUpper Slash`, `Extra Damager`) | 1 (`Dash Stab`) |
| Needle strike | **2** (slash 01, slash 02), 1 (slash behind) | 0.5 (slash 01), 1 (slash 02) |

The Reaper slashes also enable `corpseMagnitudeMult` (1.15 for the four basic slashes, 1.25 for the sprint
attack, 1.5 for the needle strike front hitboxes). Default has it disabled on its basic slashes.

---

## 3. Sprint attack (`Sprint` FSM, `REAPER` branch)

Full state details: [../mechanics/sprint.md](../mechanics/sprint.md) section 4.1. Path from a ground sprint:

`Ground Sprint L/R` (ATTACK) → `Dash Stab Dir` → `Start Attack` → `REAPER` → `Reaper Antic` → `Reaper Upper`
→ `Reaper End` → `Regain Control Normal` → `Regain Control Shared`.

| t from input | State | What happens |
|---|---|---|
| 0 | `Dash Stab Dir` | `Attack Direction` = 0 (facing right) or 180 (facing left). Instant. |
| 0 | `Start Attack` | `In Attack = True`, `IncrementAttackCounter`, `isSprinting = False`, `SetAllowNailChargingWhileRelinquished(False)`, `CheckIfCrestEquipped Reaper` → `REAPER`. Instant. |
| 0 → 0.125 s | **`Reaper Antic`** | `Attack Normal Hornet Voice`; `Tk2dPlayAnimationWithEvents("Dash Upper Antic", complete → FINISHED)` (Scythe: fps 16, 2 frames = **0.125 s = 7.5 @60**); `DecelerateV2(0.65)` (x and y ×0.65 on entry and per physics step); `SetAllowRecoilWhileRelinquished(True)`. **This clip is the entire startup delay**; there is no `Wait` action. Gravity still applies. |
| 0.125 s | **`Reaper Upper`** entry | `hornet_reaper_dash_scrape`; `AffectedByGravity(False)`; `Reaper Slash.DamageEnemies.SetDirection(Attack Direction)`; `SendMessage StartSlash` → `DashUpper Slash` hitbox live immediately (trigger at frame 0 of `Dash Upper Effect`); `CameraTarget.SetSprint(False)`; `SetVelocityByScale -50` (50 forward); `onGround = False`; `DecelerateXY(0.85)` (x only); `Dash Upper` clip (fps 22, 6 frames = 0.2727 s); `CurveFloat Speed Y` (keys (0, 2.5), (0.35, 25), (1, 7), time 0.3) applied to y every frame. |
| 0.125 → 0.3068 s | | `DashUpper Slash` polygon live until its clip completes (4/22 = 0.1818 s). `Extra Damager` polygon live 0.1 → 0.1818 s after `StartSlash`. Quickened: slash ends 0.1212 s after start. |
| 0.3977 s | `Reaper End` | y velocity = 7; `SetStartFromReaperUpperslash` (`jump_steps = JUMP_STEPS`, plays `Dash Upper Recovery`, fps 15, 5 frames); `AffectedByGravity(True)`; `SetAllowRecoilWhileRelinquished(False)`. |
| 0.3977 s | `Regain Control Normal` | `StartAnimationControl`, then the shared regain-control states. |

No state of this branch listens for a hit event; the attack does not bounce off enemies. The config's
`Dash Stab` object and `dashStab*` values are not used.

`DashUpper Slash` geometry (localPos (0, 0, -0.001), `scale` (0.95, 0.9)), hero-local: main polygon x -3.345 …
-0.577, y -0.822 … +3.311 (entirely in front); `Extra Damager` polygon x -2.603 … +0.324, y -1.975 … -0.844.

---

## 4. Needle strike (`Nail Arts` FSM, `REAPER` branch, `Charge Slash Scythe`)

Full state details: [../mechanics/needle-strike.md](../mechanics/needle-strike.md) section 6 "Reaper".

- `Antic Type` → `REAPER` → **`Reaper Kickoff`**: `CheckIsCharacterGrounded` (RayCount 3, GroundDistance 0.2);
  if grounded, `SetVelocity2d y = 10`. → **`Antic Rpr`**: `hornet_reaper_nailart_start_on_flash`,
  `flashFocusHeal`, `Sphere Flash`, clip `Slash_Charged` (Scythe: fps 15, 10 frames, triggers [4, 8]),
  `DecelerateV2 0.85` with `brakeOnExit = True`, `AffectedByGravity(False)`. Leaves on the first trigger
  (**0.2667 s**) → `Lunge?` (FALSE) → `Slash Recoil?` → `Do Slash` (activates `Charge Slash Scythe`,
  `SetKeepWorldPosition(False)`, `DecelerateV2 0.92`) → trigger 2 (0.5333 s) → `Regain Partial Control`
  (no recoil, `chargeSlashRecoils = False`; re-enables `KeepWorldPosition`) → clip complete (0.6667 s) →
  `Regain Full Control`.
- `Charge Slash Scythe`: localPos (1.34, -0.7, -0.001), localScale (1.3, 1.3, 1). Animator clip
  `Hornet Charge Slash Scythe` (0.24 s, 25 fps, event `DoCameraShake` at 0 s). `DisableAfterTime 0.5`.
  `EnemyDamagerGroup` links the three damagers: a collider damaged by one of them is excluded for the other two
  (`PreventDamage`), so each collider is hit at most once per needle strike.

| Damager | Polygon (object space) | Hero-local x range | Hero-local y range | Active (clip time) | Active (from release) |
|---|---|---|---|---|---|
| slash 01 | (-0.881,3.154) (-0.184,2.843) (0.075,-0.259) (-1.931,0.552) (-2.675,2.755) | -2.138 … +1.438 | -1.037 … +3.400 | 0 → 0.05 s | 0.2667 → 0.3167 s |
| slash 02 | (-0.032,3.1) (0.075,-0.259) (-3.774,-1.4) (-4.46,0.351) (-3.968,2.318) (-1.757,3.406) | -4.458 … +1.438 | -2.520 … +3.728 | 0.05 → 0.2 s | 0.3167 → 0.4667 s |
| slash behind | (0.286,0.295) (0.272,2.83) (0.896,2.555) (1.712,0.911) (1.57,-0.925) | +1.694 … +3.566 | -1.903 … +2.979 | 0 → 0.0833 s | 0.2667 → 0.35 s |

- Damage: `attackType = Heavy`, `nailDamageMultiplier = 2.5` (base `RoundToInt(nailDamage x 2.5)`; Hunter
  `Charge Slash Basic` uses 1.4), `stunDamage = 2`, `silkGeneration = Full`. The children are tagged
  `Nail Attack` and have a `HeroExtraNailSlash` parent, so `HealthManager` treats their hits as nail hits
  (silk gain and reaper bundles, section 6.3).
- `slash 01`/`slash 02` direction 180, `magnitudeMult = 2`, corpseMagnitudeMult 1.5. `slash behind` direction
  0, `magnitudeMult = 1`.
- All three `NailSlashTerrainThunk` have `doRecoil = False`.

---

## 5. Down attack: `RPR DOWNSLASH` (`Crest Attacks` FSM)

`HeroController.DownAttack` (Custom type): `CancelQueuedBounces()`, `isSlashing = false`,
`crestAttacksFSM.SendEvent("RPR DOWNSLASH")`, then `DidAttack()` (`attack_cooldown` = 0.5, or 0.35 when
quickened). The FSM event is processed synchronously, so `Rpr Downslash Antic` is entered in the same frame as
the press. Preconditions (`CanDownAttack`, `downSpikeRecovery`, etc.) are in
[../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) sections 1–3.

Attack object: `Attacks/Scythe/DownSlash New` (`Crest Attacks` variable `Rpr DownSlash`, found by `Init`
`FindChild "Attacks/Scythe/DownSlash New"`). The config `DownSlash` (Downspike) object is not used.

### 5.1 States and actions

**`Rpr Downslash Antic`** (entered on `RPR DOWNSLASH` from `Idle`):

| # | Action | Values |
|---|---|---|
| 0 | SetBoolValue | `In Crest Attack = True` |
| 1 | SendMessage | **`RelinquishControlNotVelocity`** (control lock starts) |
| 2 | HeroControllerMethods | `StopAnimationControl`, every frame |
| 3 | SetBoolValue | `Disabled Animation = True` |
| 4 | SendMessage | `AffectedByGravity(False)` |
| 5 | Tk2dPlayAnimation | `Hornet CrestWeapon Scythe Anim` / **`v3 Down Slash Antic`** (fps 18, 3 frames = 0.1667 s) |
| 6 | Tk2dWatchAnimationEventsV3 | complete → `FINISHED`; interrupted (clip changed) → `STOP` (re-enters this state) |
| 7 | CallMethodProper | `HeroBox.HeroBoxReaperDownSlash()`: hurtbox offset (0.37, 0.6319103), size (0.4554, 1.601192) |
| 8 | DecelerateV2 | **0.85**, `brakeOnExit = False` (x and y ×0.85 on entry and every physics step) |
| 9 | DecelerateXY | disabled (`decelerationY = 0.8`) |
| 10 | UmbrellaMotionX | disabled |

**`Rpr Downslash`** (antic clip complete):

| # | Action | Values |
|---|---|---|
| 0 | SetVelocity2d | (0, 0) |
| 1 | SetCState | `downAttacking = True` |
| 2 | Tk2dPlayAnimation | **`v3 Down Slash`** (fps 24, 8 frames = 0.3333 s, triggers at frames 4 and 6) |
| 3 | Tk2dWatchAnimationEvents | trigger → `FINISHED` |
| 4 | SendMessage | **`StartSlash`** to `Rpr DownSlash` (`DownSlash New`) |
| 5 | DecelerateV2 | **0.7** (applied on entry to (0, 0), then every physics step) |
| 6 | SetVelocityByScale | **speed -30, ySpeed -30** → vx = 30 forward (−30 when `localScale.x > 0`, +30 otherwise), vy = −30 |

Transitions: `FINISHED` (hero clip trigger at frame 4) → `Rpr Downslash Fall`; `ATTACK LANDED`, `BOUNCE TINKED`,
`BOUNCE CANCEL` → `Rpr Downslash Bounce`; `THUNKED DOWN` → `Rpr Downslash Thunk`; `LEAVING SCENE` →
`Rpr Downslash End`.

**`Rpr Downslash Fall`**: `Tk2dWatchAnimationEvents` complete → `FINISHED`; `AffectedByGravity(True)`;
`ListenForDash` pressed → `SPRINT`. → `Rpr Downslash Fall 2` / `Rpr DashCancel`.

**`Rpr Downslash Fall 2`**: `HeroBox.HeroBoxNormal()`; `Tk2dWatchAnimationEvents` complete → `FINISHED` (the
clip has already completed, so `OnUpdate` sees `!Playing` and finishes on the next update); `ListenForDash` →
`SPRINT`. → `Rpr Downslash End` / `Rpr DashCancel`.

**`Rpr Downslash End`**: `SetCState downAttacking = False`; `HeroController.FinishDownspike(true)`. → `End`.

**`Rpr Downslash Thunk`**: `Tk2dWatchAnimationEvents` trigger → `FINISHED`; `SetVelocity2d y = 28` (disabled);
`DecelerateV2 0.715`. Transitions: `FINISHED` → `Rpr Downslash Fall`; `ATTACK LANDED` / `BOUNCE TINKED` /
`BOUNCE CANCEL` → `Rpr Downslash Bounce`; `LEAVING SCENE` → `Rpr Downslash End`.

**`Rpr Downslash Bounce`**: `GetEventSender`; **`HeroController.SetStartWithDownSpikeBounce()`**
(`startWithDownSpikeBounce = true`, `attack_cooldown = 0.15`); `SetCState downAttacking = False`. → `End`.

**`Rpr DashCancel`**: `SendMessage SetStartWithDash`. → `End`.

**`End`** (shared): `downAttacking = False`; **`RegainControl`**; `StartAnimationControlToIdle`;
`downTravelling = False`; `Disabled Animation = False`; `Attack Target = null`; `In Crest Attack = False`. → `Idle`.

Global transitions: `FSM CANCEL`, `HERO DAMAGED`, `LEVEL LOADED`, `CREST ATTACK CANCEL` → `Cancel`
(clears `downAttacking`/`downTravelling`, sends cancel messages to other crests' objects, then
`Re-enable animation` → `StartAnimationControlToIdle` if `Disabled Animation`) → `Idle`. `Cancel` does not call
`RegainControl`.

### 5.2 Timeline without a hit (from the down-attack press)

| t | @60 | Event |
|---|---|---|
| 0 | 0 | `Rpr Downslash Antic`: control relinquished, gravity off, `v3 Down Slash Antic` starts, hurtbox moved, velocity ×0.85 per physics step. |
| **0.1667 s** | **10** | Antic clip complete → `Rpr Downslash`: velocity zeroed then set to (±30 forward, −30), `StartSlash` → **hitbox active** (see 5.3). |
| 0.3333 s | 20 | Hero clip trigger (frame 4) → `Rpr Downslash Fall`: gravity on, dash cancel possible. `DownSlash New` effect clip completes at the same time → hitbox off. |
| 0.5 s | 30 | Hero clip complete (frame 8) → `Rpr Downslash Fall 2`: hurtbox normal. |
| 0.5 s + 1 frame | | `Rpr Downslash End` → `FinishDownspike(true)` → `End`: **control regained**. |
| then | | `downSpikeRecovery` for 0.25 s in the air (`downspikeRecoveryTime`) or 0.1667 s on the ground. |

- **Windup before the hitbox:** 0.1667 s (10 frames @60), set only by the hero clip `v3 Down Slash Antic`
  (fps 18, 3 frames) watched by `Tk2dWatchAnimationEventsV3` in `Rpr Downslash Antic`. There is no `Wait`
  action. During the windup the velocity decays ×0.85 per physics step with gravity off (after 0.1667 s ≈ 8
  steps, 0.85^8 = 27 % of the entry velocity remains).
- **Control lock:** from `RelinquishControlNotVelocity` in `Rpr Downslash Antic` (same frame as the press) until
  `RegainControl` (in `FinishDownspike` and in `End`). `RelinquishControlNotVelocity` calls `CancelDownSpike`,
  `ResetInput`, `ResetMotionNotVelocity`, sets `hero_state = no_input`, `IgnoreInput()` (input not accepted),
  `ResetLook`, `ResetAttacks`, `CancelJump`. Without a hit the lock lasts 0.5 s plus one frame. The only inputs
  read during the lock are the dash button (`ListenForDash` in `Rpr Downslash Fall` / `Fall 2`, from 0.3333 s).
- **Dash cancel:** `SPRINT` → `Rpr DashCancel` → `SetStartWithDash` → `End` → `RegainControl` (which starts the
  dash). This path does not call `FinishDownspike`, so no `downSpikeRecovery` follows.
- `FinishDownspike(true)`: `downSpiking = false`, `downSpikeRecoveryTimer = 0`, and (when not floating)
  `downSpikeRecovery = true`, `ResetGravity()`, `RegainControl()`, `crestAttacksFSM.SendEvent("CREST ATTACK
  CANCEL")`, `vy = -10` (unless `startWithBalloonBounce`). Effects of `downSpikeRecovery` (no new down attack, no
  jump, no ground movement input) are listed in
  [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) section 3.2.

### 5.3 Diagonal movement

| Phase | Velocity source | Values |
|---|---|---|
| `Rpr Downslash Antic` (0 → 0.1667 s) | `DecelerateV2 0.85`, gravity off | entry velocity ×0.85 per physics step |
| `Rpr Downslash` (0.1667 → 0.3333 s) | `SetVelocity2d (0,0)`, `SetVelocityByScale (-30, -30)`, `DecelerateV2 0.7` | 30 forward and 30 down on entry, both ×0.7 per physics step, gravity off |
| `Rpr Downslash Fall` (0.3333 → 0.5 s) | no velocity action, `AffectedByGravity(True)` | remaining velocity plus gravity (−60 × gravityScale) |
| `Rpr Downslash End` | `FinishDownspike` | vy = −10 |

The velocity set on entry is multiplied by 0.7 in each following `FixedUpdate` before integration, so the
per-step velocities are 21, 14.7, 10.29, 7.20, 5.04, 3.53, 2.47, 1.73, … in each axis. Over the 0.1667 s of
`Rpr Downslash` (≈ 8 physics steps) this moves Hornet about 1.32 units forward and 1.32 units down (geometric
limit 30 × 0.7 / 0.3 × 0.02 = 1.4 units per axis). About 1.73 units/s per axis remain when `Rpr Downslash Fall`
starts.

### 5.4 Hitbox (`DownSlash New`)

- `NailSlash`, `animName = "v3 Down Slash Effect"` (fps 24, 4 frames, trigger at frame 0). The frame-0 trigger
  fires inside `StartSlash`, so the polygon and `Clash Tink` are enabled on the `Rpr Downslash` entry frame.
  The clip has no second trigger; clip completion at 4/24 = **0.1667 s** calls `CancelAttack()` (colliders off,
  damager inactive). With Quickening the clip runs at 36 fps and ends after 0.1111 s.
- Active window from the press: **0.1667 → 0.3333 s** (10 → 20 @60); quickened 0.1667 → 0.2778 s.
- Transform localPos (0, 0, -0.001), `scale` (1, 1, 0). `overrideLongNeedleScale = True`,
  `longNeedleScale = (1.1, 1.1, 1)`.
- Polygon (object space = hero-local): (-2.609,0.14) (-2.63,-0.933) (-1.961,-2.344) (-0.326,-3.126)
  (1.03,-2.922) (2.308,-2.033) (2.833,-0.683) (2.64,0.701) (1.94,0.042). Extents x -2.63 … +2.833,
  y -3.126 … +0.701; area 14.215, 51.7 % behind the origin.
- `Clash Tink` (localScale (0.95, 0.9, 1), `doRecoil = True`): (-2.57,-1.032) (-1.788,-2.615) (-0.309,-3.249)
  (1.01,-3.01) (2.289,-2.148) (2.793,-0.815) (2.693,-0.082) (-2.622,-0.04).
- Damage: Nail, `nailDamageMultiplier = 1`, `stunDamage = 1`, `direction = 270`, `magnitudeMult = 1.3`,
  `silkGeneration = Full`, `dealtDamageFSM` = its own `Dealt Damage Response` FSM.
- Components: `NailSlash`, `DamageEnemies`, `PlayMakerFSM` (`Dealt Damage Response`), `PlayMakerUnity2DProxy`. It has
  **no `HeroDownAttack`** (unlike Wanderer's `DownSlash` / `DownSlashAlt`); the pogo bounce comes only from the
  `Crest Attacks` FSM through `ATTACK LANDED` / `BOUNCE TINKED` / `BOUNCE CANCEL` (section 5.5).
- `NailSlashRecoil` (added at runtime) does nothing for a downward (cardinal 3) damager.
- The Reaper down-slash hurtbox (`HeroBoxReaperDownSlash`, active from the antic until `Rpr Downslash Fall 2` or
  `RegainControl`) spans hero-local x +0.142 … +0.598, y -0.169 … +1.433. The normal air hurtbox spans x
  ±0.2277, y -1.025 … +0.745.

### 5.5 Hit, bounce, thunk

- **Enemy hit.** `HealthManager` sends `HIT LANDED` to the attack object when the enemy has no active
  `NonBouncer` (both on damage and on a blocked hit). The object's `Dealt Damage Response` FSM (`State 1` →
  `Send Event`) checks `HeroController.CanCustomRecoil()` (`Time.timeAsDouble >= recoilAllowTime`); if true it
  sets `Crest Attacks.Attack Target` and sends **`ATTACK LANDED`** to `Crest Attacks`.
- **Interactive objects.** `State 1` also has `Trigger2dEventLayer` (OnTriggerEnter2D, layer 19 `Interactive
  Object`) → `HIT INTERACTIVE` → `Check`: `CanCustomRecoil` false → cancel; object has `NonBouncer` → cancel;
  otherwise `Bounce Tink` sends **`BOUNCE TINKED`**.
- **Bounce pods / balloons.** `BouncePod` and `BounceBalloon` send **`BOUNCE CANCEL`** to `Crest Attacks`.
- These three events are handled only in `Rpr Downslash` and `Rpr Downslash Thunk`. They go to
  `Rpr Downslash Bounce` (`SetStartWithDownSpikeBounce`, `attack_cooldown = 0.15`) → `End` (same frame) →
  `RegainControl` consumes `startWithDownSpikeBounce` → `DownspikeBounce(harpoonRecoil: false)` with the
  default bounce config: 5 physics steps at vy = 18.6, apex 4.092 units, 0.38 s to apex, then
  `downSpikeRecovery` for 0.25 s ([../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md)
  section 4.2). The `DownSlash New` hitbox is not cancelled by the bounce; it stays active until its effect clip
  completes.
- **Terrain thunk.** When `Clash Tink` hits terrain while `cState.downAttacking`, `NailSlashTerrainThunk` sends
  **`THUNKED DOWN`** (unless the terrain's component has `PreventDownBounce`). `Rpr Downslash Thunk` keeps
  gravity off, applies `DecelerateV2 0.715` and waits for the next hero clip trigger (frame 6, 0.25 s after
  `Rpr Downslash` began = 0.4167 s from the press), then continues with `Rpr Downslash Fall`.
- Taking damage (`HERO DAMAGED`) sends the FSM to `Cancel`.

---

## 6. Reaper mode (bind effect)

### 6.1 State and settings

```csharp
public struct ReaperCrestStateInfo
{
    public float ReaperModeDurationLeft;
    public bool IsInReaperMode;
}
```

- Stored in `HeroController.reaperState`, exposed as `HeroController.ReaperState`. Runtime only, not saved.
- `GlobalSettings.Gameplay`: `reaperCrest = Reaper`, **`reaperModeDuration = 10`**, `reaperBundlePrefab =
  Reaper Silk Bundle`. `GlobalSettings.Effects`: `reapHitEffectPrefab = Reaper_Tear_Effect`.
- `HeroController.reaperModeEffect` = `crest_reaper_effects(Clone)` (`NestedFadeGroupTimedFader`, spawned as a
  child in `Awake`; clip `Hornet_crest_reaper` 0.9 s; fader `duration = 0.2`, alpha 1 → 0,
  `disableOnEnd = True`).

### 6.2 Start

`HeroController.BindCompleted()`:

```csharp
if (Gameplay.ReaperCrest.IsEquipped)
{
    reaperState.IsInReaperMode = true;
    reaperState.ReaperModeDurationLeft = Gameplay.ReaperModeDuration;   // 10
    reaperModeEffect.gameObject.SetActive(false); reaperModeEffect.gameObject.SetActive(true);
}
```

`BindCompleted` is called from the `Bind` FSM (the Bind FSM has no Reaper-specific state):

- **`Send Bind Event`** (`Heal` → `Bind Counter` → `Bind Bell Disappear?` → `Send Bind Event`): `DID BIND`, then
  `BoolTest Skip Heal` (true → skip), then `CallMethodProper BindCompleted`. `Skip Heal` is set from
  `cState.isMaggoted` in `Maggoted?`, so a bind while maggoted does not start reaper mode.
- **`Leave Scene Bind?`** (global `LEAVING SCENE`): when `Is Binding` is true and neither Witch nor Shaman is
  equipped, it heals and calls `BindCompleted`.
- With Multibind (`Multi Bind`: `Bind Amount = 2`), `Bind Loop?` loops back through the bind and calls
  `BindCompleted` again; each call sets the duration back to 10.
- A bind that starts while already in reaper mode sets the duration back to 10.

### 6.3 Silk bundles on hit (`HealthManager.TakeDamage`)

In the `switch (hitInstance.AttackType)` of `HealthManager.TakeDamage`, `Heavy` hits from nail attack objects
(`DamageEnemies.IsNailAttackObject`) and `Nail`/`NailBeam` hits run `NailHitEnemy`, then all of them (plus
`Spell`) reach:

```csharp
if ((AttackType == Nail || (AttackType == Heavy && hitInstance.IsNailTag))
    && enemyType != Shade && enemyType != Armoured && !DoNotGiveSilk)
{
    if (!flag) HeroController.instance.SilkGain(hitInstance);        // flag = 0-damage non-lag hit
    if (hitInstance.SilkGeneration != HitSilkGeneration.None
        && PlayerData.instance.CurrentCrestID == "Reaper"
        && HeroController.instance.ReaperState.IsInReaperMode)
    {
        int num = reaperBundles switch {                               // per-enemy serialized field
            ReaperBundleTiers.Normal  => HeroController.instance.GetReaperPayout(),
            ReaperBundleTiers.Reduced => 1,
            ReaperBundleTiers.None    => 0 };
        if (num > 0) { ...FlingUtils.SpawnAndFling(...); Reaper_Tear_Effect... }
    }
}
```

Conditions for a bundle drop:

| Condition | Effect |
|---|---|
| `AttackType == Nail` | Drops. All Reaper basic slashes, `DownSlash New`, `DashUpper Slash`, `Extra Damager`, `Taunt Slash`. |
| `AttackType == Heavy` and `hitInstance.IsNailTag` (source tagged `Nail Attack`) | Drops. `Charge Slash Scythe` slash 01 / 02 / behind. |
| `NailBeam`, `Spell`, `Generic`, other types | No drop. |
| Enemy type `Shade` or `Armoured`, or `DoNotGiveSilk` | No drop. |
| `hitInstance.SilkGeneration == None` | No drop. `FirstHit` damagers drop on every hit (no `IsFirstHit` check here). |
| Crest ID is not `"Reaper"` or not in reaper mode | No drop. |
| Hit dealt 0 damage | Still drops if it reached `TakeDamage` (the 0-damage `flag` only skips `SilkGain`). |
| Hero silk full | Still drops (the silk amount is checked only on collection). |
| Hit did not reach `TakeDamage` (blocked by direction, immune, dead, evading) | No drop. |

Drop count, `HealthManager.reaperBundles` (`ReaperBundleTiers`, serialized per enemy, default `Normal` = 0):
`Normal` → `GetReaperPayout()`, `Reduced` → 1, `None` → 0.

`HeroController.GetReaperPayout()` returns 0 when not in reaper mode, otherwise
`Probability.GetRandomItemByProbability(reaperBundleDrops)` with the hard-coded array:

| Value | Probability |
|---|---|
| 1 | 0.35 |
| 2 | 0.5 |
| 3 | 0.15 |

`GetRandomItemRootByProbability` sorts the entries by probability (0.15, 0.35, 0.5), draws
`Random.Range(0, 1.0)` (sum of the weights) and picks the last entry whose cumulative start is ≤ the draw.
The resulting chances equal the listed weights: **1 bundle 35 %, 2 bundles 50 %, 3 bundles 15 %**; expected
value 1.8 bundles per qualifying hit. Each qualifying `TakeDamage` call rolls independently (one roll per
enemy `HealthManager` per hit).

Fling (`FlingUtils.SpawnAndFling`): `Prefab = Reaper Silk Bundle`, `AmountMin = AmountMax = num`, speed
`Random.Range(25, 50)`, angle `Random.Range(AngleMin, AngleMax)`, spawned at `TransformPoint(effectOrigin)`:

| Case | Angle range (degrees) |
|---|---|
| `flingSilkOrbsAimObject` set | angle to that object ±45 |
| `flingSilkOrbsDown` | 225 … 315 |
| hit direction 0 (right) | 315 … 415 |
| hit direction 1 (up) | 45 … 135 |
| hit direction 2 (left) | 125 … 225 |
| hit direction 3 (down) | 225 … 315 |

`Reaper_Tear_Effect` is spawned at the same point with a rotation and scale chosen from the cardinal direction
of the fling angle (visual only; `CameraShakeOnEnable`, `AutoRecycleSelf`).

### 6.4 `Reaper Silk Bundle` object

Prefab (`referenced-objects.txt`): layer `Interactive Object`, localScale (0.75, 0.75, 1). Components:
`PlayMakerFSM Control`, `Rigidbody2D` (Dynamic, `gravityScale = 0`, `linearDamping = 0`, Continuous,
FreezeRotation), `ObjectBounce` (`bounceFactor = 0.6`, `speedThreshold = 1`), `SetZ` (0.002),
`AutoRecycleSelf` (`afterEvent = LEVEL_UNLOAD`), `EventRegister` (`"CLEAR EFFECTS"` → sets FSM bool
`Clear Effects = True`), `BounceOnWater` (`bounceMultiplier = 0.5`).

Children: `Terrain Collider` (layer Terrain Detector, CircleCollider2D non-trigger radius 0.6580505,
`NonBouncer`); `Sprite Silk` (layer Interactive Object, CircleCollider2D **trigger** radius 0.9381612 offset
(-0.0156, 0.0156), `NonBouncer`, `RandomScale` 0.9–1.15 with random X flip, tk2d `Bundle Idle` fps 24, 15
frames, loop from frame 5); `Sprite Collect` (`Bundle Collect` fps 20, 6 frames); `Slash Effect`; particle
children; `Glow`.

`Control` FSM structure (`fsm_Reaper Silk Bundle_Control.txt`):

| State | Transitions | Actions |
|---|---|---|
| `Init` | FINISHED → `Flinging` | `reaper_orb_spawn` audio, activation/fade setup, `NextFrameEvent` |
| `Flinging` | FINISHED → `Start Collectable`; WATER → `Water Bounce` | `EaseFloat`, `SetScale` every frame, `DecelerateV2`, `Wait`, water trigger |
| `Start Collectable` | FINISHED → `Collectable` | `SetCircleCollider` |
| `Collectable` | **COLLECT → `Collect`**, **REAP → `Reap`**, **DISAPPEAR → `Disappear`**, WATER → `Water Bounce` | `BoolTest` every frame; `DecelerateV2`; `Trigger2dEventV2` (OnTriggerEnter2D, no tag filter); `Trigger2dEventV2` (tag `"Nail Attack"`); `Trigger2dEventCheckName` (tag `"Breaker"`, name `"Harpoon Breaker"`); `RandomFloat`; `Wait`; `FloatAdd`; `CallMethodProper HeroController.SetSilkPartsTimeLeft`; water trigger |
| `Collect` | FINISHED → `Get Silk` | particle emit |
| `Reap` | FINISHED → `Get Silk` | `GetAngleToTarget2D`, `SetRotation`, particle emit, activate object |
| `Get Silk` | FINISHED → `Recycle Pause` | `reaper_orb_collect` audio; spawn `Silk_Get_Effect`; `Tiny Shake`; `SendEventToRegister "REAPER ORB COLLECT"`; velocity 0; **`HeroController.AddSilkParts(1, true)`**; `Wait`; colliders/renderers off |
| `Disappear` | FINISHED → `Disappear Burst` | `reaper_orb_dissapear` audio; tk2d `Bundle Disappear` (fps 24, 6 frames); fade; collider off |
| `Disappear Burst` | FINISHED → `Recycle Pause` | particle |
| `Water Bounce` | FINISHED → `Start Collectable` | velocity set, `NextFrameEvent` |
| `Recycle Pause` → `Recycle` | | `Wait`, then `RecycleSelf` |

Both collection paths (`COLLECT` and `REAP`) end in `Get Silk`, which gives exactly **1 silk part**. The
`Disappear` path gives nothing.

Dump limitation: the bundle FSM was dumped from the uninstantiated prefab. Apart from the clip/audio names and
the `AddSilkParts` parameters (int 1, bool True), the action parameters in this file read null/0 (wait times,
random ranges, which trigger sends which event, the `SetSilkPartsTimeLeft` argument). The lifetime before
`DISAPPEAR` and the exact pickup triggers therefore cannot be read from the dump.

The `DownSlash New` `Dealt Damage Response` FSM stores the collider's object on a layer-19 trigger; the
bundle's layer-19 collider is on `Sprite Silk`, which has an active `NonBouncer`, so touching a bundle with the
down slash goes to `CANCEL` and does not pogo.

### 6.5 Silk parts

```csharp
public void AddSilkParts(int amount)
{
    if (playerData.silk < playerData.CurrentSilkMax)
    {
        playerData.silkParts += amount;
        if (playerData.silkParts >= 3) { playerData.silkParts = 0; AddSilk(1, heroEffect: false); }
        else { ResetSilkRegen(); GameCameras.instance.silkSpool.RefreshSilk(); }
    }
}
// AddSilkParts(int, bool heroEffect): also spriteFlash.flashFocusGet() when heroEffect
```

- **3 collected bundles = 1 silk.** A bundle collected while silk is at `CurrentSilkMax` adds nothing.
- `PlayerData.AddSilk` sets `silkParts = 0` whenever silk reaches the maximum.
- `silkPartsTimeLeft` (set by the bundle FSM through `SetSilkPartsTimeLeft`) counts down in
  `HeroController.Update` (game state PLAYING, not paused, inventory closed); at ≤ 0 it sets `silkParts = 0`.
- `silkParts` is also set to 0 by `BindInterrupted()` (Bind FSM `Remove Silk?`), `Die()`, and on save load
  (`GameManager.SetLoadedGameData`).

### 6.6 What ends reaper mode

`ResetReaperCrestState()`: if in reaper mode, `reaperModeEffect.Fade()` (alpha 1 → 0 over 0.2 s), then
`reaperState = default` (`IsInReaperMode = false`, duration 0).

| Trigger | Code path |
|---|---|
| Timer | `HeroController.Update`, inside `if (!gm.isPaused && !playerData.isInventoryOpen)` and `gm.GameState == PLAYING`: `ReaperModeDurationLeft -= Time.deltaTime`; at ≤ 0 → reset. The timer does not run while paused, with the inventory open, or outside PLAYING. |
| **Taking damage** | `HeroController.TakeDamage`, inside `if (damageAmount > 0)` after all reductions, right after `ResetHunterUpgCrestState()`: `if (reaperState.IsInReaperMode) ResetReaperCrestState();`. Same conditions as the Hunter meter reset (damage reduced to 0 or avoided before that block does not end it). `DoSpecialDamage` (frost, chomp, quick damage, damage tags) does **not** reset reaper mode. |
| Game state `CUTSCENE` | `Update` → `ResetAllCrestStateMinimal(false)` every frame → `ResetReaperCrestState()` |
| `TOOL EQUIPS CHANGED` | `ResetAllCrestState` → `ResetAllCrestStateMinimal` → reset. Unlike the Hunter meter, this also happens while `ToolItemManager.IsChangingActiveState`. Covers crest/tool changes, tool replenish, death (`Die` sends the event). |
| Clearing effects | `ClearEffects` / `ClearEffectsLite` → `ResetAllCrestState(false)` → reset |
| `MaxHealth()`, `SetIsMaggoted(true)`, `Awake` | `ResetAllCrestState()` → reset |

The full list of callers of these resets is in [hunter.md](hunter.md) section 3.6 (the Reaper reset is
unconditional in `ResetAllCrestStateMinimal`, so every row that reaches it applies, including the ones marked
"No" for the Hunter meter because of `clearHunterState: false`).

### 6.7 HUD

`BindOrbHudFrame`: when `Gameplay.ReaperCrest.IsEquipped`, the frame uses `reaperFrameAnims` and runs
`ReaperCrestRoutine`. On entering reaper mode it plays `reaperModeBeginAnim` and restarts `reaperModeEffect`
(alpha 1); on leaving it plays `reaperModeEndAnim` and fades `reaperModeEffect` out over
`reaperModeEffectFadeOutTime`, then deactivates it.

---

## 7. Source references

Decompiled (`scratchpad/decomp/`):
- `HeroController.cs`:
  - `ReaperCrestStateInfo` (l.210–215), `reaperModeEffectPrefab`/`reaperModeEffect` (l.1255–1257),
    `reaperBundleDrops` (l.1283–1300), `silkPartsTimeLeft` (l.1302), `reaperState` (l.1304), `ReaperState`
    (l.1482), `reaperModeEffect` spawn in `Awake` (l.1644).
  - `Update`: cutscene reset (l.2419), reaper timer (l.2455–2461), `silkPartsTimeLeft` (l.2463–2470).
  - `DownAttack` (l.4246, Custom branch), `SetStartWithDownSpikeBounce` (l.4600), `SetStartFromReaperUpperslash`
    (l.4711), `TakeDamage` reaper reset (l.5518–5521), `GetReaperPayout` (l.5938), `ResetAllCrestStateMinimal`
    (l.5947), `ResetReaperCrestState` (l.5994), `SetSilkPartsTimeLeft` (l.6003), `AddSilkParts` (l.6082/6100),
    `BindCompleted` (l.6267, reaper l.6286–6295), `BindInterrupted` (l.6307), `CanCustomRecoil` (l.6545),
    `RelinquishControlNotVelocity` (l.7449), `RegainControl` (l.7497, bounce flags l.7675),
    `ResetAttacksShared` (l.10089), `FinishDownspike` (l.10369), `ClearEffectsLite` (l.12057).
- `HealthManager.cs` – `ReaperBundleTiers` (l.80), `reaperBundles` (l.244), `HIT LANDED` (l.825, l.1069),
  bundle drop and fling (l.1116–1215).
- `Probability.cs` – `ProbabilityInt`, `GetRandomItemByProbability`, `GetRandomItemRootByProbability`.
- `FlingUtils.cs` – `SpawnAndFling`.
- `HeroBox.cs` – `HeroBoxNormal` (l.166), `HeroBoxReaperDownSlash` (l.214).
- `NailSlash.cs` – `Start` (adds `NailSlashRecoil`), `StartSlash`, `PlaySlash`, `OnAnimationEventTriggered`,
  `OnAnimationCompleted`; `NailAttackBase.cs` – `OnSlashStarting` (Long Needle scale); `NailSlashRecoil.cs`.
- `NailSlashTerrainThunk.cs` – `THUNKED DOWN` (l.241, l.316). `BouncePod.cs` (l.576), `BounceBalloon.cs` (l.292)
  – `BOUNCE CANCEL`. `BounceShared.cs` – Reaper pull clamp (`REAPER_RANGE_X = 1`, `REAPER_RANGE_Y = 1.75`).
- `DamageEnemies.cs` – `DoDamage` magnitude (l.951, l.1144). `Recoil.cs` – `Reset`, `RecoilByDirection`.
- `EnemyDamagerGroup.cs`. `BindOrbHudFrame.cs` – reaper fields (l.107–121), selection (l.354–362),
  `ReaperCrestRoutine` (l.612). `GlobalSettings/Gameplay.cs` – reaper fields (l.84, l.150–153, l.512, l.552–554).
- `PlayerData.cs` – `silkParts` (l.162), `AddSilk` (l.3728). `GameManager.cs` – `SetLoadedGameData` (l.4652).
- PlayMaker actions: `DecelerateV2.cs`, `SetVelocityByScale.cs`, `Tk2dWatchAnimationEvents.cs`,
  `Tk2dWatchAnimationEventsV3.cs`, `HeroBoxControl.cs`.

Dumps (`raw-dump/`):
- `crests.txt` – Reaper slots, localised fields, `heroConfig = Reaper` values, Scythe animation library.
- `hero-configs.txt` – configs[2] (l.5060–7892): Reaper attack objects, polygons, `DamageEnemies` fields.
- `hero-hierarchy.txt` – `Attacks/Scythe` (l.9179, inactive), `DashUpper Slash` (l.11004), `Extra Damager`
  (l.11309), `DownSlash New` (l.11498), `HeroBox` (l.4573).
- `hero-controller.txt` – `reaperState`, `reaperBundleDrops` (values not expanded), `reaperModeEffect`.
- `global-settings.txt` – `Gameplay.reaperCrest`, `reaperModeDuration = 10`, `reaperBundlePrefab`;
  `Effects.reapHitEffectPrefab`, `longNeedleMultiplier`.
- `referenced-objects.txt` – `HeroControllerConfig Reaper`, `Reaper Silk Bundle` (l.41366),
  `crest_reaper_effects` (l.31582), `Reaper_Tear_Effect` (l.34930).
- `animator-timelines.txt` – `Charge Slash Scythe` clip (transform and child activation times).
- `animations-tk2d.txt` – Scythe library clips.
- `fsm_Hero_Hornet(Clone)_Crest Attacks.txt` – `Idle`, `Cancel`, `Re-enable animation`, `Rpr Downslash Antic`,
  `Rpr Downslash`, `Rpr Downslash Fall`, `Rpr Downslash Fall 2`, `Rpr Downslash End`, `Rpr Downslash Thunk`,
  `Rpr Downslash Bounce`, `Rpr DashCancel`, `End`.
- `fsm_Hero_Hornet(Clone)~Attacks~Scythe~DownSlash New_Dealt Damage Response.txt` – `ATTACK LANDED` /
  `BOUNCE TINKED`.
- `fsm_Hero_Hornet(Clone)_Sprint.txt` – `Start Attack`, `Reaper Antic`, `Reaper Upper`, `Reaper End`.
- `fsm_Hero_Hornet(Clone)~Attacks~Scythe~DashUpper Slash~Extra Damager_Collider Timing Control.txt`.
- `fsm_Hero_Hornet(Clone)_Nail Arts.txt` – `Antic Type`, `Reaper Kickoff`, `Antic Rpr`.
- `fsm_Hero_Hornet(Clone)_Bind.txt` – `Send Bind Event`, `Leave Scene Bind?`, `Maggoted?`, `Multi Bind`,
  `Bind Loop?`, `Remove Silk?`.
- `fsm_Reaper Silk Bundle_Control.txt` – bundle FSM (parameters not deserialized).
- `statics.txt` – pooled `GlobalPool(Clone)/Reaper Silk Bundle(Clone)`.
