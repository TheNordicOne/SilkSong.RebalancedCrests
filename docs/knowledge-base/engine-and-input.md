# Engine and Input

Engine timing, physics settings, the hero's physics body and the two input-buffering systems (InputHandler button queue and HeroController queue steps), as they are in the current game. Values come from the runtime dump (`raw-dump/`) and the decompiled game assembly.

---

## 1. Time settings (`engine-settings.txt`)

| Setting | Dumped value |
|---|---|
| `Time.fixedDeltaTime` | 0.01999999 (0.02 s → 50 FixedUpdate steps per second) |
| `Time.maximumDeltaTime` | 0.3333333 |
| `Time.timeScale` | 1 |
| `Application.targetFrameRate` | -1 (no cap) |
| `QualitySettings.vSyncCount` | 1 |
| `Screen.currentResolution` | 3840 x 2160 @ 144Hz |

The frame rate is a player setting, not a fixed game value:

- `GameSettings.vSync` (dumped: 1) and `GameSettings.targetFrameRate` (dumped: -1) are the saved options. `GameSettings.ResetVideoSettings()` sets `vSync = 1`, `targetFrameRate = 60`.
- Turning V-Sync on in the menu (`MenuSetting`, `MenuSettingType.VSync`) sets `vSyncCount = 1`, `Application.targetFrameRate = -1` and disables the frame-cap option. With V-Sync on, the render rate follows the monitor refresh rate (144 Hz in this dump).
- `MenuFrameCapSetting` applies `targetFrameRate` from the runtime refresh-rate list; values in 0–30 become 30, values not in the list become 60.
- With these dumped settings, `Update` runs once per rendered frame (144 per second) and `FixedUpdate` 50 times per second.

`fixedDeltaTime` is only changed by the PlayMaker action `ScaleTime` when its `adjustFixedDeltaTime` option is set (`Time.fixedDeltaTime = 0.02 * Time.timeScale`). Hit-stop (`GameManager.FreezeMoment`, `GameManager.SetTimeScale`) changes `Time.timeScale` through `TimeManager.TimeScale`; values ≤ 0.01 become 0.

---

## 2. Physics2D (`engine-settings.txt`)

| Setting | Dumped value |
|---|---|
| `Physics2D.gravity` | (0, -60) |
| `Physics2D.velocityIterations` | 8 |
| `Physics2D.positionIterations` | 3 |
| `Physics2D.defaultContactOffset` | 0.01 |
| `Physics2D.queriesHitTriggers` | True |
| `Physics2D.simulationMode` | FixedUpdate |

- Physics is simulated once per FixedUpdate step (0.02 s).
- `GameManager.SimulateSceneStartPhysics()` temporarily switches `simulationMode` to `Script` and calls `Physics2D.Simulate(Time.fixedDeltaTime)` repeatedly for 3 s of simulated time on scene start, then restores the previous mode.
- The PlayMaker action `SetGravity2d` can set `Physics2D.gravity`.

### 2.1 Layers

| # | Name | # | Name |
|---|---|---|---|
| 0 | Default | 17 | Attack |
| 1 | TransparentFX | 18 | Particle |
| 2 | Ignore Raycast | 19 | Interactive Object |
| 4 | Water | 20 | Hero Box |
| 5 | UI | 21 | Grass |
| 7 | Currency Self Collide | 22 | Enemy Attack |
| 8 | Terrain | 23 | Water Surface |
| 9 | Player | 24 | Bouncer |
| 10 | Self Collide | 25 | Soft Terrain |
| 11 | Enemies | 26 | Corpse |
| 12 | Projectiles | 27 | Physical Pusher |
| 13 | Hero Detector | 28 | Hero Only |
| 14 | Terrain Detector | 29 | ActiveRegion |
| 15 | Enemy Detector | 30 | Physical Push React |
| 16 | Tinker | 31 | Attack Detector |

Layers 3 and 6 are unnamed. `HeroController.GROUND_LAYERS` = 8448 (bits 8 and 13). Hero nail attack objects are on layer `Attack` (17); their `Clash Tink` children are on `Tinker` (16).

---

## 3. Hero physics body

`Hero_Hornet(Clone)` (tag `Player`, layer `Player`) from `hero-hierarchy.txt`:

| Component | Dumped values |
|---|---|
| Rigidbody2D | type=Dynamic, simulated=True, gravityScale=0, mass=1, linearDamping=0, angularDamping=0, interpolation=Interpolate, collision=Continuous, constraints=FreezeRotation, material=null |
| BoxCollider2D | enabled=True, trigger=False, offset=(0, -0.5132), size=(0.5, 2.0789), edgeRadius=0 |

