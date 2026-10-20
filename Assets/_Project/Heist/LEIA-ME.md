# Heist

Motor do Golpe (Leonardo). O `HeistRunner` implementa `IHeistService` e é registrado pelo `HeistInstaller`, então o jogo já roda com ele no lugar do `MockHeistService`.

| Parte | Estado |
|---|---|
| Salas, especialista, tempo, loot com manhas, decisão de porta, velocidade | real (`HeistRules`) |
| Alerta, sirene, saídas e pagamento | igual ao mock, ganha as regras completas na task de sirene e na de fim do golpe |
| Eventos e habilidades | igual ao mock, ganham as regras completas nas tasks de eventos e de habilidades |

- `Runtime/HeistRules.cs`: fórmulas da sala (funções puras, a HUD pode usar pra estimativas).
- `Tests/EditMode`: testes do runner. `Tests/PlayMode`: garante que o jogo usa o runner real.
