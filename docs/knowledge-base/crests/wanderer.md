# Wanderer Crest (Wanderer)

The Wanderer crest is a single `ToolCrest` asset with its own `HeroControllerConfig` (`Wanderer`) and its own animation override library (`Hornet CrestWeapon Dagger Anim`). Its crest-specific runtime behaviour consists of a sprint-attack follow-up chain (recoil, then RecoilStab), the `Slash`-type down attack, a multi-hit needle strike, and a critical-hit passive tied to the silk amount. Shared mechanics are covered in [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md), [../mechanics/sprint.md](../mechanics/sprint.md) and [../mechanics/needle-strike.md](../mechanics/needle-strike.md).

---

## 1. Asset, slots, config

| Crest ID | `isHidden` | `previousVersion` | `upgradedVersion` | `heroConfig` | Gameplay field |
|---|---|---|---|---|---|
| `Wanderer` | False | null | null | `Wanderer` (instanceId 64982) | `Gameplay.wandererCrest` |

Localised fields (`crests.txt`):

| Field | Value |
|---|---|
| `displayName` | `Tools/CREST_PILGRIM_NAME` → "Wanderer" |
| `description` | `Tools/CREST_PILGRIM_DESC` → "Cut swiftly and nimbly with short, precise strikes." |
| `itemNamePrefix` | `Tools/UI_MSG_TITLE_CREST` → "Bound the Crest of" |
| `getPromptDesc` | `Tools/CREST_PILGRIM_DESC` (same text as `description`) |
| `equipText` | `Tools/UI_MSG_EQUIP_CREST` → "To change Crest, open the Crest screen of the Menu while resting at a bench." |
| `crestSprite` / `crestSilhouette` / `crestGlow` | Crest__0006_wanderer / Crest__0001_wanderer_sil / Crest__0002_wanderer_equip_glow |
| `hasCustomAction` | False |

### 1.1 Slots

| Slot | Type | Locked | AttackBinding | Position |
|---|---|---|---|---|
| 0 | Skill | False | Neutral | (0.00, 0.72) |
| 1 | Red | False | Up | (0.00, 2.67) |
| 2 | Blue | True | Down (unused for Blue) | (-2.10, 1.90) |
| 3 | Blue | True | Down (unused for Blue) | (2.10, 1.90) |
| 4 | Yellow | True | Down (unused for Yellow) | (0.00, -2.07) |
| 5 | Yellow | False | Down (unused for Yellow) | (-2.35, -0.70) |
| 6 | Yellow | False | Down (unused for Yellow) | (2.35, -0.70) |

Totals: 7 slots: Skill 1, Red 1 (0 locked), Blue 2 (2 locked), Yellow 3 (1 locked). 4 are open from the start and 3 are locked.

Attack bindings: Neutral → Skill slot 0; Up → Red slot 1. There is no Red slot with a Down binding, so the Down input always uses the Neutral (Skill) tool (`GetBoundAttackTool`).

### 1.2 Config `Wanderer` (`configs[4]`, activeRoot `Hero_Hornet(Clone)/Attacks/Wanderer`)

| Field | Wanderer | Default (Hunter), for comparison |
|---|---|---|
| heroAnimOverrideLib | Hornet CrestWeapon Dagger Anim | null |
| canPlayNeedolin / canBrolly / canDoubleJump / canNailCharge / canBind / canHarpoonDash | True ×6 | True ×6 |
| forceBareInventory | False | False |
| downSlashType | **Slash** | DownSpike |
| downSlashEvent | "" | "" |
| downspikeAnticTime / downspikeTime / downspikeSpeed | 0.09 / 0.16 / 15 (not read for the `Slash` type) | 0.1 / 0.1 / 26 |
| downspikeRecoveryTime | 0.09 | 0.25 |
| downspikeBurstEffect / downspikeThrusts | True / True | True / True |
| dashStabSpeed / dashStabTime | -30 / 0.2 (not read by the Wanderer sprint branch) | -50 / 0.12 |
| forceShortDashStabBounce | False | False |
| dashStabBounceJumpSpeed | 18.6 | 18.6 |
| dashStabSteps | 1 | 1 |
| attackDuration | **0.25** | 0.35 |
| quickAttackSpeedMult | 1.5 | 1.5 |
| attackRecoveryTime | **0.1** | 0.15 |
| attackCooldownTime | **0.3** | 0.41 |
| quickAttackCooldownTime | **0.15** | 0.205 |
| canTurnWhileSlashing | False | False |
| chargeSlashRecoils | **False** | True |
| chargeSlashLungeSpeed / chargeSlashLungeDeceleration / chargeSlashChain | 0 / 1 / 0 | 0 / 1 / 0 |
| wallSlashSlowdown | **False** | True |

Derived values (method in [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) §1.5–1.6):
- Effective time between slash starts: max(0.3, 0.25) = **0.3 s**; quickened max(0.15, 0.25) = **0.25 s**.
- `cState.attacking` lasts 0.25 s (0.1667 s quickened).
- Alternate-slash window: `attackRecoveryTime + ALT_ATTACK_RESET` = 0.1 + 0.5 = **0.6 s** after the previous slash start in the same direction.
- Wall slash does not halve or clamp the fall speed (`wallSlashSlowdown = False`).

### 1.3 Attack objects

All objects below are tagged `Nail Attack` on layer `Attack`, and each `NailSlash` object has a `Clash Tink` child (layer `Tinker`, non-trigger polygon, `NailSlashTerrainThunk`, kinematic `Rigidbody2D`). Library: `Hornet CrestWeapon Dagger Anim`.

