# nModeManager

Mode manager for CS2 servers using CounterStrikeSharp, with vote-based mode switching, safe delayed/cooldown execution, dynamic commands, and runtime config reload.

## Features

- Vote-based mode switching with proportional quorum (`VoteRatio`), vote expiration, and minimum player count.
- One vote per player (SteamID, with UserId fallback).
- Cooldown between switches to prevent spam.
- Predictable mode apply pipeline: `ResetCommand`, per-mode plugin unload/load, `ExecCommand`, optional `game_type`/`game_mode`, and `changelevel` using mode map, current map, or `de_dust2`.
- Automatic reset cfg provisioning: if `ResetCommand` is `exec <relative>.cfg` (example: `exec nmodemanager/reset.cfg`), the plugin creates the missing file under `cfg/`.
- Dynamic mode commands generated from `Modes` keys (`css_<modeKey>`, sanitized).
- Interactive RTV flow via MenuManagerAPI capability (`menu:api`) as a required integration.
- RTV selection flow: `mode -> map -> confirm`, with per-mode `MapPool` support.
- Automatic initial mode scheduling on startup when the first valid human player joins.
- Runtime config reload (`css_nmm_reload`) with validation and dynamic command rebuild.
- Localization with `en` and `pt-BR` catalogs (safe fallback behavior).
- Chat prefix is localized and customizable in language files via `ChatPrefix`.

## Requirements

- .NET 8 SDK (for local build)
- Compatible CounterStrikeSharp API (`CounterStrikeSharp.API` 1.0.284 in this project)
- CS2 server with CounterStrikeSharp installed
- MenuManagerAPI plugin installed on the server (`menu:api` capability) as a mandatory dependency (nModeManager will not load without it)

## Build

```powershell
dotnet restore
dotnet build ModeManager.sln -c Release
```

Main build outputs:

- `bin/Release/net8.0/nModeManager.dll`
- `bin/Release/net8.0/nModeManager.deps.json`
- `bin/Release/net8.0/lang/en.json`
- `bin/Release/net8.0/lang/pt-BR.json`

## Installation

- Copy the binaries to your CounterStrikeSharp plugin directory on the server.
- Keep the `lang` folder next to the plugin so external message files can be loaded.

## Configuration

Only supported config path:

- `addons/counterstrikesharp/configs/plugins/nModeManager/nModeManager.json`
- Any other JSON path is ignored by config reload.
- Mode keys must generate unique dynamic commands after sanitization and must not conflict with reserved base commands.

Example configuration:

```json
{
  "Language": "en",
  "InitialModeKey": "retake",
  "ApplyInitialModeOnStartup": true,
  "ResetCommand": "exec nmodemanager/reset.cfg",
  "VoteRatio": 0.75,
  "VoteMinPlayers": 1,
  "VoteDurationSeconds": 25,
  "SwitchCooldownSeconds": 20,
  "SwitchDelaySeconds": 5,
  "ApplyGameTypeMode": true,
  "Modes": {
    "retake": {
      "Key": "retake",
      "DisplayName": "Retake",
      "ExecCommand": "exec nmodemanager/retake.cfg",
      "DefaultMap": "de_inferno",
      "MapPool": [
        "de_inferno",
        "de_nuke",
        "de_mirage"
      ],
      "GameType": 0,
      "GameMode": 0,
      "PluginsToUnload": [
        "SomePlugin"
      ],
      "PluginsToLoad": [
        "AnotherPlugin"
      ]
    }
  }
}
```

## Commands

- `css_nmm` (`!nmm`): show general help.
- `css_modes` (`!modes`): list available modes.
- `css_rtv` (`!rtv`): open RTV flow (`mode -> map -> confirm`).
- `css_mode <key>` (`!mode <key>`): admin-only mode apply command.
- `css_nmm_reload` (`!nmm_reload`): reload config from disk and rebuild dynamic commands (admin with `@css/root` or server console only).
- `css_<key>` (`!<key>`): dynamic shortcut command to vote directly for a mode.

## Voting Flow

- Server console schedules a switch directly (no vote required).
- HLTV and bot players are excluded from voting.
- Voting for the currently active mode is rejected.
- A vote already in progress cannot be replaced by a vote for another mode.
- If a vote is already active, mode and map selection are locked to that active vote.
- Required votes = `ceil(eligible_players * VoteRatio)`.
- Vote messages display current votes, votes remaining, and time remaining in real time.
- Votes expire after `VoteDurationSeconds`.
- After approval, the plugin announces the winning mode/map and schedules switch after `SwitchDelaySeconds`.
- After apply, `SwitchCooldownSeconds` is enforced before another switch.

## Reload Behavior

- `css_nmm_reload` attempts to reload JSON from disk.
- If the file is not found, the plugin continues using in-memory config.
- On reload, pending votes and cooldown are cleared.
- Dynamic mode commands are always rebuilt after reload.

## Project Structure

- `ModeManagerPlugin.cs`: plugin metadata and lifecycle.
- `Features/Commands`: base and dynamic commands.
- `Features/Voting`: vote session and vote rules.
- `Features/Switching`: scheduling and mode apply logic.
- `Features/Configuration`: config model, validation, discovery, and reload.
- `Features/Startup`: initial mode on first valid player.
- `Features/Localization` and `lang/*.json`: localized messages.
- `Shared/`: logging, map resolver, and command sanitization utilities.

## License

This project is licensed under the MIT License. See `LICENSE`.
