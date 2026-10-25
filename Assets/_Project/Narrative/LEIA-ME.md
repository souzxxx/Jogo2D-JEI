# Narrative

Narrativa (Nicolas): bips da Dona Cida, briefings, manchetes.

## O que tem aqui

| Asset | O que é |
|---|---|
| `Resources/DonaCidaBips` (`BipSetDef`) | Textos dos bips da Dona Cida: jogo novo, 1º golpe (cofre limpo ou meteu o pé), 1ª sirene, 1º preso (`{nome}`), um por golpe liberado e prestígio liberado |
| `Runtime/DonaCidaBips` | Escuta os `GameEvents` e manda `GameEvents.Bip` nos marcos, **uma vez por save** (fica em `GameState.seenTips` com o prefixo `bip:`). Golpe liberado no meio do golpe espera o resultado; na pausa os bips esperam |
| `Runtime/NarrativeInstaller` | Sobe tudo no Play. Enquanto a UI do bip (Enzo) não existe, todo bip sai no Console como `[Bip] Dona Cida: ...` |

O bip do novato grátis fica no `GameConfig.freeRookieBip` (quem manda é o `CrewService`). Os briefings ficam em cada `HeistDef` (`Data/Heists`).

Pra ver de novo os bips de primeira vez: F1 → **Novo jogo (apaga save)**.

---

Esta página é a bíblia do mundo e o guia de tom. **Todo texto do jogo passa por aqui**: evento, bordão, briefing, bip, manchete, descrição de upgrade. Na dúvida, leia os exemplos do fim.

---

## Nova Guanabara

Cidade fictícia que mistura Rio e São Paulo, **começo dos anos 90** (por volta de 1992). A moeda é o **Cruzeiro (Cr$)** e a inflação come o salário todo mês. Ninguém é vilão de verdade: todo mundo é meio malandro, do guarda que aceita um cafezinho ao dono do câmbio que não confia em banco.

**Cotidiano que pode aparecer:** orelhão e ficha telefônica, bip, fita K7, rádio AM com futebol, novela das oito, plantão da TV, lotação, gatonet, jogo do bicho, fezinha na lotérica, feira de quarta, pastel com caldo de cana, Fusca e Kombi, computador com modem discado, post-it no monitor.

**Lugares**
| Lugar | O que é |
|---|---|
| **Zona Norte** | Onde a firma mora. Oficina da Dona Cida, a lotérica do Seu Zé, a pastelaria do cassino |
| **Centro** | Comércio, joalheria, casa de câmbio, camelô em toda calçada |
| **Avenida do Porto** | Prédios de vidro, o Banco Central e a sede da polícia |
| **Esconderijo** | "Auto Elétrica da Cida": oficina de fachada com um porão cheio de mapa na parede |

**Os 5 golpes**, do menor pro maior: Lotérica do Seu Zé → Joalheria Brilho Eterno → Casa de Câmbio Dólar Fácil → Cassino Clandestino (debaixo de uma pastelaria) → Banco Central, via túnel.

## Personagens fixos

**O Chefe (jogador).** Nunca aparece nem fala. Todo mundo se dirige a ele como **"chefia"**. Não tem gênero definido: evite "o chefe é ele" ou "meu filho"; use "chefia", "você", "meu bem".

**Dona Cida.** Uns 70 anos, lenda aposentada do crime. Diz que nunca foi presa porque "nunca deixou pista, só saudade". Passou a firma pra você e cuida do esconderijo. **Fala mansa, conselho afiado**, um pouco de deboche e muito carinho. Manda recado por **bip**: frases curtas, sem rodeio. Nunca grita, nunca xinga, nunca explica demais.
- "Fez bonito no Seu Zé. Agora não vai sair contando pra vizinhança."
- "Tão te procurando na TV. Bom sinal: quer dizer que você existe."

**Delegado Bastos.** O antagonista, de bigode e terno apertado. Adora uma câmera e um microfone. **Promete muito e pega pouco**: é o alvo da piada, não uma ameaça de verdade. Aparece nas manchetes e cresce a cada prestígio (delegado → superintendente → secretário de segurança). Nunca é violento nem cruel.
- "BASTOS GARANTE: 'É QUESTÃO DE DIAS'" (é sempre questão de dias)