| ConfigGroup slot | Object (under `Hero_Hornet(Clone)/Attacks/`) | Components of note | localPos | localScale / `NailSlash.scale` | localRot z | animName | bounceConfig |
|---|---|---|---|---|---|---|---|
| NormalSlash | `Wanderer/Slash` | NailSlash, DamageEnemies | (-0.034, -0.016) | (0.8, 1.4) | 0 | SlashEffect | Wanderer |
| AlternateSlash | `Wanderer/AltSlash` | NailSlash, DamageEnemies | (-0.12, 0.364) | (0.8, 1.35) | 0 | SlashEffectAlt | Wanderer |
| UpSlash | `Wanderer/UpSlash` | NailSlash, DamageEnemies | (-0.03, 0.181) | (0.7, -1.3) | 270.0001 | SlashEffect | null |
| AltUpSlash | null | | | | | | |
| DownSlash | `Wanderer/DownSlash` | NailSlash, DamageEnemies, HeroDownAttack | (-0.008, -0.143) | (0.8, 1) | 0 | DownSlashEffect | Wanderer |
| AltDownSlash | `Wanderer/DownSlashAlt` | NailSlash, DamageEnemies, HeroDownAttack | (0.05, -0.128) | (-0.8, 1) | 0 | DownSlashEffect | Wanderer |
| WallSlash | `Wanderer/WallSlash` | NailSlash, DamageEnemies | (0.24, 0.43) | (-0.8, 1.35) | 0 | SlashEffectAlt | null |
| DashStab | `Wanderer/DashStab` | NailSlash, DamageEnemies, PlayMakerFSM `Send Hit To Player` | (-0.58, 0.34) | (0.75, 1) | 354.535 | DashStabEffect | null |
| DashStabAlt | `Wanderer/DashStabAlt` | same as DashStab | (-0.537, -0.8146) | (0.75, -1) | 10.7899 | DashStabEffect | null |
| (Sprint FSM only) | `Wanderer/RecoilStab` | same as DashStab | (0.743, 0.8074) | (0.9871, 1.1594) | 24.8372 | Wanderer RecoilStab Efct | null |
| ChargeSlash | `Charge Slash Wanderer` | tk2dSpriteAnimator, DeactivateAfter2dtkAnimation, PolygonCollider2D, DamageEnemies, NailSlashTerrainThunk, HeroExtraNailSlash, 2× HeroNailImbuementEffect | (-1.46, 0.8042) | (0.6919, 0.9555) | 2.0924 | – | – |
| ↳ child | `Charge Slash Wanderer/Lunge Stopper` | PolygonCollider2D (layer `Enemy Detector`) | (0, 0) | (1, 1) | 0 | – | – |
| TauntSlash | `Taunt Slash` (shared with all configs) | DamageEnemies | | | | | |

The `Wanderer` root is at localPos (0, 0, 0), scale (1, 1, 1), the same as the `Default` root. Every `NailSlash` has `dontAddRecoiler = False` and `drillPull = False`, so `NailSlash.Awake` adds a `NailSlashRecoil` component to each of them at runtime (the dump shows none because the `Wanderer` root was inactive). `overrideLongNeedleScale = False` on all of them, so Longneedle scales them like the Default slashes.

`DamageEnemies` values:

| Object | attackType | specialType | useNailDamage | nailDamageMultiplier | stunDamage | direction (serialized) | magnitudeMult | multiHitter | silkGeneration | doesNotCriticalHit | dealtDamageFSM / event |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Slash, AltSlash, WallSlash | Nail | None | True | 1 | 0.8 | 0 | 1 | False | Full | False | – |
| UpSlash | Nail | None | True | 1 | 0.8 | 90 | 1 | False | Full | False | – |
| DownSlash, DownSlashAlt | Nail | None | True | 1 | 0.8 | 270 | 1 | False | Full | False | – |
| DashStab, DashStabAlt | Nail | None | True | 1 | 0.8 | 180 | **0.75** | False | Full | False | own `Send Hit To Player` / `"DASH HIT"` |
| RecoilStab | Nail | None | True | 1 | 0.8 | 180 | 1 | False | Full | False | own `Send Hit To Player` / `"DASH HIT"` |
| Charge Slash Wanderer | Nail | CocoonBreak, Heavy | True | **0.55** | 0.2 | 180 | 0.5 | **True** (`stepsPerHit = 6`, `hitsUntilDeath = 100`) | **FirstHit** | **True** | – |

All have `damageDealt = 5` (replaced because `useNailDamage = True`), `damageMultiplier = 1`, `damageMultPerHit` empty, `canWeakHit = False`, `manualTrigger = False`, `firstHitFreeze = None`. Default slashes have `stunDamage = 1`; all Wanderer damagers have 0.8 (needle strike 0.2).

### 1.4 Hitbox geometry and comparison with Default (Hunter)

Local polygon points (trigger, offset (0, 0), disabled at rest except the needle strike):

- **Slash**: (-2.276,0.375) (-3.6,0.088) (-3.859,-0.142) (-3.51,-0.402) (-2.074,-0.612) (-0.199,-0.757) (-0.267,0.505)
- **AltSlash**: (-3.266,-0.092) (-3.768,-0.379) (-3.313,-0.678) (-0.088,-1.12) (-0.152,0.195)
- **UpSlash**: (0.978,-0.578) (-1.688,-0.632) (-3.858,-0.316) (-3.831,-0.003) (-1.873,0.373) (0.948,0.349)
- **DownSlash**: (2.187,-0.237) (2.101,-1.491) (1.269,-2.21) (0.146,-2.533) (-1.181,-2.26) (-1.817,-1.591) (-1.841,-0.246)
- **DownSlashAlt**: (2.205,-0.24) (2.022,-1.56) (1.2,-2.209) (0.146,-2.474) (-1.066,-2.351) (-1.749,-1.445) (-1.908,-0.267)
- **WallSlash**: (-3.102,-0.076) (-3.816,-0.294) (-3.847,-0.441) (-3.116,-0.683) (-0.315,-1.027) (-0.303,0.142)
- **DashStab / DashStabAlt** (same points): (-3.271,-0.841) (-3.024,-1.184) (-1.813,-1.453) (-0.411,-1.439) (0.919,-1.402) (0.698,0.511) (-0.967,0.506) (-2.838,-0.184)
- **RecoilStab**: (-3.091,-0.216) (-4.332,-0.679) (-2.987,-1.219) (-0.306,-1.189) (-0.345,-0.088)
- **Charge Slash Wanderer** (root, enabled): (1.932,-0.383) (-2.925,0.065) (-4.745,-0.033) (-5.281,-1.024) (-4.791,-1.834) (-3.455,-2.153) (2.027,-1.972)
- **Lunge Stopper** (enabled): (1.932,-0.383) (-1.823,-0.113) (-1.904,-1.97) (2.027,-1.972)

