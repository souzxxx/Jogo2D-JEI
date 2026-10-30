# A Firma — Documento de Escopo

> Projeto 2D · Jogos Digitais e Interação · Insper
> Equipe: **Leonardo**, **Nicolas**, **Enzo**, **Miguel**
> Entrega: **11/11/2026 às 23:50** — build WebGL na jam do itch.io + GitHub + GDD no Milanote + Blackboard

Este documento é a fonte da verdade do escopo. O GDD (Milanote) é escrito a partir dele e os dois andam juntos: mudou aqui, muda lá.

---

## 1. Pitch

**Nova Guanabara, anos 90.** Você é o cérebro de uma firma de malandros. Monta a crew, escolhe o alvo e manda o bando pro golpe. O assalto acontece sozinho na sua frente, sala por sala, num corte lateral do prédio — mas entre uma sala e outra a decisão é sua: **abrir mais uma porta ou meter o pé com o que já pegou?**

**O jogo abre com a firma no auge, tentando o Banco Central, e dando tudo errado:** a sirene toca, a crew insiste em "só mais uma porta" e o Delegado Bastos pega todo mundo. Você sai sem nada, a Dona Cida te empresta a oficina e dois novatos, e o objetivo fica claro desde o primeiro minuto: **voltar pro Banco Central e fazer direito.**

O loot volta pro esconderijo, vira upgrade, recruta gente melhor e libera golpes maiores, da Lotérica do Seu Zé até o Banco Central. E a firma não para quando você não está olhando: quem está no bico aparece trabalhando, e os golpes que você já domina viram rotina de uma crew **no automático**, rodando em segundo plano enquanto você planeja o próximo. Fez o Banco Central? É a revanche: a Firma some do mapa, a cidade recomeça mais difícil e a sua fama fica.

**Gênero:** idle / gerenciamento com decisões de risco (push-your-luck)
**Plataforma:** WebGL (itch.io) · **Engine:** Unity 6000.6.4f1, template 2D (URP) · **Idioma:** PT-BR
**Referências:** Payday (decisões de assalto), Fallout Shelter (prédio em corte), Taskbar Hero / idles em geral (progressão), Diamant / Blackjack (push-your-luck)

---

## 2. O que é sucesso

- **Nota A+** na rubrica: 7 rubricas A incluindo as duas de peso 2, no máximo 1 C, nenhum D.
- Quem abre o jogo entende em 1 minuto o que fazer, sente a tensão de decidir "mais uma sala?" já no primeiro golpe, e quer voltar pra comprar o próximo upgrade.
- Cada pessoa do time entrega a sua frente sem travar ninguém desde o primeiro dia.

### Objetivos
- Loop completo e redondo: esconderijo → mapa → planejamento → golpe → resultado → esconderijo.
- Decisão de risco real em todo golpe (portas, eventos, sirene).
- 4 tipos de ladrão com papel claro e diferente.
- Objetivo claro desde o primeiro minuto: a abertura mostra a firma caindo no Banco Central, e o jogo inteiro é a volta por cima.
- Progressão infinita com prestígio, sempre com um próximo objetivo visível.
- Cara de idle que se vê: a firma trabalha sozinha na tela (cada bico tem a sua cena, golpes no automático rodando no esconderijo), mas golpe novo e o cofre que libera o próximo continuam sendo decisão do jogador.
- Mundo com identidade própria (cidade, personagens, humor brasileiro).

### Não-objetivos
- Controle direto de personagem, física, combate em tempo real.
- Multiplayer, contas, ranking online.
- Tradução para outros idiomas.
- Monetização, anúncios.

### Anti-objetivos (se acontecer, falhamos mesmo que "funcione")
- O jogo joga sozinho e o jogador não sente que decide nada.
- Idle sem objetivo: o jogador não sabe o que está buscando.
- O jogador fica preso sem dinheiro, sem crew e sem saída (dead end).
- Violência explícita ou tema que possa ser lido como ofensivo — o tom é malandro charmoso, não crime pesado.
- Uma parte do time esperando outra pra conseguir trabalhar.

---

## 3. Restrições

| Restrição | Consequência |
|---|---|
| Entrega 11/11 23:50, atraso tem punição por dia | Feature freeze em 05/11; build final até 10/11 |
| Build **WebGL** no itch.io | Sem threads, sem `System.IO` pra save (usar `PlayerPrefs`), sem botão "Sair" no WebGL, cuidado com tamanho e compressão |
| GDD obrigatório no **Milanote**, estruturado em **DDE** (Design · Dynamics · Experience) | `docs/GDD.md` é o rascunho que vai pro Milanote, seção por seção |
| Créditos de todo asset de terceiros no README | Cada pessoa registra o crédito **no mesmo PR** em que adiciona o asset |
| Assets do **craftpix.net** via acesso da faculdade | Fonte principal de arte; cada pack usado é creditado (nome + link) |
| Plágio: código de terceiros com mais de 2–3 linhas precisa de referência | Comentário com link no código + linha no README |
| Todos usam a **mesma versão** do Unity: `6000.6.4f1` | Versão diferente quebra o projeto pros outros |
| IA liberada como ferramenta de produção de arte (ex.: modelar no Blender e renderizar como pixel art, gerar sprite base) | Toda ferramenta de IA usada vai nos créditos |

---

## 4. Rubricas — estratégia

