# 171

171 (Nicolas): sprites, animações, retrato, habilidade, eventos, bico.

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
