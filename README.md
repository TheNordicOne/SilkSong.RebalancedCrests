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
in the config file (`BepInEx/config/com.thenordicone.rebalancedcrests.cfg`), with one section per crest.

### All crests

| Change | Config | Vanilla |
|---|---|---|
| Dash, Drifter's Cloak and double jump are not blocked during and right after an attack | `MovementAfterAttackEnabled` (in every crest section) | blocked for the attack's recovery time (Reaper: 0.3 s) |

### Hunter

| Change | Config | Vanilla |
|---|---|---|
| Down attack (pogo) has no windup | `PogoWindupEnabled`, `PogoWindupSeconds` (default 0) | 0.1 s windup |
| Sprint slash has no windup | `SprintSlashWindupEnabled`, `SprintSlashWindupSeconds` (default 0) | 0.1667 s windup |
| Needle strike no longer pushes Hornet backwards | `NeedleStrikeNoPushbackEnabled` | pushed back |
| Upgraded Hunter crest loses only some focus per hit taken | `FocusLossOnHitEnabled`, `FocusLossOnHitAmount` (default 6) | all focus lost |

### Reaper

| Change | Config | Vanilla |
|---|---|---|
| Up slash in front of Hornet | `UpSlashInFrontEnabled` | mostly behind her |
| Sprint slash has no windup | `SprintSlashWindupEnabled`, `SprintSlashWindupSeconds` (default 0) | 0.125 s windup |
| Bigger needle strike | `NeedleStrikeRangeEnabled`, `NeedleStrikeRangeScale` (default 1.25) | 1 |
| Down slash without windup, control lock and forced diagonal movement; keeps its attack speed | `DownSlashResponsiveEnabled` | 0.1667 s windup, ~0.5 s without control |
| Silk Steel: fixed silk bundles per hit | `SilkBundlesFixedEnabled`, `SilkBundlesPerHit` (default 2) | random 1-3 |
| Silk Steel: needle strike drops more bundles | `NeedleStrikeSilkBundlesEnabled`, `NeedleStrikeSilkBundlesPerHit` (default 6) | same as other hits |
| Silk Steel is kept when taking damage; each hit shortens it instead | `SilkSteelKeptOnDamageEnabled`, `SilkSteelSecondsPerHit` (default 1) | ends on damage |

### Wanderer

| Change | Config | Vanilla |
|---|---|---|
| Sprint slash has no windup | `SprintSlashWindupEnabled`, `SprintSlashWindupSeconds` (default 0) | 0.1111 s windup |

## Requirements

No other mods are required.