O enunciado avalia **12 rubricas**: Blueprint (Conceito e Narrativa) · Mecânicas · Feedback e Clareza de Interação · UI/HUD e Menus · Engajamento e Imersão · Level design e Progressão · Design de Som e Música · Efeitos Visuais e Polimento Gráfico · Replayability · Balanceamento · Gerenciamento de Estados e Erros · GDD (DDE). Conceitos: A (8,5–10), B (6–8,5), C (4–6), D (0–4).

Validado com o professor em 20/10:

- **Peso 2:** Mecânicas · Balanceamento (sistemas, onde o time é forte)
- **Descartadas:** Design de Som e Música · Efeitos Visuais (o time ainda faz som e VFX, mas sem risco na nota)
- **Removida pelo professor:** Level design e Progressão, por ser um jogo idle. Os prédios continuam no jogo como palco do golpe.
- **Obrigatórias (não podem ser descartadas nem dobradas):** Gerenciamento de Estados e Erros · GDD

Cada rubrica tem um dono e um checklist para chegar no A em [`RUBRICA.md`](RUBRICA.md).

---

## 5. Mundo e narrativa (Blueprint)

### Nova Guanabara
Cidade fictícia que mistura São Paulo e Rio nos anos 90: orelhão, bip, fita K7, lotação, gatonet, jogo do bicho, inflação. A moeda é o **Cruzeiro (Cr$)**. O humor vem do cotidiano brasileiro: ninguém é vilão de verdade, todo mundo é meio malandro.

### Personagens fixos
- **O Chefe (jogador):** nunca aparece. É "o cérebro", quem decide. Já teve a maior firma de Nova Guanabara e perdeu tudo no Banco Central (a abertura).
- **Dona Cida:** lenda aposentada do crime, dona do esconderijo (uma oficina de fachada). Te dá uma segunda chance: empresta a oficina e dois novatos, conduz o tutorial e manda recados por **bip**. Fala mansa, conselho afiado.
- **Delegado Bastos:** o antagonista. Aparece nas manchetes, promete pegar a firma, cresce a cada prestígio ("Bastos é promovido a Superintendente"). A abertura é a única vez que ele pega alguém de verdade, e ele nunca mais para de falar disso.
- **A crew:** ladrões gerados com nome + apelido de classe (ex.: "Juninho Disquete", "Tião Pé-na-Tábua"), uma manha de personalidade e bordões curtos.

### Como a história é contada
Em pílulas, sem cutscene longa:
- **Abertura (~40 s, pulável):** o golpe que deu errado no Banco Central. Ver abaixo.
- **Bip da Dona Cida:** mensagens curtas em marcos (primeiro golpe, primeiro preso, golpe novo liberado, Banco Central, prestígio).
- **Briefing do golpe:** 2–3 linhas de lore por alvo no mapa.
- **Jornal "O Guanabarense":** a tela de resultado de todo golpe é a capa do jornal, com manchete gerada a partir do que aconteceu ("QUADRILHA LEVA CR$ 12 MIL DA LOTÉRICA DO SEU ZÉ; DELEGADO BASTOS PROMETE RESPOSTA").
- **Bordões da crew:** balõezinhos durante o golpe ("Deixa comigo, chefia", "Isso aqui é mamão com açúcar").

### Abertura — o golpe que deu errado
Toca no **Novo jogo**, antes do esconderijo. É a própria Cena do Golpe tocando um golpe roteirizado no Banco Central, com a firma antiga do Chefe: **um ladrão de cada tipo**, os 4 do time. Mostra em 40 segundos tudo que o jogo tem (salas, sacola enchendo, habilidades, alerta, sirene) e ensina a regra principal pelo erro: ganância depois da sirene derruba até a melhor firma.

| Tempo | O que acontece |
|---|---|
| 0–3 s | Tela preta e letreiro: "Nova Guanabara, 1992. Avenida do Porto, 3 da manhã." |
| 3–10 s | A Kombi para no Banco Central, a crew desce pro túnel. Bordões de cada um |
| 10–20 s | Salas caindo rápido (velocidade 3x), sacola crescendo, o Disquete apaga as câmeras, o 171 enrola o vigia. O cofre abre e chove dinheiro |
| 20–30 s | Sirene. Aparece a porta: **METER O PÉ** ou **SÓ MAIS UMA**. A crew escolhe "só mais uma". A polícia chega e todo mundo sai algemado |
| 30–35 s | A cena congela e vira a capa do jornal: "BASTOS PEGA A FIRMA NO BANCO CENTRAL" |
| 35–40 s | Bip da Dona Cida (a segunda chance) e o jogo segue pro esconderijo, já no tutorial da Lotérica |

**Regras:**
- Pulável a qualquer momento (botão Pular e Esc). Toca uma vez por save; o menu tem "Ver abertura" pra rever.
- **Não mexe no save:** não paga, não prende ninguém da sua crew e não gasta bip de marco (não conta como a primeira sirene nem como o primeiro preso). Por isso a abertura não passa pelo motor do golpe: um diretor toca a sequência de eventos direto na cena.
- A crew da abertura é fixa (os 4 tipos, nível alto); ela não entra na sua crew.
- **Versão mínima** se faltar tempo: só a capa do jornal + o bip da Dona Cida. O objetivo continua igual.

### Objetivo
**Voltar pro Banco Central e fazer direito.** Fica visível o tempo todo: o esconderijo mostra "Objetivo: Banco Central" com a escada de golpes até lá. Concluir o Banco Central pela primeira vez é a **revanche**: capa especial do jornal ("A FIRMA VOLTOU: BANCO CENTRAL LIMPO; BASTOS PEDE FÉRIAS"), créditos e o prestígio liberado.

