# Crew

Crew (Leonardo). O `CrewService` implementa `ICrewService` e é registrado pelo `CrewInstaller`, então o jogo já roda com ele no lugar do `MockCrewService`.

| Parte | Estado |
|---|---|
| Atributos efetivos (manha + Ferramentas/Tênis), XP e nível até o máximo, estados, novato grátis, vagas | real |
| Recrutamento (Contatos, Fama Lenda e Reputação, troca no horário ou paga), demitir | real (`CrewRules`) |
| Prisão e fiança em tempo real (Advogado, Pão-duro, soltura no load) | real (`CrewRules`) |
| As 8 manhas: atributos, loot, loot em Pessoa, alerta, XP, fiança e eventos | real (`TraitEffectsTests`, um teste por manha) |

- No nível máximo, `XpToNextLevel` devolve 0 e o ladrão não guarda XP. A UI mostra "MÁX".
- `GetAvailable` traz só quem está Disponível. Quem está no bico também pode ir pro golpe: filtre `Crew` por status.
- Candidatos trocam sozinhos a cada 5 min de tempo real (o `Tick` avisa com `CandidatesChanged`). Recrutar os 3 não adianta a troca.
- A Fama Lenda dá níveis de graça: o preço é o do nível sorteado.
- A pena corre em tempo real, com o jogo fechado também: quem já cumpriu sai no `GameLoaded`. Preso de novo fica com a pena mais longa.
- Manha nova: o texto da ficha tem que mostrar o número real (ex.: "+20% de loot"). O `DataValidationTests` cobra.
- `Tests/EditMode`: `CrewServiceTests`, `CrewRecruitTests`, `CrewJailTests` e `TraitEffectsTests`. `Tests/PlayMode`: garante que o jogo usa a crew real.
