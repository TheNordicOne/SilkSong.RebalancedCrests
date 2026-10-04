# Action lockout after nail attacks

This page describes which actions are blocked after Hornet starts a nail attack (neutral slash, alternate
slash, up slash, wall slash, and the down attack of each `DownSlashType`), for how long, and whether the
blocked button press is buffered or dropped. Values are given per `HeroControllerConfig`.

Conventions (same as `nail-attacks-and-pogo.md`):

- "@60" is `seconds x 60`, a unit conversion only. The real frame count depends on the frame rate and on
  `Time.deltaTime` accumulation.
- 1 fixed step = `Time.fixedDeltaTime = 0.01999999` s (50 Hz, `engine-settings.txt`).
- `attack_time` and `attack_cooldown` are advanced by `Time.deltaTime` once per rendered frame in
  `HeroController.Update`. Queue step counters are advanced once per `FixedUpdate`.
- Line numbers refer to the decompiled `HeroController.cs` and are approximate.

Crest to config mapping (`crests.txt`): Hunter / Hunter_v2 / Hunter_v3 -> `Default` (heroConfig is null),
Reaper -> `Reaper`, Wanderer -> `Wanderer`, Warrior (Beast) -> `Warrior` (`HeroControllerConfigWarrior`, rage
values while `WarriorState.IsInRageMode`), Toolmaster (Architect) -> `Toolmaster`, Witch and Cursed -> `Whip`,
Spell (Shaman) -> `Shaman`, Cloakless -> `Cloakless`.

---

## 1. The attack state variables

### 1.1 Set at attack start

`LookForQueueInput()` (line ~8650) calls `DoAttack()` (line 4057) when `Attack.WasPressed` and
`acceptingInput && CanAttack()`. `DoAttack()` -> `Attack(dir)` (line 4092):

```
if (attackDir != AttackDirection.downward || Config.DownSlashType != DownSpike)   // line ~4110
{
    cState.attacking = true;
    attackDuration = Config.AttackDuration;
    if (IsUsingQuickening) attackDuration *= 1f / Config.QuickAttackSpeedMult;
}
```

`attack_time` is not written in `Attack()`. It is 0 at this point because it was zeroed by the previous
`ResetAttacksShared()` (line 10089), and `CanAttack()` requires `!cState.attacking`.

For every slash (`isSlashing == true`, i.e. neutral / alt / up / wall slash and the `Slash` down type) and
for the `DownSpike` and `Custom` down types, `DidAttack()` (line 4324) runs:

```
attack_cooldown = IsUsingQuickening ? Config.QuickAttackCooldownTime : Config.AttackCooldownTime;
if (attack_cooldown < Config.AttackDuration) attack_cooldown = Config.AttackDuration;   // un-quickened duration
```

### 1.2 Advanced and cleared in `Update`

`HeroController.Update`, inside `else if (hero_state != ActorStates.no_input)` (line ~2043):

```
if (cState.attacking && !cState.dashing)
{
    attack_time += Time.deltaTime;
    if (attack_time >= attackDuration) { ResetAttacks(); animCtrl.StopAttack(); }
}
```

`ResetAttacks()` -> `ResetAttacksShared()` sets `cState.attacking = false` and `attack_time = 0`.
`attack_cooldown -= Time.deltaTime` runs every `Update` while `> 0` (line ~2290).

In the same `Update`, the attack timer block runs before `LookForQueueInput()` (line 2197), so every
`Can*` check made by `LookForQueueInput` in a frame sees `attack_time` already advanced for that frame.

### 1.3 The three lock conditions

| Name used on this page | Condition (exact code) | Ends when |
|---|---|---|
| **Recovery lock** | `cState.attacking && attack_time < Config.AttackRecoveryTime` (written inline as `(!cState.attacking \|\| !(attack_time < Config.AttackRecoveryTime))`, or as `IsAttackLocked()`, line 11046) | `attack_time >= AttackRecoveryTime`, or `cState.attacking` cleared (at `attack_time >= attackDuration`, or by any `CancelAttack` / `ResetAttacks`) |
| **Attacking lock** | `cState.attacking` (inside `CanAttackAction()`, line 11001) | `attack_time >= attackDuration` or cancel/reset |
| **Cooldown lock** | `attack_cooldown > 0f` (inside `CanAttack()`, line 10955) | `attack_cooldown` reaches 0 |

The recovery lock is `min(AttackRecoveryTime, attackDuration)` long, because `cState.attacking` is cleared at
`attackDuration`.

### 1.4 Control relinquishment (down attacks)