### Arco
0. **Abertura:** a firma do Chefe tenta o Banco Central, insiste depois da sirene e cai. Bastos pega todo mundo.
1. Dona Cida dá uma segunda chance: a oficina e 2 novatos (um 171 e um Gazua).
2. Golpes vão crescendo: Lotérica → Joalheria → Casa de Câmbio → Cassino Clandestino.
3. **Banco Central via túnel: a revanche**, "o golpe do século" e o objetivo de cada ciclo.
4. **Sumir do mapa (prestígio):** a firma desaparece, Bastos jura vingança, a cidade recomeça mais vigiada. Infinito.

---

## 6. Mecânicas

### 6.1 Fluxo de telas

```
Boot → Menu Principal → (Novo jogo: Abertura) → Esconderijo (hub)
                          ├─ Golpes rolando (faixa sempre visível com os golpes no automático)
                          ├─ Crew (ver, recrutar, demitir, pagar fiança)
                          ├─ Bicos (alocar ladrões ociosos; cada bico com a sua cena)
                          ├─ Upgrades do esconderijo
                          ├─ Prestígio (quando liberado)
                          └─ Mapa da cidade → Planejamento ─┬─ GOLPE na mão → Jornal (resultado) → Esconderijo
                                                            └─ Mandar no automático → volta pro Esconderijo
```

### 6.2 Os 4 tipos de ladrão

| Tipo | Função | Obstáculo que resolve | Habilidade ativa (1x por golpe) | Bico | Dono |
|---|---|---|---|---|---|
| **Pé-na-Tábua** | motorista | — (cuida da fuga) | **Cavalo de pau:** fuga imediata sem perder loot, mesmo com sirene tocando | Lotação pirata | Leonardo |
| **Gazua** | arrombador | Fechadura | **Pé-de-cabra:** limpa a sala de fechadura na hora, mas +15 de alerta | Chaveiro 24h | Miguel |
| **Disquete** | hacker | Sistema | **Loop na câmera:** congela o alerta por 15 s | Gatonet | Enzo |
| **171** | vigarista | Pessoa | **Lábia:** −25 de alerta na hora | Bilhete premiado | Nicolas |

Passivas:
- **Pé-na-Tábua:** aumenta o tempo até a polícia chegar depois da sirene e reduz a chance de alguém ser preso na fuga.
- **Gazua / Disquete / 171:** são os especialistas das salas do seu tipo (ver 6.4).

### 6.3 Ladrão (crew)

Cada ladrão tem:
- **Tipo** (um dos 4)
- **Nível** 1–10, sobe com XP ganho em salas limpas
- **Perícia** — velocidade pra resolver o obstáculo do seu tipo
- **Discrição** — quanto menos alerta gera
- **Nervos** — chance de sucesso em escolhas de evento
- **Manha** (1 traço aleatório). Lista inicial: Fominha (+loot, −discrição), Pé-frio (mais eventos ruins), Sangue-frio (+nervos), Boca-aberta (+alerta), Pão-duro (fiança mais barata), Cria da rua (+XP), Supersticioso (+nervos, −perícia), Mão-boba (+loot nas salas de Pessoa)
- **Estado:** Disponível · Em golpe · No bico · Preso

Regras:
- **Recrutar:** o painel "Contatos" mostra 3 candidatos que trocam a cada 5 min (ou na hora, pagando Cr$). Custo cresce com o nível do candidato.
- **Demitir:** libera vaga, com confirmação.
- **Tamanho máximo da crew:** 3 no começo, cresce com o upgrade Quartos (até 8).
- **Vagas no golpe:** 2 no começo, cresce com o upgrade Van (até 4).

### 6.4 O golpe

Um golpe é uma **sequência linear de salas** (3 a 7). A última sala é o cofre principal.

**Sala:** tem um tipo de obstáculo (**Fechadura**, **Sistema** ou **Pessoa**), uma dificuldade, um loot e uma taxa de alerta.

**Limpar uma sala (automático):**
- Se a crew tem o especialista do obstáculo: o poder é a Perícia do melhor especialista.
- Se não tem: o poder é 35% da maior Perícia da crew e a taxa de alerta **dobra**.
- Tempo pra limpar ≈ dificuldade ÷ poder (alvo: 5 a 20 s por sala).
- Ao limpar: o loot da sala entra na **sacola** e todos ganham XP.

**Alerta (0–100):** sobe continuamente enquanto a crew trabalha.
`taxa = taxa da sala × (1 − 0,5 × Discrição média/100) × (2 se faltar especialista) × modificadores`

**Decisão entre salas** (o golpe pausa):
- **Próxima sala:** mostra o preview (tipo do obstáculo, loot estimado, se tem especialista).
- **Meter o pé:** fuga tranquila, leva 100% da sacola, ninguém é preso.

**Eventos:** ao entrar numa sala, 30% de chance de evento (o primeiro golpe tem um evento roteirizado de tutorial). O golpe pausa e aparece um cartão com 2 ou 3 escolhas. Algumas escolhas exigem um tipo de ladrão na crew ou usam Nervos/Perícia pra chance de sucesso. O resultado pode mexer em alerta, loot, tempo ou prender um ladrão. Exemplo:

> **Guarda volta do café mais cedo.**
> • [171] "Puxar papo sobre o Flamengo" — 80% de sucesso com Nervos alto: alerta −10
> • Esconder no banheiro — perde 8 s
> • Sair correndo — +20 de alerta

