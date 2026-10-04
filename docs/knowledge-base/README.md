# Silksong Knowledge Base

Describes how the game currently works. Nothing in here describes mod changes.

## Contents

- [Crest system](crest-system.md): crests, slots, hero configs, config selection, upgrades
- [Engine and input](engine-and-input.md): fixed timestep, Physics2D, layers, hero Rigidbody2D, input buffering, Update vs FixedUpdate timing
- Crests
  - [Hunter](crests/hunter.md): Hunter, Hunter_v2, Hunter_v3, focus/combo meter
- Mechanics
  - [Nail attacks and pogo](mechanics/nail-attacks-and-pogo.md): slashes, down attack types, down spike timing, bounce
  - [Sprint](mechanics/sprint.md): Sprint FSM, sprint attacks
  - [Needle strike](mechanics/needle-strike.md): nail charge, Nail Arts FSM, charge slash objects

## Sources

- Runtime dump from the `Debug.DumpCrestData` option (`Diagnostics/CrestDump.cs`), written to
  `BepInEx/RebalancedCrests-dump/`. A local copy lives in `docs/knowledge-base/raw-dump/` (gitignored). Files:
  - `crests.txt`: all `ToolCrest` fields (localised name/description keys and strings, sprites, slots), hero configs, override animation libraries
  - `hero-configs.txt`: `HeroController.configs` / `specialConfigs` with attack objects, tags and components
  - `hero-hierarchy.txt`: the full hero GameObject tree with all component values, tags and layers
  - `hero-controller.txt`: `HeroController`, `HeroAnimationController` and `PlayerData` fields
  - `global-settings.txt`: all `GlobalSettings` classes (`Gameplay`, `Effects`, `UI`, ...)
  - `tools.txt`: all tool items
  - `referenced-objects.txt`: referenced assets (`HeroControllerConfig`, `DamageTag`, `ToolCrestList`, ...)
  - `statics.txt`: static fields and constants of game classes
  - `input-handler.txt`: `InputHandler` fields, `GameSettings`, button queue
  - `engine-settings.txt`: time, frame rate, Physics2D settings, layers
  - `animations-tk2d.txt`: tk2d animation libraries and clips
  - `animator-timelines.txt`: Unity Animator clip timelines
  - `fsm_*.txt`: PlayMaker FSMs (file names contain the object path, `~` as separator), with complete action parameters
- Decompiled `Assembly-CSharp.dll` of the installed game.

## Internal names

| In game   | Internal crest / config |
|-----------|-------------------------|
| Hunter    | `Hunter`, `Hunter_v2`, `Hunter_v3` (config `Default`) |
| Beast     | `Warrior` |
| Architect | `Toolmaster` |
| Witch     | `Witch` (config `Whip`) |
| Shaman    | `Spell` (config `Shaman`) |
| (hidden)  | `Cursed`, `Cloakless` |
