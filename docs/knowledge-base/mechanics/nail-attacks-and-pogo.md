# Nail attacks and the down attack / pogo

This page covers the basic nail attacks driven by `HeroController` (neutral slash and its alternate, up slash,
wall slash) and the down attack for all three `HeroControllerConfig.DownSlashTypes` (`DownSpike`, `Slash`,
`Custom`). The Hunter path (config `Default`) is described in full. Charged slash, dash stab, needle arts and
tools are not covered here.

Conventions on this page:

- "@60" means the same duration expressed in frames at 60 fps (`seconds x 60`). It is a conversion only.
  The real number of rendered frames depends on the frame rate.
- Fixed timestep: `engine-settings.txt` has `Time.fixedDeltaTime = 0.01999999` (**50 Hz**),
  `Time.maximumDeltaTime = 0.3333333`, `Time.timeScale = 1`, `Physics2D.gravity = (0, -60)`,
  `Physics2D.simulationMode = FixedUpdate`, `Application.targetFrameRate = -1`, `QualitySettings.vSyncCount = 1`.
  The decompiled code only reads `Time.fixedDeltaTime` and never assigns it (`GameManager.cs:3206`). Everything
  counted in "steps" below is counted in `FixedUpdate`, so 1 step = 0.01999999 s (written as 0.02 s where the
  difference does not matter). Inside `FixedUpdate`, `Time.deltaTime` returns the fixed timestep.
- Physics step order (Unity): all `FixedUpdate` scripts run, then the 2D physics step integrates
  (semi-implicit Euler: `v += gravity x gravityScale x dt`, then `position += v x dt`), then trigger and
  collision callbacks (`OnTriggerEnter2D`, `OnCollisionEnter2D`, ...) run.
- Timers named `*_time`, `*Timer` or `*_cooldown` in `HeroController.Update` are advanced by `Time.deltaTime` once
  per rendered frame.
- tk2d clip durations are `frames / fps`. tk2d advances clips by `deltaTime x fps` once per rendered frame
  (`tk2dSpriteAnimator.UpdateAnimation`). A frame `n` is reached when the clip time is `>= n`, which means
  `n / fps` seconds after the start.
- Facing: the hero faces right when `transform.localScale.x < 0` and left when `> 0`.
- The source files are the decompiled `Assembly-CSharp` (`HeroController.cs` etc.) plus the
  `tk2dSpriteAnimator` class from `TeamCherry.TK2D.dll`, and the runtime dump in `raw-dump/` (section 7).

---

## 1. Attack input to attack flow (`HeroController`)

### 1.1 Which config is active

`HeroController.UpdateConfig()` is called at the start of every `Attack()`:

- When `crestConfig` is set, it picks the `configs[]` group whose `Config` equals `crestConfig`. When the
  config is the Warrior config and `warriorState.IsInRageMode` is true, it also picks the matching
  `specialConfigs[]` override group.
- When `crestConfig` is null, it uses **`configs[0]`**, which is `Default` (`hero-configs.txt`).
- `crestConfig` is assigned from `crestByName.HeroConfig` (`HeroController.cs:5969`). In `crests.txt`,
  `Hunter`, `Hunter_v2` and `Hunter_v3` all have `heroConfig=null`, so **all three Hunter crests use
  `configs[0]` = `Default`**.

| Crest (`crests.txt`) | heroConfig | configs[] index |
|---|---|---|
| Hunter, Hunter_v2, Hunter_v3 | null -> Default | 0 |
| Cloakless (hidden) | Cloakless | 1 |
| Reaper | Reaper | 2 |
| Warrior (Beast) | Warrior (`HeroControllerConfigWarrior`) | 3 (+ `specialConfigs[0]` in rage) |
| Wanderer | Wanderer | 4 |
| Toolmaster (Architect) | Toolmaster | 5 |
| Witch, Cursed (hidden) | Whip | 6 |
| Spell (Shaman) | Shaman | 7 |

### 1.2 Input, buffering and `CanAttack`

The input is read in `LookForInput()`, which runs in `Update()`:

- `Attack.WasPressed`:
  - if `acceptingInput && CanAttack()` -> `DoAttack()` immediately (same frame).
  - otherwise `attackQueueSteps = 0; attackQueuing = true` (the attack is buffered).
- Buffered attack: later in `LookForInput`, while acceptingInput is true,
  `if (Attack.IsPressed && attackQueueSteps <= ATTACK_QUEUE_STEPS && CanAttack() && attackQueuing) DoAttack();`
  - `attackQueueSteps++` runs once per `FixedUpdate` while `attackQueuing`.
  - `ATTACK_QUEUE_STEPS = 8`, so the buffer lasts 8 fixed steps = **0.16 s**. The button must stay held:
    `attackQueuing = false` as soon as `Attack.IsPressed` is false.
  - `ResetInputQueues()` clears the buffer.
- `startWithAttack` (set by `SetStartWithAttack()`, used by FSMs) makes `RegainControl()` call `DoAttack()`
  directly.
