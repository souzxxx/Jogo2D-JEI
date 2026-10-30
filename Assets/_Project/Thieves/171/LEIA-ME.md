# 171

171 (Nicolas): sprites, animações, retrato, habilidade, eventos, bico.

## Personalidade

Cresceu vendendo bilhete de rifa na porta da Central e descobriu cedo que ninguém resiste a um elogio bem colocado. Se acha artista: chama golpe de "apresentação", vítima de "plateia" e jura que já foi figurante de novela das oito. Anda de chapéu panamá e sapato bicolor até no túnel do Banco Central, porque "a primeira impressão é a que fica". Tem um cartão de visita pra cada ocasião (fiscal da matriz, companhia de luz, segurança da noite) e troca de profissão no meio da frase sem piscar. Fala macio, chama todo mundo de "meu bem" e nunca levanta a voz. Único ponto fraco: não sabe perder uma discussão, nem quando devia ficar quieto.

**Bordões** (`ThiefType_171.barks`): "Deixa que eu converso." · "Boa noite, sou da companhia de luz." · "Confia, chefia." · "Eu vendo até areia na praia." · "Ninguém resiste a um bom papo." · "Sorria, você tá sendo enrolado." · "Com licença, é rapidinho." · "Conversa mole é arte, meu bem."

## Eventos em que ele brilha (`Events/`)

Todo evento tem saída pra crew sem 171, mas a do 171 é a que dá bônus.

| Evento | Onde | Do 171 (Nervos) | Sem 171 |
|---|---|---|---|
| Caixa fazendo hora extra | sala de Pessoa, Lotérica+ | "Dizer que é o fiscal da matriz": −8 de alerta e +10% na sacola | pedir informação (−5 s), esperar escondido (Discrição) |
| Turista perdido | qualquer sala, Joalheria+ | "Vender um passeio guiado pro mirante": −6 de alerta e +5% na sacola | foto com ele (−6 s), apontar a saída (+7 de alerta) |
| O gerente voltou | sala de Pessoa, Câmbio+ | "Se apresentar como o novo segurança da noite": −15 de alerta | trancar no almoxarifado (Perícia), correr (+12 de alerta) |

## Bico: Bilhete premiado

Na calçada do centro, ele abana o bilhete "premiado" que "não pode descontar" pra um otário de terno. Rende o dobro com um 171 (`SideJob_171`, ícone em `Icons/Icon_BilhetePremiado`). A cena animada do bico é a #120.

## Arte

Vigarista de chapéu panamá, blazer vinho (a `color` do tipo), camisa branca, gravata dourada, bigodinho e sapato bicolor. De lado, olhando pra direita. No retrato ele pisca: é o 171.

| Asset | O que é |
|---|---|
| `Art/171_sheet.png` | Sheet 32×32 fatiado em 20 sprites (`171_<animação>_<n>`), pivô no pé (centro de baixo) |
| `Art/171_retrato.png` | Retrato 64×64, foto 3x4 do dossiê |
| `Art/171.controller` | Animator com os estados `idle` (padrão), `andar`, `trabalhar` e `comemorar` |
| `Art/171_*.anim` | Um clipe por estado. `comemorar` não repete e para no último frame |

| Estado | Frames | FPS | O que faz |
|---|---|---|---|
| `idle` | 4 | 4 | Respira |
| `andar` | 6 | 10 | Passada com balanço do braço |
| `trabalhar` | 6 | 8 | Conversa mole: gesticula, abre a boca e solta um balão de "..." |
| `comemorar` | 4 | 8 | Agacha e pula com o punho pra cima |

O `ThiefType_171` já aponta pro retrato e pro Animator. Na cena, quem usa troca de estado pelo nome: `animator.Play("andar")`.

**Pra mudar a arte:** edite e rode `python "Assets/_Project/Thieves/171/Art/Source~/gerar_171.py"` na raiz do repositório. O desenho é feito pixel a pixel no script, sem pack de terceiro. Se não mudar o tamanho do sheet, o fatiamento e os clipes continuam valendo. As prévias ampliadas (`previa_*.png`) ficam na mesma pasta `Source~`, que o Unity ignora.

## Lábia: ícone, som e efeito

1. `python3 -B "Assets/_Project/Thieves/171/Art/Source~/gerar_icones_171.py"` (precisa de Pillow): ícone da Lábia, ícone do Bilhete premiado e o balão "BLÁ BLÁ" do efeito, na paleta da Cena do Golpe.
2. `python3 -B "Assets/_Project/Thieves/171/Audio/Source~/gerar_sons_171.py"`: os sons, sintetizados sem sample.
3. No Unity, **A Firma › 171 › Gerar**: preenche a habilidade (ícone, som e VFX), o som de ação do `ThiefType_171` e o ícone do bico.

| Asset | O que é |
|---|---|
| `Ability_171` | Lábia: −25 de alerta na hora. Ícone do balão com a barra de alerta descendo, som de três "blá" macios e um plim descendo, e o `VFX_Labia` |
| `VFX/VFX_Labia` | O balão "BLÁ BLÁ" sobe acima da cabeça dele (na camada das falas, por cima do ícone da sala) e solta coraçõezinhos que flutuam. A Cena do Golpe instancia quando a habilidade é usada |
| `Audio/SFX_171_Conversa` | Som de ação: conversa mole abafada, toca quando ele começa a trabalhar numa sala |
| `Audio/SFX_Labia` | Som da Lábia |
| `Icons/` | Ícone da Lábia e do Bilhete premiado, 32×32 |
