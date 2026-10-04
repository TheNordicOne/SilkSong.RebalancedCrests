# Silksong Knowledge Base

Describes how the game currently works. Nothing in here describes mod changes.

## Contents

- [Crest system](crest-system.md): crests, slots, hero configs, config selection, upgrades
- Crests
  - [Hunter](crests/hunter.md): Hunter, Hunter_v2, Hunter_v3, focus/combo meter
- Mechanics
  - [Nail attacks and pogo](mechanics/nail-attacks-and-pogo.md): slashes, down attack types, down spike timing, bounce
  - [Sprint](mechanics/sprint.md): Sprint FSM, sprint attacks
  - [Needle strike](mechanics/needle-strike.md): nail charge, Nail Arts FSM, charge slash objects

## Sources

- Runtime dump from the `Debug.DumpCrestData` option (`Diagnostics/CrestDump.cs`), written to
  `BepInEx/RebalancedCrests-dump/`. A local copy lives in `docs/knowledge-base/raw-dump/` (gitignored).
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
