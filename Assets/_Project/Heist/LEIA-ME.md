# Heist

Motor do Golpe (Leonardo). O `HeistRunner` implementa `IHeistService` e é registrado pelo `HeistInstaller`, então o jogo já roda com ele no lugar do `MockHeistService`.

| Parte | Estado |
|---|---|
| Salas, especialista, tempo, loot com manhas, decisão de porta, velocidade | real (`HeistRules`) |
| Alerta (Discrição, especialista, modificador, primeiras salas, manhas, Fama), sirene, contagem, as 4 saídas e prisão | real (`HeistRules`) |
| Pagamento e resultado | igual ao mock, ganha as regras completas na task de fim do golpe |
| Eventos e habilidades | igual ao mock, ganham as regras completas nas tasks de eventos e de habilidades |

- `Runtime/HeistRules.cs`: fórmulas (funções puras). A HUD pode usar `AlertPerSecond`, `PoliceCountdown`, `LootKeep` e `ArrestChance` pra mostrar estimativas.
- `Tests/EditMode`: `HeistRunnerTests` (salas, sacola, porta) e `HeistAlertTests` (alerta, sirene, saídas, prisão). `Tests/PlayMode`: garante que o jogo usa o runner real.
