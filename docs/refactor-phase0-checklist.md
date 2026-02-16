# Phase 0 Regression Baseline

This checklist is the baseline to validate behavior before and after structural refactors.

## 1) Static Validation

1. Run `dotnet build ModeManager.sln`.
2. Validate `lang/en.json` with `Get-Content lang\\en.json | ConvertFrom-Json | Out-Null`.
3. Validate `lang/pt-BR.json` with `Get-Content lang\\pt-BR.json | ConvertFrom-Json | Out-Null`.
4. Validate `lang/es.json` with `Get-Content lang\\es.json | ConvertFrom-Json | Out-Null`.
5. Validate `lang/ru.json` with `Get-Content lang\\ru.json | ConvertFrom-Json | Out-Null`.

## 2) Runtime Smoke (Server)

1. Plugin load/unload works without exceptions.
2. `!nmm`, `!modes`, `!rtv`, `!mode`, `!nmm_reload` are registered and respond.
3. Dynamic commands (`!<modeKey>`) are registered for configured modes.
4. Initial mode scheduling works after first valid player joins.

## 3) RTV and Voting

1. Start a vote with `!rtv` and complete mode -> map -> confirm.
2. During active vote, `!rtv` restricts mode selection to active mode and still allows map choice.
3. Player who already voted and runs `!rtv` receives current vote status.
4. Vote expires without quorum and broadcasts final status.
5. After vote ends, opening menu and selecting with `E` works in a second vote in the same session.

## 4) Admin Mode Command

1. `!mode <key>` switches to mode with preferred map.
2. `!mode <key> <validMap>` switches using explicit map.
3. `!mode <key> <invalidMap>` reports invalid selection and available maps.
4. With active mode and no explicit map, command is rejected.
5. With active mode and explicit current map, command is rejected.
6. With active mode and explicit different valid map, command is accepted.

## 5) Reload Flow

1. `!nmm_reload` reloads config and rebuilds dynamic commands.
2. Pending switch and vote state are reset on reload.
3. Cooldown state is reset on reload.
4. Localization updates after reload when `Language` changes.
