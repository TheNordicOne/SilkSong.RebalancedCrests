# Rebalanced Crests

Rebalanced Crests with several tweaks and some new mechanics.

## Description

This mod is based on Holland's [Crest Reworks Series](https://www.youtube.com/playlist?list=PLOkGZTjTZ_Mlh9yaNjvVGT2tbgU-b32Kc).

It implements all the proposed changes as best as possible, while keeping everything configurable.

- **All crests:** dash, glide and double jump right after attacking
- **Hunter:** faster pogo and run-slash, no Needle Strike pushback, upgraded crest keeps some focus when hit
- **Reaper:** up-slash in front, faster run-slash, bigger Needle Strike, instant pogo, better Silk Steal, new Harvest Bind
- **Wanderer:** faster run-slash, Critical Strike harp instead of random crits, optional single-swipe Needle Strike, Silk on every Needle Strike hit

With a BepInEx configuration manager installed, press F1 in game to open the settings. There you can tweak the numbers or toggle the tweaks.

## Installation instructions

The easiest way is to install it from Thunderstore with a mod manager like r2modman or Gale.

For a manual install you will need to have the BepInEx mod loader installed. Check out
the [installation instructions on the official website](https://docs.bepinex.dev/articles/user_guide/installation/index.html).
Once you got it installed, download this mod and unpack the zip into

```
\steamapps\common\Hollow Knight Silksong
```

## Main features

Every change can be toggled and tuned individually
in the config file (`BepInEx/config/com.thenordicone.rebalancedcrests.cfg`), with one section per crest. Settings are named `<Move> - <Change>`, so everything about one move sits together.

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
| Down-slash (pogo) hits instantly, keeps control of Hornet and no longer pulls her diagonally; keeps its slow attack speed | `Down-slash - Instant, keeps control` | 0.1667 s windup, ~0.5 s without control |
| Silk Steal (bind effect): fixed Silk fragments per hit | `Silk Steal - Fixed fragments per hit`, `Silk Steal - Fragments per hit` (default 2) | random 1-3 (enemies that always drop 1 stay unchanged) |
| Silk Steal: Needle Strike drops more Silk fragments | `Silk Steal - More Needle Strike fragments`, `Silk Steal - Needle Strike fragments per hit` (default 6) | same as other hits |
| Silk Steal is kept when taking damage; each hit shortens it instead | `Silk Steal - Kept on damage`, `Silk Steal - Seconds lost per hit` (default 1) | ends on damage |
| Harvest Bind: Up + Bind is a quick bind (like Architect's Craft Bind) that starts Silk Steal without healing; Warding Bell and Injector Band apply | `Bind - Harvest Bind on Up`, `Harvest Bind - Silk cost` (default 4) | Up + Bind is a normal bind |

### Wanderer

| Change | Config | Vanilla |
|---|---|---|
| Run-slash has no windup | `Run-slash - Shorter windup`, `Run-slash - Windup seconds` (default 0) | 0.1111 s windup |
| Critical Strike replaces random critical hits: once you have Needle Strike, nail hits fill the harp (HUD harp opens at the first hit, strings appear at half, full harp at the end); a full harp makes the next Needle Strike critical (3× damage) | `Critical hits - Critical Strike`, `Critical Strike - Hits to fill harp` (default 7) | 2 % crit chance per nail hit at 9+ Silk |
| Needle Strike is a single powerful swipe (Hunter's Needle Strike) instead of a flurry of weak hits | `Needle Strike - Single powerful swipe`, `Needle Strike - Swipe damage multiplier` (default 1) | multi-hit flurry |
| The single swipe hits once with the damage of both of its hits (the second swing stays visible) | `Needle Strike - Swipe hits once` | Hunter's Needle Strike hits twice |
| Every hit of the multi-hit Needle Strike gives Silk (only matters with the single swipe off) | `Needle Strike - Silk on every hit` | only the first hit gives Silk |

## Requirements

No other mods are required. A configuration manager is optional for the in-game settings menu.
