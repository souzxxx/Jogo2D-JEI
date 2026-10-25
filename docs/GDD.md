# A Firma — Game Design Document (DDE)

> **Este arquivo é o rascunho do GDD oficial, que vive no Milanote** (link: _colar aqui_).
> Estrutura no framework **DDE: Design → Dynamics → Experience**. As rubricas Blueprint, Mechanics e Interface vêm daí.
> Cada seção tem um dono. Toda quinta o dono atualiza a sua seção aqui e no Milanote, e registra a mudança no fim do documento.

| Seção | Dono |
|---|---|
| 0. Ficha técnica | Nicolas |
| 1.1 Blueprint | Nicolas |
| 1.2 Mechanics | Leonardo (golpe e crew) + Nicolas (economia e progressão) |
| 1.3 Interface | Enzo (UI e áudio) + Miguel (feedback no mundo) |
| 2. Dynamics | Leonardo + Nicolas |
| 3. Experience | Enzo |
| 4. Arte e Som | Miguel + Enzo |
| 5. Produção | Leonardo |

---

## 0. Ficha técnica

| | |
|---|---|
| **Título** | A Firma |
| **Gênero** | Idle / gerenciamento com decisões de risco (push-your-luck) |
| **Plataforma** | Navegador (WebGL, itch.io) |
| **Engine** | Unity 6000.6.4f1, 2D URP |
| **Idioma** | PT-BR |
| **Público** | Quem curte idle, gerenciamento e humor brasileiro; sessões de 10–60 min |
| **Controles** | Só mouse (clique). Esc pausa. |
| **Equipe** | Leonardo, Nicolas, Enzo, Miguel |
| **Referências** | Payday (decisão de assalto), Fallout Shelter (prédio em corte), Taskbar Hero / idles (progressão), Diamant (push-your-luck) |

**Pitch:** Nova Guanabara, anos 90. Você é o cérebro de uma firma de malandros. Monta a crew, escolhe o alvo e assiste ao golpe acontecer sala por sala — mas entre uma porta e outra a decisão é sua: **abrir mais uma ou meter o pé com o que já pegou?**

---

## 1. DESIGN

### 1.1 Blueprint — conceito, mundo e narrativa

**Conceito central.** Um idle em que o "automático" tem tensão: a crew trabalha sozinha, mas cada sala nova é uma aposta. A graça não é clicar rápido, é decidir na hora certa.

**Mundo: Nova Guanabara.** Cidade fictícia que mistura São Paulo e Rio no começo dos anos 90 (por volta de 1992): orelhão, bip, fita K7, lotação, gatonet, jogo do bicho, inflação. Moeda: Cruzeiro (Cr$). Ninguém é vilão de verdade, todo mundo é meio malandro. A firma mora na **Zona Norte** (oficina da Dona Cida, lotérica, cassino debaixo da pastelaria); o **Centro** tem joalheria e câmbio; na **Avenida do Porto** ficam o Banco Central e a polícia.

**Personagens.**
- **O Chefe (jogador):** nunca aparece; é "o cérebro". Todo mundo chama de "chefia", sem gênero definido.
- **Dona Cida:** uns 70 anos, lenda aposentada do crime ("nunca deixou pista, só saudade"), dona do esconderijo (a "Auto Elétrica da Cida", oficina de fachada). Passa a firma pro jogador, conduz o tutorial e manda recados por bip. Fala mansa, conselho afiado, deboche com carinho.
- **Delegado Bastos:** antagonista de bigode que adora uma câmera. Promete muito e pega pouco: é piada, não ameaça. Aparece nas manchetes e é promovido a cada prestígio.
- **A crew:** ladrões gerados com nome + apelido de classe ("Juninho Disquete", "Tião Pé-na-Tábua"), uma manha de personalidade e bordões.

