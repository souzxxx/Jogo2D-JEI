# Rubrica — dono e checklist pra chegar no A

Fonte: enunciado do Projeto 2D (12 rubricas, conceitos A/B/C/D).

**Meta: A+** = 7 rubricas A **incluindo as de peso 2**, no máximo 1 C, nenhum D (as descartadas não contam).

**Validado com o professor em 20/10** (registrar na planilha de acompanhamento):
- **Peso 2:** Mecânicas · Balanceamento
- **Descartadas:** Design de Som e Música · Efeitos Visuais
- **Removida pelo professor:** Level design e Progressão (não se aplica a um jogo idle)
- **Obrigatórias, não podem ser descartadas nem dobradas:** Estados e Erros · GDD

Contam nota 9 rubricas.

Toda issue do board tem a label `rubrica:*` da rubrica que ela ajuda. Filtre por label pra ver como cada rubrica está.

| Rubrica | Peso | Dono | Label |
|---|---|---|---|
| Blueprint (Conceito e Narrativa) | 1 | Nicolas | `rubrica:blueprint` |
| Mecânicas | **2** | Leonardo | `rubrica:mecanicas` |
| Feedback e Clareza de Interação | 1 | Miguel | `rubrica:feedback` |
| UI/HUD e Menus | 1 | Enzo | `rubrica:ui-menus` |
| Engajamento e Imersão | 1 | Enzo | `rubrica:engajamento` |
| Level design e Progressão | removida | — | `rubrica:level-design` |
| Design de Som e Música | descartada | Enzo | `rubrica:som` |
| Efeitos Visuais e Polimento | descartada | Miguel | `rubrica:vfx` |
| Replayability | 1 | Nicolas | `rubrica:replay` |
| Balanceamento | **2** | Nicolas | `rubrica:balanceamento` |
| Estados e Erros | 1 (obrigatória) | Leonardo | `rubrica:estados-erros` |
| GDD (DDE) | 1 (obrigatória) | Nicolas | `rubrica:gdd` |

Os prédios continuam no jogo (são o palco do golpe), mas não têm mais rubrica própria: o que importava do Level design foi pra Engajamento, UI/Menus e Balanceamento. As issues antigas com `rubrica:level-design` não contam nota.

---

## Blueprint — Conceito e Narrativa · Nicolas
**O A pede:** conceito, mundo, personagens e história com muito cuidado e criatividade, coesos e originais, com detalhes que enriquecem a experiência.
- [ ] Nova Guanabara tem identidade clara: anos 90, Cr$, orelhão, bip, gatonet, jornal.
- [ ] Dona Cida e Delegado Bastos aparecem com voz própria (bips, manchetes).
- [ ] Cada golpe tem briefing com uma piada ou detalhe de lore.
- [ ] Os 4 ladrões têm personalidade: bordões, manhas, falas nos eventos.
- [ ] Manchetes do jornal variam por resultado e citam o golpe e o valor.
- [ ] A abertura conta de onde o Chefe veio: a firma cai no Banco Central e a Dona Cida dá a segunda chance (ESCOPO §5).
- [ ] A revanche no Banco Central fecha a história: capa especial do jornal e créditos.
- [ ] O prestígio tem "história" (a firma some, Bastos é promovido).
- [ ] Tom malandro charmoso consistente em todos os textos (revisão final).

## Mecânicas · Leonardo · peso 2
**O A pede:** mecânicas polidas, coesas, com profundidade/variação, que se combinam pra criar uma jogabilidade central sólida.
- [ ] Decisão de porta com informação suficiente pra escolher (loot estimado, obstáculo, especialista).
- [ ] Especialista faz diferença clara (sem ele: mais lento e o dobro de alerta).
- [ ] Sirene + contagem + 3 tipos de fuga funcionando como no escopo.
- [ ] Cada ladrão tem papel único (passiva + ativa + eventos).
- [ ] Eventos com escolhas que usam a crew e os atributos.
- [ ] Manhas mudam o jeito de jogar.
- [ ] Bicos, upgrades e prestígio se conectam ao golpe (combinação de sistemas).
- [ ] Zero bug que trave o golpe (testes EditMode do motor).

## Feedback e Clareza de Interação · Miguel
**O A pede:** feedback visual e sonoro **integrado ao mundo** muito claro na maioria das situações, sem depender só da HUD.
- [ ] Obstáculo visível na sala (ícone) e animação do ladrão trabalhando.
- [ ] Progresso da sala no mundo (anel/barra sobre a sala).
- [ ] Alerta visível na cena: guardas "!", câmeras mudando de cor, luz de alarme ≥70%, vinheta.
- [ ] Sirene: viatura na janela, luz azul/vermelha, som.
- [ ] Loot: notas voando pra sacola e SFX.
- [ ] Habilidades com efeito visual próprio.
- [ ] Balões de bordão.

