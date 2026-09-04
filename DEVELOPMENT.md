# Campfire Respawn — Development Notes

`Peak_NoEndGame` keeps a PEAK run alive after the whole party is dead or fully passed out. Version 2.0.0 targets the current game API represented by the `游戏源码/Assembly-CSharp` snapshot in this repository.

## Behavior

- The host automatically revives the party at the current segment's reconnect/campfire point, including PEAK and Void-specific spawn rules.
- Fog and the active rising hazard are reset before the party is revived.
- `recordItemsAtCampfire = true` restores the last campfire inventory checkpoint, including the backpack slot.
- `recordItemsAtCampfire = false` attempts to recover the actual items dropped during the wipe.
- Each item uses `RespawnItemChance`; each run is limited by `RespawnMaxTimes`.
- `RespawnHotkey` performs the same reset manually on the host.
- `CampfireClearStatus` clears curable negative statuses for each local player while resting by a lit campfire.
- Respawn count and UI are host-authoritative; each client keeps a local campfire snapshot so normal host migration does not discard it.

The plugin keeps its original GUID and configuration keys, including the legacy `ReviveClearStatus` key, so existing config files continue to load.

## Architecture

- `Plugin.cs`: plugin lifecycle and compatible configuration.
- `Patches.cs`: narrow Harmony entry points only.
- `CampfireRespawn.cs`: one room-level respawn state machine and Photon synchronization.
- `InventoryCheckpoint.cs`: actor-number-based inventory snapshots using the game's `ItemInstanceData.Copy()` API.
- `ReviewUI.cs`: remaining-respawn display cloned from the game's current `AscentUI` styling.

## Build

Install BepInEx in PEAK, then run:

```powershell
dotnet build .\Peak_NoEndGame.sln -c Release -p:PeakGameDir="D:\Program Files (x86)\Steam\steamapps\common\PEAK"
```

If the managed assemblies or BepInEx core are elsewhere, pass `PeakManagedDir`, `BepInExCoreDir`, and/or `GameAssemblyPath` explicitly. The output is `Peak_NoEndGame/bin/Release/netstandard2.1/Peak_NoEndGame.dll`.

## Install

Copy `Peak_NoEndGame.dll` to `PEAK/BepInEx/plugins/CampfireRespawn/`. Every player should install the same plugin version; the host controls respawn and item restoration.

For a multiplayer smoke test, light a campfire, let the full party become dead or fully passed out, and verify the spawn position, hazard reset, inventory result, and remaining-respawn counter. Test both values of `recordItemsAtCampfire` because they intentionally use different recovery paths.