**Os 4 tipos de ladrão.**
| Tipo | Papel | Personalidade |
|---|---|---|
| Pé-na-Tábua | motorista, cuida da fuga | Ex-motorista de lotação pirata, conhece a cidade pelos buracos da rua. Fala como quem tá dirigindo ("segura", "retrovisor limpo"), nunca desliga o motor e ninguém mexe no rádio dele. Medo: lombada eletrônica. |
| Gazua | arrombador, fechaduras e cofres | _dono escreve_ |
| Disquete | hacker, câmeras e alarmes | _dono escreve_ |
| 171 | vigarista, pessoas e lábia | _dono escreve_ |

**Como a história é contada.** Em pílulas: bips da Dona Cida nos marcos, briefing de 2–3 linhas por golpe, capa do jornal *O Guanabarense* no fim de todo golpe, bordões da crew durante o assalto.

**Arco.** Dona Cida passa a firma → golpes crescem (Lotérica → Joalheria → Câmbio → Cassino) → **Banco Central via túnel**, o golpe do século → **sumir do mapa** (prestígio): a firma desaparece, Bastos jura vingança, a cidade recomeça mais vigiada.

**Tom.** Malandro charmoso: leve, bem-humorado, sem violência explícita. A saída é sempre a lábia ou o improviso, nunca machucar alguém; nada de ofensa, coisa real ou anacronismo. Guia completo, com gírias, tamanhos de texto e exemplos bons e ruins: `Assets/_Project/Narrative/LEIA-ME.md`.

### 1.2 Mechanics — regras e sistemas

**Loop principal**
```
Esconderijo ─▶ Mapa ─▶ Planejamento ─▶ GOLPE ─▶ Jornal ─▶ Esconderijo
   ▲  (upgrades, recrutar, bicos)                              │
   └──────────────────────── Cr$, XP, liberações ◀─────────────┘
```

**Golpe.**
- Sequência de 3 a 7 salas; a última é o cofre. Cada sala tem um obstáculo (Fechadura, Sistema ou Pessoa), uma dificuldade, um loot e uma taxa de alerta.
- **Limpar a sala (automático):** o poder é a Perícia do melhor especialista do obstáculo; sem especialista, 35% da maior Perícia da crew. Tempo = dificuldade × modificador ÷ poder (nunca menos de 0,5 s).
- **Loot:** loot da sala × modificador × manhas (Fominha +20%, Mão-boba +30% em sala de Pessoa) entra na sacola, e todo mundo ganha XP.
- **Alerta por segundo:** taxa da sala × (1 − 0,5 × Discrição média/100) × 2 sem especialista × modificador × manhas (Boca-aberta +20%) × (1 − Fama Sangue-frio%). Não sobe com a crew parada na porta nem durante um evento.
- **Porta:** entre salas o golpe pausa e mostra a próxima (obstáculo, loot estimado, se tem especialista): **próxima sala** ou **meter o pé**.
- **Eventos:** 30% por sala (Pé-frio +50%), filtrados por golpe e tipo de sala e sorteados por peso. Cada um tem 2 ou 3 escolhas: algumas pedem um tipo de ladrão (indisponível mostra o motivo), outras testam um atributo (chance = base + por ponto × melhor atributo da crew). Efeitos: alerta, % da sacola, atraso ou prender alguém (vai em cana no fim do golpe). Todo evento tem pelo menos uma escolha sem requisito; se mesmo assim a crew não tiver nenhuma escolha possível, o evento não abre. O primeiro golpe tem o evento roteirizado do Seu Zé. Hoje são 13 eventos, 3 deles do Pé-na-Tábua.
- **Habilidades (uma vez por golpe; o botão diz o motivo quando não dá):**

| Ladrão | Habilidade | Efeito |
|---|---|---|
| Pé-na-Tábua | Cavalo de pau | Fuga perfeita a qualquer hora, até com a polícia chegando: 100% da sacola e ninguém preso (nem quem um evento ia prender) |
| Gazua | Pé-de-cabra | Limpa na hora a sala de Fechadura, com +15 de alerta |
| Disquete | Loop na câmera | Congela o alerta por 15 s |
| 171 | Lábia | −25 de alerta (não dá pra usar com o alerta zerado) |

