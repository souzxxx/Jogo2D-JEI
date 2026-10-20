# Arquitetura — A Firma

Guia técnico pra cada um trabalhar na sua frente **sem esperar ninguém**. Leia antes de escrever a primeira linha.

## Princípios

1. **As frentes só conversam pelo Core.** Ninguém referencia classe de outra frente. A UI não conhece o `HeistRunner`, só conhece `IHeistService` e os `GameEvents`.
2. **Tudo tem mock.** Cada serviço tem uma versão de mentira no Core que já funciona. Você desenvolve contra o mock e, quando a versão real chega, nada muda pro seu lado.
3. **Dados em ScriptableObject, não no código.** Golpe, sala, evento, upgrade, ladrão, número de balanceamento: tudo é asset editável no Inspector.
4. **Cada pessoa é dona das suas pastas e cenas.** Precisa mexer na pasta de outro? Fala com o dono ou abre issue.
5. **Código em inglês, texto do jogo em PT-BR.** Namespace `AFirma.<Frente>` (ex.: `AFirma.Heist`, `AFirma.UI`).

## Pastas e donos

```
Assets/
├─ _Project/
│  ├─ Core/            Fundação: contratos, eventos, save, mocks, debug, testes       (Leonardo)
│  ├─ Heist/           Motor do Golpe: HeistRunner : IHeistService                     (Leonardo)
│  ├─ Crew/            CrewService : ICrewService                                      (Leonardo)
│  ├─ Economy/         EconomyService : IEconomyService                                (Nicolas)
│  ├─ Progression/     ProgressionService : IProgressionService                        (Nicolas)
│  ├─ Narrative/       Bips da Dona Cida, briefings, manchetes                         (Nicolas)
│  ├─ UI/              Menus, telas, HUD, popups, tutorial                             (Enzo)
│  ├─ Audio/           AudioService : IAudioService, mixer, músicas, SFX               (Enzo)
│  ├─ HeistScene/      Salas, prédios, crew na cena, guardas, luzes, VFX, câmera       (Miguel)
│  ├─ Thieves/
│  │  ├─ PeNaTabua/    Pé-na-Tábua: arte, habilidade, eventos, bico                    (Leonardo)
│  │  ├─ Gazua/        Gazua                                                           (Miguel)
│  │  ├─ Disquete/     Disquete                                                        (Enzo)
│  │  └─ 171/          171                                                             (Nicolas)
│  ├─ Data/            Assets de dados (ver tabela abaixo)
│  ├─ Scenes/          Boot (Leonardo) · MainMenu, Hideout (Enzo) · Heist (Miguel)
│  └─ Sandbox/<Nome>/  Cena de testes de cada um — só o dono mexe
└─ ThirdParty/         Packs do craftpix e afins (listar em docs/ASSETS.md)
```

O arquivo `.github/CODEOWNERS` pede revisão automática do dono quando alguém mexe na pasta dele.

## Serviços

Acesso sempre por `Services.X`. Nunca dê `new` num serviço de outra frente.

| Serviço | Interface | Dono | Mock que já funciona | O que o mock **não** faz |
|---|---|---|---|---|
| Golpe | `IHeistService` (inclui `IHeistControl`) | Leonardo | `MockHeistService` | Regra de especialista, Discrição, manhas, testes de evento completos |
| Crew | `ICrewService` | Leonardo | `MockCrewService` | Efeitos completos das manhas, bônus de Fama no recrutamento |
| Economia | `IEconomyService` | Nicolas | `MockEconomyService` | Ganho offline, bônus de Fama |
| Progressão | `IProgressionService` | Nicolas | `MockProgressionService` | Sorteio de modificadores, prestígio |
| Áudio | `IAudioService` | Enzo | `MockAudioService` | Música de verdade, mixer, camadas |
| Save | `ISaveService` | Core | `PlayerPrefsSaveService` (real) | — |

Além disso:
- `Services.State` é o `GameState` atual (o que vai pro save).
- `Services.Db` é o `GameDatabase` (todos os dados).
- `Services.Config` é a `GameConfig` (números globais).

### Como plugar o seu serviço real

Crie a classe na sua pasta implementando a interface e registre antes da primeira cena:

```csharp
namespace AFirma.Economy
{
    public class EconomyService : IEconomyService, ITickable { /* ... */ }

    static class EconomyInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install() => Services.Register((IEconomyService)new EconomyService());
    }
}
```

Ordem de inicialização (automática, ninguém precisa chamar nada):

1. `SubsystemRegistration`: o Core zera tudo (`GameLoop.ResetStatics`).
2. `AfterAssembliesLoaded`: cada frente registra o seu serviço real.
3. `BeforeSceneLoad`: `Services.Initialize()` carrega dados e save, e completa com mock o que ninguém registrou. Depois cria o `[GameLoop]`.
4. Primeira cena carrega.

