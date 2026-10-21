# Crew

Crew (Leonardo). O `CrewService` implementa `ICrewService` e é registrado pelo `CrewInstaller`, então o jogo já roda com ele no lugar do `MockCrewService`.

| Parte | Estado |
|---|---|
| Atributos efetivos (manha + Ferramentas/Tênis), XP e nível até o máximo, estados, novato grátis, vagas | real |
| Recrutamento (Contatos), demitir | igual ao mock, ganha as regras completas na task de recrutamento |
| Prisão e fiança | igual ao mock, ganha as regras completas na task de prisão |
| Efeitos das 8 manhas fora de atributo/loot/alerta/XP | na task das manhas |

- No nível máximo, `XpToNextLevel` devolve 0 e o ladrão não guarda XP. A UI mostra "MÁX".
- `GetAvailable` traz só quem está Disponível. Quem está no bico também pode ir pro golpe: filtre `Crew` por status.
- `Tests/EditMode`: `CrewServiceTests`. `Tests/PlayMode`: garante que o jogo usa a crew real.
