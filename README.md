# A Firma

> **Nova Guanabara, anos 90.** Você é o cérebro de uma firma de malandros. Monte a crew, escolha o alvo e assista ao golpe sala por sala. Entre uma porta e outra, a decisão é sua: **abrir mais uma ou meter o pé com o que já pegou?**

Idle de assalto 2D feito em Unity para o Projeto 2D de Jogos Digitais e Interação (Insper). Entrega: **11/11/2026**.

| | |
|---|---|
| 🎮 Jogar | _link do itch.io (em breve)_ — jam: <https://itch.io/jam/projeto-jogo-2d-insper-20262> |
| 📋 Board de tarefas | <https://github.com/users/souzxxx/projects/2> |
| 📄 Escopo | [docs/ESCOPO.md](docs/ESCOPO.md) |
| 📘 GDD | [docs/GDD.md](docs/GDD.md) · Milanote: _link (em breve)_ |

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
4. Unity Hub → **Add → Add project from disk** → selecione a pasta clonada. A primeira abertura demora (importação).
5. Abra a sua sandbox (`Assets/_Project/Sandbox/<SeuNome>/`) e dê Play. **F1** abre o painel de debug.
6. Pra jogar o fluxo completo: abra `Assets/_Project/Scenes/Boot.unity` e dê Play.

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
| Asset | Autor / Fonte | Licença | Link | Usado em |
|---|---|---|---|---|
| _preencher a cada pack adicionado_ | | | | |

### Ferramentas de IA
| Ferramenta | Pra quê |
|---|---|
| _preencher_ | |

### Código de terceiros
| Trecho | Fonte | Onde está no projeto |
|---|---|---|
| _preencher se usar código com mais de 2–3 linhas de outra fonte_ | | |