## UI/HUD e Menus · Enzo
**O A pede:** UI clara, intuitiva, responsiva, com estética coesa e assets de qualidade.
- [ ] Todas as telas do escopo (seção 7.2) existem e seguem a identidade "dossiê da firma".
- [ ] Menu principal, pausa, opções (volumes, tela cheia, apagar save) e créditos.
- [ ] Botão indisponível sempre mostra o motivo; ação irreversível pede confirmação.
- [ ] Hover, clique e transições com feedback (som + animação).
- [ ] Números de dinheiro animados; formatação Cr$ consistente.
- [ ] Funciona bem em 1280×720 e em tela cheia no navegador.
- [ ] O mapa mostra claramente o que está liberado, bloqueado e qual o próximo golpe.

## Engajamento e Imersão · Enzo
**O A pede:** experiência muito envolvente: desafio ajustado, ritmo, feedback claro e objetivos motivadores.
- [ ] Abertura de ~40 s, pulável, que prende nos primeiros segundos e já dá o objetivo do jogo.
- [ ] Tutorial da Dona Cida curto e pulável.
- [ ] Sempre tem um "próximo objetivo" visível ("Objetivo: Banco Central", próximo golpe, próximo upgrade, prestígio).
- [ ] Dicas de primeira vez nos momentos certos.
- [ ] Playtest com pelo menos 3 pessoas de fora, com anotações (Sprint 2/3).
- [ ] Música e ambiente reforçam a tensão do golpe.
- [ ] Cada golpe novo apresenta algo novo, na ordem do escopo 6.9 (Lotérica só Pessoa/Fechadura → Joalheria introduz Sistema → …).
- [ ] O Banco Central é um "final de ciclo" memorável (túnel, cofre central).

## Design de Som e Música · Enzo · descartada
- [ ] 4 faixas (menu, esconderijo, golpe com camadas, sirene).
- [ ] SFX de UI e de golpe, sem buracos nos momentos chave.
- [ ] Mixer com volumes salvos.

## Efeitos Visuais e Polimento · Miguel · descartada
- [ ] Luzes 2D (lanternas, alarme, viatura).
- [ ] Partículas (faísca, notas, fumaça, poeira do túnel).
- [ ] Pós-processamento (vinheta por alerta, bloom leve).
- [ ] Screen shake leve na sirene.

## Replayability · Nicolas
**O A pede:** muitas opções, caminhos, desafios e incentivos pra rejogar.
- [ ] Prestígio funcionando, com Nível de Procurado deixando tudo mais difícil e mais rico.
- [ ] 6 bônus de Fama que mudam a estratégia.
- [ ] Modificadores de golpe sorteados a cada visita ao mapa.
- [ ] Recrutas aleatórios com manhas diferentes (crews diferentes a cada ciclo).
- [ ] Eventos variados (≥15) com escolhas diferentes conforme a crew.

## Balanceamento · Nicolas · peso 2
**O A pede:** dificuldade bem ajustada, sistemas justos e interligados, desafiador na medida e recompensador.
- [ ] Planilha de balanceamento com as fórmulas reais (loot, custos, tempos, alerta).
- [ ] Metas de tempo batidas em playtest cronometrado (Lotérica ~1 min, Banco Central 40–60 min).
- [ ] Nenhum upgrade inútil e nenhum obviamente roubado.
- [ ] Ficar sem especialista dói, mas não impossibilita.
- [ ] Prisão e fiança punem sem frustrar.
- [ ] Registro dos ajustes feitos (o que mudou e por quê) no BALANCEAMENTO.md.

## Estados e Erros · Leonardo · obrigatória
**O A pede:** jogo muito robusto, sem dead ends, falha tratada de forma clara e justa, erros raros.
- [ ] Save automático + backup + recuperação de save corrompido (testado).
- [ ] Golpe interrompido recuperado ao abrir o jogo.
- [ ] Anti dead-end: catar latinha, novato grátis da Dona Cida, prisão que acaba sozinha, Lotérica sempre aberta.
- [ ] Nenhuma ação leva a erro: botões desabilitados com motivo.
- [ ] Pausa segura em qualquer momento.
- [ ] Bug bash final com checklist de cenários estranhos (aba em segundo plano, fechar no meio, sem dinheiro, crew toda presa).

## GDD (DDE) · Nicolas · obrigatória
**O A pede:** GDD bem escrito, completo, organizado, atualizado, refletindo o produto final e útil durante o desenvolvimento.
- [ ] Board no Milanote com a estrutura Design (Blueprint, Mechanics, Interface) → Dynamics → Experience, a partir do `docs/GDD.md`.
- [ ] Cada dono atualiza a sua seção no fim de cada sprint (toda quinta).
- [ ] Registro de mudanças com data (documento vivo).
- [ ] Imagens: mockups, prints, diagrama do loop.
- [ ] Revisão final batendo com o jogo entregue.