The dump was taken while the hero was sitting at a bench (`PlayerData.atBench = True`, `hero_state = airborne`), with gravity switched off: `gravityScale = 0`, `HeroController.prevGravityScale = 1`.

Gravity-related `HeroController` values (`hero-controller.txt`):

| Field | Value |
|---|---|
| `DEFAULT_GRAVITY` | 1 (`ResetGravity()` sets `rb2d.gravityScale = DEFAULT_GRAVITY`) |
| `UNDERWATER_GRAVITY` | 0.225 |
| `AIR_HANG_GRAVITY` | 0.1 (`StartAirHang()`) |
| `AIR_HANG_ACCEL` | 5 (in `FixedUpdate`, while gravityScale < `DEFAULT_GRAVITY` and control is not relinquished: gravityScale += 5 × 0.02 per step; set straight to `DEFAULT_GRAVITY` when Jump is not held) |
| `MAX_FALL_VELOCITY` | 30 (`MAX_FALL_VELOCITY_WEIGHTED` 38 with Weighted Anklet) |
| `MAX_FALL_VELOCITY_DJUMP` | 10 |

- With gravityScale 1 the hero accelerates downward at 60 units/s².
- `AffectedByGravity(false)` stores the current scale in `prevGravityScale` and sets it to 0; `AffectedByGravity(true)` restores it.
- Fall speed is clamped in `FixedUpdate` to `-GetMaxFallVelocity()` (30, or 38 with Weighted Anklet) when `ForceClampTerminalVelocity`, back-sprinting/back-scuttling, or control is not relinquished and the hero is not shadow-dashing or quaking.

---

## 4. InputHandler button queue

`InputHandler` (on `_GameManager`, `input-handler.txt`, `InputHandler.cs`):

| Constant / field | Value |
|---|---|
| `BUTTON_QUEUE_TIME` | 0.1 (const; the code writes the literal `0.1f`) |
| `STAG_LOCKOUT_DURATION` | 1.2 |
| `buttonQueueTimers` | `float[24]`, one per `HeroActionButton` |

`HeroActionButton` indices: 0 JUMP, 1 ATTACK, 2 DASH, 3 SUPER_DASH, 4 CAST, 5 TAUNT, 6 QUICK_MAP, 7 INVENTORY, 8 MENU_SUBMIT, 9 MENU_CANCEL, 10 DREAM_NAIL, 11 UP, 12 DOWN, 13 LEFT, 14 RIGHT, 15 QUICK_CAST, 16 MENU_PANE_LEFT, 17 MENU_PANE_RIGHT, 18 MENU_EXTRA, 19 MENU_SUPER, 20 INVENTORY_MAP, 21 INVENTORY_JOURNAL, 22 INVENTORY_TOOLS, 23 INVENTORY_QUESTS.

`UpdateButtonQueueing()` runs at the start of every `InputHandler.Update()` (once per rendered frame):

- If `Time.timeScale <= Mathf.Epsilon`, nothing happens (timers freeze while time is stopped).
- For each button: if its action `WasPressed` this frame, the timer is set to 0.1. Otherwise, if the timer is > 0, it is decreased by `Time.unscaledDeltaTime`.
- Timers count in unscaled real time, so slow motion (0 < timeScale < 1) does not lengthen the window.

`GetWasButtonPressedQueued(button, consume)` returns true while the button's timer is > 0. With `consume = true` it sets the timer to 0. A press therefore stays readable for 0.1 s of real time, or until consumed. The dumped timers were -0.004062511 for JUMP and MENU_SUBMIT and 0 for all others (not queued).

Readers of this queue:

- `HeroController.RegainControl(allowInput)`: if `CanJump()` and a JUMP press is queued (consumed), performs `HeroJump()`; otherwise, if Jump was pressed while control was relinquished, sets `regainControlJumpQueued`.
- `SlideSurface.Update()`: JUMP (consumed).
- PlayMaker action `GetWasButtonPressedQueued` (skipped while the hero is paused). In the dumped FSMs it is used only by the `Sprint` FSM, always with `Consume = True` and `EveryFrame = True`: ATTACK in `Ground Sprint R`, `Ground Sprint L`, `Turn Anim`, `Air Sprint L`, `Air Sprint R`, `Air Sprint Loop`; JUMP in `Ground Sprint R`, `Ground Sprint L`, `Skid End 1`, `Skid End 2`.

