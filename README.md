# Rebalanced Crests

Rebalanced Crests with several tweaks and some new mechanics. 

## Description

This mod is based on Holland's [Crest Reworks Series](https://www.youtube.com/playlist?list=PLOkGZTjTZ_Mlh9yaNjvVGT2tbgU-b32Kc) and is approved by him. We had a great time discussing further changes that go beyond what was discussed in the videos.

It implements all the proposed changes as best as possible, while keeping everything configurable.

- **All crests:** dash, glide and double jump right after attacking
- **Hunter:** faster pogo and run-slash, no Needle Strike pushback, upgraded crest keeps some focus when hit
- **Reaper:** up-slash in front, faster run-slash, bigger Needle Strike, instant pogo, better Silk Steal, new Harvest Bind, second silk skill slot
- **Wanderer:** faster run-slash, and two reworks to pick from: Critical Strike harp with a single-swipe Needle Strike, or a harp that turns the multi-hit Needle Strike into a critical swipe
- **Beast:** a blue tool slot

All settings are in the game's menu under **Options → Mods → Rebalanced Crests** (from the main menu or the pause menu), with one page per crest and move. Select a setting and press R / Backspace (controller: X / Square) to reset it, or use "Reset to default" at the bottom of a page of settings to reset that page. Under General, the preset `Default` sets everything to the recommended values. Changing any setting switches to `Custom`, and switching back to `Custom` restores your own values. With a BepInEx configuration manager installed, F1 opens the same settings.

## Installation instructions

The easiest way is to install it from Thunderstore with a mod manager like r2modman or Gale.