Bounding boxes in hero-local space (points × scale, rotated, + localPos; Hornet's local −x is forward when she faces left, mirrored when facing right). Default values use the main polygons from [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) §5.2 and the Default transforms.

| Attack | Wanderer x range (width) | Wanderer y range (height) | Default x range (width) | Default y range (height) |
|---|---|---|---|---|
| Slash | -3.121 … -0.193 (2.928) | -1.076 … 0.691 (1.767) | -3.170 … 0.424 (3.595) | -1.429 … 1.231 (2.660) |
| AltSlash | -3.134 … -0.190 (2.944) | -1.148 … 0.627 (1.775) | -3.167 … 0.158 (3.325) | -1.329 … 0.957 (2.286) |
| UpSlash | -0.515 … 0.792 (1.307) | -0.504 … 2.882 (3.385) | -1.063 … 1.476 (2.539) | -0.939 … 3.188 (4.127) |
| WallSlash | 0.482 … 3.318 (2.835) | -0.956 … 0.622 (1.578) | 0.133 … 3.664 (3.532) | -1.416 … 1.210 (2.626) |
| DownSlash / DownSlashAlt | -1.481 … 1.742 / -1.714 … 1.576 | -2.676 … -0.380 / -2.602 … -0.368 | -1.594 … 0.676 (DownSpike `DownSlash`) | -2.709 … -0.194 |
| Sprint attack | DashStab -3.102 … -0.010; DashStabAlt -3.104 … 0.073 | -1.121 … 0.913; -1.447 … 0.692 | `Dash Stab` -3.470 … 0.225 | -1.219 … 0.059 |
| RecoilStab | -2.807 … 1.048 | -1.714 … 0.572 | – | – |
| Needle strike | `Charge Slash Wanderer` -5.076 … 0.010 | -1.339 … 0.792 | – | – |

Needle length: the forward tip of the Wanderer `Slash` (3.121) and `AltSlash` (3.134) is 0.05 / 0.03 units shorter than the Default (Hunter) `Slash` (3.170) / `AltSlash` (3.167). The Wanderer horizontal slashes do not extend behind Hornet (max x ≈ -0.19, Default reaches +0.42 / +0.16) and are 66 % / 78 % of the Default height. The up slash reaches 2.882 above the origin (Default 3.188) and is about half as wide. The Wanderer sprint attack reaches 3.10 forward, against 3.47 for the Default `Dash Stab`.

Hero body clips (Dagger library): `Slash` 25 fps × 5 frames = 0.2 s, `SlashAlt` 25 × 4 = 0.16 s, `UpSlash` 25 × 5 = 0.2 s, `Wall Slash` 25 × 4 = 0.16 s, `DownSlash` / `DownSlashAlt` 25 × 7 = 0.28 s.

Slash-effect hitbox windows (method in [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) §5.3: 1st counted trigger enables the colliders, 2nd disables them, clip completion cancels):

| Clip (Dagger) | Used by | fps / frames / triggers | Collider on | Collider off | Clip end | Quickened (×1.5) on / off |
|---|---|---|---|---|---|---|
| SlashEffect | Slash, UpSlash | 25 / 4 / [0, 3] | 0 s | 0.12 s (7.2 @60) | 0.16 s | 0 / 0.08 s |
| SlashEffectAlt | AltSlash, WallSlash | 25 / 4 / [0, 3] | 0 s | 0.12 s (7.2 @60) | 0.16 s | 0 / 0.08 s |
| DownSlashEffect | DownSlash, DownSlashAlt | 25 / 5 / [1, 3 'Bounce', 4] | 0.04 s (2.4 @60) | 0.16 s (9.6 @60) | 0.2 s | 0.0267 / 0.1067 s |
| DashStabEffect | DashStab, DashStabAlt | 25 / 3 / [0] | 0 s | clip end (only one trigger) | 0.12 s (7.2 @60) | 0 / 0.08 s |
| Wanderer RecoilStab Efct | RecoilStab | 24 / 5 / [0] | 0 s | clip end (only one trigger) | 0.2083 s (12.5 @60) | 0 / 0.1389 s |

Default `Slash` for comparison: collider 0 → 0.1 s, clip end 0.2 s.

---

## 2. Sprint attack (`Sprint` FSM, Wanderer branch)

The shared sprint states (`Start Sprint`, `Ground Sprint L/R`, `Dash Stab Dir`, `Start Attack`, `Continue Sprint?`, `Regain Control*`) are described in [../mechanics/sprint.md](../mechanics/sprint.md). `Start Sprint` calls `RelinquishControlNotVelocity` and `SetAllowRecoilWhileRelinquished(False)`; no Wanderer state sets it back to True.

### 2.1 Timeline from the attack press (ground, no hit)

| # | State | Actions (exact values) | Leaves on | Time in state |
|---|---|---|---|---|
| 1 | `Ground Sprint L/R` | sees ATTACK while `Attack Cooldown == 0`; `Alt Timer` −1/s | `ATTACK` | 0 |
| 2 | `Dash Stab Dir` | `Attack Direction` = 0 (facing right) or 180, `Is Stabbing Right` | `FINISHED` | instant |
| 3 | `Start Attack` | `In Attack = True`, `IncrementAttackCounter()`, `cState.isSprinting = False`; `CheckIfCrestEquipped` Reaper (no) → Wanderer (yes) | `WANDERER` | instant |
| 4 | `Wanderer Set ` | `Can Attack Bonk = 0.1`; `FloatCompare(Alt Timer, 0)`: > 0 → `ALT` | `FINISHED` → `Wanderer Nml`, `ALT` → `Wanderer Alt` | instant |
| 5 | `Wanderer Nml` / `Wanderer Alt` | `Tk2dPlayAnimationWithEvents("Wanderer Dash Attack"` / `"Wanderer Dash Attack Alt"`, trigger → FINISHED); `GetScale` → `X Scale`; `Sprint Speed` = Regular -17.5 / Quick -23.5 (one of Quickening, Sprintmaster) / Quicker -27.5 (both); `Attack Speed Crt = Sprint Speed × X Scale`; `Dash Stab Crt` = `DashStab` (Nml) / `DashStabAlt` (Alt); `Alt Timer` = 0.5 (Nml) / 0 (Alt). No velocity action: the sprint velocity is kept. | `FINISHED` on the frame-2 trigger | **2/18 = 0.1111 s (6.67 @60; the 7th rendered frame at a steady 60 fps)** |
| 6 | `Wanderer Nml Audio` / `Wanderer Alt Audio` | `hornet_wanderer_dash_sword_1` / `_2` | `FINISHED` | instant |
| 7 | `Wanderer Attack Start` | `Attack Heavy Hornet Voice`; `Attack Time End = now + 0.15`; `SetVelocity2d x = Attack Speed Crt`; `DamageEnemies.SetDirection(Attack Direction)` on `Dash Stab Crt`; `SendMessage StartSlash` → `DashStabEffect` frame-0 trigger → **collider on** | `FINISHED` | instant: **hitbox live at 0.1111 s** |
| 8 | `Wanderer Attacking` | `TimeLimitCheckV2(Attack Time End)`; `SetVelocity2d x = Attack Speed Crt` every frame (`DecelerateXY 0.925` disabled); `HeroCheckForBump` ±1; `WALL` / `BUMP` checks | `FINISHED` / `DASH HIT` / `DAMAGER TINKED` → `Bonk` / `WALL` → `Atk To WallBonk` / `BUMP` → `Bump Up 2` (+0.1 y, back) | **0.15 s** |
| 9 | `Wanderer Recovery` | all actions disabled | `FINISHED` | instant |
| 10 | `Continue Sprint?` | dash held → `SPRINT` → `Set Atk Cooldown` (`Attack Cooldown = 0.1` for Wanderer, 0.25 otherwise); else `Regain Control To Idle` | | instant |