Some down attacks call `RelinquishControl()` or `RelinquishControlNotVelocity()` (lines 7471 / 7449). These
set `controlReqlinquished = true` and call `IgnoreInput()` (`acceptingInput = false`).
`RelinquishControlNotVelocity()` also sets `hero_state = no_input` and calls `ResetAttacks()`, which clears
`cState.attacking` and `attack_time`. While control is relinquished, every action whose check contains
`controlReqlinquished`, `hero_state != no_input`, `acceptingInput`, `CanInput()` or `CanDoFSMCancelMove()` is
blocked. `RegainControl()` (line 7497) calls `AcceptInput()` and clears `controlReqlinquished`.

`CanInput()` (line 4980) = `acceptingInput && !IsPaused() && transitionState == WAITING_TO_TRANSITION &&
!gm.RespawningHero && !cState.hazardRespawning && !cState.hazardDeath`.
`CanDoFSMCancelMove()` (line 7874) = `CanInput() || cState.isInCancelableFSMMove`.

---

## 2. Per-action check chains

Only the conditions relevant to attacks are discussed; each method is quoted in full.

### 2.1 Dash — `CanDash()` (line 10901)

```
hero_state != no_input && hero_state != hard_landing && hero_state != dash_landing
&& dashCooldownTimer <= 0f && !cState.dashing && !cState.backDashing
&& (!cState.attacking || !(attack_time < Config.AttackRecoveryTime))      // recovery lock
&& !cState.preventDash && (cState.onGround || !airDashed || cState.wallSliding)
&& !cState.hazardDeath && playerData.hasDash
```

Called from `LookForQueueInput` on `Dash.WasPressed` (line ~8638, together with `acceptingInput`) and from
the dash queue (line 8714). A dash press leads to `HeroDashPressed()` (line 9029), which also routes to the
scuttle tool (Down held + Scuttle charm) or wall scramble (`CanWallScramble()`, line 11297, which has the same
recovery-lock clause at line 11329 and also requires `!controlReqlinquished` and `hero_state != no_input`).
`HeroDash` calls `ResetAttacksDash()`, which ends the attack.

Attack-related block: **recovery lock**; also `hero_state == no_input` (Custom down attacks) and, through the
`acceptingInput` gate on the press, any relinquished control.

### 2.2 Sprint — `CanSprint()` (line 10937)

```
hero_state != no_input && hero_state != hard_landing && hero_state != dash_landing && !cState.dashing
&& (!cState.attacking || !(attack_time < Config.AttackRecoveryTime))      // recovery lock
&& !cState.preventDash && !controlReqlinquished
```

Read by the `Sprint` FSM state `Try` through the `CheckHeroCanSprint` action (`isFalse -> CANCEL ->
No Sprint`). `Try` is entered by the event `TRY SPRINT`, which `HeroController` sends from
`FinishedDashing()` when on the ground (line ~10342) and when a shuttlecock ends with Dash held
(lines ~2088, ~3283). A sprint therefore starts from a dash; a dash blocked by the recovery lock also produces
no sprint.

### 2.3 Jump — `CanJump()` (line 10822)

```
!IsInputBlocked()
&& hero_state not in {no_input, hard_landing, dash_landing}
&& !cState.wallSliding && !cState.dashing && !cState.isSprinting && !cState.backDashing && !cState.jumping
&& !cState.bouncing && !cState.shroomBouncing && !cState.downSpikeRecovery
&& (cState.onGround || ledge buffer)
```

There is **no** `cState.attacking`, `attack_time` or `attack_cooldown` condition. A grounded jump is possible
at any time during a slash. `HeroJump()` (line ~8791) cancels the attack only after the recovery:
`if (cState.attacking && attack_time >= Config.AttackRecoveryTime) CancelAttack();`. Before that, the slash
continues while Hornet jumps.

`cState.downSpikeRecovery` blocks `CanJump` after a down attack that ends through `FinishDownspike()`
(section 3.2). Horizontal movement (`DoMovement`, line 3379) has no attack condition; only turning around is
gated by the recovery lock (line ~3391).

Wall jump (`CanWallJump()`, line 11259) has no attack condition.

### 2.4 Double jump — `CanDoubleJump(checkControlState = true)` (line 10862)

```
if (checkControlState && (hero_state in {no_input, hard_landing, dash_landing} || controlReqlinquished)) return false;
playerData.hasDoubleJump && !doubleJumped && !IsDashLocked() && !cState.wallSliding && !cState.backDashing
&& !IsAttackLocked()                                                      // recovery lock
&& !cState.bouncing && !cState.shroomBouncing && !cState.onGround && !cState.doubleJumping
&& Config.CanDoubleJump
then: false if TryQueueWallJumpInterrupt(), false if IsApproachingSolidGround()
```

`IsAttackLocked()` (line 11046) = `cState.attacking && attack_time < Config.AttackRecoveryTime`.

