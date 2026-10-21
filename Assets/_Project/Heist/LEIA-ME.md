# Heist

Motor do Golpe (Leonardo). O `HeistRunner` implementa `IHeistService` e é registrado pelo `HeistInstaller`, então o jogo já roda com ele no lugar do `MockHeistService`.

| Parte | Estado |
|---|---|
| Salas, especialista, tempo, loot com manhas, decisão de porta, velocidade | real (`HeistRules`) |
| Alerta (Discrição, especialista, modificador, primeiras salas, manhas, Fama), sirene, contagem, as 4 saídas e prisão | real (`HeistRules`) |
| Resultado, pagamento (Lavanderia + Fama Esquema), XP por sala com manha, prisões e liberação do próximo golpe | real (`HeistRules`) |
| Eventos: chance por sala (Pé-frio), filtros, peso, tutorial, requisitos, chance por atributo e efeitos | real (`EventRules`) |
| Habilidades | igual ao mock, ganham as regras completas na task de habilidades |

- `Runtime/HeistRules.cs`: fórmulas (funções puras). A HUD pode usar `AlertPerSecond`, `PoliceCountdown`, `LootKeep` e `ArrestChance` pra mostrar estimativas.
- Evento novo: crie um `HeistEventDef`, coloque no `GameDatabase` e garanta uma escolha sem requisito (o `DataValidationTests` cobra). Se a crew não tiver nenhuma escolha possível, o evento não abre.
- Preso por evento vai em cana no fim do golpe (o Cavalo de pau salva).
- Jornal: o `HeistResult` traz `xpGains` (XP e nível de cada ladrão), `arrestedThiefIds` e `leveledUpThiefIds`.
- `Tests/EditMode`: `HeistRunnerTests` (salas, sacola, porta), `HeistAlertTests` (alerta, sirene, saídas, prisão), `HeistFinishTests` (resultado, pagamento, XP, liberação) e `HeistEventTests` (sorteio, escolhas, efeitos). `Tests/PlayMode`: garante que o jogo usa o runner real.
