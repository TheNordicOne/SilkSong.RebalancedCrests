# Rebalanced Crests

TODO short description

## Description

TODO

Installation instructions
The easiest way is to install it from Thunderstore with a mod manager like r2modman or Gale.

For a manual install you will need to have the BepInEx mod loader installed. Check out
the [installation instructions on the official website](https://docs.bepinex.dev/articles/user_guide/installation/index.html).
Once you got it installed, download this mod and unpack the zip into

```
\steamapps\common\Hollow Knight Silksong
```

## Main features

Based on Holland's "Fixing Silksong's ... Crest" video series. Every change can be toggled and tuned individually
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
| Silk Steal (bind effect): fixed Silk fragments per hit | `Silk Steal - Fixed fragments per hit`, `Silk Steal - Fragments per hit` (default 2) | random 1-3 |
| Silk Steal: Needle Strike drops more Silk fragments | `Silk Steal - More Needle Strike fragments`, `Silk Steal - Needle Strike fragments per hit` (default 6) | same as other hits |
| Silk Steal is kept when taking damage; each hit shortens it instead | `Silk Steal - Kept on damage`, `Silk Steal - Seconds lost per hit` (default 1) | ends on damage |
| Harvest Bind: Up + Bind is a quick bind (like Architect's Craft Bind) that starts Silk Steal without healing; Warding Bell and Injector Band apply | `Bind - Harvest Bind on Up`, `Harvest Bind - Silk cost` (default 4) | Up + Bind is a normal bind |

### Wanderer

| Change | Config | Vanilla |
|---|---|---|
| Run-slash has no windup | `Run-slash - Shorter windup`, `Run-slash - Windup seconds` (default 0) | 0.1111 s windup |
| Critical Strike replaces random critical hits: once you have Needle Strike, nail hits fill the harp (HUD harp appears when full); a full harp makes the next Needle Strike critical (3× damage) | `Critical hits - Critical Strike`, `Critical Strike - Hits to fill harp` (default 7) | 2 % crit chance per nail hit at 9+ Silk |

## Requirements

No other mods are required.
