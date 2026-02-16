# nModeManager

`nModeManager` is a CS2 server plugin for CounterStrikeSharp that lets players vote game modes, choose maps from a menu, and switch modes safely with cooldown/delay controls.

## What You Get

- Player voting for mode changes (`!rtv` menu flow).
- Map selection per mode (`mode -> map -> confirm`).
- Dynamic commands like `!retake`, `!dm`, based on your config keys.
- Cooldown and vote expiration to prevent spam.
- Startup mode auto-apply when first real player joins.
- English, Brazilian Portuguese, Spanish, and Russian messages.

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
- `css_mode` accepts optional map (`css_mode <key> [map]`); explicit maps must be valid for the selected mode.
- If a mode is already active, `css_mode` only proceeds when a different valid map is explicitly provided.
- `ResetCommand` can auto-create a missing cfg file when using `exec <relative>.cfg`.
- When a vote expires without quorum, the plugin announces final vote status and clears the active vote.

## Commands

<details>
<summary>Player Commands</summary>

- `!nmm` (`css_nmm`): show help.
- `!modes` (`css_modes`): list available modes.
- `!rtv` (`css_rtv`): open RTV vote menu (or active-vote map selection).
- `!<modeKey>` (`css_<modeKey>`): vote directly for a mode from chat.

</details>

<details>
<summary>Admin/Console Commands</summary>

- `!mode <key> [map]` (`css_mode <key> [map]`): force a mode switch (admin), optionally targeting a specific map.
- `!nmm_reload` (`css_nmm_reload`): reload config and rebuild dynamic commands (`@css/root` or server console).

</details>

## Command Examples

- `!mode retake`: apply `retake` with the mode's preferred map.
- `!mode retake de_nuke`: apply `retake` and force `de_nuke` (must be valid for that mode).
- `!rtv`: open mode/map vote menu, or map-only menu for the active vote mode.

## How Voting Works

- Console can schedule a mode switch directly (no vote).
- HLTV and bots are ignored for vote counting.
- One vote per player identity.
- Required votes use: `ceil(eligible_players * VoteRatio)`.
- Votes expire after `VoteDurationSeconds`.
- While a vote is active, the mode is locked to that active vote.
- During an active vote, players using `!rtv` only vote for that mode (they can still pick other maps).
- If a player already voted and runs `!rtv` again during the active vote, they receive current vote status in chat.
- Once approved, switch is scheduled with `SwitchDelaySeconds`.
- After apply, switch cooldown uses `SwitchCooldownSeconds`.
- If the timer ends before quorum, chat shows final progress and the active vote is closed.

## Menu Flow

1. Player runs `!rtv`.
2. If no vote is active, player selects a mode.
3. Player selects a map from that mode's `MapPool`.
4. Player confirms vote.
5. If a vote is already active, `!rtv` opens map selection for that active mode.
6. Plugin announces progress and either switches on quorum or expires with a final status message.

## Localization

- Supported languages: `en`, `pt-BR`, `es`, `ru`.
- Change language with `Language` in config.
- Language files under `lang/*.json` are grouped by feature (for example `menu`, `vote`, `error`) and use `snake_case` keys.

## License

MIT. See `LICENSE`.