**Habilidades ativas:** cada ladrão no golpe tem um botão de habilidade, uso único por golpe (ver 6.2).

**Sirene (alerta chega a 100):**
1. Toca a sirene e começa a **contagem da polícia** (10 s base + bônus do Pé-na-Tábua e do upgrade Rádio da Polícia).
2. Durante a contagem a crew continua trabalhando (cada sala a mais ainda rende loot) e o jogador pode **meter o pé** a qualquer momento: é uma **fuga apressada**, perde 20% da sacola (menos com Pé-na-Tábua), mas ninguém é preso.
3. Se a contagem zera: **fuga forçada** — a crew perde 50% da sacola (menos com Pé-na-Tábua) e cada ladrão tem chance de ser preso (menor com Discrição, Pé-na-Tábua e upgrades).
4. **Cavalo de pau** (ativa do Pé-na-Tábua) pode ser usado em qualquer momento, inclusive na fuga forçada: fuga perfeita, 100% da sacola e ninguém preso. É o "seguro" de quem levou um motorista.

Resumo das saídas:

| Quando sai | Sacola | Prisão |
|---|---|---|
| Meter o pé antes da sirene | 100% | ninguém |
| Meter o pé durante a contagem | 80% (+ bônus Pé-na-Tábua) | ninguém |
| Contagem zerou (fuga forçada) | 50% (+ bônus Pé-na-Tábua) | chance por ladrão |
| Cavalo de pau, a qualquer hora | 100% | ninguém |

**Velocidade:** botão 1x / 2x. **Pausa:** Esc ou botão (tudo congela, inclusive a contagem).

### 6.5 Resultado — o jornal
Ao fim de todo golpe, a capa do **"O Guanabarense"** mostra: manchete (escolhida pelo tipo de resultado: limpo, com sirene, com preso, fracasso), Cr$ ganho, XP e nível de cada ladrão, quem foi preso. Botão "Voltar pro esconderijo".

### 6.6 Prisão e fiança
- Ladrão preso fica indisponível por um tempo (cresce com o tier do golpe; base 2 min no primeiro golpe).
- Pode sair na hora pagando **fiança** em Cr$ (o upgrade Advogado reduz tempo e preço).
- O tempo corre também com o jogo fechado.

### 6.7 Bicos (renda passiva) e ganho offline
- Ladrão **Disponível** pode ser mandado pra um bico. Gera Cr$/s enquanto estiver lá. Mandar pro golpe tira ele do bico.
- Cada tipo tem seu bico característico (ver 6.2), que rende **2x** se o ladrão for do tipo certo.
- **Catar latinha:** renda mínima fixa e sempre ativa, sem ladrão (garante que o dinheiro nunca para).
- **Offline:** ao voltar, o jogo calcula os bicos do tempo fora (até 4 h, a 50%) e mostra o popup "Enquanto você tava fora…".
- **Cada bico tem a sua cena.** O cartão do bico vira uma vitrine animada. Vazio, a cena fica parada e apagada, com a placa "volto já". Com gente, o ladrão que está lá aparece trabalhando (com o sprite do tipo dele) e um "+Cr$" sobe a cada poucos segundos, no ritmo da renda:

| Bico | Cena | Dono |
|---|---|---|
| Lotação pirata (Pé-na-Tábua) | Kombi lotada saindo do ponto, cobrador pendurado na porta gritando "Centro! Centro!" | Leonardo |
| Chaveiro 24h (Gazua) | Banquinha com o quadro de chaves e a máquina de copiar soltando faísca | Miguel |
| Gatonet (Disquete) | Poste com um ninho de fios, o ladrão na escada puxando cabo, TVs acendendo nas janelas | Enzo |
| Bilhete premiado (171) | Calçada do centro, o 171 abanando o bilhete pra um otário de terno | Nicolas |
| Catar latinha (sem ladrão) | Carrinho de supermercado com o saco de latinhas, sempre rodando | Miguel |

### 6.8 Esconderijo — upgrades
Cada upgrade tem níveis e o custo cresce a cada nível.

| Upgrade | Efeito |
|---|---|
| Van | +1 vaga no golpe (2 → 4) |
| Quartos | +1 vaga na crew (3 → 8) |
| Ferramentas | +Perícia de todos |
| Tênis de molinha | +Discrição de todos |
| Advogado de porta de cadeia | −tempo de prisão e −fiança |
| Contatos | candidatos melhores e troca mais barata |
| Lavanderia | +% de todo loot |
| Rádio da polícia | +segundos na contagem da polícia |
| Ponto de bico | +% de renda dos bicos |
| Orelhão | +1 golpe no automático ao mesmo tempo (1 → 3) |

### 6.9 Mapa e golpes
Os golpes formam uma escada. Cada um libera o próximo e apresenta algo novo:

| # | Golpe | Salas | O que ensina / introduz | Libera com |
|---|---|---|---|---|
| 1 | **Lotérica do Seu Zé** | 3 | Tutorial: loop básico, decisão de porta, um evento roteirizado. Só Fechadura e Pessoa (os 2 ladrões iniciais) | início |
| 2 | **Joalheria Brilho Eterno** | 4 | Obstáculo **Sistema** → precisa recrutar um Disquete. Primeira sirene provável | Lotérica 1x |
| 3 | **Casa de Câmbio Dólar Fácil** | 5 | Mistura dos 3 obstáculos. Fuga começa a importar → Pé-na-Tábua | Joalheria 1x |
| 4 | **Cassino Clandestino** | 6 | Muitos guardas e eventos, alerta alto. Testa crew completa e ativas | Câmbio 1x |
| 5 | **Banco Central (via túnel)** | 7 | Começa no túnel (alerta baixo, salas longas), termina no cofre mais protegido do jogo. Concluir libera o prestígio | Cassino 1x |