**A crew.** Nome + apelido de classe ("Juninho Disquete", "Tião Pé-na-Tábua"). Cada tipo tem um jeito de falar:
| Tipo | Jeito | Exemplo |
|---|---|---|
| **171** | Lábia, elogio, conversa mole, se acha artista | "Deixa que eu converso, chefia." |
| **Gazua** | Calado, prático, ama ferramenta, fala de fechadura como de gente | "Essa aqui é das teimosas." |
| **Disquete** | Nerd do computador, gíria de tecnologia de 1992 | "Tô dentro. Literalmente." |
| **Pé-na-Tábua** | Fala como quem tá dirigindo, nunca desliga o motor | "Segura que eu dirijo!" |

---

## Guia de tom

**Malandro charmoso, não crime pesado.** O humor vem do cotidiano, do improviso e da autoconfiança de quem está sempre a um passo de dar errado. A crew é simpática; as vítimas são instituições, cofres e gente distraída, nunca alguém frágil.

### Pode
- **Gírias da época:** chefia, mermão, meu, chapa, grana, bufunfa, cascalho, trampo, moleza, mamão com açúcar, na moita, dar no pé, meter o pé, sujou, maneiro, irado, da hora, fezinha, molhar a mão, conversa mole.
- Fala informal nas falas: "tá", "pra", "tô", "cê".
- Exagero cômico, ironia, autoconfiança fora de lugar ("Mamão com açúcar." logo antes de tocar a sirene).
- Referências ao cotidiano brasileiro (novela, futebol, feira, pastel) sem citar pessoa real.

### Evite
- **Violência:** arma, tiro, sangue, morte, ferimento, sequestro, refém, ameaça a pessoa. Guarda é distraído, enganado, trancado no almoxarifado: nunca machucado.
- **Ofensa:** xingamento pesado, palavrão, piada com raça, religião, gênero, orientação sexual, deficiência, sotaque ou região. Nada de "favela" como lugar de bandido.
- **Coisa real:** político, marca, empresa, time como alvo de piada maldosa, crime real. (Torcer pro Flamengo pode; xingar torcida não.)
- **Drogas e álcool** como parte do golpe.
- **Anacronismo:** celular, internet comum, Pix, Real (R$), "deu ruim", "mó", "top", "crush".
- **Texto longo.** Ninguém lê parágrafo no meio do golpe.

### Tamanhos
| Texto | Tamanho |
|---|---|
| Bordão da crew | até ~40 caracteres |
| Bip da Dona Cida | 1–2 frases, até ~140 caracteres |
| Briefing do golpe | 2–3 frases |
| Evento: descrição | 1–2 frases |
| Evento: opção | 2–6 palavras, verbo no começo ("Puxar papo sobre o Flamengo") |
| Evento: resultado | 1 frase |
| Manchete | caixa alta, até ~70 caracteres |
| Descrição de upgrade | efeito claro primeiro: "+5% de Perícia pra todo mundo." |

### Formato
- Dinheiro sempre **"Cr$ 1.000"** (espaço, ponto de milhar). No código: `Formulas.Money`.
- Fala da crew e bip podem ser informais. **Texto de interface** (botões, avisos, motivo de botão desabilitado) é claro e direto: "Falta Cr$ 70.", "Nível máximo.".
- Sem emoji. Reticências com moderação.

### Bons e ruins
| Ruim | Bom | Por quê |
|---|---|---|
| "Ameaçar o guarda com a arma" | "Dizer que é da dedetização" | Sem violência: a saída é a lábia |
| "O vigia levou uma pancada e apagou" | "O vigia pegou no sono abraçado no rádio" | O mesmo efeito, sem machucar ninguém |
| "Esse guarda é um idiota" | "O guarda voltou mais cedo do café" | Situação engraçada, não insulto |
| "A crew avançou para a sala seguinte com sucesso." | "Retrovisor limpo, chefia." | Personagem falando, não relatório |
| "Bastos vai prender e matar todos vocês" | "BASTOS GARANTE: 'É QUESTÃO DE DIAS'" | Bastos é piada, não ameaça |
| "Liga no celular do gerente" | "Liga do orelhão da esquina" | Anos 90 |
| "Dona Cida: Parabéns! Você concluiu o primeiro golpe e agora pode acessar a Joalheria no mapa." | "Dona Cida: Fez bonito no Seu Zé. Tem uma joalheria no Centro que tá te esperando." | Bip curto, com voz |

Exemplos que já estão no jogo e acertam o tom: os eventos *Guarda voltou do café*, *Segredo de novela*, *Segurança novato* e os bordões do Pé-na-Tábua (`Thieves/PeNaTabua/LEIA-ME.md`).