Default bindings in the dumped `GameSettings`: keyboard jump `Z`, attack `X`, dash `C`, cast `A`, superDash `S`, dreamNail `D`, taunt `V`, quickMap `Tab`, quickCast `F`, arrows for directions; controller (XBOX_ONE) jump `Action1`, attack `Action3`, dash `RightTrigger`, cast `Action2`, superDash `LeftTrigger`, dreamNail `Action4`, quickMap `LeftBumper`, quickCast `RightBumper`, taunt `RightStickButton`.

---

## 5. HeroController input queue (steps)

Separate from the InputHandler queue, `HeroController` buffers presses that happen while the action is not yet allowed. The windows are counted in FixedUpdate steps.

| Constant | Value (dump) |
|---|---|
| `JUMP_QUEUE_STEPS` | 2 |
| `DOUBLE_JUMP_QUEUE_STEPS` | 10 |
| `JUMP_RELEASE_QUEUE_STEPS` | 2 (only used when `jumpReleaseQueueingEnabled`; dumped False) |
| `DASH_QUEUE_STEPS` | 10 |
| `ATTACK_QUEUE_STEPS` | 8 |
| `TOOLTHROW_QUEUE_STEPS` | 5 |
| `HARPOON_QUEUE_STEPS` | 8 |
| `LANDING_BUFFER_STEPS` | 5 |
| `LEDGE_BUFFER_STEPS` | 4 |
| `HEAD_BUMP_STEPS` | 3 |

Mechanism (`LookForQueueInput()`, called every `Update`; counters in `FixedUpdate`):

1. On `WasPressed` (Update): if the action is allowed now (`acceptingInput && Can…()`), it runs immediately. Otherwise `<x>QueueSteps = 0` and `<x>Queuing = true`. For Jump, a failed press also starts the double-jump queue when not swimming, not jumping and not sliding.
2. Each `FixedUpdate` increments `<x>QueueSteps` for every active queue.
3. Each `Update`, while the button is still held (`IsPressed`), the queued action runs as soon as `<x>QueueSteps <= <X>_QUEUE_STEPS` and the action is allowed.
4. `LookForInput()` clears `dashQueuing`, `attackQueuing`, `harpoonQueuing` and `toolThrowQueueing` as soon as the respective button is not held. Releasing the button ends the buffer.
5. `ResetInputQueues()` clears all queues.

The ledge buffer (coyote time): `LeftGround()` sets `ledgeBufferSteps = LEDGE_BUFFER_STEPS` (4) when the hero was on the ground in the previous check (`cState.wasOnGround`), decremented each FixedUpdate. `CanJump()` returns true off-ground while `ledgeBufferSteps > 0`, not dead/hazard-dying, control not relinquished, `headBumpSteps <= 0` and not near a roof. `Jump()` resets it to 0.

---

## 6. Update vs FixedUpdate timing

| Runs in | What | Granularity |
|---|---|---|
| `FixedUpdate` (50 Hz) | Physics simulation; `HeroController.FixedUpdate`: `Jump()` (`JUMP_STEPS` 8, `JUMP_STEPS_MIN` 2), `DoubleJump()` (`DOUBLE_JUMP_RISE_STEPS` 4, `DOUBLE_JUMP_FALL_STEPS` 4), `Dash()` (`dash_timer`), `Downspike()` thrust phase (`downSpikeTimer` vs `downspikeTime`), recoil (`recoilStepsLeft`, `RECOIL_HOR_STEPS` 8), downspike rebound (`DOWNSPIKE_REBOUND_STEPS` 6), wall-jump lock (`WJLOCK_STEPS_SHORT` 5, `WJLOCK_STEPS_LONG` 15, `WJLOCK_CHAIN_STEPS` 10), `WALL_STICKY_STEPS` 3, queue-step counters, ledge/landing/head-bump buffers, air-hang gravity ramp, terminal velocity clamp | 0.02 s per step |
| Custom `LateFixedUpdate` phase (added to the FixedUpdate player-loop by `CustomPlayerLoop`) | `DamageEnemies.LateFixedUpdate()`: evaluation of colliders entered since the last step and damage application; `DamageEnemies.OnFixedUpdate` counts `stepsToNextHit` for `multiHitter` damagers (`stepsPerHit` is in FixedUpdate steps) | 0.02 s per step |
| `Update` (once per rendered frame) | `InputHandler` button queue, `LookForInput`, `LookForQueueInput`, `attack_time` (vs `attackDuration`/`attackRecoveryTime`), `attack_cooldown`, `throwToolCooldown`, `dashCooldownTimer`, downspike antic timer (vs `downspikeAnticTime`), `downSpikeRecoveryTimer`, `nailChargeTimer`, crest timers (Warrior rage, Reaper mode, quickening, taunt effects), PlayMaker FSM `OnUpdate` actions | one rendered frame |