**Golpes extras (ramo ao lado da escada).** Não são caminho pro Banco Central: dão variedade, são farm pro automático e cada um é a vitrine de um ladrão. No mapa aparecem como pinos "extra", ligados ao golpe que os libera.

| Golpe | Salas | Quem brilha | O que tem de diferente | Libera com |
|---|---|---|---|---|
| **Banca do Bicho do Tonhão** | 3 | 171 | Quase só Pessoa (apontador, gerente, segurança). Loot e alerta baixos: o farm ideal pro automático no começo | Lotérica 1x |
| **Carro-forte da Avenida do Porto** | 3 | Gazua + Pé-na-Tábua | Golpe curto na rua: o baú é uma Fechadura difícil e o alerta sobe muito rápido. Muito loot pro tamanho, a fuga decide | Câmbio 1x |
| **Museu de Nova Guanabara** | 6 | Disquete | Quase só Sistema (sensores, alarmes, câmeras), alerta alto e o quadro famoso no cofre | Cassino 1x |

Todo golpe pode ser repetido (farm) com loot base fixo. A cada visita ao mapa, cada golpe pode sortear **um modificador**:

| Modificador | Efeito |
|---|---|
| Feriado | Pessoa −30% dificuldade |
| Troca de turno | −20% de alerta nas 2 primeiras salas |
| Cofre novo | Fechadura +40% dificuldade, +30% loot |
| Blitz na área | −3 s na contagem da polícia |
| Dia de pagamento | +50% loot, +20% alerta |
| Câmeras quebradas | Sistema −50% dificuldade |

### 6.10 Prestígio — "Sumir do mapa"
- Liberado depois de concluir o Banco Central pela primeira vez.
- **Reseta:** Cr$, upgrades, golpes liberados e a crew (Dona Cida manda 2 novatos de novo).
- **Mantém:** Fama e bônus de Fama.
- **Ganha:** Fama = função do total de loot do ciclo (ex.: `floor(sqrt(lootTotal / 10.000))`).
- **Nível de Procurado +1:** em todos os golpes dificuldade ×1,12, loot ×1,5 e alerta ×1,05 por nível (números de `BALANCEAMENTO.md`). O Delegado Bastos é promovido.
- **Bônus de Fama** (comprados com Fama, permanentes): Reputação (recrutas mais baratos), Herança (começa o ciclo com Cr$), Lenda (recrutas nascem com mais nível), Esquema (+loot), Rede (+bicos), Sangue-frio (−alerta).

### 6.11 Golpes no automático (segundo plano)
Pra o jogo ter cara de idle: a firma trabalha enquanto você faz outra coisa. Sem virar "o jogo joga sozinho" (anti-objetivo), porque o automático só repete o que você já fez na mão e rende menos.

- **Libera:** um golpe pode ir pro automático depois que você limpa o cofre dele na mão pelo menos uma vez. A Dona Cida avisa por bip na primeira vez ("A Lotérica a rapaziada já faz de olho fechado. Manda eles sozinhos.").
- **Mandar:** no planejamento, o botão **Mandar no automático** usa a mesma escolha de crew (vagas da Van) e pede a **ordem** pra crew:
  - **Na moita:** mete o pé antes da sirene (alerta passou de 70 ou a próxima sala não cabe). Seguro, rende menos.
  - **Até o cofre:** só para no cofre ou na sirene, saindo na fuga apressada. Rende mais e pode dar cana.
  - **Repetir** (liga/desliga): a crew emenda um golpe atrás do outro até você chamar de volta ou alguém ir preso.
- **Como roda:** o mesmo motor do golpe, sem cena e na velocidade 1x. As portas seguem a ordem; nos eventos a crew escolhe sozinha a escolha com melhor chance que ela pode fazer; as habilidades disparam sozinhas na hora certa (Lábia e Loop com alerta alto, Pé-de-cabra em sala de Fechadura, Cavalo de pau quando a polícia está chegando).
- **Rende menos que na mão:** loot × 0,6 (ponto de partida). Cofre limpo no automático não libera golpe novo e não dá bip de marco: golpe novo é sempre na mão.
- **Quantos ao mesmo tempo:** 1 no começo, até 3 com o upgrade **Orelhão**. Quem está no automático fica "Em golpe": não vai pra bico nem pro golpe na mão. O golpe na mão e os automáticos rodam juntos.
- **Na tela:** a faixa **Golpes rolando** no esconderijo mostra cada golpe no automático: miniatura do prédio, retratos da crew, as salas como bolinhas (feita, atual, falta), barra de alerta, sacola subindo e o giroflex piscando na sirene. Terminou: mini manchete do jornal e "+Cr$" voando pro caixa. Botão **Chamar de volta** (vira "meter o pé" na hora).
- **Offline:** os golpes com Repetir ligado continuam com o jogo fechado, junto dos bicos e com a mesma regra (até 4 h, a 50%). Fora do jogo ninguém vai preso: fuga forçada só perde sacola. Entra no popup "Enquanto você tava fora…".

---

## 7. Interface e feedback

