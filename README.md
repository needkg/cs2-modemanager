# nModeManager

`nModeManager` is a CS2 plugin for CounterStrikeSharp focused on practical mode rotation:
- player voting with `!rtv`
- map selection per mode
- safe mode switching with delay and cooldown
- admin mode/map override command

## Player Experience

- `!rtv` opens a guided flow: mode -> map -> confirm vote.
- If a vote is already active, players can only vote for that same mode.
- During an active vote, players can still choose a different map inside that mode.
- If a player already voted and runs `!rtv` again, they receive live vote status in chat.
- If a vote expires without enough votes, the plugin announces final vote progress and closes the vote.

## Admin Experience

- Force mode switch quickly with:
`!mode <mode_key> [map]`
- Reload plugin config and dynamic commands without restarting server:
`!nmm_reload`
- Dynamic vote commands are auto-created from configured mode keys:
if mode key is `retake`, command `!retake` is available.

## Requirements

- CS2 Dedicated Server
- [Metamod 2.0 Dev](https://www.sourcemm.net/downloads.php?branch=dev)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)
- [MenuManagerAPI](https://github.com/nickj609/MenuManagerAPI)

## Quick Installation

1. Install [MenuManagerAPI](https://github.com/nickj609/MenuManagerAPI) and confirm the `menu:api` capability is available.
2. Copy `nModeManager.dll` and `nModeManager.deps.json` to:
`addons/counterstrikesharp/plugins/nModeManager/`
3. Copy the `lang` folder to:
`addons/counterstrikesharp/plugins/nModeManager/lang/`
4. Create or edit:
`addons/counterstrikesharp/configs/plugins/nModeManager/nModeManager.json`
5. Restart the server (or reload plugins).

## Minimal Config (Copy/Paste)

```json
{
  "Language": "en",
  "InitialModeKey": "retake",
  "ApplyInitialModeOnStartup": true,
  "ResetCommand": "exec nmodemanager/reset.cfg",
  "VoteRatio": 0.6,
  "VoteMinPlayers": 1,
  "VoteDurationSeconds": 120,
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
      "PluginsToUnload": [],
      "PluginsToLoad": []
    }
  }
}
```

## Commands

### Player Commands

| Command | Description |
|---|---|
| `!nmm` | Show help |
| `!modes` | List available modes |
| `!rtv` | Open RTV vote menu |
| `!<mode_key>` | Vote directly for a mode (example: `!retake`) |

### Admin (`@css/root`) and Console Commands

| Command | Description |
|---|---|
| `!mode <mode_key> [map]` | Force a mode switch with optional map |
| `!nmm_reload` | Reload config and rebuild dynamic mode commands |

## Voting Rules

1. Player opens vote with `!rtv`.
2. Player selects mode.
3. Player selects map.
4. Player confirms vote.
5. Plugin broadcasts vote progress.
6. If quorum is reached, mode switch is scheduled.
7. If timer expires before quorum, vote closes and final status is announced.

Important:
- one vote per player identity
- bots and HLTV are excluded from eligible player count
- required votes are calculated as:
`ceil(eligible_players * VoteRatio)`
- while a vote is active, mode choice is locked to the active vote mode
- map target can still be adjusted within that active vote mode

## Admin `!mode` Behavior

Examples:
- `!mode retake`
- `!mode retake de_nuke`

Rules:
1. If map is invalid for the selected mode, plugin returns available maps for that mode.
2. If selected mode is already active and no map is provided, command is blocked.
3. If selected mode is already active and map equals current map, command is blocked.
4. If selected mode is already active and map is valid and different, command is allowed.

## Localization

Supported languages:
- `en`
- `pt-BR`
- `es`
- `ru`

Set language in config:
`"Language": "en"`

## Practical Tips

- Keep `MapPool` populated for each mode to improve vote UX.
- Use `SwitchDelaySeconds` to give players warning before switch.
- Use `SwitchCooldownSeconds` to prevent switch spam.
- Run `!nmm_reload` after each config change.

## Troubleshooting

### `!rtv` does not open menu

Check if MenuManagerAPI is installed, loaded, and exposing `menu:api`.

### `!mode` or `!nmm_reload` denied

Player must have `@css/root` permission.

### Dynamic mode command is missing

Check mode key under `Modes` and run `!nmm_reload`.

## License

MIT. See [`LICENSE`](https://github.com/needkg/DayNightPvP/blob/main/LICENSE).