- **Alerta 100 = sirene:** começa a contagem da polícia: 10 s + Rádio da polícia + Pé-na-Tábua (+4 s) + modificador (Blitz −3 s), nunca menos de 3 s. A contagem corre com a crew trabalhando e parada na porta, e pausa em evento e na pausa do jogo. Saídas possíveis:

| Quando sai | Sacola | Prisão |
|---|---|---|
| Antes da sirene | 100% | ninguém |
| Durante a contagem (inclusive limpando o cofre) | 80% (+10% com Pé-na-Tábua) | ninguém |
| Contagem zerou | 50% (+10% com Pé-na-Tábua) | chance por ladrão: (40% − 15 pontos com Pé-na-Tábua) × (1 − 0,5 × Discrição dele/100) |
| Cavalo de pau (a qualquer hora) | 100% | ninguém |

- **Fim do golpe:** paga sacola × saída × (1 + Lavanderia%) × (1 + Fama Esquema%), uma vez só. Limpar o cofre libera o próximo golpe. Presos vão em cana pelo tempo do golpe e o resto volta disponível. O jornal recebe resultado, XP e nível de cada ladrão e quem subiu de nível.

**Crew.**
- **Ladrão:** tipo, nível 1–10, Perícia, Discrição, Nervos, 1 manha, estado (disponível, no golpe, no bico, preso).
- **Atributos efetivos:** base × manha × (1 + Ferramentas% na Perícia, Tênis de molinha% na Discrição). Nenhum upgrade mexe em Nervos.
- **XP e nível:** 1 XP por sala limpa (Cria da rua +30%, a fração vai somando sala a sala). O próximo nível custa 5 × nível atual e cada nível soma o ganho do tipo de ladrão. No nível 10 para e não guarda XP.
- **Manhas (8):** Fominha (+20% de loot, −15% de Discrição), Pé-frio (+50% de chance de evento), Sangue-frio (+30% de Nervos), Boca-aberta (+20% de alerta), Pão-duro (−40% na fiança), Cria da rua (+30% de XP), Supersticioso (+20% de Nervos, −10% de Perícia), Mão-boba (+30% de loot em sala de Pessoa).
- **Recrutamento:** 3 candidatos que trocam a cada 5 min de tempo real, ou na hora por Cr$ 25 (−10% por nível de Contatos). Nível sorteado de 1 a 1 + Contatos, mais os níveis da Fama Lenda de graça. Custo = Cr$ 50 × 1,8^(nível sorteado − 1) × (1 − Fama Reputação%).
- **Vagas:** a crew começa com 3 (até 8 com Quartos); o golpe começa com 2 (até 4 com Van). Demitir nunca tira o último ladrão nem quem está no golpe.
- **Prisão:** tempo do golpe × (1 − Advogado%), em tempo real (corre com o jogo fechado). Fiança = segundos restantes × Cr$ 2 × manha (Pão-duro) × (1 − Advogado%). Preso não vai pra golpe nem pra bico; preso de novo fica com a pena mais longa.

**Economia.**
- **Cr$** vem dos golpes e dos bicos.
- **Bicos:** ladrões ociosos geram renda passiva, em dobro no bico do próprio tipo. Catar latinha rende sempre, sem ladrão.
- **Ganho offline:** até 4 h, a 50%.
- **Upgrades:** 9 upgrades do esconderijo com níveis.

**Progressão.**
- **Golpes:** escada de 5. Concluir o cofre de um libera o próximo.
- **Modificadores:** sorteados a cada visita ao mapa (feriado, dia de pagamento…).
- **Prestígio:** depois do Banco Central, "sumir do mapa" reseta o ciclo, dá Fama e +1 Nível de Procurado (tudo mais difícil e mais rico). A Fama compra 6 bônus permanentes.