### 7.1 Feedback no mundo (rubrica Feedback)
O jogador entende o golpe **olhando a cena**, não só a HUD:
- O ícone do obstáculo flutua sobre a sala (cadeado, monitor, pessoa).
- O ladrão faz a animação do trabalho na sala: Gazua gira a fechadura com faísca, Disquete digita com tela piscando, 171 conversa gesticulando.
- Anel de progresso no mundo, sobre a sala.
- Notas voando pra sacola quando a sala é limpa.
- Guarda com lanterna (luz 2D) patrulhando; vira "!" e a lanterna fica vermelha quando o alerta sobe.
- Câmera de segurança com cone de visão que muda de cor com o alerta.
- Luz de alarme vermelha girando a partir de 70% de alerta; luz azul e vermelha de viatura pela janela na sirene.
- Vinheta na tela que fecha conforme o alerta.
- Balões de bordão dos ladrões.
- Música do golpe que ganha camadas conforme o alerta.

### 7.2 UI/HUD e menus
**Identidade visual:** "dossiê da firma" — papel kraft, carimbo CONFIDENCIAL, fotos 3x4 dos ladrões em clipes, bip pras notificações, jornal no resultado. Fonte pixel legível.

**Telas:**
- Menu principal (Continuar, Novo jogo com confirmação, Ver abertura, Opções, Créditos, Sair só no desktop)
- Abertura (letreiro, capa do jornal, botão Pular)
- Pausa (Continuar, Opções, Voltar ao esconderijo/menu)
- Opções (volume Geral/Música/SFX, tela cheia, apagar save com dupla confirmação)
- Créditos
- Esconderijo (hub), sempre com o objetivo atual ("Objetivo: Banco Central") e a faixa **Golpes rolando**
- Crew (ficha de cada ladrão)
- Contatos (recrutamento)
- Bicos (cada cartão com a cena animada do bico)
- Upgrades
- Prestígio
- Mapa da cidade (com os golpes extras)
- Planejamento (escolher a crew do golpe, ver estimativa, jogar na mão ou mandar no automático)
- HUD do golpe (alerta, sacola, retratos com botão da ativa, velocidade, pausa)
- Popup de decisão de porta
- Cartão de evento
- Contagem da polícia
- Jornal de resultado
- Popup offline
- Bip da Dona Cida (notificação)
- Tutorial guiado

**Regras de UX:**
- Botão indisponível sempre diz o motivo ("Falta Cr$ 200", "Precisa de 1 Disquete").
- Ação irreversível sempre pede confirmação (prestígio, demitir, novo jogo, apagar save).
- Todo número de dinheiro animado ao mudar (contador rolando).

### 7.3 Tutorial
Começa logo depois da abertura. O primeiro golpe (Lotérica) é guiado pela Dona Cida via bip: destaca o mapa, a escolha de crew, explica a barra de alerta, força uma decisão de porta e um evento roteirizado. Pulável. Depois disso, dicas pontuais na primeira vez que algo aparece (primeira sirene, primeiro preso, primeiro bico, prestígio liberado).

---

## 8. Áudio
- **Música:** esconderijo (samba-jazz / bossa lo-fi), golpe (tensão com camadas que entram por faixa de alerta), sirene/fuga (percussão + sirene), menu.
- **SFX:** UI (clique, hover, compra, erro), golpe (passos, fechadura, digitação, conversa abafada, loot, porta abrindo), alarme, sirene, viatura, bip, máquina de escrever no jornal, ativas de cada ladrão.
- **Mixer:** Master / Música / SFX, com volume salvo.
- **Fontes:** bancos gratuitos (freesound, Pixabay, OpenGameArt, Kenney) e/ou IA, tudo creditado.