**Regras para o serviço real:**
- Leia e escreva o estado sempre em `Services.State` (nunca guarde cópia própria do que vai pro save).
- Precisa rodar todo frame? Implemente `ITickable`. O `GameLoop` chama `Tick(dt)` e para quando o jogo pausa.
- Mudou algo que outros mostram? Dispare o `GameEvents.RaiseX` correspondente.
- Tempo que corre com o jogo fechado (prisão, recrutas, offline) usa `GameClock.NowUnix`, não `Time.time`.

## Eventos (`GameEvents`)

Quem escuta assina no `OnEnable` e cancela no `OnDisable`. Quem dispara chama `GameEvents.RaiseX(...)`.

| Evento | Quem dispara | Quem costuma escutar |
|---|---|---|
| `GameLoaded`, `NewGameStarted`, `GameSaved`, `PauseChanged`, `Notice` | Core | UI |
| `MoneyChanged(atual, delta)` | Economia | UI (contador animado), Áudio |
| `UpgradePurchased`, `SideJobsChanged`, `OfflineEarnings` | Economia | UI, Narrativa |
| `HeistUnlocked`, `HeistModifiersRolled`, `Prestiged`, `FamePerkPurchased` | Progressão | UI, Narrativa |
| `CrewChanged`, `CandidatesChanged`, `ThiefJailed`, `ThiefReleased`, `ThiefLeveledUp` | Crew | UI, Narrativa |
| `HeistStarted(golpe)` | Golpe | Cena, HUD, Áudio |
| `RoomEntered(i, sala)`, `RoomProgress(i, 0..1)`, `RoomCleared(i, sala, loot)` | Golpe | Cena (andar, animar, notas voando), HUD |
| `AlertChanged(0..100)` | Golpe | Cena (luzes, guardas, vinheta), HUD, Áudio (camadas) |
| `BagChanged(valor)` | Golpe | HUD |
| `DoorDecisionRequested(decisão)` / `DoorDecisionMade(bool)` | Golpe | HUD (popup), Cena (porta) |
| `HeistEventTriggered(evento)` / `HeistEventResolved(resultado)` | Golpe | HUD (cartão), Cena |
| `AbilityUsed(ladrão, habilidade)` | Golpe | Cena (VFX), HUD, Áudio |
| `SirenStarted(segundos)`, `PoliceCountdownTick(segundos)` | Golpe | Cena (viatura), HUD (contagem), Áudio (sirene) |
| `ThiefBark(ladrão, fala)` | Golpe | Cena (balão) |
| `HeistFinished(resultado)` | Golpe | HUD (jornal), Narrativa, Áudio |
| `Bip(remetente, mensagem)`, `TipRequested(id)` | Narrativa | UI (bip, dicas) |

Precisa de um evento novo? Abra uma issue com a label `frente:core` descrevendo quem dispara e quem escuta. **Não crie eventos paralelos fora do `GameEvents`.**

## Ciclo de vida de um golpe

```
Planejamento (UI) ──SceneFlow.GoToHeist(golpe, crew)──▶ cena Heist carrega
HeistSceneBootstrap.Start() ──▶ Services.Heist.StartHeist(golpe, crew)
   HeistStarted
   loop por sala:
      RoomEntered ─▶ (talvez) HeistEventTriggered ─▶ espera ChooseEventOption ─▶ HeistEventResolved
      RoomProgress… / AlertChanged… (alerta 100 ─▶ SirenStarted ─▶ PoliceCountdownTick…)
      RoomCleared ─▶ BagChanged
      se não é a última: DoorDecisionRequested ─▶ espera ChooseDoor(true/false)
   HeistFinished(resultado) ─▶ jornal (UI) ─▶ SceneFlow.GoToHideout()
```

A HUD chama `ChooseDoor`, `ChooseEventOption`, `UseAbility`, `Flee` e `Speed`. A Cena do Golpe **só escuta**, nunca chama o serviço.

Abriu a cena `Heist` direto no editor? O `HeistSceneBootstrap` começa um golpe de teste (o `testHeist` dele, ou a Lotérica) com a crew disponível.

## Dados (ScriptableObjects)

Todos os tipos ficam em `Core/Data` e aparecem no menu **Create > A Firma**. Todo asset novo precisa entrar na lista certa do `Data/Resources/GameDatabase.asset`, senão o jogo não enxerga.

| Tipo | Onde ficam os assets | Dono do conteúdo |
|---|---|---|
| `GameConfig` (números globais) | `Data/Config` | Nicolas (balanceamento) |
| `HeistDef` (golpe + salas) | `Data/Heists` | Miguel (layout) + Nicolas (números) |
| `HeistEventDef` genéricos | `Data/Events` | Leonardo |
| `HeistEventDef` de cada ladrão | `Thieves/<Ladrão>/Events` | dono do ladrão |
| `ThiefTypeDef`, habilidade, bico do ladrão | `Thieves/<Ladrão>` | dono do ladrão |
| `TraitDef` (manhas), `NamePoolDef` | `Data/Crew` | Leonardo |
| `UpgradeDef`, `SideJobDef` (Catar latinha) | `Data/Economy` | Nicolas |
| `HeistModifierDef`, `FamePerkDef` | `Data/Progression` | Nicolas |
| `HeadlineSetDef` | `Data/Narrative` | Nicolas |