### 2.5 Drifter's Cloak (float) — `CanFloat(checkControlState = true)` (line 11068)

```
if (checkControlState && (hero_state in {no_input, hard_landing, dash_landing} || controlReqlinquished)) return false;
if (CanInfiniteAirJump()) return false;
playerData.hasBrolly && !cState.onGround && !IsDashLocked() && !cState.swimming && ledgeBufferSteps <= 0
&& !cState.wallSliding
&& !IsAttackLocked()                                                      // recovery lock
&& !cState.hazardDeath && !cState.hazardRespawning && (!cState.doubleJumping || cState.inUpdraft)
&& CanDoFSMCancelMove() && Config.CanBrolly
then: false if TryQueueWallJumpInterrupt(), false if IsApproachingSolidGround()
```

On success `StartFloat()` (line 8992) sends `FLOAT` to the `Umbrella Float` FSM (`Idle`-side transition
`on FLOAT -> Take Control`). The FSM does not check the attack state itself; the `Umbrella Float` FSM calls
`CanHarpoonDash` only to allow cancelling the float into Clawline.

### 2.6 Jump button routing in `LookForQueueInput` (line ~8596)

On `Jump.WasPressed`, in order: wall jump (`acceptingInput && CanWallJump()`), jump (`acceptingInput &&
CanJump()`), double jump (`acceptingInput && !wallLocked && !cState.swimming && CanDoubleJump()`), infinite
air jump, float (`acceptingInput && !wallLocked && CanFloat() && !SlideSurface.IsInJumpGracePeriod`). If none
applies and `TryQueueWallJumpInterrupt()` is false, the press is queued (section 4).

### 2.7 Clawline (harpoon dash) — `CanHarpoonDash()` (line 10928)

```
hero_state != hard_landing && hero_state != dash_landing && harpoonDashCooldown <= 0f
&& (!cState.dashing || dash_timer <= 0f)
&& (!cState.attacking || !(attack_time < Config.AttackRecoveryTime))      // recovery lock
&& CanDoFSMCancelMove() && HasHarpoonDash() && !cState.dead && !cState.hazardDeath && !cState.hazardRespawning
```

`HasHarpoonDash()` = `playerData.hasHarpoonDash && Config.CanHarpoonDash`. Called from `LookForQueueInput`
(lines ~8575 to ~8595) on `SuperDash.WasPressed` (when not "only Down held with `CanSuperJump()`") and from the
harpoon queue. This block runs **before** the `acceptingInput` early return of `LookForQueueInput`; the
`acceptingInput` dependency comes only through `CanDoFSMCancelMove()`. On success it sends `DO MOVE` to the
`Harpoon Dash` FSM, whose state `Can Do?` checks only `CanTryHarpoonDash()` (`Time.timeAsDouble >
HARPOON_DASH_TIME`, `HARPOON_DASH_TIME = 0`) and silk.

### 2.8 Silk skills and tools — `CanThrowTool()` (lines 10969 / 10974)

Silk skills (`ToolItemType.Skill`) and red tools share one path: the `QuickCast` button.

```
CanThrowTool(checkGetWillThrow):
    ThrowToolCooldownReady()          // throwToolCooldown <= 0
    && CanAttackAction()
    && (!checkGetWillThrow || GetWillThrowTool(reportFailure: true))

CanAttackAction():                    // line 11001
    !cState.attacking                                                     // attacking lock (full attackDuration)
    && (!cState.dashing || dashingDown) && !cState.dead && !cState.hazardDeath && !cState.hazardRespawning
    && !controlReqlinquished
    && hero_state not in {no_input, hard_landing, dash_landing}
    && CanInput()
    && (!Down.IsPressed || CanDownAttack())
```

`GetWillThrowTool()` (line 3526) picks the tool bound to Up / Down / Neutral and calls
`CanThrowTool(tool, binding, reportFailure)` (line 3548): Red tools need charges (or usable-when-empty) and
`SilkRequired`; Skill tools need `playerData.silk >= playerData.SilkSkillCost`.

`LookForQueueInput` on `QuickCast.WasPressed` (line ~8662): `acceptingInput && CanThrowTool(false)` ->
`ThrowTool(false)` if `GetWillThrowTool(true)`; otherwise queued. `ThrowTool()` (line 3588) returns early when
`Time.timeAsDouble < canThrowTime`. For a Skill it calls `CancelAttack(resetNailCharge: true)`, sends
`FSM CANCEL` and sends `usage.FsmEventName` (`NEEDLE THROW`, `THREAD SPHERE`, `PARRY`, `SILK CHARGE`,
`SILK BOMB`, `BOSS NEEDLE`, `tools.txt`) to `skillEventTarget`; these are global transitions of the
`Silk Specials` FSM. For FSM tools it sends `TAKE CONTROL` plus the event to `toolEventTarget` (`Tool Attacks`
FSM).

