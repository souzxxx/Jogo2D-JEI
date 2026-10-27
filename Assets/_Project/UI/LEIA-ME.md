# UI

Interface (Enzo): menus, telas, HUD, popups, tutorial. Feita em **UI Toolkit** (UXML + USS), sem prefab de Canvas.

## Onde fica cada coisa

| Pasta | O quê |
|---|---|
| `Styles/Firma.uss` | Tema do "dossiê": paleta em variáveis (`var(--kraft)`), fonte, painéis, botões, carimbo |
| `Settings/Tema.tss` + `PanelSettings.asset` | Tema padrão do Unity + `Firma.uss`; tela de referência 640×360 (cada pixel vira 2 em 1280×720) |
| `Fonte/FirmaPixel.ttf` | Fonte pixel com acento, feita das letras do Miguel (`Art/Source~/gerar_fonte.py`) |
| `Screens/` | Uma tela por arquivo: `Nome.uxml` (layout) + `Nome.uss` (o que só ela usa) |
| `Runtime/` | Um script por tela (`MainMenuScreen`) e os atalhos `Ui` (clique com som, confirmação, botão indisponível) |
| `Editor/UiBuilder.cs` | **A Firma › Interface › Gerar tudo**: PanelSettings, sons da UI e as telas nas cenas |

## Regras do tema

- Tamanho de fonte só em múltiplos de 11 px (11, 22, 33, 44, 66): a fonte é desenhada em 11 px de altura e fora disso borra.
- Classes prontas: `panel` (kraft), `paper`, `dark` (caixa da HUD), `stamp` (carimbo torto), `btn`, `btn--green`, `btn--red`, `h1`, `h2`, `money`, `muted`, `hidden`.
- Botão indisponível usa `SetAvailable(false)` (fica cinza mas continua clicável pra dizer o motivo). Ação irreversível usa `Ui.Confirm`.
- Cor só pela paleta (`var(--nome)`), nunca hex solto na tela.

## Trocar a fonte

1. Edite as letras em `HeistScene/Art/Source~/paleta.py` (`GLYPHS`) ou os extras em `Art/Source~/gerar_fonte.py`.
2. `python3 -B Assets/_Project/UI/Art/Source~/gerar_fonte.py` (precisa de `pip install fonttools`).

## Prints das telas

Os testes de PlayMode da UI tiram print de cada tela em 1280×720 quando a variável `AFIRMA_PRINTS` aponta pra uma pasta:

```
AFIRMA_PRINTS=/tmp/prints Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames AFirma.UI.PlayModeTests
```
