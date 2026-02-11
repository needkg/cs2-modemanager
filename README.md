# ModeManager

Mode manager for CS2 servers using CounterStrikeSharp, with vote-based mode switching, safe delayed/cooldown execution, dynamic commands, and runtime config reload.

## Features

- Vote-based mode switching with proportional quorum (`VoteRatio`), vote expiration, and minimum player count.
- One vote per player (SteamID, with UserId fallback).
- Cooldown between switches to prevent spam.
- Predictable mode apply pipeline: `ResetCommand`, per-mode plugin unload/load, `ExecCommand`, optional `game_type`/`game_mode`, and `changelevel` using mode map, current map, or `de_dust2`.
- Dynamic mode commands generated from `Modes` keys (`css_<modeKey>`, sanitized).
- Automatic initial mode scheduling on startup when the first valid human player joins.
- Runtime config reload (`css_mm_reload`) with validation and dynamic command rebuild.
- Localization with `en` and `pt-BR` catalogs (safe fallback behavior).

## Requirements

- .NET 8 SDK (for local build)
- Compatible CounterStrikeSharp API (`CounterStrikeSharp.API` 1.0.284 in this project)
- CS2 server with CounterStrikeSharp installed

## Build

```powershell
dotnet restore
dotnet build ModeManager.sln -c Release
```

Main build outputs:

- `bin/Release/net8.0/ModeManager.dll`
- `bin/Release/net8.0/ModeManager.deps.json`
- `bin/Release/net8.0/lang/en.json`
- `bin/Release/net8.0/lang/pt-BR.json`

## Installation

- Copy the binaries to your CounterStrikeSharp plugin directory on the server.
- Keep the `lang` folder next to the plugin so external message files can be loaded.

## Configuration

Only supported config path:

- `addons/counterstrikesharp/configs/plugins/ModeManager/ModeManager.json`
- Any other JSON path is ignored by config reload.
- Mode keys must generate unique dynamic commands after sanitization and must not conflict with reserved base commands.

Example configuration:

```json
{
  "Language": "en",
  "InitialModeKey": "retake",
  "ApplyInitialModeOnStartup": true,
  "ResetCommand": "exec cfg/modes/reset.cfg",
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
      "ExecCommand": "exec cfg/modes/retake.cfg",
      "DefaultMap": "de_inferno",
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

- `css_mm` (`!mm`): show general help.
- `css_modes` (`!modes`): list available modes.
- `css_setmode <key>` (`!setmode <key>`): start or join a mode vote.
- `css_mm_vote` (`!mm_vote`): show current vote/pending switch status.
- `css_mm_reload` (`!mm_reload`): reload config from disk and rebuild dynamic commands (admin with `@css/root` or server console only).
- `css_<key>` (`!<key>`): dynamic shortcut command to vote directly for a mode.

## Voting Flow

- Server console schedules a switch directly (no vote required).
- HLTV and bot players are excluded from voting.
- Voting for the currently active mode is rejected.
- A vote already in progress cannot be replaced by a vote for another mode.
- Required votes = `ceil(eligible_players * VoteRatio)`.
- Votes expire after `VoteDurationSeconds`.
- After approval, switch execution waits for `SwitchDelaySeconds`.
- After apply, `SwitchCooldownSeconds` is enforced before another switch.

## Reload Behavior

- `css_mm_reload` attempts to reload JSON from disk.
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