- `InputHandler` has its own press buffer (`BUTTON_QUEUE_TIME = 0.1`, `input-handler.txt`; a press sets the
  button's timer to 0.1 s, decremented by `Time.unscaledDeltaTime`). It is read only through
  `GetWasButtonPressedQueued`, which `HeroController` uses for Jump (in `RegainControl`), `SlideSurface` uses for
  Jump, and FSMs use via the `GetWasButtonPressedQueued` action (`Sprint` FSM). The nail attack input does not
  use it.

`CanAttack()`:

```
attack_cooldown > 0  -> false
else CanAttackAction():
  !cState.attacking && (!cState.dashing || dashingDown) && !dead && !hazardDeath && !hazardRespawning
  && !controlReqlinquished && hero_state not in {no_input, hard_landing, dash_landing} && CanInput()
  && (!Down.IsPressed || CanDownAttack())
CanDownAttack(): allowAttackCancellingDownspikeRecovery
                 || (!cState.downSpikeBouncing && !cState.downSpikeRecovery)
```

`attack_cooldown` is decremented by `Time.deltaTime` every `Update`.

### 1.3 `DoAttack()` direction selection

`DoAttack()` clears `queuedWallJumpInterrupt`, calls `ResetLook()`, cancels a dash (`CancelDash`) if dashing,
and clears `cState.recoiling`. It then picks the direction:

- `Up.IsPressed` -> `Attack(upward)`.
- else `Down.IsPressed` -> `Attack(downward)` if `allowAttackCancellingDownspikeRecovery || !cState.onGround`.
  Otherwise (on the ground) -> `Attack(normal)`.
- else -> `Attack(normal)`.

`allowAttackCancellingDownspikeRecovery` is only set to true in the `startWithBalloonBounce` branch of
`RegainControl` (`HeroController.cs:7707`). It is set back to false when a downspike becomes active and in all
`DownspikeBounce*` methods.

### 1.4 `Attack(dir)`

In order:

1. `TrySetCorrectFacing(force: true)`, attack audio (the rage table in Warrior rage), `cState.floating = false`,
   `cState.attackCount++`, `UpdateConfig()`.
2. **Alternate-slash reset.** `if (Time.timeSinceLevelLoad - altAttackTime > Config.AttackRecoveryTime +
   ALT_ATTACK_RESET || attackDir != prevAttackDir) cState.altAttack = false;` with `ALT_ATTACK_RESET = 0.5`.
   For Default this is `0.15 + 0.5 = 0.65 s` after the previous slash start in the same direction.
3. **Attack state and duration.** Unless the attack is downward **and** `DownSlashType == DownSpike`:
   `cState.attacking = true; attackDuration = Config.AttackDuration;` and with quickening
   `attackDuration *= 1 / Config.QuickAttackSpeedMult`. A DownSpike never sets `cState.attacking`.
4. Wanderer only: if `wandererDashComboWindowTimer > 0` and the crest is `"Wanderer"`, it sends
   `WANDERER DASH COMBO` to the Sprint FSM and returns. No slash, no `DidAttack`.
5. **Wall slash.** When `(wallSliding || wallScrambling) && dir == normal` and the Toolmaster crest is not
   equipped:
   - if `Config.WallSlashSlowdown`: `vy = vy / 2`, then if `vy < -5`, `vy = -5`.
   - `wallSlashing = true`, `SlashComponent = wallSlash`, `currentSlashDamager = wallSlashDamager`.
   - With Toolmaster equipped, wall-sliding plus a normal attack instead flips the sprite, calls
     `CancelWallsliding()` and does a normal slash.
6. Otherwise:
   - **normal**: if `cState.altAttack` -> `alternateSlash`, and `altAttack = false`. Else -> `normalSlash`, and
     `altAttack = true` if an `alternateSlash` exists. Consecutive neutral slashes therefore alternate
     Slash / AltSlash while they stay inside the 0.65 s window (Default).
   - **upward**: if wall-sliding, `rb2d.MovePosition(+/-0.8 x)` away from the wall, then
     `AttackCancelWallSlide()` (flip + cancel wall slide). It alternates `upSlash` / `altUpSlash` the same way
     (`altUpSlash` is null in every config). `cState.upAttacking = true`.
   - **downward**: `AttackCancelWallSlide()`, then `DownAttack(ref isSlashing)` (section 2).
7. If `isSlashing` (all slashes, and the `Slash` down type): the damager direction is set with
   `currentSlashDamager.SetDirection(...)`. Wall-sliding or scrambling: 180 if facing right, else 0. Normal:
   0 when facing right, 180 when facing left. Upward: 90. Downward: 270. Then
   `altAttackTime = Time.timeSinceLevelLoad`, `SlashComponent.StartSlash()`, `DidAttack()`.
8. `prevAttackDir = attackDir`.

### 1.5 Duration, cooldown, quickening

`DidAttack()`:

```
float num = Config.AttackDuration;                         // NOT divided by the quickening multiplier
attack_cooldown = IsUsingQuickening ? Config.QuickAttackCooldownTime : Config.AttackCooldownTime;
if (attack_cooldown < num) attack_cooldown = num;
```

- `cState.attacking` ends in `Update` when `attack_time >= attackDuration` (`ResetAttacks()` +
  `animCtrl.StopAttack()`). `attack_time` only advances while `cState.attacking && !cState.dashing`.
- The time from one attack to the next possible attack is `attack_cooldown`. During quickening the cooldown
  is floored at the un-quickened `AttackDuration`. For Default the quickened cooldown is therefore
  `max(0.205, 0.35) = 0.35 s`, not 0.205 s.
- Quickening (`IsUsingQuickening` = `quickeningTimeLeft > 0`) also:
  - plays the slash effect clip at `fps x QuickAttackSpeedMult` (`NailSlash.PlaySlash`),
  - plays the hero body clips at `x QuickAttackSpeedMult` (`HeroAnimationController`, line 589),
  - raises the slash audio pitch by 0.05.
- **AttackRecoveryTime.** While `cState.attacking && attack_time < Config.AttackRecoveryTime`, the attack
  cannot be cancelled into the following: dash (`CanDash`), back dash, sprint, harpoon dash, super jump,
  cast, bind, special, quick map, inspect, needolin, wall scramble. It also cannot be cancelled by turning
  (`DoMovement` -> `TrySetCorrectFacing` -> `CancelAttack`, or `CancelAttackNotSlash` when
  `CanTurnWhileSlashing`). `IsAttackLocked()` returns `attack_time < AttackRecoveryTime`.
  `HeroJump()` cancels the attack only when `attack_time >= AttackRecoveryTime`.

### 1.6 Comparison across configs

Raw values from `hero-configs.txt` (config values block of each `configs[i]`):

| Field | Default | Cloakless | Reaper | Warrior | Warrior (rage) | Wanderer | Toolmaster | Whip | Shaman |
|---|---|---|---|---|---|---|---|---|---|
| attackDuration | 0.35 | 0.35 | 0.35 | 0.3 | 0.3 (`rageAttackDuration`) | 0.25 | 0.35 | 0.4 | 0.35 |
| quickAttackSpeedMult | 1.5 | 2 | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 |
| attackRecoveryTime | 0.15 | 0.1 | 0.3 | 0.2 | 0.2 | 0.1 | 0.2 | 0.3 | 0.1 |
| attackCooldownTime | 0.41 | 0.41 | 0.5 | 0.39 | 0.32 | 0.3 | 0.45 | 0.45 | 0.5 |
| quickAttackCooldownTime | 0.205 | 0.205 | 0.25 | 0.195 | 0.195 | 0.15 | 0.225 | 0.2375 | 0.25 |
| canTurnWhileSlashing | False | False | False | False | False | False | False | False | True |
| wallSlashSlowdown | True | True | True | True | True | False | True | False | True |
| downSlashType | DownSpike | DownSpike | Custom | Custom | Custom | Slash | Custom | Custom | Custom |
| downSlashEvent | "" | "" | "RPR DOWNSLASH" | "WARRIOR DOWNSLASH" | same | "" | "TOOLMASTER DOWNSLASH" | "WITCH DOWNSLASH" | "SHAMAN DOWNSLASH" |
| downspikeAnticTime | 0.1 | 0.07 | 0.07 | 0.07 | 0.07 | 0.09 | 0.07 | 0.1 | 0.1 |
| downspikeTime | 0.1 | 0.04 | 0.15 | 0.08 | 0.08 | 0.16 | 0.16 | 0.16 | 0.1 |
| downspikeSpeed | 26 | 33 | 13 | 20 | 20 | 15 | 5 | 5 | 26 |
| downspikeRecoveryTime | 0.25 | 0.25 | 0.25 | 0.25 | 0.25 | 0.09 | 0.25 | 0.25 | 0.25 |
| downspikeBurstEffect | True | True | False | True | True | True | False | False | True |
| downspikeThrusts | True | True | False | True | True | True | False | True | True |
| heroAnimOverrideLib | null | Hornet Cloakless Anim | Hornet CrestWeapon Scythe Anim | Hornet CrestWeapon Warrior Anim | same | Hornet CrestWeapon Dagger Anim | Hornet CrestWeapon Drill Lance Anim | Hornet CrestWeapon Whip Anim | Hornet CrestWeapon Shaman Anim |

Warrior rage values come from `HeroControllerConfigWarrior`, which overrides `AttackDuration`,
`AttackRecoveryTime`, `AttackCooldownTime` and `QuickAttackCooldownTime` with `rage*` fields while
`WarriorState.IsInRageMode` is true. The fields are `rageAttackDuration = 0.3`,
`rageAttackRecoveryTime = 0.2`, `rageAttackCooldownTime = 0.32` and `rageQuickAttackCooldownTime = 0.195`.

`downspikeAnticTime`, `downspikeTime`, `downspikeSpeed`, `downspikeBurstEffect` and `downspikeThrusts` are
only read by the DownSpike code path in `HeroController` (`Update`, `FixedUpdate`, `DownAttack`,
`Downspike`). For `Slash` and `Custom` configs they have no effect in `HeroController`. `downspikeRecoveryTime`
is read by the recovery timer, which `FinishDownspike()` starts for every down-attack type, including Custom
FSMs that call `FinishDownspike`.

Derived attack timings. Both cooldowns follow `DidAttack`, and the effective cooldown is the minimum time
between two attack starts:

| Config | attacking state lasts (normal / quickened) | effective cooldown, normal | effective cooldown, quickened |
|---|---|---|---|
| Default | 0.35 / 0.2333 | max(0.41, 0.35) = **0.41** | max(0.205, 0.35) = **0.35** |
| Cloakless | 0.35 / 0.175 | 0.41 | 0.35 |
| Reaper | 0.35 / 0.2333 | 0.5 | 0.35 |
| Warrior | 0.3 / 0.2 | 0.39 | 0.3 |
| Warrior (rage) | 0.3 / 0.2 | 0.32 | 0.3 |
| Wanderer | 0.25 / 0.1667 | 0.3 | 0.25 |
| Toolmaster | 0.35 / 0.2333 | 0.45 | 0.35 |
| Whip | 0.4 / 0.2667 | 0.45 | 0.4 |
| Shaman | 0.35 / 0.2333 | 0.5 | 0.35 |

Attack objects per config (`hero-configs.txt`, fields `NormalSlash / AlternateSlash / UpSlash / AltUpSlash /
DownSlash / AltDownSlash / WallSlash`):

| Config | Normal | Alternate | Up | AltUp | Down | AltDown | Wall |
|---|---|---|---|---|---|---|---|
| Default | Slash | AltSlash | UpSlash | null | DownSlash (`Downspike`) | null | WallSlash |
| Cloakless | Kick | AltKick | UpKick | null | DownKick (`Downspike`) | null | WallKick |
| Reaper | Slash | AltSlash | UpSlash | null | DownSlash (`Downspike`) | null | WallSlash |
| Warrior | Slash | AltSlash | UpSlash | null | DownSlash (`Downspike`) | null | WallSlash |
| Wanderer | Slash | AltSlash | UpSlash | null | DownSlash (`NailSlash`) | DownSlashAlt (`NailSlash`) | WallSlash |
| Toolmaster | AltSlash | Slash | UpSlash | null | DownSlash (`Downspike`) | null | WallSlash |
| Whip | AltSlash | Slash | UpSlash | null | DownSlash (`Downspike`) | null | WallSlash |
| Shaman | AltSlash | Slash | UpSlash | null | DownSlash (`NailSlash`, + `NailSlashTravel`, PlayMakerFSM) | null | WallSlash |

For Reaper, Warrior, Toolmaster, Whip and Shaman (Custom type), `HeroController.downSlash`/`downSpike` are
not started by `DownAttack()`. The Custom path only sends an FSM event.

---

## 2. Down attack types

`HeroController.DownAttack(ref bool isSlashing)` always calls `CancelQueuedBounces()` first. It then switches on
`Config.DownSlashType`:

| Type | Configs | What happens |
|---|---|---|
| `DownSpike` (enum 0) | Default (Hunter x3), Cloakless | `isSlashing = false`. A HeroController-driven antic, then a thrust, then recovery (section 3). `cState.attacking` is **not** set. `DidAttack()` is called. |
| `Slash` (1) | Wanderer | Picks `downSlash` / `altDownSlash` with the same alternation as other slashes and sets `cState.downAttacking = true`. `isSlashing` stays true, so `Attack()` sets the direction to 270 and calls `NailSlash.StartSlash()` + `DidAttack()` in the same frame. |
| `Custom` (2) | Reaper, Warrior, Toolmaster, Whip (Witch, Cursed), Shaman | `isSlashing = false`. `crestAttacksFSM.SendEvent(Config.DownSlashEvent)` + `DidAttack()`. The rest is done by the `Crest Attacks` FSM (section 6). `cState.attacking` **is** set, because the type is not DownSpike. |

---

## 3. Hunter / Default DownSpike in detail

Default config values: `downspikeAnticTime = 0.1`, `downspikeTime = 0.1`, `downspikeSpeed = 26`,
`downspikeRecoveryTime = 0.25`, `downspikeBurstEffect = True`, `downspikeThrusts = True`.
Constants (`hero-controller.txt`): `DOWNSPIKE_ANTIC_CLAMP_VEL_Y = MinMaxFloat(Start = -12, End = 12)`,
`DOWNSPIKE_ANTIC_DECELERATION = 0.8`, `DOWNSPIKE_LAND_RECOVERY_TIME = 0.1666667`.

### 3.1 Preconditions

- Down must be held, and the hero must be airborne. On the ground, Down + Attack produces a normal slash
  unless `allowAttackCancellingDownspikeRecovery`.
- `CanAttack()` must be true: `attack_cooldown <= 0`, not in `downSpikeRecovery` or `downSpikeBouncing`
  (via `CanDownAttack`), control not relinquished, etc.

### 3.2 Timeline from the button press

**Frame F0, `Update` -> `LookForInput` -> `DoAttack` -> `Attack(downward)` -> `DownAttack`:**

1. `CancelQueuedBounces()`.
2. `downSpikeHorizontalSpeed = -26` if `localScale.x > 0` (facing left), else `+26` (facing right).
3. `linearVelocity = rb2d.linearVelocity` is read **before** control is relinquished.
4. Because `DownspikeThrusts` is true:
   - `RelinquishControl()`. This calls `CancelDownSpike()`, and if control was held it also calls
     `ResetInput()`, `ResetMotion()` (velocity -> 0, cancels jump, double jump, dash, bounce, recoil and wall
     slide, then `ResetGravity()`), `IgnoreInput()` (`acceptingInput = false`), sets
     `controlReqlinquished = true`, and calls `ResetLook()`, `CancelAttack()` and `CancelJump()`.
   - `linearVelocity.y = Clamp(linearVelocity.y, -12, 12)`.
   - `AffectedByGravity(false)`.
   - (With `DownspikeThrusts = false` it would instead `CancelJump()` and set a positive `vy` to 0, without
     relinquishing control or disabling gravity.)
5. `cState.downSpikeAntic = true; downSpikeTimer = 0`.
6. `currentDownspike = downSpike` (the `DownSlash` object). `altDownSpike` is null for Default, so there is no
   alternation.
7. `rb2d.linearVelocity = linearVelocity`. The pre-press x velocity is kept and y is clamped to [-12, 12].
8. `DidAttack()` -> `attack_cooldown = max(0.41, 0.35) = 0.41` (quickened: `max(0.205, 0.35) = 0.35`).

Later in the same `Update` (antic block, `HeroController.cs:2337`): `downSpikeTimer (0) >= 0.1` is false, so
`downSpikeTimer += Time.deltaTime`.

**Antic phase (F0 ... F5 at 60 fps):**

- `Update`: `downSpikeTimer += Time.deltaTime` each frame.
- `FixedUpdate`: `rb2d.linearVelocity *= 0.8` every step while `downSpikeAntic && DownspikeThrusts &&
  downSpikeTimer < 0.1`. Gravity is off. The terminal-velocity clamp is skipped because control is
  relinquished. With about 5 fixed steps in 0.1 s, the velocity at the end of the antic is about
  `0.8^5 = 0.328` of the clamped entry velocity. The exact number of steps depends on how frames line up with
  physics steps.
- Animation: `HeroAnimationController` plays `"DownSpike Antic"` while `cState.downSpikeAntic`. Knight
  library: fps 42.85714, 3 frames, wrap Once = **0.07 s** (4.2 @60), after which the last frame holds.
- No player input is processed (`acceptingInput = false`). The antic cannot be cancelled by the player.

**Antic -> active (frame F6 at a steady 60 fps):** in the first `Update` where `downSpikeTimer >= 0.1`:

```
if (Config.DownspikeBurstEffect) downspikeBurstPrefab.SetActive(true);   // "Downspike Burst"
downSpikeTimer = 0;  cState.downSpikeAntic = false;  cState.downSpiking = true;
allowAttackCancellingDownspikeRecovery = false;
currentDownspike.StartSlash();
```

`Downspike.StartSlash()` sets `enemyDamager.ExtraUpDirection` to 45 when facing right or 135 when facing left.
It then runs `OnSlashStarting()`, which does the imbuement, sets `localScale` to `scale` (or the Long Needle
scale), and plays the vibration. After that it plays audio, starts `anim.PlayFromFrame("DownSlashEffect", 0)`,
enables the mesh and the `Clash Tink` polygon, sets `CanDamageEnemies = IsDamagerActive = true`, calls
`heroBox.HeroBoxDownspike()` (hurtbox offset (0, -0.1), size (0.4554, 1.218)), runs `OnPlaySlash()`
(`DamageEnemies.StartDamage()`), and finally sets **`poly.enabled = true`**.

**Delay from input to active hitbox (Default DownSpike):**

- It equals the number of rendered frames needed for the accumulated `Time.deltaTime` to reach `0.1`. The
  first increment happens in the press frame itself, so the hitbox is enabled in the `Update` of frame
  `F0 + ceil(0.1 / dt)`.
- At a steady 60 fps that is **6 frames = 0.1 s** after the press frame. In float32, 6 x (1/60) accumulates to
  exactly 0.1, which passes `>= 0.1`. Other rates: 30 fps gives 3 frames (0.1 s), and 144 fps gives 15 frames
  (0.1042 s).
- The collider is enabled inside `Update`. Unity reports trigger contacts during the physics step, so the
  first possible contact is at the next `FixedUpdate` after that frame, 0 to 0.02 s later.

**Active (thrust) phase, `FixedUpdate` -> `Downspike()`:**

```
if (downSpikeTimer > Config.DownSpikeTime) { FinishDownspike(); return; }
if (Config.DownspikeThrusts) rb2d.linearVelocity = (downSpikeHorizontalSpeed, -DownspikeSpeed);  // (+/-26, -26)
downSpikeTimer += Time.deltaTime;   // 0.02 per step
```

- Velocity is a 45 degree diagonal down and forward, `(+/-26, -26)`, with magnitude 36.77.
- With the 0.01999999 s step, the float32 timer before each step is 0, 0.01999999, 0.03999998, 0.059999973,
  0.07999996, 0.09999995. That gives **6 thrust steps (0.12 s of physics time)**. `FinishDownspike()` runs on the
  7th step (timer 0.11999994 > 0.1). Measured from the frame that enabled the hitbox, the end therefore comes
  0.12 to 0.14 s later (7.2 to 8.4 @60), depending on the frame / physics phase.
- Unobstructed travel during the thrust: 6 x 0.01999999 x 26 = **3.12 units down and 3.12 units forward**.
  Gravity is off (`gravityScale = 0` from `AffectedByGravity(false)`), and the `MAX_FALL_VELOCITY = 30` clamp is
  skipped while control is relinquished, so vy stays exactly -26.
- Animation: `"DownSpike"` (Knight: fps 25, 4 frames, wrap LoopSection loopStart 2, so frames 0-1 once in
  0.08 s and then frames 2-3 loop). The slash effect `"DownSlashEffect"` on the DownSlash object
  (`Hero_Hornet(Clone)/Attacks/Default/DownSlash`) is in the
  Knight library: fps 33.33333, 5 frames, wrap Once, loopStart 1, **no trigger frames**, 0.15 s, then it
  holds. Because the clip has no trigger frames, `Downspike.OnAnimationEventTriggered` never fires.
  `IsDamagerActive` therefore stays true for the whole thrust (`waitForBounceTrigger = False`).
- Hitting the ground ends it early: `HandleCollisionTouching` (`OnCollisionEnter2D` / `Stay2D`) calls
  `FinishDownspike()` when `cState.downSpiking` and the collision side is `bottom`. It also spawns
  `nailTerrainImpactEffectPrefabDownSpike` and calls `NailSlashTerrainThunk.ReportDownspikeHitGround`.

**End, `FinishDownspike(standardRecovery: true)`:**

```
cState.downSpiking = false;
downSpikeRecoveryTimer = standardRecovery ? 0 : 0.1;
if (!cState.floating) {
    cState.downSpikeRecovery = true;
    ResetGravity(); RegainControl();
    crestAttacksFSM.SendEvent("CREST ATTACK CANCEL");
    if (!startWithBalloonBounce) rb2d.linearVelocity = (vx, -10);
}
currentDownspike.CancelAttack();   // poly, mesh and Clash Tink off, HeroBoxNormal(), damagers EndDamage()
```

**Recovery (`Update`):**

- In the air, `downSpikeRecovery` ends when `downSpikeRecoveryTimer >= Config.DownspikeRecoveryTime = 0.25 s`.
- On the ground it ends at `>= DOWNSPIKE_LAND_RECOVERY_TIME = 0.1666667 s`. It is the same timer, checked
  against the ground threshold while `onGround`.
- While `downSpikeRecovery` is true: `CanDownAttack()` is false (no new down attack unless
  `allowAttackCancellingDownspikeRecovery`), `CanJump()` is false, and `Move()` zeroes horizontal input on the
  ground. Other attacks are limited only by `attack_cooldown` (0.41 s from the press).
- Animation: `"Downspike Recovery"` (Knight: fps 15, 4 frames = 0.2667 s) in the air, or
  `"Downspike Recovery Land"` (fps 15, 3 frames = 0.2 s) on the ground. These play while
  `downSpikeRecovery && !playedDownSpikeBounce && !doubleJumping`.

**Whole sequence without a hit (air, 60 fps):** 0.1 s antic, then 0.12 to 0.14 s thrust, then 0.25 s
recovery, which totals about 0.47 to 0.49 s. The next attack of any direction is gated by `attack_cooldown =
0.41 s` from the press. The next down attack is gated by the end of `downSpikeRecovery`.

### 3.3 Cancelling

The player cannot cancel the antic or the thrust, because input is ignored while control is relinquished.
Code paths that end it:

- `CancelAttack()` -> `CancelAttackNotSlash()`: if `downSpikeAntic`, it clears it and calls
  `FinishDownspike()` (unless already spiking). If `downSpiking`, it calls `FinishDownspike()`.
- `RelinquishControl()` -> `CancelDownSpike()`: clears `downSpikeAntic` and the `startWithDownSpikeBounce*`
  flags, and calls `FinishDownspike()` if spiking.
- `ResetMotion()` sets `cState.downSpiking = false`.
- `CancelDownspike()` / `CancelDownspikeAndDamage()` only cancel the `Downspike` attack object. The second one
  also sets `IsDamagerActive = false`.
- Ground contact (above) and a successful bounce (section 4).

### 3.4 Comparison: Wanderer `Slash`-type down attack, time to active hitbox

Wanderer (`downSlashType = Slash`) uses `NailSlash` objects `DownSlash` / `DownSlashAlt` (library
`Hornet CrestWeapon Dagger Anim`). Both have `animName = "DownSlashEffect"`, `bounceConfig = Wanderer`, and
`scale` (0.80, 1.00, 1.00) / (-0.80, 1.00, 1.00).

- At the press (F0), `Attack()` sets `cState.attacking`, `attackDuration = 0.25`, `cState.downAttacking`, and
  direction 270, then calls `NailSlash.StartSlash()` in the same frame. There is no antic, no
  `RelinquishControl`, no gravity change and no velocity change.
- Objects: `Hero_Hornet(Clone)/Attacks/Wanderer/DownSlash` (localPos (-0.008, -0.143), localScale (0.8, 1, 1))
  and `.../Wanderer/DownSlashAlt` (localPos (0.05, -0.128), localScale (-0.8, 1, 1)). `DamageEnemies`:
  `useNailDamage = True`, `nailDamageMultiplier = 1`, `direction = 270`, `manualTrigger = False`,
  `multiHitter = False`. DownSlash polygon (trigger, disabled at rest): (2.187,-0.237) (2.101,-1.491)
  (1.269,-2.21) (0.146,-2.533) (-1.181,-2.26) (-1.817,-1.591) (-1.841,-0.246).
- Clip `DownSlashEffect` (Dagger lib): fps 25, 5 frames, wrap Once, triggers `[1:'' 3:'Bounce' 4:'']`.
  - frame 1, the first counted trigger, at 1/25 = **0.04 s (2.4 @60)**: `SetCollidersActive(true)`.
  - frame 3 `'Bounce'` at 0.12 s (7.2 @60): bounce-timing event (section 4.3). It is not counted by
    `animTriggerCounter`.
  - frame 4, the second counted trigger, at 0.16 s (9.6 @60): colliders off, `IsDamagerActive = false`,
    `enemyDamager.EndDamage()`.
  - complete at 0.2 s: `CancelAttack()`.
  - quickened (fps x 1.5 = 37.5): colliders on at 0.0267 s, off at 0.1067 s.
- Hero body clip: `DownSlash` / `DownSlashAlt` (Dagger lib): fps 25, 7 frames = 0.28 s.

| | Input -> hitbox active | Hitbox active for | Movement |
|---|---|---|---|
| Default DownSpike | 0.1 s (6 frames @60 at steady 60 fps) | 0.12 to 0.14 s (6 thrust steps + finish step) | relinquished, gravity off, (+/-26, -26) |
| Wanderer Slash | 0.04 s (2.4 @60; reached in the 3rd rendered frame counting the press frame at 60 fps) | 0.12 s (0.04 -> 0.16) | unchanged (normal air control and gravity) |

The DownSpike hitbox appears 0.06 s (3.6 @60) later than Wanderer's down slash.

---

## 4. Successful pogo hit

### 4.1 Detection, DownSpike (Default `DownSlash` object)

The `DownSlash` object carries `Downspike`, `DamageEnemies` (`manualTrigger = True`) and `HeroDownAttack`.

- With `manualTrigger = True`, `DamageEnemies.LateFixedUpdate` does not run `EvaluateDamage()` (only
  `EvaluateSpikeColliders()`), and the per-target `PreventDamage(healthManager)` after a hit is skipped.
  Damage is issued by `HeroDownAttack.OnTriggerEnter2D`.
- `HeroDownAttack.OnTriggerEnter2D(other)` runs only while `attack.IsDamagerActive`:
  - Layer ENEMIES (`manualTrigger` true): requires `attack.CanDamageEnemies`. It calls
    `EnemyDamager.DoDamage(other)`. If `DoDamage` returns **false** (no damage dealt), it goes straight to
    `ContinueBounceTrigger`. If damage was dealt, the bounce arrives through `HitResponded` (below).
  - Layer INTERACTIVE_OBJECT: `DoDamage(other)`. If that returns false, `ContinueBounceTrigger`.
  - Layer HERO_ATTACK: `ContinueBounceTrigger` without `DoDamage`, unless the object has `TinkEffect` or
    `ToolBoomerang`; in that case `ContinueBounceTrigger` runs only if `DoDamage` returns false.
  - Other layers: nothing.
- `HeroDownAttack.OnHitResponded(hitResponse)` is subscribed to `DamageEnemies.HitResponded`. It requires
  `IsDamagerActive` and `!hc.IsStunned`, and the target layer must not be TERRAIN or SOFT_TERRAIN and must not
  be a non-bouncer. It then:
  - `hc.StartDownspikeInvulnerability()`: `downspikeInvulnerabilitySteps = DOWNSPIKE_INVULNERABILITY_STEPS = 8`,
    decremented once per `FixedUpdate`, which is **0.16 s**. While > 0, damage with `hazardType == ENEMY` is
    ignored unless the `DamageHero` has `noBounceCooldown`, and `CanTakeDamage` checks fail.
  - if `hc.CanCustomRecoil()` (`Time.timeAsDouble >= recoilAllowTime`): `AffectedByGravity(false)` and
    `ResetVelocity()`.
  - `ContinueBounceTrigger(target)`.
- `ContinueBounceTrigger(obj)`:
  - no bounce if `obj` has an active `NonBouncer` or a `BounceBalloon`.
  - if `obj` has `DamageHero`: return when `!attack.CanHitSpikes && hazardType == SPIKES`. If
    `hazardType <= ENEMY && !noBounceCooldown` and there is no `HealthManager`, call `SetCooldown(0.5f)` on
    it. Then try to damage it (`TryDoDamage(lastCollider)`, or `DoDamage` once per GameObject).
  - if `hc.CanCustomRecoil()`: `bounceQueued = true; attack.QueueBounce()`.
- `Downspike.QueueBounce()`: `queuedBounce = true`, then `TryDownBounce()`:
  `if (queuedBounce && (!waitForBounceTrigger || bounceTriggerHit) && heroCtrl.CanCustomRecoil())
  heroCtrl.DownspikeBounce(harpoonRecoil: false)`. For Default, `waitForBounceTrigger = False`, so the bounce
  happens **immediately on the hit**, with the default bounce config.
- A nail clash parry in `DamageHero` whose direction falls in the `225 <= direction < 360` branch (down),
  without `noClashFreeze`, also calls `hc.DownspikeBounce(false)`. So does `BouncePod.DoBounceOff`.

### 4.2 `HeroController.DownspikeBounce(harpoonRecoil, bounceConfig = null)`

```
animCtrl.ResetDownspikeBounce();
cState.jumping = true;  cState.bouncing = false;
downspike_rebound_steps = 0;  cState.downSpikeBouncing = true;
allowAttackCancellingDownspikeRecovery = false;
ResetAirMoves();
downspike_rebound_xspeed = 0;
bounceConfig ??= HeroSlashBounceConfig.Default;   // jumpSteps = 4, jumpedSteps = -20
jump_steps = bounceConfig.JumpSteps;  jumped_steps = bounceConfig.JumpedSteps;  jumpQueueSteps = 0;
downspikeBurstPrefab.SetActive(false);
FinishDownspike(standardRecovery: !harpoonRecoil);   // recovery timer 0 (or 0.1 for harpoon)
BecomeAirborne();
```

The upward velocity comes from `Jump()` in `FixedUpdate` (`cState.jumping && !dashing && !isSprinting`):

```
if (jump_steps <= JUMP_STEPS)   // JUMP_STEPS = 8
    vy = isDashStabBouncing ? Config.DashStabBounceJumpSpeed
                            : (useUpdraftExitJumpSpeed ? JUMP_SPEED_UPDRAFT_EXIT (20) : JUMP_SPEED (18.6));
    jump_steps++; jumped_steps++;
else CancelJump();
```

- Default bounce config (`jumpSteps = 4`): steps 4, 5, 6, 7, 8 make **5 fixed steps (0.1 s) at vy = 18.6**.
  After that the jump is cancelled and gravity (restored by `FinishDownspike` -> `ResetGravity`) takes over.
  `FinishDownspike`'s `vy = -10` is overwritten by the first `Jump()` step.
- `jumped_steps` starts at -20 and reaches only -15 after the 5 steps. `JumpReleased()` only halves vy when
  `jumped_steps >= JUMP_STEPS_MIN (2)`, so **releasing Jump does not shorten a pogo bounce**.
- `downSpikeBouncing` lasts while `downspike_rebound_steps <= DOWNSPIKE_REBOUND_STEPS (6)`: 7 fixed steps =
  0.14 s. During it, x velocity is overridden only when `downspike_rebound_xspeed != 0` (harpoon variant:
  +/-`DOWNSPIKE_REBOUND_SPEED = 20`).
- `ResetAirMoves()` sets `airDashed = false`, and sets `doubleJumped = false` unless `cState.doubleJumping`.
  This restores the double jump and the air dash.
- After the bounce, `downSpikeRecovery` is true with timer 0, so **the next down attack is blocked for 0.25 s**
  (Default `downspikeRecoveryTime`). `downSpikeBouncing` also blocks it for the first 0.14 s. All attacks
  remain gated by `attack_cooldown` (0.41 s from the down-attack press). The direct `DownspikeBounce` path does
  not change `attack_cooldown`.
- `CanJump()` is false while `downSpikeRecovery`, which covers the 0.25 s after the bounce.
- Animation: on the first frame of `downSpikeBouncing`, `HeroAnimationController` plays `"DownSpikeBounce 1"`
  if `downSpikeBouncingShort`. Otherwise it picks one of `"DownSpikeBounce 1"`, `"DownSpikeBounce 2"`,
  `"Recoil Twirl"` (each weight 1, `GetRandomItemByProbabilityFair`). Knight clips:
  - DownSpikeBounce 1: fps 40, 37 frames (0.925 s)
  - DownSpikeBounce 2: fps 14, 12 frames (0.857 s)
  - Recoil Twirl: fps 15, 13 frames (0.867 s)

**Pogo bounce height (Default config, unobstructed).** Inputs:

- `Physics2D.gravity = (0, -60)`, `Time.fixedDeltaTime = 0.01999999` (`engine-settings.txt`).
- Hero `Rigidbody2D`: `linearDamping = 0`, `interpolation = Interpolate`, `collision = Continuous`
  (`hero-hierarchy.txt`; the snapshot shows `gravityScale = 0` with `IsGravityApplied = False`,
  `prevGravityScale = 1`, a transient state). During the bounce the scale is `DEFAULT_GRAVITY = 1`, because
  `FinishDownspike` calls `ResetGravity()` after `OnHitResponded`'s `AffectedByGravity(false)`.
  `RegainControl`'s `AffectedByGravity(true)` does not change a scale that is already > 0.
- Velocity change from gravity per step: `60 x 1 x 0.01999999 = 1.2` (1.1999994) units/s.
- `JUMP_SPEED = 18.6`, `JUMP_STEPS = 8`, `MAX_FALL_VELOCITY = 30` (only clamps downward speed, not involved in
  the rise), `AIR_HANG_GRAVITY = 0.1` / `AIR_HANG_ACCEL = 5` (only applied once `vy < 0`, so it does not change
  the apex).

Sequence (step N = the physics step whose trigger callback runs `DownspikeBounce`):

1. Step N: position integration has already happened. The callback leaves `vy = -10` (`FinishDownspike`).
2. Steps N+1 ... N+5: `Jump()` sets `vy = 18.6` (jump_steps 4 -> 8), physics makes it `18.6 - 1.2 = 17.4` and
   moves `17.4 x 0.02 = 0.348` per step. Total **1.740**.
3. Step N+6: jump_steps = 9 > 8, `CancelJump()` (velocity unchanged). From here only gravity acts: vy after
   each step is 16.2, 15.0, ..., 0.6 (14 steps with vy > 0), then -0.6. Rise = `0.02 x (16.2 + 15.0 + ... + 0.6)
   = 0.02 x 14 x (16.2 + 0.6) / 2 = 2.352`.
4. Apex = 1.740 + 2.352 = **4.092 units above the hero's position at the bounce step**, reached at the end of
   step N+19, i.e. **19 fixed steps = 0.38 s** after the bounce. A float32 simulation with dt = 0.01999999 gives
   4.0919995.

General form for the same rise speed: apex = `n x 0.348 + 2.352` where `n` is the number of `Jump()` steps
(`JUMP_STEPS + 1 - jump_steps at start`):

| Bounce | start jump_steps | Jump() steps at 18.6 | Apex (units) | Steps to apex |
|---|---|---|---|---|
| `DownspikeBounce`, default config (Hunter, `SetStartWithDownSpikeBounce`, FSM pogo bounces) | 4 | 5 | 4.092 | 19 |
| `DownspikeBounceSlightlyShort` | 5 | 4 | 3.744 | 18 |
| `DownspikeBounceShort` | 7 | 2 | 3.048 | 16 |
| `DownspikeBounce` with `Wanderer` config (`jumpSteps = 8`), started from vy <= 0 | 8 | 1 | 2.700 | 15 |

These assume nothing else sets vy during the rise (no ceiling, no updraft, no damage; Jump release cannot cut
it because `jumped_steps` starts at -20, see above). For the `SetStartWith*` variants the bounce happens when
`RegainControl()` runs (from an FSM `End` state); the first `Jump()` step is the next `FixedUpdate`, so the
rise from that point is the same.

### 4.3 Slash-type pogo (Wanderer, `NailSlash`)

The `HeroDownAttack` detection is the same as in 4.1, except `manualTrigger` is false on Wanderer's down slash
damagers, so `DamageEnemies` evaluates damage itself. Then:

- `NailSlash.QueueBounce()`:
  - if the clip's `'Bounce'` frame (frame 3, 0.12 s) has already passed (`queuedDownspikeBounce` true):
    `DownBounce()` = `hc.CancelAttack()` + `hc.DownspikeBounce(false, bounceConfig)`.
  - otherwise: `hc.DownspikeBounce(false)` immediately, **with the default config**
    (`jumpSteps = 4`), and `queuedDownspikeBounce = true`. When the `'Bounce'` frame is later reached,
    `OnAnimationEventTriggered` sees the flag and calls `DownBounce()`, which runs `DownspikeBounce` again with
    the Wanderer config.
- `HeroDownAttack.OnEndedDamage(didHit)`: if a bounce was queued, it calls `AffectedByGravity(true)` and
  `nailSlash.DoDownspikeBounce()` (Wanderer config).
- `HeroSlashBounceConfig Wanderer` (`referenced-objects.txt`): `jumpSteps = 8`, `jumpedSteps = -20`,
  `hideSlashOnBounceCancel = False`. It is the `bounceConfig` of `Attacks/Wanderer/DownSlash` and
  `DownSlashAlt`. A `DownspikeBounce` with this config gives **1 fixed step** of `Jump()` at 18.6 (`jump_steps`
  8 -> 9). When it follows the default-config bounce, it resets `jump_steps` to 8, which ends the remaining
  default-config rise after one more step.
- Resulting heights (method of 4.2): a Wanderer-config-only bounce rises 0.348 + 2.352 = **2.700 units**
  (15 steps). When the hit lands before the `'Bounce'` frame, the default bounce runs first (`Jump()` at 18.6
  every step), and the Wanderer re-bounce at the `'Bounce'` frame (0.12 s after the slash start) leaves exactly
  one more `Jump()` step at 18.6. The total is then `(k + 1) x 0.348 + 2.352`, where `k` is the number of
  `Jump()` steps already done by the default bounce. The hitbox is only on from 0.04 s (frame 1), so at normal
  speed at most 0.08 s (about 4 fixed steps) passes between the hit and the `'Bounce'` frame: `k` is 0 to 4 and
  the total is 2.700 to 4.092 units. The default bounce is still in its jump phase at that point.
- Wanderer `downspikeRecoveryTime = 0.09`, so `downSpikeRecovery` after the bounce lasts 0.09 s.

### 4.4 `startWithDownSpikeBounce*` variants

These flags are set by FSMs and other systems, and are consumed in `RegainControl()` (else-if chain, in this
order after `startWithAttack` / tool throw):

| Setter | Flag consumed in RegainControl | Effect method | Extra on set |
|---|---|---|---|
| `SetStartWithDownSpikeBounce()` | `startWithDownSpikeBounce` | `DownspikeBounce(false)` (default config: 5 steps at 18.6) | `attack_cooldown = 0.15` |
| `SetStartWithDownSpikeBounceSlightlyShort()` | `startWithDownSpikeBounceSlightlyShort` | `DownspikeBounceSlightlyShort(false)` | `attack_cooldown = 0.15`, `dashCooldownTimer = 0.1` |
| `SetStartWithDownSpikeBounceShort()` | `startWithDownSpikeBounceShort` | `DownspikeBounceShort(false)` | none |
| `SetStartWithHarpoonBounce()` | `startWithHarpoonBounce` | `DownspikeBounce(true)` (recovery timer starts at 0.1) | `attack_cooldown = 0.15` |
| `SetStartWithDashStabBounce()` | Short if `Config.ForceShortDashStabBounce` or a ray 3.5 units up (mask 8448) hits; else normal | as above | `attack_cooldown = 0.1` (forced short) or `0.15`; `isDashStabBouncing = true` (rise speed = `DashStabBounceJumpSpeed`, 18.6 for Default) |
| `SetStartWithWitchSprintBounce()` | `startWithDownSpikeBounce` | `DownspikeBounce(false)` | `attack_cooldown = 0.15`, `downspike_rebound_xspeed = +/-20` |
| `SetStartWithDownSpikeEnd()` | `startWithDownSpikeEnd` | `FinishDownspike(); CancelJump(); velocity = 0` | none |

- `DownspikeBounceSlightlyShort`: `downSpikeBouncingShort = true`, `jump_steps = 5` (4 steps at 18.6 = 0.08 s),
  `jumped_steps = -20`, `FinishDownspike()`, then `vy = 0` immediately.
- `DownspikeBounceShort`: `downSpikeBouncingShort = true`, `jump_steps = 7` (2 steps = 0.04 s),
  `jumped_steps = -20`, `FinishDownspike()`.
- Both apply `+/-DOWNSPIKE_REBOUND_SPEED (20)` x velocity for the rebound steps when `harpoonRecoil` is true.
- Callers found in the dumps: the `Crest Attacks` FSM (`Rpr Downslash Bounce`, `Witch Downslash Bounce`,
  `SpinBall Bounce`, `Shaman Downslash Bounce`, `Drill Bounce`), `Sprint` (3x `SetStartWithDownSpikeBounce`,
  4x `SetStartWithDashStabBounce`), `Harpoon Dash` (`SetStartWithDownSpikeBounce`, 2x
  `SetStartWithHarpoonBounce`), `Wall Scramble` (`SetStartWithDownSpikeBounceSlightlyShort`), `Tool Attacks`
  (2x `SetStartWithDownSpikeBounce`), and `SlideSurface.cs:575`. `SetStartWithDownSpikeEnd` is called by
  `Crest Attacks` (`ToolM Downslash End`), `Tool Attacks` (2x) and `Bind` (1x). All FSMs are on
  `Hero_Hornet(Clone)` (`fsm_Hero_Hornet(Clone)_<FSM name>.txt`).
- `DownAttack()` and `CancelDownSpike()` clear the queued flags (`CancelQueuedBounces`), and so does
  `CancelBounce()`.

The generic `Bounce()` / `BounceShort()` / `BounceHigh()` (`cState.bouncing`, vy = `BOUNCE_VELOCITY = 12`
for `BOUNCE_TIME = 0.25 s`, half time for `BounceShort`, timer starting at -0.03 for `BounceHigh`) are a
separate mechanism. They are not called by the nail down-attack path.

---

## 5. Default config attack objects

All five objects are children of `Hero_Hornet(Clone)/Attacks/Default` and use the `Knight` tk2d library
(`hero-configs.txt` lines 35-1899, `hero-hierarchy.txt` lines 5138-6995). Damage fields shared by all
five `DamageEnemies`:

- `attackType = Nail`, `useNailDamage = True`, `nailDamageMultiplier = 1`, `damageDealt = 5`. `damageDealt` is
  replaced: with `useNailDamage` the damage is `RoundToInt(PlayerData.nailDamage x nailDamageMultiplier)`.
- `stunDamage = 1`, `magnitudeMult = 1`, `multiHitter = False`, `hitOnceUntilEnd = False`,
  `silkGeneration = Full`, `canTriggerBouncePod = False`, `circleDirection = False`, `doesNotTink = False`,
  `doesNotParry = False`, `firstHitFreeze = None`, `lagHitOptionsProfile = null`.

Then imbuement, Warrior rage (`warriorDamageMultiplier = 1.25`, nail attack objects only) and Hunter combo
multipliers are applied in `DamageEnemies` (`global-settings.txt`: `hunterComboHits = 6`,
`hunterComboDamageMult = 1.3`, `hunterCombo2Hits = 6`, `hunterCombo2DamageMult = 1.3`, `hunterCombo2ExtraHits = 6`,
`hunterCombo2ExtraDamageMult = 1.5`; `warriorDamageMultiplier = 1.25`).
The combo is outside this page's scope.

### 5.1 Per-object values

| | Slash | AltSlash | UpSlash | DownSlash | WallSlash |
|---|---|---|---|---|---|
| Component | NailSlash | NailSlash | NailSlash | Downspike + HeroDownAttack | NailSlash |
| Transform localPos | (-0.07, 0, -0.001) | (-0.097, 0.027, -0.001) | (-0.04, 0.19, -0.001) | (0.69, -0.37, -0.001) | (0.641, 0.008, -0.001) |
| Transform localScale (dump) | (0.9, 1, 1) | (0.9, 1, 1) | (1, 0.9, 1) | (1.2, 1.2, 1.33) | (-0.9, 1, 1) |
| `scale` field (written to localScale at each start, no Long Needle) | (0.90, 1.00, 1.00) | (0.90, 1.00, 1.00) | (1.00, 0.90, 1.00) | (1.00, 1.00, 1.00) | (-0.90, 1.00, 1.00) |
| animName | SlashEffect | SlashEffectAlt | UpSlashEffect | DownSlashEffect | SlashEffect |
| imbuedSlashAnimName | SlashEffect_Glow | SlashEffectAlt_Glow | UpSlashEffect_Glow | DownSlashEffect_Glow | SlashEffect_Glow |
| bounceConfig | null | null | null | n/a | null |
| drillPull / dontAddRecoiler | False / False | False / False | False / False | n/a | False / False |
| canHitSpikes | True | True | True | True | True |
| DamageEnemies.direction (dump) | 0 | 0 | 0 | 270 | 0 |
| manualTrigger | False | False | False | **True** | False |
| forceSpikeUpdate | False | False | False | True | False |
| NailSlashRecoil | yes (drillPull False) | yes | yes | no | yes |

The direction of the four `NailSlash` damagers is overwritten on every attack by `Attack()` (section 1.4).
The DownSpike direction is not set by `Attack()`, so the serialized 270 is used.

`Downspike` fields on DownSlash: `leftExtraDirection = 135`, `rightExtraDirection = 45`,
`waitForBounceTrigger = False`, `useKnockbackDamagers = False` (children `Downspike Knockback Top` /
`Downspike Knockback Bottom` are referenced, `knockbackDamagerActiveSteps = 3`, unused while
`useKnockbackDamagers` is false). `ExtraUpDirection` is copied into `HitInstance.ExtraUpDirection` and returned
by `HitInstance.GetOverriddenDirection`. That value is used by `HealthManager` (hit effect rotation),
`HitResponse`, `BouncePod`, `BounceBalloon`, `HealthFlyer`, `HealthCocoon`, `BreakablePoleSimple` and
`ActivatorPlatform`.

Long Needle (`global-settings.txt`: `longNeedleMultiplier = (1.2, 1.075)`): `OnSlashStarting` multiplies
`scale` by (1.2, 1.075), or by the swapped (1.075, 1.2) while `cState.upAttacking`, unless
`overrideLongNeedleScale` (False on all five).

### 5.2 Collider shapes (PolygonCollider2D, trigger, offset (0,0), local points, disabled at rest)

- **Slash**: (-1.379,1.109) (-2.64,0.59) (-3.445,-0.164) (-3.406,-0.838) (-2.657,-1.285) (-0.834,-1.429)
  (0.549,-1.069) (0.26,-0.629) (0.128,0.732) (0.215,1.231)
- **AltSlash**: (-3.372,0.01) (-3.411,-0.654) (-2.771,-1.158) (-1.648,-1.356) (0.14,-1.011) (0.283,0.834)
  (-0.715,0.93) (-2.154,0.674)
- **UpSlash**: (0.093,3.331) (0.979,2.845) (1.516,1.23) (1.489,-0.898) (1.07,-1.254) (-0.441,-1.231)
  (-0.817,-0.898) (-1.023,1.284) (-0.715,2.797)
- **DownSlash**: (-0.087,0.147) (-0.012,-0.509) (-1.024,-1.399) (-1.903,-1.949) (-1.533,-1.011) (-0.969,0.056)
- **WallSlash**: (-1.413,1.144) (-2.569,0.652) (-3.359,-0.139) (-3.342,-0.836) (-2.551,-1.321) (-1.153,-1.424)
  (0.565,-1.069) (0.26,-0.629) (0.128,0.732) (0.215,1.202)

All five objects are tagged `Nail Attack` on layer `Attack` (17). Each also has a `Clash Tink` child (layer
`Tinker` (16), `PolygonCollider2D` with `trigger = False`, `NailSlashTerrainThunk`, `Rigidbody2D`), enabled
together with the main polygon, and an `Imbuement` child sprite. DownSlash `Clash Tink` points: (-0.087,0.147)
(0.001,-0.559) (-1.235,-1.536) (-1.746,-1.751) (-1.607,-1.185) (-0.969,0.056).

### 5.3 Hitbox timing of the NailSlash objects

`NailSlash.StartSlash()` resets `animTriggerCounter`, disables the polygon, sets `IsDamagerActive = true`, and
calls `PlaySlash()`. That method calls
`anim.Play(clip, Mathf.Epsilon, clip.fps x (quickening ? QuickAttackSpeedMult : 1))`. tk2d `Play` warps to
frame 0 and fires frame 0's trigger immediately if it has one. In `OnAnimationEventTriggered`, the 1st
counted trigger enables the colliders (`IsSlashOut = true`). The 2nd disables them, sets
`IsDamagerActive = false` and calls `enemyDamager.EndDamage()`. On clip completion `CancelAttack()` hides the
mesh. A `"Bounce"` trigger is not counted.

| Clip (Knight) | fps / frames / triggers | Collider on | Collider off | Clip end | Quickened (x1.5) on / off |
|---|---|---|---|---|---|
| SlashEffect (Slash, WallSlash) | 20 / 4 / [0, 2] | 0 s (press frame) | 0.1 s (6 @60) | 0.2 s (12 @60) | 0 / 0.0667 s |
| SlashEffectAlt (AltSlash) | 20 / 4 / [0, 2] | 0 s | 0.1 s (6 @60) | 0.2 s | 0 / 0.0667 s |
| UpSlashEffect (UpSlash) | 24 / 6 / [1, 3] | 0.0417 s (2.5 @60) | 0.125 s (7.5 @60) | 0.25 s (15 @60) | 0.0278 / 0.0833 s |

The colliders are switched in the animator update, which runs once per rendered frame. Contacts are evaluated
in physics steps.

Hero body clips (Knight, played by `HeroAnimationController` while `cState.attacking`):

| Clip | fps / frames | Duration |
|---|---|---|
| Slash | 20 / 5 | 0.25 s |
| SlashAlt | 20 / 5 | 0.25 s |
| UpSlash | 24 / 7 | 0.2917 s |
| Wall Slash | 24 / 7 | 0.2917 s |

`cState.attacking` lasts `attackDuration = 0.35 s` (0.2333 s quickened).

### 5.4 NailSlashRecoil (Slash, AltSlash, UpSlash, WallSlash)

`NailSlashRecoil.HitResponse` acts only when the cardinal direction of the hit equals the cardinal direction of
the damager (`DirectionUtils.GetCardinalDirection`: 0 = right, 1 = up, 2 = left, 3 = down):

- Direction right (0): enemies (no active `NonBouncer`) cause `RecoilLeft()`. A `BounceShroom` instead causes
  `RecoilLeftLong()` and a large shroom bounce. Terrain or interactive objects tagged `Recoiler` also cause
  `RecoilLeft()`.
- Direction left (2): mirror of the above (`RecoilRight` / `RecoilRightLong`).
- Direction up (1): `RecoilDown()` for enemies (and `Recoiler` terrain or interactive objects). With
  `BounceShroom`, also a shroom bounce.
- Direction down (3): nothing.

`RecoilLeft/Right` -> `CanRecoil()` -> `Recoil(isRight, steps, RECOIL_HOR_VELOCITY = 3.75)` for
`RECOIL_HOR_STEPS = 8` fixed steps (0.16 s). `Witch` / `Cursed` crest: `witchCrestRecoilSteps = 5`. Weighted
Anklet: `weightedAnkletRecoilSteps = 4`, or 0 together with Witch/Cursed. The long variant uses
`RECOIL_HOR_VELOCITY_LONG = 16` and calls `ResetAttacks()`.

`RecoilDown()`: `CancelJump()`, then when `vy > RECOIL_DOWN_VELOCITY (0)` and control is not relinquished, it
sets `vy = 0`. With Witch/Cursed or Weighted Anklet it sets `vy / 2` instead, and does nothing when
Witch/Cursed and Anklet are combined.

---

## 6. Custom down slashes (`Crest Attacks` FSM, brief)

`DownAttack()` sends `Config.DownSlashEvent` to `crestAttacksFSM` (the `Crest Attacks` FSM on
`Hero_Hornet(Clone)`). From `Idle`:

| Event | First state | Summary (from `fsm_Hero_Hornet(Clone)_Crest Attacks.txt`) |
|---|---|---|
| `RPR DOWNSLASH` (Reaper) | `Rpr Downslash Antic` | Antic clip `v3 Down Slash Antic` (Scythe lib: fps 18, 3 frames = 0.1667 s), `DecelerateV2 0.85`, `HeroBoxReaperDownSlash`. Then `Rpr Downslash`: velocity set to 0, `downAttacking = true`, clip `v3 Down Slash` (fps 24, 8 frames, triggers [4, 6]), `SetVelocityByScale speed -30 / ySpeed -30` (forward-down), `DecelerateV2 0.7`, object `DownSlash New` (child `Attacks/Scythe/DownSlash New`). `ATTACK LANDED` / `BOUNCE TINKED` / `BOUNCE CANCEL` go to `Rpr Downslash Bounce`, which calls `SetStartWithDownSpikeBounce`. `THUNKED DOWN` goes to `Rpr Downslash Thunk`. |
| `WITCH DOWNSLASH` (Whip) | `Witch Downslash Antic` | Clip `DownSpike Antic` (Witch lib), `DecelerateV2 0.98`. Then `Witch Downslash`: clip `DownSpike`, `downAttacking = true`, sets vy = -10 with x from the current velocity (clamped), `DecelerateV2 0.78` / `0.9`, object `DownSlash New` (`Attacks/Witch/DownSlash New`). Hits lead into a lash-grab sequence (`Set Rotation Offset` -> `NewLash *`). The bounce state `Witch Downslash Bounce` plays `Downspike Followup`, zeroes velocity, calls `StartDownspikeInvulnerability` + `SetStartWithDownSpikeBounce`. |
| `WARRIOR DOWNSLASH` (Warrior) | `SpinBall Antic` | Clip `SpinBall Antic` (Warrior lib: fps 15, 2 frames), `SetVelocityByScale -10 / 5`. Then `SpinBall Launch` (`SetVelocityByScale -25 / 0`) -> rage check -> `SpinBall Start` (`StartDownspikeInvulnerabilityLong` = 16 steps, `downTravelling = true`) -> `SpinBall Air` (`AddForce2d y = -90`). `ATTACK LANDED` -> `SpinBall Grind` -> `SpinBall Bounce` (`SetStartWithDownSpikeBounce`). |
| `SHAMAN DOWNSLASH` (Shaman) | `Shaman Downslash Antic` | `ClampVelocity2D`, `DecelerateXY x 0.9`, `QueueCancelDownAttack`, clip `DownSlash`. Then `Shaman Downslash`: `SetVelocityByScale 10 / 12`, `downAttacking = true`, object `DownSlash` (the Shaman `DownSlash` NailSlash with `NailSlashTravel`). Hits go to `Shaman Downslash Bounce` (velocity 0, `SetStartWithDownSpikeBounce`). |
| `TOOLMASTER DOWNSLASH` (Toolmaster) | `ToolM ChargeStart` | Clip `DownSpike Charge` (Drill Lance lib), `DecelerateV2 0.8`. Charged/uncharged branch, then `ToolM Antic` (`DownSpike Antic`) and `Drill Uncharged` (`DownSpike`, `SetVelocityByScale -12 / -8`) or `Drill Charged` (`DownSpike Charged`, `-27 / -19`). Objects `DownSlash New` / `DownSlash New Charged` (`Attacks/Toolmaster/...`). `ATTACK LANDED` -> grind -> `Drill Bounce` (`SetStartWithDownSpikeBounce`). The end state calls `SetStartWithDownSpikeEnd` + `FinishDownspike(true)`. |

- Global transitions `FSM CANCEL`, `HERO DAMAGED`, `LEVEL LOADED` and `CREST ATTACK CANCEL` go to `Cancel`.
  `FinishDownspike()` sends `CREST ATTACK CANCEL`.
- The `Rpr/Witch/Shaman/ToolM ... End` and `SpinBall End` states call `FinishDownspike(true)` and clear
  `downAttacking` (`SetCState("downAttacking", false)`). The shared `End` state clears `downAttacking` and
  `downTravelling`, and sends `RegainControl` and `StartAnimationControlToIdle` to the hero; that
  `RegainControl` is what consumes a `startWithDownSpikeBounce` flag set by a `... Bounce` state (each
  `... Bounce` state goes to `End` on `FINISHED`; `SpinBall Bounce` can instead go to `SpinBall Stop` on
  `NO BOUNCE`). The bounce is therefore the default-config `DownspikeBounce(false)` (apex 4.092 units,
  section 4.2).
- `SendMessage` calls to the hero, per antic state: `Rpr Downslash Antic`, `SpinBall Antic`,
  `Shaman Downslash Antic` and `ToolM ChargeStart` send `RelinquishControlNotVelocity`, `StopAnimationControl`
  and `AffectedByGravity(false)`. `Witch Downslash Antic` sends `RelinquishControlNotVelocity` and
  `StopAnimationControl` (its `RelinquishControl` action is disabled); `Witch Downslash` then sends
  `AffectedByGravity(false)` and later `AffectedByGravity(true)`. Gravity is restored by
  `AffectedByGravity(true)` in `Shaman Downslash`, `SpinBall Launch` and `Rpr Downslash Fall`, and by
  `FinishDownspike` -> `ResetGravity()`.
- The attack objects are started with `SendMessage StartSlash` on the `Crest Attacks` variables
  `Rpr DownSlash` = `Attacks/Scythe/DownSlash New`, `Witch Downslash` = `Attacks/Witch/DownSlash New`,
  `Shaman Downslash` = `Attacks/Shaman/DownSlash`, `Toolmaster Downslash` = `Attacks/Toolmaster/DownSlash New`,
  `Toolmaster Downslash Charged` = `Attacks/Toolmaster/DownSlash New Charged` (all under `Hero_Hornet(Clone)`),
  and `Current SpinSlash` (Warrior `SpinSlash` / `SpinSlashRage`). `Cancel` sends `CancelAttack` /
  `FullCancelAttack` to `Current SpinSlash`, `CancelAttack` to `Current DrillSlash`, and
  `CancelLagHitsForSource(Witch Downslash)` to `Attack Target`.
- For Custom types `cState.attacking` is set by `Attack()` with the config's `attackDuration`, and
  `DidAttack()` sets `attack_cooldown`.

`Dealt Damage Response` FSMs on the custom down-slash objects. They forward hits to `Crest Attacks`. Each dump
file name carries the full object path:

| Object (`Hero_Hornet(Clone)/Attacks/...`) | Dump file | Variant |
|---|---|---|
| `Scythe/DownSlash New` (Reaper) | `fsm_Hero_Hornet(Clone)~Attacks~Scythe~DownSlash New_Dealt Damage Response.txt` | A |
| `Toolmaster/DownSlash New` | `fsm_Hero_Hornet(Clone)~Attacks~Toolmaster~DownSlash New_Dealt Damage Response.txt` | A |
| `Toolmaster/DownSlash New Charged` | `fsm_Hero_Hornet(Clone)~Attacks~Toolmaster~DownSlash New Charged_Dealt Damage Response.txt` | A |
| `Shaman/DownSlash` | `fsm_Hero_Hornet(Clone)~Attacks~Shaman~DownSlash_Dealt Damage Response.txt` | A |
| `Witch/DownSlash New` | `fsm_Hero_Hornet(Clone)~Attacks~Witch~DownSlash New_Dealt Damage Response.txt` | B |

The four variant-A files are identical apart from the object path.

- Variant A: `State 1` listens for `HIT LANDED` and runs a `Trigger2dEventLayer` (`OnTriggerEnter2D`, layer 19
  = `Interactive Object`) that stores the collider's object and sends `HIT INTERACTIVE`.
  - `HIT LANDED` -> `Send Event`: `CallMethodProper HeroController.CanCustomRecoil`; if false -> `CANCEL`. Else it
    sets `Crest Attacks.Attack Target` and sends `ATTACK LANDED` to `Crest Attacks`.
  - `HIT INTERACTIVE` -> `Check`: `CanCustomRecoil` false -> `CANCEL`; object has `NonBouncer` -> `CANCEL`;
    otherwise `Bounce Tink`: sends `BOUNCE TINKED` and sets `Attack Target`, then goes on to `Send Event`.
  - `CANCEL` and `FINISHED` go to `Clear Hit Object` (clears `Hit Object`) and back to `State 1`.
- Variant B (Witch) differs: `State 1` listens for `DEALT ACTUAL DAMAGE` instead of `HIT LANDED`, and has an
  `ATTACK BLOCKED` -> `Send Blocked` path (sets `Attack Target`, sends `BOUNCE TINKED`). `Bounce Tink` also reads
  the hit object's world y and writes it to `Crest Attacks.Attack Target Y`. There is no `Clear Hit Object`
  state; `CANCEL` / `FINISHED` return directly to `State 1`.

---

## 7. Source references

- `HeroController`
  - `Update()`: `attack_time` / `attackDuration` check, `attack_cooldown` decrement, `downSpikeRecovery` and
    `downSpikeAntic` timer blocks (`HeroController.cs` ~2040-2360).
  - `FixedUpdate()`: `Downspike()` call, `Jump()`, `downSpikeBouncing` rebound steps, antic deceleration
    (`DOWNSPIKE_ANTIC_DECELERATION`), `attackQueueSteps++`, `downspikeInvulnerabilitySteps--`
    (~2973-3360).
  - `LookForInput()`: attack press, buffering and `ATTACK_QUEUE_STEPS` (~8366-8725). `ResetInputQueues()`.
  - `DoAttack()`, `Attack(AttackDirection)`, `AttackCancelWallSlide()`, `DownAttack(ref bool)`, `DidAttack()`,
    `Downspike()`, `FinishDownspike()` / `FinishDownspike(bool)`.
  - `CanAttack()`, `CanAttackAction()`, `CanDownAttack()`, `IsAttackLocked()`, `CanJump()`, `DoMovement()`,
    `Move()`, `TrySetCorrectFacing()`.
  - `UpdateConfig()`, `SetConfigGroup()`.
  - `RelinquishControl()`, `RegainControl(bool)` (the `startWith*` chain), `CancelDownSpike()`,
    `CancelDownSpikeBounces()`, `CancelQueuedBounces()`, `CancelAttack()`, `CancelAttackNotDownspikeBounce()`,
    `CancelAttackNotSlash()`, `ResetAttacks()`, `ResetAttacksShared()`, `ResetMotion()`, `CancelBounce()`.
  - `DownspikeBounce(bool, HeroSlashBounceConfig)`, `DownspikeBounceSlightlyShort(bool)`,
    `DownspikeBounceShort(bool)`, `Bounce()`, `BounceShort()`, `BounceHigh()`.
  - `SetStartWithDownSpikeBounce()`, `SetStartWithDownSpikeBounceSlightlyShort()`,
    `SetStartWithDownSpikeBounceShort()`, `SetStartWithHarpoonBounce()`, `SetStartWithDashStabBounce()`,
    `SetStartWithWitchSprintBounce()`, `SetStartWithDownSpikeEnd()`, `SetStartWithAttack()`.
  - `StartDownspikeInvulnerability()`, `StartDownspikeInvulnerabilityLong()`, `CanCustomRecoil()`,
    `CanRecoil()`, `RecoilLeft/Right/LeftLong/RightLong()`, `Recoil(bool, bool)`, `Recoil(bool, int, float)`,
    `RecoilDown()`.
  - `HandleCollisionTouching(Collision2D)`.
- `HeroControllerConfig` (fields and `DownSlashTypes`), `HeroControllerConfigWarrior` (rage overrides).
- `HeroAnimationController`: attack and downspike clip selection (~lines 554-665), `downspikeAnims`,
  `ResetDownspikeBounce()`.
- `NailAttackBase`: `Awake`, `OnSlashStarting` (scale and Long Needle), `OnPlaySlash`, `OnCancelAttack`,
  `OnEndedDamage`.
- `NailSlash`: `StartSlash`, `PlaySlash`, `OnAnimationEventTriggered`, `CancelAttack`, `QueueBounce`,
  `DownBounce`, `DoDownspikeBounce`.
- `Downspike`: `StartSlash`, `CancelAttack`, `CancelAttackAndDamage`, `QueueBounce`, `TryDownBounce`,
  `OnAnimationEventTriggered`.
- `HeroDownAttack`: `OnTriggerEnter2D`, `ContinueBounceTrigger`, `OnHitResponded`, `OnEndedDamage`,
  `IsNonBounce`.
- `NailSlashRecoil`: `HitResponse`, `Add`.
- `HeroSlashBounceConfig`: defaults `jumpSteps = 4`, `jumpedSteps = -20`, `hideSlashOnBounceCancel = true`,
  `Default`.
- `DamageEnemies`: `LateFixedUpdate` (`manualTrigger`), damage calculation (`useNailDamage`,
  `PlayerData.nailDamage`), `HitResponded`, `SendParried` / `OnBounceableTink*`.
- `HitInstance.GetOverriddenDirection`. `HeroBox.HeroBoxDownspike` / `HeroBoxNormal`. `DamageHero` (clash
  -> `DownspikeBounce`). `BouncePod.DoBounceOff`.
- `tk2dSpriteAnimator` (`TeamCherry.TK2D.dll`): `Play(clip, clipStartTime, overrideFps)`,
  `WarpClipToLocalTime`, `UpdateAnimation`, `ProcessEvents`, `PlayFromFrame`.
- `InputHandler`: `UpdateButtonQueueing`, `GetWasButtonPressedQueued`.
- Data (`raw-dump/`):
  - `engine-settings.txt`: `Time.fixedDeltaTime`, `Physics2D.gravity`, layers.
  - `hero-controller.txt`: `HeroController` fields and constants.
  - `hero-hierarchy.txt`: hero `Rigidbody2D`, attack objects under `Hero_Hornet(Clone)/Attacks/...` (components,
    colliders, tags, layers).
  - `hero-configs.txt`: config groups and their attack objects. `crests.txt`: crest -> `heroConfig`.
  - `referenced-objects.txt`: `HeroSlashBounceConfig Wanderer`.
  - `animations-tk2d.txt`: tk2d clips (Knight, Hornet CrestWeapon * libraries).
  - `global-settings.txt`: `longNeedleMultiplier`, Hunter combo and Warrior rage values.
  - `input-handler.txt`: `BUTTON_QUEUE_TIME`.
  - `fsm_Hero_Hornet(Clone)_Crest Attacks.txt`, `fsm_Hero_Hornet(Clone)~Attacks~*~DownSlash*_Dealt Damage
    Response.txt`, `fsm_Hero_Hornet(Clone)_Sprint.txt`, `fsm_Hero_Hornet(Clone)_Harpoon Dash.txt`,
    `fsm_Hero_Hornet(Clone)_Wall Scramble.txt`.
