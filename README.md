# nModeManager

`nModeManager` is a CS2 server plugin for CounterStrikeSharp that lets players vote game modes, choose maps from a menu, and switch modes safely with cooldown/delay controls.

## What You Get

- Player voting for mode changes (`!rtv` menu flow).
- Map selection per mode (`mode -> map -> confirm`).
- Dynamic commands like `!retake`, `!dm`, based on your config keys.
- Cooldown and vote expiration to prevent spam.
- Startup mode auto-apply when first real player joins.
- English and Brazilian Portuguese messages.

## Server Requirements

- CS2 dedicated server with CounterStrikeSharp installed.
- [`MenuManagerAPI`](https://github.com/nickj609/MenuManagerAPI) installed and working (`menu:api` capability).

## Installation

1. Download the plugin release files.
2. Copy `nModeManager.dll` and `nModeManager.deps.json` to:
`addons/counterstrikesharp/plugins/nModeManager/`
3. Copy the `lang` folder next to the plugin DLL:
`addons/counterstrikesharp/plugins/nModeManager/lang/`
4. Create or edit config at:
`addons/counterstrikesharp/configs/plugins/nModeManager/nModeManager.json`
5. Restart the server or load plugins again.

## Quick Config Example

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

## Important Config Notes

- `MapPool` controls maps shown in the RTV menu for that mode.
- If `MapPool` is empty, plugin falls back to `DefaultMap`, current map, then `de_dust2`.
- Mode keys generate dynamic commands (`css_<key>` / `!<key>`). Keys must be unique after sanitization.
- `ResetCommand` can auto-create a missing cfg file when using `exec <relative>.cfg`.

## Commands

<details>
<summary>Player Commands</summary>

- `!nmm` (`css_nmm`): show help.
- `!modes` (`css_modes`): list available modes.
- `!rtv` (`css_rtv`): open vote menu (`mode -> map -> confirm`).
- `!<modeKey>` (`css_<modeKey>`): vote directly for a mode from chat.

</details>

<details>
<summary>Admin/Console Commands</summary>

- `!mode <key>` (`css_mode <key>`): force a mode switch (admin).
- `!nmm_reload` (`css_nmm_reload`): reload config and rebuild dynamic commands (`@css/root` or server console).

</details>

## How Voting Works

- Console can schedule a mode switch directly (no vote).
- HLTV and bots are ignored for vote counting.
- One vote per player identity.
- Required votes use: `ceil(eligible_players * VoteRatio)`.
- Votes expire after `VoteDurationSeconds`.
- Once approved, switch is scheduled with `SwitchDelaySeconds`.
- After apply, switch cooldown uses `SwitchCooldownSeconds`.

## Menu Flow

1. Player runs `!rtv`.
2. Player selects a mode.
3. Player selects a map from that mode's `MapPool`.
4. Player confirms vote.
5. Plugin announces progress and switches when quorum is reached.

## Localization

- Supported languages: `en`, `pt-BR`.
- Change language with `Language` in config.

## Troubleshooting

- Plugin not loading:
  Check if `MenuManagerAPI` is installed and exposing `menu:api`.
- Commands not updating after config change:
  Run `css_nmm_reload` from console or `!nmm_reload` as root admin.
- Wrong config file being ignored:
  Ensure path is exactly `addons/counterstrikesharp/configs/plugins/nModeManager/nModeManager.json`.

## License

MIT. See `LICENSE`.
