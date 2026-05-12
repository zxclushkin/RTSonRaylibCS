# Tiny RTS 3D Prototype

A small original 3D RTS prototype built with C# and raylib-cs.

The project is inspired by the genre feel of classic base-building RTS games, but it does not use protected names, characters, factions, lore, art, or assets from other games.

## Run

```powershell
dotnet run
```

## Controls

- `WASD` or arrow keys: move RTS camera
- Mouse wheel: zoom in/out
- Left mouse: select a Human worker
- Left mouse: select a Human building
- Right mouse on ground: move selected unit, or set rally point for selected building
- Right mouse on visible enemy: attack
- Right mouse on ore with a worker: gather ore
- `B`: enter construction placement mode for the selected worker
- `T`: train a unit from the selected completed building
- `S`: stop selected unit or clear selected building queue
- Left mouse in construction mode: place the building foundation
- `Esc`: cancel construction mode or clear selection

## Current Prototype

- 3D map plane with a 64x64 tile grid
- RTS-style angled camera
- Tile data for walkable/buildable/resource/building occupancy
- Human and Orc faction definitions
- Human main building and Orc main building
- Human worker and Orc worker placeholders
- Human Vanguard and Orc Brute combat units
- Ore resource nodes
- A* pathfinding on the tile grid
- Worker and combat unit movement
- Worker gathering loop: gather, return to main building, deposit, repeat
- Ore counter HUD
- Building placement and construction timer
- Building selection, rally points, and training queues
- Combat commands, auto-acquisition, health, damage, attack ranges, cooldowns
- Simple Orc AI: economy, War Hut construction, unit training, attack waves
- Fog of war with unseen/explored/visible tile states
- Command panel for build, train, stop, and cancel actions
- Placeholder primitive rendering for units, buildings, resources, selection, and build preview
- JSON map save data classes ready for a future map editor

## Architecture

- `Core`: game loop, state, config
- `World`: tile grid, resources, map save data
- `Entities`: entity, unit, worker, building
- `Gameplay`: factions, players, commands, resources, building logic
- `Input`: selection and command input
- `Rendering`: RTS camera and 3D/debug rendering
- `UI`: HUD