## 9. Visual e VFX
- **Pixel art.** Fonte principal de assets: **[craftpix.net](https://craftpix.net)**, com acesso fornecido pela faculdade (interiores, prédios, cidade, props, ícones, GUI e personagens base). Complemento com packs gratuitos (itch.io, Kenney, OpenGameArt) só quando o craftpix não tiver o que precisa. Tudo recolorido pra uma paleta única do jogo, pra não parecer colagem de packs.
- **Os 4 ladrões** feitos pelo time (32x32), podendo partir de um personagem base do craftpix e customizar. IA permitida como ferramenta.
- **UI:** a base de botões, painéis e ícones também vem do craftpix (GUI packs), adaptada pra identidade "dossiê da firma".
- **Escolha de packs é coordenada:** antes de baixar, o pack vai pra lista em `docs/ASSETS.md` (o que é, quem usa, link). Isso evita dois packs com estilos diferentes no mesmo jogo.
- **Por ladrão:** animações idle, andar, trabalhar e comemorar, mais um retrato 3x4 pra UI.
- **Salas modulares:** um prefab por tipo de sala (Entrada, Corredor, Balcão/Caixa, Sala de Câmeras, Escritório, Cofre, Túnel, Garagem), reaproveitados entre golpes com troca de props e paleta.
- **VFX:** luzes 2D (URP), partículas (faísca, notas, fumaça, poeira do túnel), screen shake leve na sirene, pós-processamento (vinheta e bloom).
- **Idle que se vê:**
  - **Cena de cada bico** (§6.7): 96×64 px, 4 a 8 quadros em loop (~8 fps), na paleta de 32 cores e gerada por script como o resto da arte. O ladrão da cena usa o sprite 32×32 do tipo dele.
  - **Miniatura de cada prédio** (64×48), pra faixa Golpes rolando e pro mapa, com uma versão de sirene (giroflex vermelho e azul).
  - **Esconderijo vivo** (extra): a crew disponível aparece no porão da Cida em idle; quem está no bico ou no golpe some; preso vira foto 3x4 com "EM CANA" no mural.

---

## 10. Estados e erros (rubrica obrigatória)
- **Save automático** a cada 10 s e em todo marco (fim de golpe, compra, recrutamento, prestígio). JSON versionado em `PlayerPrefs`.
- **Backup:** sempre guarda o último save válido. Se o save atual estiver corrompido, carrega o backup e avisa. Se os dois falharem, começa um jogo novo e avisa.
- **Abertura não toca no save:** nada do golpe roteirizado vira dinheiro, prisão ou bip de marco. Fechou no meio da abertura? No próximo Novo jogo ela toca de novo; pulou, segue direto pro esconderijo.
- **Fechou no meio do golpe:** o golpe não é salvo no meio. Ao voltar, conta como "meteu o pé": a crew volta, leva o loot já na sacola, ninguém é preso. Mensagem explicando.
- **Golpes no automático:** o save guarda cada golpe no automático (golpe, crew, ordem, Repetir). Ao carregar, o golpe que estava no meio conta como "meteu o pé" e, se Repetir estava ligado, o tempo fora entra na conta do offline. Crew presa, demitida ou golpe que deixou de existir: o automático para sozinho e avisa.
- **Anti dead-end:**
  - Catar latinha sempre rende.
  - Se a crew estiver vazia ou toda presa e não houver Cr$ pra recrutar, Dona Cida manda um novato de graça.
  - A prisão sempre acaba sozinha com o tempo.
  - A Lotérica nunca fica bloqueada.
- **Botões nunca levam a erro:** ação impossível fica desabilitada com o motivo.
- **Pausa segura:** pausar congela tudo; decisões pendentes esperam.
- **WebGL:** nada de `Application.Quit` no WebGL, `PlayerPrefs.Save()` após salvar, foco da aba perdido não quebra a contagem.

## 11. Balanceamento — metas
- 1º golpe (Lotérica) em ~1 min, tutorial incluso em ~3 min.
- Joalheria liberada em ~3 min; Banco Central alcançável em **~40–60 min** de jogo ativo no primeiro ciclo.
- Ciclos de prestígio seguintes mais rápidos no começo (bônus de Fama) e mais difíceis no topo.
- Nenhum upgrade inútil; nenhum ladrão sem função; ficar sem especialista sempre dói, mas é possível.
- Jogar na mão sempre rende mais: o mesmo golpe no automático rende ~60%, e 3 golpes no automático + bicos não podem passar a renda de quem joga ativo o golpe mais alto liberado.
- Loot por tier cresce ~×6 (ponto de partida: Lotérica ~Cr$ 500 → Joalheria ~3 mil → Câmbio ~18 mil → Cassino ~100 mil → Banco Central ~600 mil). Custos de upgrade crescem ~×1,6 por nível.
- Todos os números ficam em ScriptableObjects e numa planilha de balanceamento; ninguém coloca número mágico no código.

---

## 12. Arquitetura (resumo)
Detalhes em [`ARQUITETURA.md`](ARQUITETURA.md).

- **Pastas por dono:** cada frente tem a sua pasta em `Assets/_Project/` e só o dono edita. As cenas principais são só "montagem" de prefabs.
- **Contratos no Core** (entregues na Sprint 0, antes de todo mundo começar):
  - Definições de dados (ScriptableObjects): ladrão, manha, golpe, sala, evento, upgrade, bico, modificador, bônus de Fama
  - Estado do jogo serializável (`GameState`)
  - Interfaces de serviço (economia, crew, golpe, progressão, save, áudio)
  - Canal de eventos global (`GameEvents`) — as frentes conversam só por aqui
- **Mocks:** cada interface tem uma implementação falsa, e o `HeistSimulator` de debug toca um golpe roteirizado disparando os mesmos eventos do real. Assim a Cena do Golpe e a UI funcionam antes do Motor ficar pronto.
- **Sandbox por pessoa:** uma cena de testes própria pra cada um.
- **Código em inglês, textos do jogo em PT-BR.**

## 13. Divisão do time

| Pessoa | Frente | Entrega principal | Ladrão | Rubricas que puxa |
|---|---|---|---|---|
| **Leonardo** | Motor do Golpe + Crew + Core | Simulação do golpe, alerta, decisões, eventos, ativas, fuga; sistema de ladrões e recrutamento; save; build WebGL | Pé-na-Tábua | Mecânicas (×2), Estados e Erros |
| **Nicolas** | Economia + Progressão | Cr$, upgrades, bicos, offline, mapa e liberação de golpes, modificadores, prestígio e Fama, planilha de balanceamento, narrativa (bips, briefings, manchetes) | 171 | Balanceamento (×2), Replayability, Blueprint |
| **Enzo** | Interface + Áudio | Todos os menus e telas, HUD, popups, jornal, tutorial, identidade visual da UI, música, SFX, mixer, página do itch | Disquete | UI/Menus, Engajamento, Som |
| **Miguel** | Cena do Golpe | Salas modulares, os 5 prédios, crew andando, guardas, câmeras, luzes, VFX, câmera 2D, feedback no mundo | Gazua | Feedback, VFX |

GitHub: Leonardo `souzxxx` · Nicolas `NicolasVolf` · Enzo `canavero` · Miguel `EuMiguelll`

**Abertura e objetivo (§5), quem faz o quê:**

| Pessoa | Parte |
|---|---|
| **Leonardo** | Diretor da abertura: toca a sequência roteirizada de eventos direto na Cena do Golpe, sem passar pelo motor de verdade (nada vai pro save, nenhum bip de marco). Fluxo Novo jogo → Abertura → Esconderijo, Pular, "Ver abertura" e a marca de vista no save |
| **Nicolas** | Roteiro e textos: letreiro, bordões da crew antiga, manchete da queda, bip da segunda chance (substitui o bip de jogo novo), texto do objetivo e a capa da revanche no Banco Central |
| **Enzo** | Letreiro e botão Pular, capa do jornal da abertura e da revanche, "Objetivo" no esconderijo, créditos depois da revanche, música e SFX da abertura |
| **Miguel** | Banco Central à noite com a crew de 4, a porta "SÓ MAIS UMA", o momento da prisão (todo mundo algemado, a viatura) e o congelamento antes do jornal. Bastos aparece só na foto do jornal; descer da viatura é extra |

**Pedidos do professor (28/10): idle na tela, golpes no automático, abertura e golpes extras, quem faz o quê:**

| Pessoa | Parte |
|---|---|
| **Leonardo** | Motor dos golpes no automático (§6.11), com o contrato no Core até 30/10 (interface + mock, campo de miniatura no `HeistDef`). Diretor e fluxo da abertura (API até 31/10). Contrato da cena do bico no `SideJobDef` (29/10) e a cena da Lotação pirata. Eventos dos golpes extras (#113–#116) |
| **Nicolas** | Balanceamento do automático: multiplicador de loot, upgrade Orelhão, offline. Os 3 golpes extras: dados, liberação, mapa, briefings e manchetes. Textos da abertura e da revanche. Cena do Bilhete premiado (#117–#120) |
| **Enzo** | "Mandar no automático" no planejamento e a faixa Golpes rolando no esconderijo. Bicos com cena (vitrine animada e o flipbook). Telas e som da abertura e do objetivo. Cena do Gatonet (#121–#124) |
| **Miguel** | Cena da abertura no Banco Central. Prédios dos 3 golpes extras. Miniaturas dos 8 prédios (normal e sirene). Cenas do Chaveiro 24h e do Catar latinha. Esconderijo vivo (extra) (#125–#129) |

Ordem pra ninguém esperar ninguém: contratos no Core até 30/10 (com mock), API do diretor da abertura até 31/10, todas as P0 e P1 até o feature freeze (05/11); o Esconderijo vivo fica pra S3.

**Todo mundo:**
- O seu ladrão: sprite, animações, retrato, habilidade ativa, 3 eventos em que ele brilha, bico (com a cena dele), bordões, SFX de ação
- Os créditos dos seus assets
- A sua seção no GDD

**Se faltar tempo, corta primeiro:** tasks que só servem Som ou VFX (rubricas descartadas), o Esconderijo vivo, depois o visual próprio dos prédios 2 a 5 e dos golpes extras (podem reaproveitar as salas modulares da Lotérica), o offline dos golpes no automático e o polimento de UI. A abertura cai pra versão mínima (só a capa do jornal + o bip), mas o objetivo fica. **Nunca corta:** o que serve Mecânicas, Balanceamento, Estados e Erros e GDD, nem o que o professor pediu direto: golpes no automático com a faixa Golpes rolando e os bicos com cena.

## 14. Cronograma

As sprints fecham na **quinta** (dia provável de aula).

| Sprint | Período | Meta | Pronto quando… |
|---|---|---|---|
| **0 · Fundação** | 20/10 → 22/10 | Projeto Unity com Core, contratos, mocks e sandboxes; todos com o ambiente rodando; GDD criado no Milanote; rubricas validadas com o professor | Cada um abre o projeto, roda a sua sandbox e faz um PR de teste |
| **1 · Primeiro golpe** | 23/10 → 29/10 | Lotérica jogável de ponta a ponta: esconderijo → mapa → golpe → jornal → esconderijo, com save. Sistemas reais onde der, mock onde faltar | Alguém de fora joga a Lotérica sem ajuda e entende |
| **2 · Jogo completo** | 30/10 → 05/11 | Os 5 golpes + 3 extras, os 4 ladrões, eventos, upgrades, bicos com cena, golpes no automático, offline, prestígio, todas as telas, música, a abertura e o objetivo. **Feature freeze 05/11** | Dá pra ir da abertura ao prestígio com golpes rolando no automático |
| **3 · Polimento e entrega** | 06/11 → 11/11 | Balanceamento, bugs, juice, VFX, áudio final, tutorial; página do itch; GDD final; README com créditos. **Build final até 10/11**, entrega 11/11 | Checklist de entrega completo |

Build WebGL de teste **toda quinta**, pra não descobrir problema de WebGL no fim.

## 15. Entregáveis
- [ ] Build WebGL publicada na jam: <https://itch.io/jam/projeto-jogo-2d-insper-20262>
- [ ] Página do itch caprichada: descrição, como jogar, screenshots, GIF/vídeo, créditos
- [ ] Repositório GitHub com código e README (como abrir, equipe, créditos de todos os assets e ferramentas)
- [ ] GDD no Milanote atualizado, estrutura DDE, refletindo o jogo final
- [ ] Entrega no Blackboard com todos os links

## 16. Fica a critério de quem constrói
- Números finais de balanceamento (as metas acima mandam).
- Layout exato de cada sala e prédio, respeitando a ordem de introdução dos obstáculos.
- Paleta, fontes e packs específicos, desde que coesos e creditados.
- Texto final de eventos, bordões, manchetes e bips, no tom malandro charmoso.
- Faixas e efeitos sonoros específicos.