**Estados e erros.**
- Save automático com backup.
- Golpe interrompido vira "meteu o pé": paga a sacola salva, a crew volta e ninguém é preso.
- No load: quem cumpriu a pena com o jogo fechado sai; ladrão "no golpe" ou "no bico" sem golpe ou bico de verdade volta pra disponível.
- Anti dead-end: catar latinha; se ninguém está fora da cadeia e não dá pra recrutar (sem Cr$ ou sem vaga), a Dona Cida manda um novato com um bip; a prisão acaba sozinha; a Lotérica nunca fica bloqueada.

_Números atuais: ver [ESCOPO.md](ESCOPO.md) e [BALANCEAMENTO.md](BALANCEAMENTO.md)._

### 1.3 Interface — UI, feedback e controles

**Identidade:** "dossiê da firma": papel kraft, carimbo CONFIDENCIAL, fotos 3x4 dos ladrões, bip pras notificações, jornal no resultado.

**Telas:**
- Menu principal, Pausa, Opções, Créditos
- Esconderijo (hub) com Crew, Contatos, Bicos, Upgrades e Prestígio
- Mapa da cidade e Planejamento
- HUD do golpe, popup de porta, cartão de evento, contagem da polícia
- Jornal, popup offline, bip e tutorial

**Feedback no mundo** (a cena conta o que está acontecendo):
- **Salas:** ícone do obstáculo sobre a sala, ladrão animado trabalhando e anel de progresso.
- **Loot e falas:** notas voando pra sacola e balões de bordão.
- **Vigilância:** guardas com lanterna que viram "!" e câmeras com cone que muda de cor.
- **Alerta:** luz de alarme a partir de 70%, viatura na janela na sirene e vinheta que fecha conforme o alerta.

**Regras de UX:** botão indisponível sempre explica o motivo; ação irreversível pede confirmação; dinheiro animado.

**Áudio como interface:** a música do golpe ganha camadas com o alerta; cada ação importante tem som.

_Mockups das telas: colar aqui e no Milanote._

---

## 2. DYNAMICS — o que acontece quando os sistemas se encontram

- **Ganância vs. segurança (núcleo).** Cada porta aberta aumenta o loot e o risco. O jogador aprende a ler alerta, obstáculo e crew pra saber a hora de parar. A sirene não é game over, é um segundo nível de aposta (seguir trabalhando com a polícia chegando).
- **Montar a crew é a estratégia.** Golpes misturam obstáculos e as vagas são poucas. Levar o especialista certo, o motorista como seguro ou o 171 pra apagar incêndio são escolhas que mudam o resultado.
- **Recursos que conversam.** Ladrão no bico rende pouco e seguro; no golpe rende muito e arrisca prisão. A fiança tira dinheiro dos upgrades. Upgrades de Perícia e Discrição mudam até onde dá pra ir em cada golpe.
- **Modificadores quebram a rotina.** "Dia de pagamento" convida ao risco; "Blitz" pede cautela. Repetir o mesmo golpe nunca é exatamente igual.
- **Prestígio reescreve a curva.** Com Fama o começo fica rápido e o topo mais difícil (Procurado). Cada ciclo pede uma estratégia um pouco diferente.
- **Estratégias emergentes esperadas:** farm seguro de golpe baixo, "uma porta a mais" no limite da sirene, crew especializada vs. generalista, guardar o Cavalo de pau pro cofre.

_Dinâmicas observadas em playtest: registrar aqui (Sprint 2–3)._

---

## 3. EXPERIENCE — o que o jogador deve sentir

| Momento | Sentimento alvo | O que causa |
|---|---|---|
| Primeiros 3 min | Curiosidade, "entendi!" | Tutorial curto da Dona Cida, Lotérica fácil |
| Cada porta | Tensão, ganância | Alerta subindo, loot estimado, música crescendo |
| Sirene | Adrenalina | Luzes, viatura, contagem, decisão sob pressão |
| Jornal | Orgulho ou riso | Manchete sobre o que você fez |
| Esconderijo | Planejamento, progresso | Upgrades, recrutas, próximo golpe à vista |
| Banco Central | Clímax | Golpe longo, túnel, cofre central |
| Prestígio | Recomeço com poder | Fama, cidade nova mais difícil, Bastos promovido |

