# Página do itch.io

Textos e configuração prontos pra colar na página do jogo (#13 e #62). Jam: <https://itch.io/jam/projeto-jogo-2d-insper-20262>.

## Configuração do projeto (#13)

| Campo | Valor |
|---|---|
| Title | A Firma |
| Project URL | `a-firma` |
| Kind of project | HTML |
| Release status | In development (vira Released na entrega) |
| Visibility | Draft até a build final; Public na entrega |
| Uploads | zip da build WebGL, marcado "This file will be played in the browser" |
| Embed options | Viewport 1280 × 720 · Mobile friendly: não · Fullscreen button: sim · Scrollbars: não |
| Frame options | Click to launch in fullscreen: não · Automatically start on page load: sim |
| Genre | Strategy |
| Tags | idle, heist, pixel-art, management, push-your-luck, brazil, 90s, singleplayer, unity |
| Languages | Portuguese (Brazil) |
| Inputs | Mouse, Keyboard |
| Admins | Leonardo, Nicolas e Miguel (pedir o usuário do itch de cada um) |

Depois: entrar na jam pelo link acima e pôr o link da página no README (linha "Jogar").

## Textos da página (#62)

**Short description**

> Monte a crew, escolha o alvo e decida: mais uma porta ou meter o pé com o que já pegou?

**Descrição**

> **Nova Guanabara, 1992.** A firma caiu tentando o Banco Central e o Delegado Bastos levou todo mundo. Sobrou você, a oficina da Dona Cida e dois novatos.
>
> Monte a crew, escolha o alvo no mapa e assista ao golpe sala por sala, no corte do prédio. Cada ladrão tem a sua especialidade: o **Gazua** abre fechadura, o **Disquete** dá conta de câmera e computador, o **171** conversa com quem estiver no caminho e o **Pé-na-Tábua** tira todo mundo de lá antes da polícia chegar.
>
> Entre uma sala e outra, a decisão é sua: **abrir mais uma porta ou meter o pé com o que já pegou?** Se a sirene tocar, é contagem regressiva.
>
> O loot vira upgrade no esconderijo, gente melhor nos contatos e golpes maiores, da Lotérica do Seu Zé até o Banco Central.

**Como jogar**

- Tudo é no **mouse**: abas da pasta no esconderijo, mapa, planejamento e as escolhas do golpe.
- **Esc** pausa o golpe (a contagem da polícia para junto). Na pausa tem Opções e "Meter o pé".
- O botão **1x** troca pra **2x** e acelera o golpe. **Meter o pé** foge na hora com o que estiver na sacola.
- Cada ladrão tem uma **habilidade** por golpe, no botão embaixo do retrato.

**Dicas**

- Especialista na sala certa trabalha mais rápido.
- Alerta cheio liga a sirene. Antes dela a fuga é limpa; com a contagem rolando a fuga é apressada, e se a polícia chegar alguém pode ser preso.
- Quem não vai pro golpe pode ir pro **bico** e render dinheiro parado.
- Ladrão preso sai sozinho com o tempo, ou na hora pagando a fiança.
- O jogo salva sozinho no navegador.

**Créditos**

> **Equipe**
> Leonardo de Souza Lima e Silva · Motor do Golpe, Crew e Core · Pé-na-Tábua
> Nicolas Volf · Economia, Progressão e Narrativa · 171
> Enzo Canavero · Interface e Áudio · Disquete
> Miguel Damásio · Cena do Golpe · Gazua
>
> **Música** (OpenGameArt, CC0)
> 8bit Bossa, Joth · Two Left Socks, congusbongus · Heavy Heist, bertsz · Chase - Diamond Dust, NBJDLukasAbsolute
>
> **Sons**
> Car engine start 01, looneybits (CC0) · Car Tire Skid Squealing, Mike Koenig (Soundbible), editado por qubodup (CC-BY 3.0). Os outros sons foram sintetizados por script pela equipe.
>
> **Arte**
> Ladrões, salas, prédios, esconderijo, mapa e fonte pixel desenhados por script pela equipe. Packs da CraftPix.net (CraftPix License) como referência.
>
> **Ferramentas**
> Unity 6 (URP 2D, UI Toolkit). Claude (Anthropic) escreveu os scripts que desenham a arte pixel a pixel e sintetizam os sons, o tema e as telas da UI.
>
> Feito pro Projeto 2D de Jogos Digitais e Interação, Insper, 2026.

A lista completa é a seção Créditos do README. A tela de Créditos do jogo (`UI/Resources/Creditos.txt`) tem um teste que confere os nomes com o README. O itch não tem teste, então confira na mão antes de publicar (#63).

## Imagens

- **Capa** (630 × 500): print do esconderijo ou do golpe com a sirene. Pedir o GIF/print final ao Miguel (#76).
- **Screenshots** (`docs/img`): `ui_menu.png`, `ui_esconderijo.png`, `ui_mapa.png`, `ui_planejamento.png`, `ui_hud.png`, `ui_hud_evento.png`, `ui_jornal.png`, `ui_bip.png`.

## Antes de publicar

- [ ] Jogar a build no Chrome e em outro navegador (Firefox ou Safari), em 1280×720 e no botão de tela cheia
- [ ] Save sobrevive a recarregar a página
- [ ] Créditos da página batendo com o README
- [ ] Link da página no README