Totals (ground, no hit):

| Segment | Wanderer | Hunter (Default), from [../mechanics/sprint.md](../mechanics/sprint.md) §3.3 |
|---|---|---|
| Input → hitbox | **0.1111 s (6.67 @60)** | 0.1667 s (10 @60) |
| Hitbox active | 0.12 s (`DashStabEffect`; 0.08 s quickened) | 0.12 s (`Attack Time`) |
| Forward movement | 0.15 s at the current sprint speed (17.5 / 23.5 / 27.5) | 0.12 s from 50, ×0.925 per step |
| Recovery | 0 (no animation in `Wanderer Recovery`) | 0.1 s |
| Input → control back / sprint resumed | **0.2611 s (15.7 @60)** | 0.3867 s |
| `Attack Cooldown` after resuming the sprint | **0.1 s** | 0.25 s |

Consecutive sprint attacks alternate `DashStab` / `DashStabAlt` while the next one starts within 0.5 s of accumulated `Ground Sprint` time (`Alt Timer`). `Alt Timer` is reset to 0 in `Idle`, `Turn Anim`, `Jump Antic`, `Bonk?` and `Wanderer Alt`.

### 2.2 On hit: recoil away from the enemy

`DASH HIT` reaches the Sprint FSM only through the `Send Hit To Player` FSM on the damager (`Init` → on `DASH HIT` → `Send`: `SendEventByName "DASH HIT"` to FSM `Sprint` on the hero). `DamageEnemies.DoDamage` sends it to that FSM when the target has a `HealthManager`, the computed damage is > 0 (or `canWeakHit`), and the target has no active `NonBouncer` (`DamageEnemies.cs` l.1181–1199). Only `Wanderer Attacking` listens for `DASH HIT`.

| # | State | Actions | Leaves on | Time |
|---|---|---|---|---|
| 1 | `Hit Enemy Wait 2` | two `NextFrameEvent` (no event) | `FINISHED` (`DAMAGER TINKED` → `Bonk`) | 1 frame |
| 2 | `Wanderer Recoil` | `cState.onGround = False`; `hornet_forward_spin_single`; `Attack Queued = False`; `SendMessage CancelAttack` to `Dash Stab Crt` (hitbox off); clip `Wanderer DashRecoil` (24 fps, 8 frames, trigger at 5 → FINISHED); `AffectedByGravity(False)`; `DecelerateV2(0.85)`; `SetVelocityByScale(speed none → x 0, y 20)`; `SetVelocityByScale(speed 30)`; `ListenForAttackV2` queue into `Attack Queued` after 0.13 s; `Can Jump cancel = True` after 0.13 s; `ListenForJump` queue into `Jump Queued` | `FINISHED` | **5/24 = 0.2083 s** |
| 3 | `W Recoil Cancelable` | listens: ATTACK (held with `Attack Queued`, or pressed) → `RecoilStab Dir`; JUMP (held with `Jump Queued`, or pressed) → `W JumpCancel`; dash pressed → `W DashCancel`; clip complete → FINISHED. No velocity action. | | until 8/24 = **0.3333 s** after `Wanderer Recoil` started (0.125 s in this state) |
| 4 | `W Recoil End` | `cState.onGround = False`; `HeroController.StartWandererDashComboWindow()` | `FINISHED` → `Regain Control Normal` → `Regain Control Shared` (`RegainControl()` restores gravity) | instant |

Recoil velocity:
- `SetVelocityByScale` sets x = `speed` when `localScale.x > 0` (facing left) and x = −`speed` otherwise. With speed 30 the result is **30 units/s opposite to the facing direction**. The direction is taken from Hornet's facing, not from the enemy's position.
- Entry velocity: **(30 backwards, +20 up)**, gravity off. `DecelerateV2(0.85)` multiplies both components by 0.85 on entry (before the sets) and then every FixedUpdate (`fixedDeltaTime` 0.02 s). After n fixed steps: vx = 30 × 0.85ⁿ, vy = 20 × 0.85ⁿ. The state lasts 0.2083 s (≈ 10 fixed steps): about (5.9, 3.9) when it ends. That velocity is not changed in `W Recoil Cancelable`.
- No Wanderer state calls `SetAllowRecoilWhileRelinquished(True)`, and control is relinquished during the sprint attack, so `HeroController.CanRecoil()` returns false and the runtime `NailSlashRecoil` on `DashStab` adds no extra recoil.