`CanAttackAction()` contains `!cState.attacking`, not the recovery comparison. Silk skills and tools are
therefore blocked for the **whole** `attackDuration`, which is longer than the recovery lock in every config.

`CanCast()` (line 7836, recovery lock) is called only by the `Silk Specials` state `Taunt Check`. The
`Silk Specials` state `Idle` listens only for the Needolin button (`ListenForDreamNail`) and taunt.
`CanDoSpecial()` (line 7883, recovery lock) has no caller in the decompiled code or the FSM dumps.

### 2.9 Bind — `CanBind()` (line 7849)

```
CanDoFsmMove() && !IsHardLanding() && !IsInputBlocked() && ToolItemManager.ActiveState != Cutscene
&& !gm.isPaused && !cState.dashing && !cState.backDashing
&& (!cState.attacking || !(attack_time < Config.AttackRecoveryTime))      // recovery lock
&& !cState.recoiling && !cState.transitioning && !cState.recoilFrozen && !cState.hazardDeath
&& !cState.hazardRespawning && !cState.dead && Config.CanBind && CanDoFSMCancelMove()
&& !HeroChargeEffects.Instance.IsCharging
```

Called by the `Bind` FSM, not by `HeroController`. `Bind` state `Idle` runs `ListenForCast` with
`wasPressed = BUTTON DOWN` (fires on `Cast.WasPressed`) -> state `Can Bind?` -> `CallMethodProper
HeroController.CanBind` -> `BoolTest isFalse = CANCEL` -> `Cancel Frame` (`NextFrameEvent`) -> `Idle`.

### 2.10 Attack — `CanAttack()` (line 10955)

```
if (attack_cooldown > 0f) return false;     // cooldown lock
return CanAttackAction();                   // includes !cState.attacking
```

### 2.11 Other actions with the recovery lock

Back dash (`CanBackDash`, 7969), super jump (`CanSuperJump`, 10946), quick map (7921), inspect (7960),
Needolin (`CanPlayNeedolin`, 7978), wall scramble (11297), turning (`DoMovement`, ~3391).
`CanNailArt()` (7896) is not covered here.

### 2.12 Summary

