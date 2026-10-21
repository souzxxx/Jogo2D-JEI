# Heist

Motor do Golpe (Leonardo). O `HeistRunner` implementa `IHeistService` e é registrado pelo `HeistInstaller`, então o jogo já roda com ele no lugar do `MockHeistService`.

| Parte | Estado |
|---|---|
| Salas, especialista, tempo, loot com manhas, decisão de porta, velocidade | real (`HeistRules`) |
| Alerta (Discrição, especialista, modificador, primeiras salas, manhas, Fama), sirene, contagem, as 4 saídas e prisão | real (`HeistRules`) |
| Resultado, pagamento (Lavanderia + Fama Esquema), XP por sala com manha, prisões e liberação do próximo golpe | real (`HeistRules`) |
| Eventos e habilidades | igual ao mock, ganham as regras completas nas tasks de eventos e de habilidades |

- `Runtime/HeistRules.cs`: fórmulas (funções puras). A HUD pode usar `AlertPerSecond`, `PoliceCountdown`, `LootKeep` e `ArrestChance` pra mostrar estimativas.
- Jornal: o `HeistResult` traz `xpGains` (XP e nível de cada ladrão), `arrestedThiefIds` e `leveledUpThiefIds`.
- `Tests/EditMode`: `HeistRunnerTests` (salas, sacola, porta), `HeistAlertTests` (alerta, sirene, saídas, prisão) e `HeistFinishTests` (resultado, pagamento, XP, liberação). `Tests/PlayMode`: garante que o jogo usa o runner real.
