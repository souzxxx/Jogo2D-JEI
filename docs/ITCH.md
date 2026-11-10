# Página do itch.io

Textos e configuração da página do jogo (#13 e #62). Jam: <https://itch.io/jam/projeto-jogo-2d-insper-20262>.

**Publicada em 10/11/2026** na conta do Leonardo:

| | |
|---|---|
| Página | <https://souzxxx.itch.io/a-firma> |
| Inscrição na jam | <https://itch.io/jam/projeto-jogo-2d-insper-20262/rate/5300361> (curso: Ciência da Computação) |
| Build no ar | `a-firma-html.zip` no canal `html` (butler, 10/11 15:11). A build de teste de 04/11 ficou escondida ("Hide this file") |

## Configuração do projeto (#13)

| Campo | Valor |
|---|---|
| Title | A Firma |
| Project URL | `a-firma` |
| Kind of project | HTML |
| Release status | Released |
| Visibility | Public (desde 10/11) |
| Uploads | build WebGL no canal `html` pelo butler: `butler push Builds/WebGL souzxxx/a-firma:html --userversion 1.0` (o itch já marca pra rodar no navegador). Só uma build de navegador na página |
| Embed options | Viewport 1280 × 720 · Mobile friendly: não · Fullscreen button: sim · Scrollbars: não |
| Frame options | Click to launch in fullscreen: não · Automatically start on page load: sim |
| Genre | Strategy |
| Tags | idle, heist, pixel-art, management, push-your-luck, brazil, 90s, singleplayer, unity |
| Languages | Portuguese (Brazil) |
| Inputs | Mouse, Keyboard |
| Average session | About an hour (Metadata) |
| Engine | Unity (Metadata › Engines & tools) |
| AI disclosure | Sim: graphics, audio e code (os scripts que desenham a arte e sintetizam os sons, como nos créditos) |
| Pricing | No payments |
| Admins | Leonardo, Nicolas e Miguel (pedir o usuário do itch de cada um) |

Feito: inscrita na jam e link da página no README (linha "Jogar").

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
> O loot vira upgrade no esconderijo, gente melhor nos contatos e golpes maiores, da Lotérica do Seu Zé até o Banco Central, onde a firma caiu. **Voltar lá e fazer direito é a revanche.**
>
> Golpe que você já limpou na mão pode ir pro **automático**: a crew repete sozinha enquanto você planeja o próximo, e a faixa Golpes rolando mostra a sacola, o alerta e a sala de cada um. Os **bicos** rendem em cena no esconderijo, e a firma continua trabalhando com o jogo fechado.

**Como jogar**

- Tudo é no **mouse**: abas da pasta no esconderijo, mapa, planejamento e as escolhas do golpe.
- **Esc** pausa o golpe (a contagem da polícia para junto). Na pausa tem Opções e "Meter o pé".
- O botão **1x** troca pra **2x** e acelera o golpe. **Meter o pé** foge na hora com o que estiver na sacola.
- Cada ladrão tem uma **habilidade** por golpe, no botão embaixo do retrato.

**Dicas**

- Especialista na sala certa trabalha mais rápido.
- Alerta cheio liga a sirene. Antes dela a fuga é limpa; com a contagem rolando a fuga é apressada, e se a polícia chegar alguém pode ser preso.
- Quem não vai pro golpe pode ir pro **bico** e render dinheiro parado. Ladrão no bico do tipo dele rende o dobro.
- Golpe com o cofre limpo libera **Mandar no automático** no Planejar: "Na moita" mete o pé antes da sirene e é seguro; "Até o cofre" rende mais e pode dar cana. Com **Repetir**, a crew volta sozinha.
- Com o jogo fechado, os bicos e os golpes com Repetir continuam rendendo (até 4 h, a 50%).
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

- **Capa** (630 × 500): `docs/img/itch_capa.png`, o logo do menu em cima da Joalheria com a sirene tocando.
- **Screenshots na página** (8, de `docs/img`): `gif_sirene_cassino.gif`, `cena_cambio_porta.png`, `abertura_porta.png`, `ui_esconderijo.png`, `ui_golpes_rolando.png`, `esconderijo_vivo.png`, `ui_hud_evento.png`, `ui_jornal.png`.
- Outras opções em `docs/img`: `ui_menu.png`, `ui_bicos.png`, `ui_mapa.png`, `ui_planejamento.png`, `ui_hud.png`, `ui_abertura_jornal.png`, `ui_jornal_revanche.png`, `ui_bip.png`, `gif_abertura.gif`, `gif_golpe_loterica.gif`.

**Descrição na página:** os textos acima (descrição, como jogar, dicas e créditos) mais uma seção "O que tem no jogo" (5 golpes + 3 extras, 4 ladrões e 8 manhas, 28 eventos, automático e bicos, prestígio, save com backup) e o link do repositório.

## Antes de publicar

- [x] Jogar a build no Chrome, em 1280×720 (página do itch abre direto no menu)
- [ ] Jogar em outro navegador (Firefox ou Safari) e no botão de tela cheia
- [x] Save sobrevive a recarregar a página (build WebGL, 10/11; save corrompido cai no backup)
- [x] Créditos da página batendo com o README
- [x] Link da página no README
