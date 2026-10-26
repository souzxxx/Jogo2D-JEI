# A Firma

> **Nova Guanabara, anos 90.** Você é o cérebro de uma firma de malandros. Monte a crew, escolha o alvo e assista ao golpe sala por sala. Entre uma porta e outra, a decisão é sua: **abrir mais uma ou meter o pé com o que já pegou?**

Idle de assalto 2D feito em Unity para o Projeto 2D de Jogos Digitais e Interação (Insper). Entrega: **11/11/2026**.

| | |
|---|---|
| 🎮 Jogar | _link do itch.io (em breve)_ — jam: <https://itch.io/jam/projeto-jogo-2d-insper-20262> |
| 📋 Board de tarefas | <https://github.com/users/souzxxx/projects/2> |
| 📄 Escopo | [docs/ESCOPO.md](docs/ESCOPO.md) |
| 📘 GDD | [docs/GDD.md](docs/GDD.md) · [Milanote](https://app.milanote.com/1Xl1js1SfE3bar/a-firma-gdd) |

## Equipe

| Pessoa | GitHub | Frente | Ladrão |
|---|---|---|---|
| Leonardo | [@souzxxx](https://github.com/souzxxx) | Motor do Golpe + Crew + Core | Pé-na-Tábua |
| Nicolas | [@NicolasVolf](https://github.com/NicolasVolf) | Economia + Progressão + Narrativa | 171 |
| Enzo | [@canavero](https://github.com/canavero) | Interface + Áudio | Disquete |
| Miguel | [@EuMiguelll](https://github.com/EuMiguelll) | Cena do Golpe | Gazua |

## Como abrir o projeto

1. No **Unity Hub**, instale o **Unity 6000.6.4f1** com o módulo **WebGL Build Support**. Tem que ser exatamente essa versão.
2. Clone o repositório:
   ```bash
   git clone https://github.com/souzxxx/Jogo2D-JEI.git
   ```
3. Configure o merge de cenas do Unity (uma vez por máquina): [docs/CONTRIBUINDO.md](docs/CONTRIBUINDO.md#configurar-o-smart-merge-uma-vez-por-máquina).
4. Baixe o `AFirma-ThirdParty-Craftpix.zip` do Drive do grupo e extraia **na raiz do repositório** (os packs do craftpix não vão pro GitHub: [docs/ASSETS.md](docs/ASSETS.md)).
5. Unity Hub → **Add → Add project from disk** → selecione a pasta clonada. A primeira abertura demora (importação).
6. Abra a sua sandbox (`Assets/_Project/Sandbox/<SeuNome>/`) e dê Play. **F1** abre o painel de debug.
7. Pra jogar o fluxo completo: abra `Assets/_Project/Scenes/Boot.unity` e dê Play.

## Documentação

| Documento | Pra quê |
|---|---|
| [ESCOPO.md](docs/ESCOPO.md) | O que é o jogo, regras, divisão, cronograma (fonte da verdade) |
| [ARQUITETURA.md](docs/ARQUITETURA.md) | Como o código é organizado e como cada um trabalha sem travar o outro |
| [CONTRIBUINDO.md](docs/CONTRIBUINDO.md) | Fluxo de branch/PR, regras do Unity, Definition of Done |
| [RUBRICA.md](docs/RUBRICA.md) | Dono e checklist de cada rubrica pra chegar no A |
| [GDD.md](docs/GDD.md) | Rascunho do GDD (DDE) que vai pro Milanote |
| [BALANCEAMENTO.md](docs/BALANCEAMENTO.md) | Metas, fórmulas e registro de ajustes |
| [ASSETS.md](docs/ASSETS.md) | Direção de arte, padrões de pixel art e lista de packs |

## Estrutura

```
Assets/_Project/
├─ Core/          contratos, eventos, save, mocks, debug, testes
├─ Heist/  Crew/  Economy/  Progression/  Narrative/  UI/  Audio/  HeistScene/
├─ Thieves/       PeNaTabua/  Gazua/  Disquete/  171/
├─ Data/          golpes, eventos, upgrades, config... (ScriptableObjects)
├─ Scenes/        Boot, MainMenu, Hideout, Heist
└─ Sandbox/       uma cena de testes por pessoa
Assets/ThirdParty/   packs de terceiros (craftpix etc.)
```

## Testes

**Window > General > Test Runner** → EditMode e PlayMode → **Run All**. Rode antes de abrir PR.

## Build

**A Firma > Build > WebGL** gera em `Builds/WebGL`. Zipe o conteúdo da pasta (com o `index.html` na raiz do zip) e suba no itch.io como jogo de navegador.

## Créditos

### Equipe
Leonardo de Souza Lima e Silva · Nicolas Volf · Enzo Canavero · Miguel Damásio

### Assets de terceiros
Os arquivos dos packs não estão no repositório (licença do craftpix não permite redistribuir). Lista completa em [docs/ASSETS.md](docs/ASSETS.md).

| Asset | Autor / Fonte | Licença |
|---|---|---|
| Bandits Pixel Art Character Sprite Sheets Pack | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Gangster Pixel Character Sprite Sheets Pack 2 | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| City Man Pixel Character Sprite Pack 2 | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Free Homeless Character Sprite Sheets | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Police Character Sprites Pixel Art | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| SWAT 2D Pixel Art Character Sprite Pack | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Free Soldier Sprite Sheets | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Urban Animals Sprite Sheet Pixel Art Pack | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Free SWAT Dialog Portraits | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Free Pixel Art Street 2D Backgrounds | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Night City Street Game Background Tiles | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Free Currency Loot Vector Icons | [CraftPix.net](https://craftpix.net) | [CraftPix License](https://craftpix.net/file-licenses/) |
| Car engine start 01 (som de ação do Pé-na-Tábua) | looneybits, [OpenGameArt](https://opengameart.org/node/93658) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) |
| Car Tire Skid Squealing (som do Cavalo de pau) | Mike Koenig ([Soundbible](http://soundbible.com/1178-Tires-Squealing.html)), editado por qubodup, [OpenGameArt](https://opengameart.org/node/5634) | [CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/) |

### Ferramentas de IA
| Ferramenta | Pra quê |
|---|---|
| Claude (Anthropic) | Escreveu o script Python que desenha pixel a pixel o sprite, as animações e o retrato do 171 (`Thieves/171/Art/Source~/gerar_171.py`) |
| Claude (Anthropic) | Escreveu os scripts Python que desenham pixel a pixel a arte da Cena do Golpe (salas, prédios, NPCs, ícones, fundo da cidade e fonte pixel: `HeistScene/Art/Source~`) e do Gazua (`Thieves/Gazua/Art/Source~`), e o que sintetiza os sons da cena e do Gazua (`HeistScene/Audio/Source~`) |

### Código de terceiros
| Trecho | Fonte | Onde está no projeto |
|---|---|---|
| _preencher se usar código com mais de 2–3 linhas de outra fonte_ | | |