Cancels:
- `W JumpCancel`: `hasDoubleJump` → `W To D Jump` (`SetStartWithDoubleJump`); otherwise `hasBrolly` → `W To Brolly` (`SetStartWithBrolly`); otherwise `Regain Control Normal`.
- `W DashCancel`: `SetStartWithDash` → `Regain Control Normal`.

### 2.3 Dash combo window and RecoilStab

- `DASHCOMBO_WINDOW_TIME = 0.15` (`hero-controller.txt`). `StartWandererDashComboWindow()` sets `wandererDashComboWindowTimer = 0.15`; `HeroController.Update` subtracts `Time.deltaTime` while it is > 0.
- `HeroController.Attack(dir)`: after `UpdateConfig()`, the alt-slash reset and the `cState.attacking` / `attackDuration` setup, `if (wandererDashComboWindowTimer > 0 && CurrentCrestID == "Wanderer")` it sends `WANDERER DASH COMBO` to the Sprint FSM and returns. No slash starts and `DidAttack()` is not called. This applies to any attack direction.
- The Sprint FSM handles `WANDERER DASH COMBO` only in `Idle` → `To Wanderer Combo` (`RelinquishControlNotVelocity`, `StopAnimationControl`) → `RecoilStab Dir`.
- RecoilStab can therefore be started: from `W Recoil Cancelable` by an attack press queued from 0.13 s or made until 0.3333 s after the recoil started; or by a normal attack within the next 0.15 s (until 0.4833 s).

RecoilStab chain (all three states play hero clip `Wanderer RecoilStab`, 20 fps, 6 frames, triggers at 2 and 3; the clip is not restarted by the later states):

| State | Actions | Leaves on | Time from `RecoilStab Dir` |
|---|---|---|---|
| `RecoilStab Dir` | held left → `RecoilStab L` (`FaceLeft`), held right → `RecoilStab R` (`FaceRight`), else by current facing | | 0 |
| `RecoilStab Antic` | velocity (0, 0); `Attack Normal Hornet Voice` | frame-2 trigger | 0 → 0.1 s |
| `RecoilStab Dash` | `Wanderer DashCombo Burst` on; `hornet_hunter_new_dash`; `SetVelocityByScale(speed -45, ySpeed -15)` = **45 forward, 15 down** | frame-3 trigger | 0.1 → 0.15 s |
| `RecoilStab Attack` | `StartSlash` on `Wanderer RecoilStab` (**hitbox on**); `DecelerateV2(0.9)` on entry and per FixedUpdate | clip complete → `Regain Control Normal`; `DASH HIT` → `RecoilStab Bounce` | 0.15 → 0.3 s |
| `RecoilStab Bounce` | `CancelAttack` on RecoilStab; `AffectedByGravity(True)`; `HeroController.SetStartWithDashStabBounce()` | → `Regain Control Normal` | instant |

- RecoilStab hitbox: 0.15 → 0.3583 s (0.2083 s, `Wanderer RecoilStab Efct`), so it stays live for 0.0583 s after control returns at 0.3 s unless a `DASH HIT` cancels it.
- `SetStartWithDashStabBounce()` with `forceShortDashStabBounce = False`: 3.5-unit upward ray (mask 8448) → short bounce if it hits, otherwise a normal down-spike bounce; `attack_cooldown = 0.15`; `isDashStabBouncing = true`, so the bounce jump uses `dashStabBounceJumpSpeed = 18.6` ([../mechanics/sprint.md](../mechanics/sprint.md) §3.3, §5.1).

Timing summary: input → first sprint-attack hitbox 0.1111 s; hit → recoil start one frame later; recoil → RecoilStab input accepted from 0.13 s; RecoilStab input → hitbox 0.15 s.

---

## 3. Needle strike (`Nail Arts` FSM, `WANDERER` branch)

The shared start/end sequences (release detection, `Cancel All`, `Take Control`, recoil-method selection, `Regain Partial/Full Control`, `NeedleArtRecovery` → `attack_cooldown = 0.35`) are in [../mechanics/needle-strike.md](../mechanics/needle-strike.md). `Antic Type` checks Reaper first, then Wanderer → `WANDERER`.

### 3.1 States

| State | Actions | Leaves on | Time from release |
|---|---|---|---|
| `Wanderer Antic` | `Slash_Charged` (Dagger: 18 fps, 12 frames, trigger at 2 → FINISHED); `DecelerateV2(0.85)` with `brakeOnExit = True` (velocity (0, 0) on exit); `flashFocusHeal`; `Sphere Flash` on | frame-2 trigger | 0 → **0.1111 s** |
| `Wanderer Attack` | `hornet_wanderer_needle_art`; `AffectedByGravity(False)`; `SetVelocityByScale(-45, y 0)` = 45 forward; (`DecelerateV2 0.78` disabled); `SetVelocity2d y = 0`; `AccelerateToY(0.25, target -10)` (vy −0.25 per FixedUpdate down to −10); `DecelerateXY(x 0.78)` (vx ×0.78 on entry and per FixedUpdate, snapped to 0 below 0.001); **`Current Charge Slash` activated**; `Tk2dWatchAnimationEvents` complete → FINISHED; two identical `Trigger2dEvent OnTriggerEnter2D` on `Lunge Stopper` → `ATTACK LANDED` | clip complete → `Set Finished`; `ATTACK LANDED` → `Wanderer DmgBrake` | 0.1111 → 0.6667 s |
| `Wanderer DmgBrake` | `SetVelocity2d x = 0`; waits for clip complete | → `Set Finished` | until 0.6667 s |
| `Set Finished` → `Regain Partial Control` → `Regain Full Control` | `chargeSlashRecoils = False`, so the recoil method does nothing | | instant at 0.6667 s |

The branch bypasses `Lunge?` and `Slash Recoil?`, so the config lunge fields are not read. Hornet's own movement: 45 × 0.78 = 35.1 forward after entry, ×0.78 per fixed step; stopped to vx = 0 when the `Lunge Stopper` trigger is entered.