For a manual install you will need to have the BepInEx mod loader and [ModMenu](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/ModMenu/) (with its dependencies) installed. Check out
the [installation instructions on the official website](https://docs.bepinex.dev/articles/user_guide/installation/index.html).
Once you got it installed, download this mod and unpack the zip into

```
\steamapps\common\Hollow Knight Silksong
```

## Main features

Every change can be toggled and tuned individually, in the in-game menu or in the config file
(`BepInEx/config/com.thenordicone.rebalancedcrests.cfg`), with one section per crest. `General` → `Mod - Enable tweaks` turns all of them off at once. Settings are named `<Move> - <Change>`, so everything about one move sits together.

### All crests

| Change | Config | Vanilla |
|---|---|---|
| Dash, glide (Drifter's Cloak) and double jump are not blocked during and right after an attack | `Attacking - Allow dash, glide and double jump` (every crest) | blocked for the attack's recovery time (Reaper: 0.3 s) |

### Hunter

| Change | Config | Vanilla |
|---|---|---|
| Down-slash (pogo) has no windup | `Down-slash - Shorter windup`, `Down-slash - Windup seconds` (default 0) | 0.1 s windup |
| Run-slash has no windup | `Run-slash - Shorter windup`, `Run-slash - Windup seconds` (default 0) | 0.1667 s windup |
| Needle Strike no longer pushes Hornet backwards | `Needle Strike - No pushback` | pushed back |
| Upgraded Hunter crest loses only some focus per hit taken | `Focus - Keep some on hit`, `Focus - Lost per hit` (default 6) | all focus lost |

### Reaper

| Change | Config | Vanilla |
|---|---|---|
| Up-slash hits in front of Hornet | `Up-slash - In front of Hornet` | mostly behind her |
| Run-slash has no windup | `Run-slash - Shorter windup`, `Run-slash - Windup seconds` (default 0) | 0.125 s windup |
| Bigger Needle Strike | `Needle Strike - Bigger`, `Needle Strike - Size multiplier` (default 1.25) | 1 |
| Down-slash (pogo) hits instantly, keeps control of Hornet and no longer pulls her diagonally. It keeps its slow attack speed | `Down-slash - Instant, keeps control` | 0.1667 s windup, ~0.5 s without control |
| Silk Steal (bind effect): fixed Silk fragments per hit | `Silk Steal - Fixed fragments per hit`, `Silk Steal - Fragments per hit` (default 2) | random 1-3 (enemies that always drop 1 stay unchanged) |
| Silk Steal: Needle Strike drops more Silk fragments | `Silk Steal - More Needle Strike fragments`, `Silk Steal - Needle Strike fragments per hit` (default 6) | same as other hits |
| Silk Steal is kept when taking damage, so it always lasts its full duration | `Silk Steal - Kept on damage` | ends on damage |
| Silk Steal lasts a configurable time | `Silk Steal - Custom duration`, `Silk Steal - Duration seconds` (default 10) | 10 seconds |
| The glow around Hornet fades in and out during the last seconds of Silk Steal | `Silk Steal - Blink on Hornet before ending`, `Silk Steal - Blink seconds` (default 3), `Silk Steal - Blink speed` (default 0.15) | glow stays |
| Harvest Bind: Up + Bind is a quick bind (like Architect's Craft Bind) that starts Silk Steal without healing. Warding Bell and Injector Band apply | `Bind - Harvest Bind on Up`, `Bind - Harvest Bind Silk cost` (default 4) | Up + Bind is a normal bind |
| The Up tool slot holds a silk skill instead of a red tool. Up + skill uses it (a red tool equipped there is unequipped) | `Tool slots - Up slot holds a silk skill` | red tool slot |
| The Memory Locket lock moves to the Up silk skill slot and the Down red tool slot is free, so the number of lockets stays the same (a locket already used on the Down slot unlocks the Up slot) | `Tool slots - Swap locket to the Up skill slot` | Down slot needs the locket |

### Wanderer

| Change | Config | Vanilla |
|---|---|---|
| Run-slash has no windup | `Run-slash - Shorter windup`, `Run-slash - Windup seconds` (default 0) | 0.1111 s windup |

`Rework - Mode` picks one of two reworks (or `Vanilla`). Each mode has its own settings.

**CriticalStrike** (default): random critical hits are replaced by the Critical Strike harp, and Needle Strike is a single powerful swipe (Hunter's normal slash, scaled up, with the damage of Hunter's Needle Strike).

| Change | Config | Vanilla |
|---|---|---|
| Nail hits fill the harp (HUD harp opens at the first hit, strings appear at half, full harp at the end). A full harp makes the next Needle Strike critical (3× damage) | `Critical Strike - Hits to fill harp` (default 7) | 2 % crit chance per nail hit at 9+ Silk |
| Needle Strike is the single swipe | `Critical Strike - Swipe damage multiplier` (default 1), `Critical Strike - Swipe size multiplier` (default 1.5) | multi-hit flurry |

**HarpNeedleStrike**: the multi-hit Needle Strike is used until the harp is full. Then the next Needle Strike is the critical single swipe.

| Change | Config | Vanilla |
|---|---|---|
| Nail hits fill the harp. A full harp turns the next Needle Strike into the critical swipe | `Harp Needle Strike - Hits to fill harp` (default 7) | 2 % crit chance per nail hit at 9+ Silk |
| A multi-hit Needle Strike that hits adds strings to the harp | `Harp Needle Strike - Harp charge per strike` (default 2) | no harp |
| Every Nth hit of the multi-hit Needle Strike gives Silk (1, 3, 5… with 2, at most 3 Silk per enemy from a full flurry) | `Harp Needle Strike - Silk every Nth hit` (default 2) | only the first hit per enemy gives Silk |
| The full-harp swipe gives 1 Silk | `Harp Needle Strike - Swipe gives 1 Silk` | no swipe |
| Size and damage of the swipe | `Harp Needle Strike - Swipe damage multiplier` (default 1), `Harp Needle Strike - Swipe size multiplier` (default 1.5) | no swipe |

### Beast

| Change | Config | Vanilla |
|---|---|---|
| Blue tool slot: replaces the left yellow slot (still has to be unlocked), or a new unlocked slot above the Up tool slot | `Tool slots - Extra blue slot`, `Tool slots - Extra blue slot placement` (default ReplaceYellowSlot) | no blue slot |

## Requirements

[ModMenu](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/ModMenu/) is required. It provides the in-game settings menu. A mod manager installs it (and its dependencies) automatically. A BepInEx configuration manager is optional (F1).
