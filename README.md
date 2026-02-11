# ModeManager

Mode manager para servidores CS2 com CounterStrikeSharp, com troca de modo por votacao, execucao segura com delay/cooldown, comandos dinamicos e reload de configuracao em runtime.

## Recursos

- Votacao de troca de modo com quorum proporcional (`VoteRatio`), tempo de expiracao e minimo de jogadores.
- Um voto por jogador (SteamID, com fallback para UserId).
- Cooldown entre trocas para evitar spam.
- Aplicacao de modo previsivel: `ResetCommand`, unload/load de plugins por modo, `ExecCommand`, `game_type`/`game_mode` (opcional) e `changelevel` com mapa definido no modo, mapa atual ou `de_dust2`.
- Comandos dinamicos gerados a partir das chaves em `Modes` (`css_<modeKey>` sanitizado).
- Modo inicial automatico no startup quando o primeiro jogador humano valido entra.
- Reload de config em runtime (`css_mm_reload`) com validacao e rebuild dos comandos dinamicos.
- Localizacao com catalogos `en` e `pt-BR` (fallback seguro).

## Requisitos

- .NET 8 SDK (para compilar localmente)
- CounterStrikeSharp API compativel (`CounterStrikeSharp.API` 1.0.284 no projeto)
- Servidor CS2 com CounterStrikeSharp instalado

## Build

```powershell
dotnet restore
dotnet build ModeManager.sln -c Release
```

Arquivos principais gerados:

- `bin/Release/net8.0/ModeManager.dll`
- `bin/Release/net8.0/ModeManager.deps.json`
- `bin/Release/net8.0/lang/en.json`
- `bin/Release/net8.0/lang/pt-BR.json`

## Instalacao

- Copie os binarios para a pasta de plugins do CounterStrikeSharp no servidor.
- Mantenha a pasta `lang` junto ao plugin para carregar mensagens externas.

## Configuracao

Caminho unico suportado:

- `addons/counterstrikesharp/configs/plugins/ModeManager/ModeManager.json`
- Qualquer outro caminho de JSON nao e considerado no reload de configuracao.

Exemplo de configuracao:

```json
{
  "Language": "pt-BR",
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

## Comandos

- `css_mm` (`!mm`): ajuda geral.
- `css_modes` (`!modes`): lista os modos disponiveis.
- `css_setmode <key>` (`!setmode <key>`): inicia ou participa da votacao.
- `css_mm_vote` (`!mm_vote`): mostra status da votacao/troca pendente.
- `css_mm_reload` (`!mm_reload`): recarrega config do disco e reconstrui comandos dinamicos (somente admin com `@css/root` ou console).
- `css_<key>` (`!<key>`): atalho dinamico para votar diretamente em um modo.

## Fluxo de Votacao

- Console do servidor agenda troca sem passar por votacao.
- Jogadores HLTV/bot nao contam para voto.
- Votos necessarios = `ceil(jogadores_elegiveis * VoteRatio)`.
- A votacao expira em `VoteDurationSeconds`.
- Ao aprovar, a troca respeita `SwitchDelaySeconds`.
- Depois de aplicar, o plugin ativa `SwitchCooldownSeconds`.

## Reload e Operacao

- `css_mm_reload` tenta recarregar o JSON do disco.
- Se o arquivo nao for encontrado, o plugin continua com a config em memoria.
- Ao recarregar, votos pendentes/cooldown sao limpos.
- Comandos dinamicos sao sempre reconstruidos apos reload.

## Estrutura

- `ModeManagerPlugin.cs`: metadados e lifecycle.
- `Features/Commands`: comandos base e dinamicos.
- `Features/Voting`: sessao e regras de voto.
- `Features/Switching`: agendamento e aplicacao de modo.
- `Features/Configuration`: modelo, validacao, descoberta e reload.
- `Features/Startup`: modo inicial no primeiro jogador valido.
- `Features/Localization` e `lang/*.json`: mensagens localizadas.
- `Shared/`: logging, resolver de mapa e sanitizacao de comandos.

## Licenca

Este projeto esta licenciado sob a MIT License. Veja `LICENSE`.