Consequences for times and frame counts in the other documents:

- Values given in seconds that are compared against `Time.deltaTime` sums in `Update` are real durations, ended on the first rendered frame at or after the threshold. At 144 fps that is within 1/144 s (≈ 0.0069 s) of the value; at 60 fps within 1/60 s.
- Values given in steps are multiples of 0.02 s: N steps = N × 0.02 s = N × 1.2 frames @60. Example: `RECOIL_HOR_STEPS` 8 = 0.16 s; `ATTACK_QUEUE_STEPS` 8 = 0.16 s; `LEDGE_BUFFER_STEPS` 4 = 0.08 s.
- A queue that is started in `Update` and counted in `FixedUpdate` is accepted while the counter is ≤ N. Since the press can land anywhere inside a 0.02 s step, the real-time window is between N × 0.02 s and (N + 1) × 0.02 s.
- At render rates above 50 fps (60, 144), some rendered frames contain no FixedUpdate step and others contain one. Below 50 fps, a rendered frame can contain more than one step. `Time.maximumDeltaTime` (0.333 s) limits how much time one frame can catch up.
- "@60" frame counts in the other documents are the second values × 60. They do not describe the number of `Update` calls at other frame rates.
- tk2d animation timings in the other documents are `frame index / fps` seconds of clip time. The dumped hero animators have `isRealtime = False`.
- Inside `FixedUpdate`, `Time.deltaTime` returns `Time.fixedDeltaTime`, so `Dash()` and `Downspike()` timers advance by 0.02 s per step.

---

## 7. Source references

Decompiled (`scratchpad/decomp/`):
- `InputHandler.cs` – `BUTTON_QUEUE_TIME` (l.53), `buttonQueueTimers` (l.101, l.156), `Update` (l.300), `UpdateButtonQueueing` (l.370), `GetWasButtonPressedQueued` (l.393).
- `GlobalEnums/HeroActionButton.cs`, `HutongGames.PlayMaker.Actions/GetWasButtonPressedQueued.cs`, `SlideSurface.cs` (l.236).
- `HeroController.cs` – queue constants (l.427–455), `Update` (l.1920; `LookForQueueInput` call l.2197), `FixedUpdate` (l.2973; queue counters l.3203–3226; buffer decrements l.3334–3352), `Jump` (l.4002), `DoubleJump` (l.4026), `Downspike` (l.4386), `GetMaxFallVelocity` (l.4866), `RegainControl` (l.7497, queued jump l.7735), `AffectedByGravity` (l.8289), `ResetGravity` (l.8313), `LookForInput` (l.8366), `LookForQueueInput` (l.8568), `ResetInputQueues` (l.8729), `CanJump` (l.10822), `LeftGround` (l.11600).
- `DamageEnemies.cs` – `Start` (l.555), `OnFixedUpdate` (l.661), `LateFixedUpdate` (l.674); `DamageEnemiesCallbackHooks.cs`; `CustomPlayerLoop.cs`.
- `GameManager.cs` – `SimulateSceneStartPhysics` (l.3177), `SetTimeScale` (l.5068), `FreezeMoment` (l.5146). `TimeManager.cs`.
- `MenuSetting.cs` (VSync, l.156), `UnityEngine.UI/MenuFrameCapSetting.cs`, `GameSettings.cs` (`ResetVideoSettings`, l.206), `HutongGames.PlayMaker.Actions/ScaleTime.cs`, `SetGravity2d.cs`.

Dumps (`raw-dump/`):
- `engine-settings.txt` – time, frame rate, vSync, Physics2D settings, layers.
- `input-handler.txt` – `InputHandler` fields, `GameSettings`, `buttonQueueTimers`, constants.
- `hero-controller.txt` – movement/queue constants, `prevGravityScale`, `jumpReleaseQueueingEnabled`, `GROUND_LAYERS`.
- `hero-hierarchy.txt` – hero `Rigidbody2D` (l.3827), `BoxCollider2D` (l.3887).
- `statics.txt` – `BUTTON_QUEUE_TIME`.
- `fsm_Hero_Hornet(Clone)_Sprint.txt` – `GetWasButtonPressedQueued` states.