| Action | Check | Attack-related lock |
|---|---|---|
| Move left/right | `DoMovement` | none (turning: recovery lock) |
| Jump (ground) / wall jump | `CanJump`, `CanWallJump` | none |
| Double jump | `CanDoubleJump` -> `IsAttackLocked` | recovery |
| Float (Drifter's Cloak) | `CanFloat` -> `IsAttackLocked` | recovery |
| Dash | `CanDash` | recovery |
| Sprint | `CanSprint` (Sprint FSM `Try`) | recovery (and needs a dash first) |
| Clawline | `CanHarpoonDash` | recovery |
| Bind | `CanBind` (Bind FSM `Can Bind?`) | recovery |
| Silk skill / tool | `CanThrowTool` -> `CanAttackAction` | attacking (full `attackDuration`) |
| Attack | `CanAttack` | cooldown (`max(cooldown, AttackDuration)`) |

---

## 3. Down attacks

### 3.1 `Slash` (Wanderer)

Same as a neutral slash: `cState.attacking = true`, the timers of section 1 apply.

### 3.2 `DownSpike` (Default = Hunter, Cloakless)

`cState.attacking` is **not** set. `DownAttack()` (line 4246), with `downspikeThrusts = True` in both configs,
calls `RelinquishControl()` (line ~4265), sets `cState.downSpikeAntic = true`, and calls `DidAttack()`.

- Antic: `Update` advances `downSpikeTimer` by `Time.deltaTime` until `>= Config.DownSpikeAnticTime`
  (Default 0.1 s, Cloakless 0.07 s), then sets `cState.downSpiking` (line ~2337).
- Spike: `Downspike()` (line 4386) runs in `FixedUpdate`, adds `Time.deltaTime` (fixed step) and calls
  `FinishDownspike()` once `downSpikeTimer > Config.DownSpikeTime` (Default 0.1 s, Cloakless 0.04 s).
  Without a hit, `FinishDownspike()` runs on the 7th `FixedUpdate` after the spike starts (6 increments of 0.01999999 are needed to exceed 0.1), i.e. 0.12 to 0.14 s for Default; a hit ends it earlier through the bounce path.
- `FinishDownspike()` (line 10369) sets `cState.downSpikeRecovery = true`, `downSpikeRecoveryTimer = 0` and
  calls `RegainControl()`.
- `cState.downSpikeRecovery` ends in `Update` (line ~2322) when `downSpikeRecoveryTimer >=
  Config.DownspikeRecoveryTime` (0.25 s, airborne) or `>= DOWNSPIKE_LAND_RECOVERY_TIME` (0.1666667 s, on
  ground). It blocks `CanJump` and down attacks (`CanDownAttack`), nothing else in this list.

While relinquished (antic + spike, about 0.1 + 0.12 = 0.22 s for Default without a hit),
`controlReqlinquished` and `acceptingInput == false` block dash, double jump, float, Clawline (via
`CanDoFSMCancelMove`), bind (via `CanDoFSMCancelMove`), tools / silk skills (`CanAttackAction`) and attack.
`attack_cooldown` (Default 0.41 s) runs in parallel.

### 3.3 `Custom` (Reaper, Warrior, Toolmaster, Whip, Shaman)

`Attack()` sets `cState.attacking = true`, then `DownAttack()` sends `Config.DownSlashEvent` to the
`Crest Attacks` FSM and calls `DidAttack()`. The first state of each branch (`Rpr Downslash Antic`,
`Witch Downslash Antic`, `SpinBall Antic`, `Shaman Downslash Antic`, `ToolM ChargeStart`) calls
`RelinquishControlNotVelocity`, which sets `hero_state = no_input`, `controlReqlinquished = true`,
`acceptingInput = false` and calls `ResetAttacks()` (`cState.attacking = false`, `attack_time = 0`). Control
returns in state `End` (`RegainControl`), after `FinishDownspike` in the `... End` state, or through the bounce
states (`SetStartWithDownSpikeBounce`). While `hero_state == no_input`, `LookForInput()` is not called, so the
release of a button does not clear its queue flag in that period.

Reaper without a hit: `Rpr Downslash Antic` waits for the end of `v3 Down Slash Antic` (3 frames at 18 fps =
0.1667 s), `Rpr Downslash` waits for the first trigger of `v3 Down Slash` (frame 4 at 24 fps = 0.1667 s), and
`Rpr Downslash Fall` waits for the end of the same clip (8 frames at 24 fps = 0.3333 s from its start). Control
is therefore relinquished for at least 0.5 s (30 @60) before `Rpr Downslash Fall 2` -> `Rpr Downslash End` ->
`End`. In `Rpr Downslash Fall` and `Rpr Downslash Fall 2`, `ListenForDash` (`wasPressed = SPRINT`) leads to
`Rpr DashCancel`; this is the only dash input read during that period.

---

## 4. Buffered or dropped

### 4.1 `HeroController` input queues

When a press fails its check in `LookForQueueInput`, the queue for that button is armed (`*QueueSteps = 0`,
`*Queuing = true`). `FixedUpdate` (lines ~3203 to ~3226) increments each armed counter by 1 per fixed step.
Later in `LookForQueueInput` (lines ~8688 to ~8724) the action fires if the button **is still held**, the
counter is `<= N`, and the `Can*` check now passes. `LookForInput()` (lines ~8430 to ~8445) clears
`dashQueuing`, `attackQueuing`, `harpoonQueuing`, `toolThrowQueueing` when the button is not held;
`JumpReleased()` (line 10297) clears `jumpQueuing` and `doubleJumpQueuing`.

The counter passes `N` at the `(N+1)`-th fixed step after the press, which occurs between `N x 0.02` and
`(N+1) x 0.02` s after the press.

| Queue | Constant (`hero-controller.txt`) | Buffer length | Fires via |
|---|---|---|---|
| Dash | `DASH_QUEUE_STEPS = 10` | 0.20 to 0.22 s (12 to 13.2 @60) | `CanDash()` -> `HeroDashPressed()` |
| Attack | `ATTACK_QUEUE_STEPS = 8` | 0.16 to 0.18 s (9.6 to 10.8 @60) | `CanAttack()` -> `DoAttack()` |
| Clawline | `HARPOON_QUEUE_STEPS = 8` | 0.16 to 0.18 s | `CanHarpoonDash()` -> `DO MOVE` |
| Tool / silk skill | `TOOLTHROW_QUEUE_STEPS = 5` | 0.10 to 0.12 s (6 to 7.2 @60) | `CanThrowTool()` -> `ThrowTool()` |
| Jump | `JUMP_QUEUE_STEPS = 2` | 0.04 to 0.06 s | `CanJump()` -> `HeroJump()` |
| Double jump | `DOUBLE_JUMP_QUEUE_STEPS = 10` | 0.20 to 0.22 s | `CanDoubleJump()` -> `DoDoubleJump()` (or `HeroJump()` if grounded) |

Details:

- The queued executions (jump, double jump, dash, attack, tool) sit after `if (!acceptingInput) return;`
  (line 8676); they cannot fire while control is relinquished, but their counters keep advancing. The
  Clawline queue is checked before that return.
- `doubleJumpQueuing` is armed only if `!cState.swimming && !cState.jumping && !SlideSurface.IsHeroSliding`.
- **Float has no queue.** A jump press during the recovery lock arms the jump / double-jump queues only; the
  queue can produce a double jump but never `StartFloat()`. The only float paths from the queue section are
  `regainControlJumpQueued && CanFloat()` (one `LookForQueueInput` call after `RegainControl()` when Jump was
  pressed while relinquished, line ~7739 / ~8705) and `RegainControl` start flags. Without an available double
  jump (`doubleJumped` true, `playerData.hasDoubleJump` false or `Config.CanDoubleJump` false), a float press during the recovery lock is dropped.
- `ResetInputQueues()` clears all queues.

### 4.2 Bind: no queue

The `Bind` FSM reacts to `Cast.WasPressed` once. If `CanBind()` is false the FSM goes `Cancel Frame` ->
`Idle`; the press is dropped. Holding the button does not retry.

### 4.3 Dropped presses with a queue

A queued press is still dropped when the lock outlasts the buffer: a press made more than `buffer` seconds
before the lock ends expires, even if the button is held. A queued press is also dropped when the button is
released before the lock ends.

`InputHandler`'s own press buffer (`BUTTON_QUEUE_TIME = 0.1`) is read only through
`GetWasButtonPressedQueued`; among the actions on this page it is used only for Jump in `RegainControl()`
(line ~7735).

### 4.4 Same-frame order

`LookForQueueInput` handles new presses in this order: Clawline, jump, dash, attack, tool. A dash and an
attack pressed in the same frame run the dash first.

---

## 5. Lock windows per config

Measured from the attack press frame (`attack_time = 0`). Values from `hero-configs.txt`; Warrior rage from
`HeroControllerConfigWarrior` (`rageAttackDuration 0.3`, `rageAttackRecoveryTime 0.2`,
`rageAttackCooldownTime 0.32`, `rageQuickAttackCooldownTime 0.195`).

- Recovery lock = `min(AttackRecoveryTime, attackDuration)`; applies to dash, sprint, double jump, float,
  Clawline, bind, wall scramble, turning.
- Attacking lock = `attackDuration`; applies to silk skills and tools.
- Cooldown lock = `max(AttackCooldownTime, AttackDuration)`; applies to the next attack.
- Quickening: `attackDuration = AttackDuration / QuickAttackSpeedMult`; cooldown =
  `max(QuickAttackCooldownTime, AttackDuration)` (un-quickened duration).

These apply to neutral / alternate / up / wall slashes, and to Wanderer's down slash. Hunter / Cloakless
down spikes and Custom down attacks are described in section 3.

### 5.1 Without quickening

| Config (crest) | Recovery R | Duration D | Cooldown | Dash / sprint / double jump / float / Clawline / bind | Silk skill / tool | Next attack |
|---|---|---|---|---|---|---|
| Default (Hunter) | 0.15 | 0.35 | 0.41 | 0.15 s (9 @60) | 0.35 s (21 @60) | 0.41 s (24.6 @60) |
| Cloakless | 0.1 | 0.35 | 0.41 | 0.10 s (6 @60) * | 0.35 s (21) | 0.41 s (24.6) |
| Reaper | 0.3 | 0.35 | 0.5 | 0.30 s (18 @60) | 0.35 s (21) | 0.50 s (30) |
| Warrior (Beast) | 0.2 | 0.3 | 0.39 | 0.20 s (12) | 0.30 s (18) | 0.39 s (23.4) |
| Warrior rage | 0.2 | 0.3 | 0.32 | 0.20 s (12) | 0.30 s (18) | 0.32 s (19.2) |
| Wanderer | 0.1 | 0.25 | 0.3 | 0.10 s (6) | 0.25 s (15) | 0.30 s (18) |
| Toolmaster (Architect) | 0.2 | 0.35 | 0.45 | 0.20 s (12) | 0.35 s (21) | 0.45 s (27) |
| Whip (Witch, Cursed) | 0.3 | 0.4 | 0.45 | 0.30 s (18) | 0.40 s (24) | 0.45 s (27) |
| Shaman | 0.1 | 0.35 | 0.5 | 0.10 s (6) | 0.35 s (21) | 0.50 s (30) |

\* Cloakless has `canDoubleJump = False`, `canBrolly = False`, `canHarpoonDash = False`; those actions are
unavailable regardless of the lock.

### 5.2 With quickening

| Config | QuickAttackSpeedMult | D / mult | Recovery-locked actions | Silk skill / tool | Next attack |
|---|---|---|---|---|---|
| Default | 1.5 | 0.2333 | 0.15 s (9) | 0.2333 s (14) | max(0.205, 0.35) = 0.35 s (21) |
| Cloakless | 2 | 0.175 | 0.10 s (6) | 0.175 s (10.5) | max(0.205, 0.35) = 0.35 s (21) |
| Reaper | 1.5 | 0.2333 | **0.2333 s (14)** (D/mult < R) | 0.2333 s (14) | max(0.25, 0.35) = 0.35 s (21) |
| Warrior | 1.5 | 0.2 | 0.20 s (12) | 0.20 s (12) | max(0.195, 0.3) = 0.30 s (18) |
| Warrior rage | 1.5 | 0.2 | 0.20 s (12) | 0.20 s (12) | max(0.195, 0.3) = 0.30 s (18) |
| Wanderer | 1.5 | 0.1667 | 0.10 s (6) | 0.1667 s (10) | max(0.15, 0.25) = 0.25 s (15) |
| Toolmaster | 1.5 | 0.2333 | 0.20 s (12) | 0.2333 s (14) | max(0.225, 0.35) = 0.35 s (21) |
| Whip | 1.5 | 0.2667 | **0.2667 s (16)** (D/mult < R) | 0.2667 s (16) | max(0.2375, 0.4) = 0.40 s (24) |
| Shaman | 1.5 | 0.2333 | 0.10 s (6) | 0.2333 s (14) | max(0.25, 0.35) = 0.35 s (21) |

### 5.3 Press that is dropped even if held (without quickening)

A press made at time `t` after the attack press is dropped when `t < lock - buffer` (buffer upper bound in
brackets; the exact edge lies within one fixed step). "never" = the buffer covers the whole lock.

| Config | Dash (0.20-0.22) | Double jump (0.20-0.22) | Clawline (0.16-0.18) | Tool / silk skill (0.10-0.12) | Attack (0.16-0.18) | Float | Bind |
|---|---|---|---|---|---|---|---|
| Default (Hunter) | never | never | never | t < 0.23-0.25 | t < 0.23-0.25 | always during 0.15 s ** | always during 0.15 s |
| Reaper | t < 0.08-0.10 | t < 0.08-0.10 | t < 0.12-0.14 | t < 0.23-0.25 | t < 0.32-0.34 | always during 0.30 s ** | always during 0.30 s |
| Wanderer | never | never | never | t < 0.13-0.15 | t < 0.12-0.14 | always during 0.10 s ** | always during 0.10 s |
| Warrior | never | never | t < 0.02-0.04 | t < 0.18-0.20 | t < 0.21-0.23 | always during 0.20 s ** | always during 0.20 s |
| Toolmaster | never | never | t < 0.02-0.04 | t < 0.23-0.25 | t < 0.27-0.29 | always during 0.20 s ** | always during 0.20 s |
| Whip | t < 0.08-0.10 | t < 0.08-0.10 | t < 0.12-0.14 | t < 0.28-0.30 | t < 0.27-0.29 | always during 0.30 s ** | always during 0.30 s |
| Shaman | never | never | never | t < 0.23-0.25 | t < 0.32-0.34 | always during 0.10 s ** | always during 0.10 s |
| Cloakless | never | n/a | n/a | t < 0.23-0.25 | t < 0.23-0.25 | n/a | always during 0.10 s |

\*\* A jump press in the air during the recovery lock becomes a double jump through the double-jump queue when
a double jump is available and Jump is held; a float press is never queued.

Each queued action additionally requires the button to be held until the lock ends.

---

## 6. Code locations (`HeroController.cs`, approximate lines)

| Item | Method | Line |
|---|---|---|
| attack timer, attack end | `Update` (`if (cState.attacking && !cState.dashing)`) | ~2043 |
| `attack_cooldown` decrement | `Update` | ~2290 |
| down spike antic / recovery timers | `Update` | ~2322 to ~2355 |
| `LookForQueueInput()` call | `Update` | 2197 |
| queue step increments | `FixedUpdate` | ~3203 to ~3226 |
| turn gated by recovery | `DoMovement` | ~3391 |
| tool selection | `GetWillThrowTool`, `CanThrowTool(ToolItem, ...)` | 3526, 3548 |
| tool / silk skill start | `ThrowTool` | 3588 |
| `cState.attacking = true` | `Attack` | ~4110 |
| cooldown set | `DidAttack` | 4324 |
| down attack types | `DownAttack` | 4246 |
| down spike FixedUpdate | `Downspike` | 4386 |
| relinquish / regain | `RelinquishControlNotVelocity`, `RelinquishControl`, `RegainControl` | 7449, 7471, 7497 |
| cast check (used only by taunt) | `CanCast` | 7836 |
| bind check | `CanBind` | 7849 |
| FSM cancel move | `CanDoFSMCancelMove` | 7874 |
| unused special check | `CanDoSpecial` | 7883 |
| queue flag clear on release | `LookForInput` | ~8430 to ~8445 |
| press handling and queue execution | `LookForQueueInput` | 8568 (harpoon ~8575, jump ~8600, dash ~8638, attack ~8650, tool ~8662, return 8676, queued ~8688 to ~8724) |
| attack cancel on jump | `HeroJump(bool)` | ~8791 |
| attack reset | `ResetAttacksShared`, `ResetAttacks` | 10089, 10105 |
| jump release clears queues | `JumpReleased` | 10297 |
| down spike finish | `FinishDownspike(bool)` | 10369 |
| jump | `CanJump` | 10822 |
| double jump | `CanDoubleJump` | 10862 |
| dash | `CanDash` | 10901 |
| Clawline | `CanHarpoonDash` | 10928 |
| sprint | `CanSprint` | 10937 |
| attack | `CanAttack` | 10955 |
| tool / silk skill | `CanThrowTool()`, `CanThrowTool(bool)` | 10969, 10974 |
| shared attack/tool gate | `CanAttackAction` | 11001 |
| recovery helper | `IsAttackLocked` | 11046 |
| float | `CanFloat` | 11068 |
| wall scramble | `CanWallScramble` | 11297 (lock at 11329) |

FSM-side checks:

| FSM (raw-dump file) | State | Check |
|---|---|---|
| `Bind` | `Idle` -> `Can Bind?` | `ListenForCast` wasPressed, `CallMethodProper CanBind` |
| `Sprint` | `Try` | `CheckHeroCanSprint` (`HutongGames.PlayMaker.Actions/CheckHeroCanSprint.cs`, calls `CanSprint`) |
| `Harpoon Dash` | `Can Do?` | `HeroControllerMethods CanTryHarpoonDash` |
| `Umbrella Float` | `on FLOAT -> Take Control` | none for attacks; `CanHarpoonDash` for float cancel |
| `Silk Specials` | `Taunt Check` | `CallMethodProper CanCast` (taunt only) |
| `Crest Attacks` | `* Antic` / `ToolM ChargeStart` | `RelinquishControlNotVelocity` |

---

## 7. Sources

- Decompiled `Assembly-CSharp`: `HeroController.cs` (methods and lines in section 6),
  `HeroControllerConfig.cs` (`DownSlashTypes { DownSpike, Slash, Custom }`, property getters),
  `HeroControllerConfigWarrior.cs`, `HutongGames.PlayMaker.Actions/CheckHeroCanSprint.cs`,
  `HutongGames.PlayMaker.Actions/ListenForCast.cs`, `HutongGames.PlayMaker.Actions/HeroControllerMethods.cs`.
- `raw-dump/hero-configs.txt`: per-config `attackDuration`, `quickAttackSpeedMult`, `attackRecoveryTime`,
  `attackCooldownTime`, `quickAttackCooldownTime`, `downSlashType`, `canDoubleJump`, `canBrolly`,
  `canHarpoonDash`, `canBind`.
- `raw-dump/crests.txt`: crest -> heroConfig mapping.
- `raw-dump/hero-controller.txt`: `DASH_QUEUE_STEPS = 10`, `ATTACK_QUEUE_STEPS = 8`,
  `HARPOON_QUEUE_STEPS = 8`, `TOOLTHROW_QUEUE_STEPS = 5`, `JUMP_QUEUE_STEPS = 2`,
  `DOUBLE_JUMP_QUEUE_STEPS = 10`, `DOWNSPIKE_LAND_RECOVERY_TIME = 0.1666667`.
- `raw-dump/engine-settings.txt`: `Time.fixedDeltaTime = 0.01999999`.
- `raw-dump/animations-tk2d.txt`: `Hornet CrestWeapon Scythe Anim` clips `v3 Down Slash Antic` (18 fps,
  3 frames), `v3 Down Slash` (24 fps, 8 frames, triggers at 4 and 6).
- `raw-dump/fsm_Hero_Hornet(Clone)_Bind.txt`, `_Sprint.txt`, `_Harpoon Dash.txt`, `_Umbrella Float.txt`,
  `_Silk Specials.txt`, `_Crest Attacks.txt`, `_Tool Attacks.txt`; `raw-dump/tools.txt` (skill
  `FsmEventName` values).
- Related pages: `mechanics/nail-attacks-and-pogo.md`, `engine-and-input.md`.