Os dados de exemplo foram gerados por **A Firma > Setup > Rodar setup da fundação**. Rodar de novo só cria o que estiver faltando, nunca sobrescreve.

### Habilidades dos ladrões
Cada ladrão aponta pra um `ThiefAbilityDef`. Hoje são `SimpleAbilityDef` configurados por dados. O dono do ladrão pode criar uma subclasse própria na pasta dele, pra adicionar efeito especial:

```csharp
[CreateAssetMenu(menuName = "A Firma/Habilidades/Loop na câmera")]
public class LoopNaCameraAbility : ThiefAbilityDef
{
    public float seconds = 15f;
    public override void Use(IHeistControl heist) => heist.FreezeAlert(seconds);
}
```

A habilidade só mexe no golpe através do `IHeistControl`. O som vai em `ability.sfx` e o efeito visual em `ability.vfxPrefab`: a Cena do Golpe instancia o prefab na sala atual quando escuta `AbilityUsed`. Assim o dono do ladrão entrega o efeito dele sem mexer na cena do Miguel.

### Manchetes do jornal
`Services.Db.headlines.Pick(resultado, nomeDoGolpe, rng)` devolve a manchete pronta (prisão tem prioridade; `{golpe}` e `{valor}` já preenchidos). O Nicolas escreve as manchetes no asset e o Enzo só chama o método.

## Cenas

| Cena | Dono | O que tem hoje |
|---|---|---|
| `Scenes/Boot` | Leonardo | `BootLoader`: vai pro menu |
| `Scenes/MainMenu` | Enzo | Placeholder OnGUI (Continuar / Novo jogo) |
| `Scenes/Hideout` | Enzo | Placeholder OnGUI (crew, golpes) |
| `Scenes/Heist` | Miguel | `HeistSceneBootstrap` + placeholder OnGUI. O HUD entra como **prefab do Enzo** |
| `Sandbox/<Nome>/Sandbox_<Nome>` | cada um | Vazia, pra testes livres |

- Navegação sempre por `SceneFlow` (`GoToMainMenu`, `GoToHideout`, `GoToHeist`), nunca `SceneManager` direto.
- Os placeholders (`ScenePlaceholder`) somem quando a tela real existir: o dono da cena apaga o GameObject.
- Pra testar o fluxo inteiro: abra `Boot` e dê Play.

## Save e estados

- `GameState` é serializado com `JsonUtility` no `PlayerPrefs`. No WebGL isso vira IndexedDB do navegador, então o save do itch é por navegador.
- Precisa salvar um campo novo? Adicione no `GameState`. Só use tipos que o `JsonUtility` aceita: sem `Dictionary` e sem classe `null` (use `bool hasX` + objeto vazio).
- Mudou o formato de algo existente? Suba `GameState.CurrentVersion` e trate a conversão em `PlayerPrefsSaveService.Migrate`.
- O autosave roda a cada `GameConfig.autosaveSeconds`, ao perder o foco e ao trocar de cena. `Services.SaveNow()` força um save.
- O golpe em andamento grava `hasActiveHeist`. Se o jogo fechar no meio, o `GameLoop` chama `RecoverInterruptedHeist()` ao abrir.

## Debug

- **F1** abre o painel de debug (só no editor e em Development Build). Dá pra:
  - dar dinheiro e Fama
  - liberar golpes e soltar presos
  - pausar, salvar e apagar o save
  - começar golpes, escolher portas e eventos, usar habilidades e mudar a velocidade
- `GameClock.UnixNowProvider` pode ser trocado em teste pra simular tempo passando.

## Testes

- Os testes ficam em `Core/Tests/EditMode`. Rodar em **Window > General > Test Runner > EditMode > Run All**.
- `DataValidationTests` checa o conteúdo: ids únicos, golpes terminando em cofre, eventos com pelo menos 2 escolhas. **Rode sempre que mexer em dado.**
- Quer testar o seu serviço? Crie `<SuaPasta>/Tests/EditMode` com um asmdef que referencia `AFirma.Core`. Nesse caso o seu código também precisa de um asmdef. Use `TestSupport.InitServicesWithMocks()` como base.

## Build

**A Firma > Build > WebGL** gera em `Builds/WebGL`. Zipe o **conteúdo** da pasta (com o `index.html` na raiz do zip) e suba no itch marcando "This file will be played in the browser". A compressão é Gzip com *decompression fallback*, que funciona no itch sem configurar servidor.

Linha de comando:
`Unity -batchmode -quit -projectPath . -executeMethod AFirma.EditorTools.BuildTools.BuildWebGL -buildPath Builds/WebGL`