**Público e sessão:** quem joga no navegador nos intervalos; sessão típica de 10–20 min, primeiro ciclo completo em 40–60 min.

**Anti-experiências (o que evitar):** sentir que o jogo joga sozinho; ficar travado sem dinheiro; tédio de repetir o mesmo golpe sem motivo.

---

## 4. Arte e Som

- **Arte:** pixel art 32×32 (PPU 32). Cenário e UI com packs do craftpix.net (acesso da faculdade) recoloridos numa paleta única. Os 4 ladrões feitos pelo time. Prédios em corte lateral com salas modulares. _Paleta e referências visuais: colar aqui._
- **VFX:** luzes 2D (lanternas, alarme, viatura), partículas (faísca, notas, fumaça, poeira), vinheta por alerta, screen shake leve.
- **Música:** esconderijo (samba-jazz/bossa lo-fi), golpe com camadas de tensão, sirene, menu.
- **SFX:** UI, ações dos ladrões, loot, alarme, sirene, bip, máquina de escrever do jornal.

Detalhes e lista de packs: [ASSETS.md](ASSETS.md).

---

## 5. Produção

- **Equipe e frentes:** ver [ESCOPO.md §13](ESCOPO.md#13-divisão-do-time).
- **Cronograma:**
  - S0 Fundação (20–22/10)
  - S1 Primeiro golpe (23–29/10)
  - S2 Jogo completo (30/10–05/11, feature freeze)
  - S3 Polimento e entrega (06–11/11)
- **Ferramentas:**
  - Unity 6
  - GitHub (código, board, issues)
  - Milanote (GDD)
  - Google Sheets (balanceamento)
  - craftpix.net
  - Aseprite/Blender (arte, IA permitida)
- **Board:** <https://github.com/users/souzxxx/projects/2>

---

## Registro de mudanças (documento vivo)

| Data | Seção | Mudança | Por quê |
|---|---|---|---|
| 20/10/2026 | Todas | Primeira versão do GDD a partir do escopo | Início do projeto |
| 20/10/2026 | Mechanics | Fuga durante a sirene passa a custar 20% da sacola | Dar função ao Cavalo de pau e manter a sirene como aposta |
| 20/10/2026 | Blueprint | Tipos renomeados pra Pé-na-Tábua, Gazua, Disquete e 171; Atirador vira 171 (vigarista) | Tom malandro charmoso, sem violência |
| 21/10/2026 | Mechanics | Golpe e crew descritos com as regras implementadas: alerta, sirene, saídas, prisão, eventos, habilidades, XP, manhas, recrutamento e fiança | Fim da Sprint 1: motor e crew reais no jogo |
| 21/10/2026 | Mechanics | A Discrição de cada ladrão reduz a chance dele ser preso na fuga forçada | O ESCOPO já pedia; dá peso à Discrição fora do alerta |
| 21/10/2026 | Mechanics | Fama Lenda dá níveis de graça no recruta (o preço é o do nível sorteado); trocar candidatos fica mais barato com Contatos | Bônus de Fama que não se anula; Contatos com o efeito "troca mais barata" do ESCOPO |
| 21/10/2026 | Mechanics | Lábia não pode ser usada com o alerta zerado; evento sem nenhuma escolha possível não abre | Habilidade não é gasta à toa; golpe nunca trava |
| 21/10/2026 | Blueprint | Personalidade do Pé-na-Tábua | Dono do ladrão escreveu |
| 25/10/2026 | Blueprint | Bíblia do mundo (época, lugares, Dona Cida, Bastos, jeito de falar de cada tipo) e guia de tom | Todo mundo escreve texto: o guia deixa tudo coeso |
