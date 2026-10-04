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
| Dash, Drifter's Cloak and double jump are not blocked during and right after an attack | `Attacking - Allow dash, cloak and double jump` (every crest) | blocked for the attack's recovery time (Reaper: 0.3 s) |

### Hunter

| Change | Config | Vanilla |
|---|---|---|
| Pogo has no windup | `Pogo - Shorter windup`, `Pogo - Windup seconds` (default 0) | 0.1 s windup |
| Sprint attack has no windup | `Sprint attack - Shorter windup`, `Sprint attack - Windup seconds` (default 0) | 0.1667 s windup |
| Needle Strike no longer pushes Hornet backwards | `Needle Strike - No pushback` | pushed back |
| Upgraded Hunter crest loses only some focus per hit taken | `Focus - Keep some on hit`, `Focus - Lost per hit` (default 6) | all focus lost |

### Reaper

| Change | Config | Vanilla |
|---|---|---|
| Up attack hits in front of Hornet | `Up attack - In front of Hornet` | mostly behind her |
| Sprint attack has no windup | `Sprint attack - Shorter windup`, `Sprint attack - Windup seconds` (default 0) | 0.125 s windup |
| Bigger Needle Strike | `Needle Strike - Bigger`, `Needle Strike - Size multiplier` (default 1.25) | 1 |
| Pogo hits instantly, keeps control of Hornet and no longer pulls her diagonally; keeps its slow attack speed | `Pogo - Instant, keeps control` | 0.1667 s windup, ~0.5 s without control |
| Silk Steel: fixed silk fragments per hit | `Silk Steel - Fixed fragments per hit`, `Silk Steel - Fragments per hit` (default 2) | random 1-3 |
| Silk Steel: Needle Strike drops more fragments | `Silk Steel - More Needle Strike fragments`, `Silk Steel - Needle Strike fragments per hit` (default 6) | same as other hits |
| Silk Steel is kept when taking damage; each hit shortens it instead | `Silk Steel - Kept on damage`, `Silk Steel - Seconds lost per hit` (default 1) | ends on damage |

### Wanderer

| Change | Config | Vanilla |
|---|---|---|
| Sprint attack has no windup | `Sprint attack - Shorter windup`, `Sprint attack - Windup seconds` (default 0) | 0.1111 s windup |

## Requirements

No other mods are required.
