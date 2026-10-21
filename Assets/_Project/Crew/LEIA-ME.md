# Crew

Crew (Leonardo). O `CrewService` implementa `ICrewService` e é registrado pelo `CrewInstaller`, então o jogo já roda com ele no lugar do `MockCrewService`.

| Parte | Estado |
|---|---|
| Atributos efetivos (manha + Ferramentas/Tênis), XP e nível até o máximo, estados, novato grátis, vagas | real |
| Recrutamento (Contatos, Fama Lenda e Reputação, troca no horário ou paga), demitir | real (`CrewRules`) |
| Prisão e fiança | igual ao mock, ganha as regras completas na task de prisão |
| Efeitos das 8 manhas fora de atributo/loot/alerta/XP | na task das manhas |

- No nível máximo, `XpToNextLevel` devolve 0 e o ladrão não guarda XP. A UI mostra "MÁX".
- `GetAvailable` traz só quem está Disponível. Quem está no bico também pode ir pro golpe: filtre `Crew` por status.
- Candidatos trocam sozinhos a cada 5 min de tempo real (o `Tick` avisa com `CandidatesChanged`). Recrutar os 3 não adianta a troca.
- A Fama Lenda dá níveis de graça: o preço é o do nível sorteado.
- `Tests/EditMode`: `CrewServiceTests` e `CrewRecruitTests`. `Tests/PlayMode`: garante que o jogo usa a crew real.