### 3.2 `Charge Slash Wanderer` hitbox

- `DeactivateAfter2dtkAnimation`: `OnEnable` → `PlayFromFrame(0)` of the default clip `Slash_Charged Effect` (20 fps, 10 frames = 0.5 s); the object deactivates when no listed animator is playing. The root collider is enabled in the asset, so the hitbox exists for **0.5 s from activation = 0.1111 → 0.6111 s after release**. `Lunge Stopper` has the same lifetime.
- Multi-hit (`DamageEnemies.LateFixedUpdate` / `OnFixedUpdate`): `stepsToNextHit` starts at 0 (`StartDamage`). A successful evaluation sets it to `stepsPerHit = 6`; it decreases by 1 per FixedUpdate. Repeat hits on targets already hit (`DoDamage(isFirstHit: false)`) happen only when it reaches 0, i.e. every **6 fixed steps = 0.12 s**. Targets entering for the first time are evaluated immediately (`isFirstHit = true`). With 0.5 s = 25 fixed steps of activity, one target inside the hitbox from the first step is hit at steps 0, 6, 12, 18, 24: **at most 5 hits**. `hitsUntilDeath = 100` is never reached.
- Damage per hit: `RoundToInt(PlayerData.nailDamage × 0.55)`, then the shared `DamageEnemies` multipliers (imbuement, silk taunt ×1.5, Barbed Wire ×1.25; the object is `attackType = Nail`) and `HealthManager.ApplyDamageScaling`. There is no `damageMultPerHit`.
- Knockback: `magnitudeMult = 0.5`, `direction = 180` (the FSM's `SetDamageEnemyDirectionV2` actions in `Left` / `Right` are disabled).
- `NailSlashTerrainThunk doRecoil = True`, with `multiHitter` = the root `DamageEnemies`.

### 3.3 Silk gain

- Decided by `DamageEnemies.silkGeneration = FirstHit` on `Charge Slash Wanderer` and applied by `HeroController.SilkGain(HitInstance)`, called from `HealthManager.TakeDamage` for `AttackTypes.Nail` hits when the enemy is not `Shade` / `Armoured`, `DoNotGiveSilk` is false, and the hit is not a weak hit (damage ≤ 0).
- `SilkGain` with `FirstHit` calls `AddSilk(1)` only when `hitInstance.IsFirstHit` is true. `IsFirstHit` is the `isFirstHit` argument of `DoDamage`: true on the first contact of a collider during this activation (`damagedColliders.Add` succeeds), false on the repeat multi-hits.
- Result: **+1 silk per enemy (per hit collider) per needle strike**, not per strike. No FSM is involved (the `Only Award Silk First Hit` FSMs exist only on the Witch damagers).

---

## 4. Down slash (summary)

`downSlashType = Slash`: `DownAttack` alternates `DownSlash` / `DownSlashAlt` and starts the `NailSlash` in the press frame, with no antic, no control change and no velocity change. Hitbox 0.04 → 0.16 s (`DownSlashEffect`), `'Bounce'` event at 0.12 s. A pogo uses `HeroSlashBounceConfig Wanderer` (`jumpSteps = 8`, `jumpedSteps = -20`, `hideSlashOnBounceCancel = False`); rise 2.700 to 4.092 units depending on when the hit lands; `downspikeRecoveryTime = 0.09`. `BounceShared.BouncePull` (bounce objects, nail-tagged non-harpoon hits) keeps Hornet's x position unchanged for `"Wanderer"`. Full details: [../mechanics/nail-attacks-and-pogo.md](../mechanics/nail-attacks-and-pogo.md) §2, §3.4, §4.3.

---

## 5. Critical-hit passive

### 5.1 Settings (`GlobalSettings.Gameplay`)

| Setting | Asset value | Code default (`Gameplay.cs`) | Use |
|---|---|---|---|
| `wandererCrest` | `Wanderer` | – | crest check |
| `wandererCritChance` | **0.02** | 1 | chance per qualifying hit |
| `wandererCritMultiplier` | **3** | 2 | damage multiplier |
| `wandererCritMagnitudeMult` | **1.5** | 2 | knockback magnitude multiplier |
| `wandererCritEffect` | `Wanderer_Precision_strike` (GameObject) | – | hit effect |

### 5.2 State

```csharp
public struct WandererCrestStateInfo
{
    public bool QueuedNextHitCritical;
    public bool CriticalHitsLocked;
}
public WandererCrestStateInfo WandererState { get; set; }

public bool IsWandererLucky =>
    !cState.isMaggoted && Gameplay.WandererCrest.IsEquipped && playerData.silk >= 9;
```

- Both flags are runtime-only (dump: both False). `ResetWandererCrestState()` sets them to `default`.
- `ResetWandererCrestState()` is called unconditionally from `ResetAllCrestStateMinimal`, so it runs on every path listed in [hunter.md](hunter.md) §3.6 that goes through `ResetAllCrestState` / `ResetAllCrestStateMinimal`, including the paths with `clearHunterState: false` (`ClearEffects*`, every frame of the `CUTSCENE` game state, `ToolItemManager.SetActiveState`). It is not called from `TakeDamage` / `DoSpecialDamage`.
- The only writer of the two flags apart from the reset and the consume in 5.3 is the PlayMaker action `HeroSetWandererCrestState` (`QueuedNextHitCritical`, `CriticalHitsLocked`, each written only when not None). No dumped FSM contains this action.
- The silk threshold 9 is a literal in `IsWandererLucky`. `SilkSpool.BindCost` also returns 9 (`float.MaxValue` when cursed). `IsWandererLucky` is re-evaluated from `playerData.silk` at every check, so any silk loss below 9 (binding, silk skills, damage effects) ends it immediately.

### 5.3 Roll (`DamageEnemies.DoDamage`, l.1021–1033)

```csharp
bool criticalHit = false;
if ((!doesNotCriticalHit && attackType == AttackTypes.Nail && isFirstHit) || isNailAttack)
{
    if (instance.WandererState.QueuedNextHitCritical)
    {
        criticalHit = true;
        wasCriticalHitForced = true;
    }
    else if (!instance.WandererState.CriticalHitsLocked && instance.IsWandererLucky
             && Random.Range(0f, 1f) <= Gameplay.WandererCritChance * instance.GetLuckModifier())
    {
        criticalHit = true;
    }
}
...
hitInstance.CriticalHit = criticalHit;
```

- Qualifying damager: either (`doesNotCriticalHit` false **and** `attackType == Nail` **and** first hit on that collider), **or** `isNailAttack` (GameObject tagged `Nail Attack`, set in `DamageEnemies.Start`). The `isNailAttack` branch ignores `doesNotCriticalHit`, the attack type and `isFirstHit`.
- The roll is independent of `useNailDamage`; it happens once per `DoDamage` call, i.e. per target per hit.
- Chance: `Random.Range(0f, 1f) <= 0.02 × GetLuckModifier()`. `GetLuckModifier()` = 1, +0.1 with `Magnetite Dice` (`luckyDiceTool`) equipped → **2 %**, or **2.2 %** with Magnetite Dice.
- Requirements for the random crit: `CriticalHitsLocked` false, not maggoted, Wanderer equipped, silk ≥ 9.
- `QueuedNextHitCritical` forces the crit without checking `IsWandererLucky`, the crest or `CriticalHitsLocked`. It is cleared in the `finally` block of `ProcessDamageBuffer` of the `DamageEnemies` that used it (`wasCriticalHitForced`), after that damager's buffered hits of the step are delivered. All targets evaluated by that damager before the clear are forced crits.

Qualifying damagers with Wanderer equipped (`hero-hierarchy.txt`):

| Damager | Tag | attackType | doesNotCriticalHit | Qualifies |
|---|---|---|---|---|
| Wanderer `Slash`, `AltSlash`, `UpSlash`, `DownSlash`, `DownSlashAlt`, `WallSlash`, `DashStab`, `DashStabAlt`, `RecoilStab` | Nail Attack | Nail | False | every hit |
| `Charge Slash Wanderer` (needle strike) | Nail Attack | Nail | **True** | every hit, including each multi-hit (tag branch) |
| `Taunt Slash` | Nail Attack | Nail | False | every hit |
| `Attacks/Harpoon Dash Damager` (Clawline dash, `Dash Damager` of the `Harpoon Dash` FSM) | Nail Attack | Nail | True | every hit (tag branch) |
| `Attacks/Harpoon Damager` (Clawline thread hit, `DoDamage` in `Harpoon Dash`) | Untagged | Nail | True | **no** |
| `Special Attacks/Super Jump Damager` | Special Attack | Nail | False | first hit per collider |
| `Special Attacks/Super Jump Needle Throw/Damager` | Untagged | Nail | False | first hit per collider |
| `Tool Effects/Snare Loop Damager` (Silk Snare) | Untagged | Nail | False | first hit per collider |
| `Needle Throw/.../Needle Damage` (Silk Spear), `Silk Charge Damager`, `SnipeShot Trail` | Special Attack / Untagged | Heavy | False | no |
| `Screw Attack Damager` | Untagged | Nail | True | no |

Damagers on objects outside the hero hierarchy (thrown tool prefabs) follow the same rule but are not listed in the dump.

### 5.4 Effect (`HealthManager.TakeDamage`, l.1011–1020)

Applied after `IsImmuneTo`, `ApplyDamageScaling` and the rapid-bullet/bomb/storm reductions, before the Hunter combo effect and before silk gain:

- `DamageDealt = RoundToInt(DamageDealt × 3)`.
- `MagnitudeMultiplier *= 1.5` (passed to the enemy's `Recoil.RecoilByDirection`).
- Spawns `Wanderer_Precision_strike` at `transform.TransformPoint(effectOrigin)`, rotated to `hitInstance.GetOverriddenDirection(...)`. The prefab: Animator clip `Precision_Strike_Burst` (0.75 s, 40 fps, event `EnemyKillShake` at 0.025 s), `AutoRecycleSelf` after 0.7 s, `CameraShakeOnEnable` (profile `Enemy Kill`, `doFreeze = True`, vibrate), sprites `precise_strike`, `rune_blast_0001_burst_lines`, `light_effect_v02`, `White Flash`.
- `GameManager.FreezeMoment(FreezeMomentTypes.HeroDamageShort)`.
- Lag hits (`HealthManager` lag-hit path) set `CriticalHit = false`.
- Silk gain is unchanged (one `SilkGain` call per hit as usual).

### 5.5 HUD (`BindOrbHudFrame`)

- When the Wanderer crest is equipped the frame uses `wandererFrameAnims` (`BasicFrameAnims`: `AppearFromNone`, `Appear`, `Idle`, `Disappear`, `ActivateEvent`) and, after the appear animation and `Idle`, starts `WandererCrestRoutine`.
- `WandererCrestRoutine`, each unpaused frame, reads `hc.IsWandererLucky` (`wasLucky` starts false):
  - false → true: `PlayFrameAnim(wandererFullAnim)` and `wandererHarpAppearAudio`.
  - true → false: `PlayFrameAnim(wandererFullEndAnim)` and `wandererHarpDisappearAudio`.
  - The sounds play only when `!hc.IsRefillSoundsSuppressed`, `HudCanvas.IsVisible` and `ScreenFaderState.Alpha < 0.5`.
- The HUD therefore shows the silk ≥ 9 / not maggoted state, not the crit itself or `QueuedNextHitCritical` / `CriticalHitsLocked`.
- "Harp" appears in the code only in the two audio field names.
- Clip frames (`hud_assets_all.bundle`, sprites `HUD_frame_Wanderer0000`–`0012`, all with the same untrimmed bounds, so every frame is drawn in the same place):

  | Clip | Sprites |
  |---|---|
  | `HUD Frame Wanderer Appear` | 0000–0005 |
  | `HUD Frame Wanderer Idle` | 0005 |
  | `HUD Frame Wanderer End` | 0004, 0003, 0002 |
  | `HUD Frame Wanderer` | 0002–0005 |
  | `HUD Frame Wanderer Full` | 0008, 0009, 0010, 0011 |
  | `HUD Frame Wanderer Full End` | 0010, 0009, 0008, 0005 |

- Sprite contents: 0005 is the closed harp frame. 0008 has the harp opened with no strings, 0009 has the strings starting to appear, 0010 shows them fully lit and 0011 is the held "full" frame with about 7 vertical strings. Each frame is a separate drawing: the frame outline moves between 0008, 0009, 0010 and 0011, and no stringless version of 0011 exists. Steel Soul uses `HUD_Frames_Wanderer_SS0000`–`0012` from another collection (via `SteelSoulAnimProxy`).

---

## 6. Bind

Vanilla. `canBind = True`. The `Bind` FSM's crest checks (`Can Bind?`: Warrior, Witch, Spell; `Can Quick Craft?` and `Quick Craft?`: Toolmaster) do not include Wanderer, and no Bind-related C# references the Wanderer crest. The bind cost is the normal 9 silk, which also ends `IsWandererLucky` unless at least 9 silk remain.

---

## 7. Source references

Decompiled (`scratchpad/decomp/`):
- `HeroController.cs`: `WandererCrestStateInfo` (l.224–229), `DASHCOMBO_WINDOW_TIME` (l.391), `wandererDashComboWindowTimer` (l.572), `WandererState` (l.1486), `IsWandererLucky` (l.1492–1504), timer tick in `Update` (l.2488–2491), `Attack` dash-combo branch (l.4120–4124), `StartWandererDashComboWindow` (l.5115), `GetLuckModifier` (l.5735), `SilkGain(HitInstance)` (l.5860), `ResetAllCrestStateMinimal` (l.5947–5956), `ResetWandererCrestState` (l.6013), `CanRecoil` (l.6532), `Recoil` (l.6564), `RegainControl(bool)` (gravity restore).
- `DamageEnemies.cs`: `doesNotCriticalHit` (l.269), `wasCriticalHitForced` (l.316), `Start` (`isNailAttack`, l.557), `OnFixedUpdate` (l.661), `LateFixedUpdate` multi-hit (l.674–728), `EvaluateDamage` (l.732), `ProcessDamageBuffer` and forced-crit clear (l.783–862), `DoDamage` (l.932), crit roll (l.1021–1033), `hitInstance.CriticalHit` (l.1158), `DASH HIT` / `NonBouncer` (l.1181–1199).
- `HealthManager.cs`: `TakeDamage` (l.943), crit application (l.1011–1020), `NailHitEnemy` / silk branch (l.1101–1117), lag hit `CriticalHit = false` (l.2192).
- `HutongGames.PlayMaker.Actions/HeroSetWandererCrestState.cs`, `SetVelocityByScale.cs`, `DecelerateV2.cs`, `DecelerateXY.cs`, `AccelerateToY.cs`.
- `BindOrbHudFrame.cs`: Wanderer fields (l.123–137), crest frame selection (l.298, l.364–373), `WandererCrestRoutine` (l.660–690).
- `GlobalSettings/Gameplay.cs`: wanderer fields (l.157–170), getters (l.556–564).
- `SilkSpool.cs`: `BindCost` (l.129–139). `BounceShared.cs`: `BouncePull` Wanderer case (l.37). `NailSlash.cs`: `NailSlashRecoil.Add` (l.65–67), `StartSlash` (l.80–100), `CancelAttack` (l.102–117), trigger counter (l.150–170), `OnAnimationCompleted` (l.186).

Dumps (`raw-dump/`):
- `crests.txt` – Wanderer slots, localised fields, sprites, `heroConfig = Wanderer`, Dagger library clip list (l.247–345).
- `hero-configs.txt` – `configs[4]` Wanderer values and attack objects (l.9978–13041; `Charge Slash Wanderer` l.12513).
- `hero-hierarchy.txt` – `Attacks/Wanderer` root (l.16455), `RecoilStab` (l.18926), `Charge Slash Wanderer` (l.31444), `Effects/Wanderer DashCombo Burst` (l.40422), all hero damagers (tags, `doesNotCriticalHit`).
- `hero-controller.txt` – `DASHCOMBO_WINDOW_TIME = 0.15`, `wandererDashComboWindowTimer = 0`, `WandererState` (l.2100–2102), `RECOIL_HOR_*`.
- `global-settings.txt` – `wandererCrest`, `wandererCritChance`, `wandererCritMultiplier`, `wandererCritMagnitudeMult`, `wandererCritEffect` (l.206–210), `luckyDiceTool`.
- `referenced-objects.txt` – `HeroSlashBounceConfig Wanderer` (l.551), `Wanderer_Precision_strike` (l.41793).
- `animator-timelines.txt` – `Precision_Strike_Burst` (l.13561).
- `animations-tk2d.txt` – `Hornet CrestWeapon Dagger Anim` (l.228), `HUD Anim` / `HUD anim Steel Soul` Wanderer frame clips (l.497–685).
- `engine-settings.txt` – `Time.fixedDeltaTime = 0.02`.
- `fsm_Hero_Hornet(Clone)_Sprint.txt` – `Idle` (`WANDERER DASH COMBO`), `Start Sprint`, `Start Attack`, `Wanderer *`, `W *`, `RecoilStab *`, `Hit Enemy Wait 2`, `To Wanderer Combo`, `Set Atk Cooldown`.
- `fsm_Hero_Hornet(Clone)~Attacks~Wanderer~DashStab_Send Hit To Player.txt`, `...DashStabAlt...`, `...RecoilStab...`.
- `fsm_Hero_Hornet(Clone)_Nail Arts.txt` – `Antic Type` (l.617), `Wanderer Antic` (l.705), `Wanderer Attack` (l.729), `Wanderer DmgBrake` (l.793).
- `fsm_Hero_Hornet(Clone)_Bind.txt` – `Can Bind?` crest checks (l.490).
- `fsm_Hero_Hornet(Clone)_Harpoon Dash.txt`, `fsm_Hero_Hornet(Clone)_Superjump.txt`, `fsm_Hero_Hornet(Clone)_Tool Attacks.txt` – damager variables used in 5.3.
- `fsm__GameCameras~HudCamera~...~Bind Orb_Display.txt` – `BindOrbHudFrame` calls.
