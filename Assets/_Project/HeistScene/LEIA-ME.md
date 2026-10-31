# HeistScene

Cena do Golpe (Miguel): salas, prédios, crew na cena, guardas, câmeras, luzes, VFX e câmera 2D. A cena **só escuta** os `GameEvents` do golpe e conta tudo no mundo; quem decide porta, evento e fuga é a HUD.

## Como funciona

A `HeistView` (cena `Scenes/Heist`) monta o prédio do golpe no `HeistStarted` e reage a cada evento:

| Evento | O que aparece na cena |
|---|---|
| `HeistStarted` | Prédio do golpe (`HeistDef.buildingPrefab`), van da firma estacionada e a crew descendo dela; luz ambiente do prédio |
| `RoomEntered` | Porta abre (com som), crew corre pra sala, sala acende (as outras ficam no escuro). Quem trabalha é o especialista do obstáculo; sem especialista, o melhor da crew vai com um aviso vermelho em cima da cabeça |
| `RoomProgress` | Anel de progresso em volta do ícone do obstáculo (cadeado, monitor, pessoa); a fechadura/cofre treme enquanto é trabalhado |
| `RoomCleared` | Grade abre, cofre abre, painel fica verde, a pessoa de posto fica distraída ("zZ"); notas voam até a sacola nas costas da crew, "+Cr$" sobe da sala e a crew comemora |
| `BagChanged` | A sacola cresce (pequena, média, grande) |
| `DoorDecisionRequested` | Crew vai pra porta, a porta pisca e a próxima sala aparece em penumbra com o ícone piscando (e o aviso de "sem especialista") |
| `HeistEventTriggered` / `Resolved` | "!?" em cima da crew; depois check ou X e o texto do resultado num balão. Atraso mostra um relógio; perder sacola derruba notas |
| `AlertChanged` | Guardas: calmos → "?" e lanterna amarela (35) → "!" e lanterna vermelha (70). Câmeras: cone verde → amarelo → vermelho. Giroflex vermelho girando a partir de 70. Vinheta fechando e luz do prédio esfriando |
| `SirenStarted` | Viatura chega com giroflex, luz vermelha/azul entra pela janela, tela treme, guardas correm e alguém da crew grita |
| `AbilityUsed` | VFX da habilidade (`ability.vfxPrefab`) na sala; sem VFX, o ícone da habilidade pula. Loop na câmera (Disquete) apaga as câmeras pelo tempo da habilidade |
| `ThiefBark` | Balão de fala com a fonte pixel do jogo |
| `HeistFinished` | Crew corre de volta pra van (preso fica algemado), a van arranca; no Cavalo de pau ela rodopia antes |

De vez em quando uma lotação passa na rua, atrás do prédio. Tudo respeita a pausa e a velocidade 1x/2x (`SceneClock`). Luzes de salas longe da câmera apagam (poupa o WebGL).

## Pastas

| Pasta | O que tem |
|---|---|
| `Runtime/` | `HeistView` (orquestra), `BuildingView`, `RoomView` e peças da sala (`ObstacleProp`, `ObstacleBadge`, `DoorView`, `AlarmLight`), `GuardView`, `SecurityCameraView`, `ThiefActor`, `VehicleView`, `HeistCameraRig`, `CityBackdrop`, `AlertAtmosphere`, fonte pixel (`PixelFont`, `PixelText`), efeitos (`PopIcon`, `FlyingLoot`, `FloatingText`, `SpeechBubble`) e a `HeistSceneFallbackHud` |
| `Editor/` | `HeistSceneBuilder`: importa a arte e gera prefabs, animações do Gazua e a cena |
| `Prefabs/Salas` | **Uma sala modular por `RoomKind`** (Entrada, Corredor, Balcão, Sala de Câmeras, Escritório, Cofre, Túnel, Garagem). Cada uma tem os 3 adereços (Fechadura, Sistema, Pessoa) e liga o da `RoomData.obstacle` |
| `Prefabs/Predios` | Os 5 prédios, montados com as salas modulares + casca própria (fachada, letreiro, rua). Já ligados no `buildingPrefab` de cada `HeistDef` |
| `Prefabs/Personagens`, `Veiculos` | Guarda (ronda ou posto), câmera de segurança, ladrão, van e viatura |
| `Prefabs/CameraPixelPerfect` | Câmera padrão do jogo pra qualquer cena: Pixel Perfect (PPU 32, 640×360), fundo noturno e pós-processamento |
| `Art/` | Toda a arte, gerada pixel a pixel na paleta (`Art/Paleta.png`). `Art/Source~` tem os scripts e o `layout.json`. `Art/Predios/Miniatura_<golpe>.png` (64×48) e `_sirene_0/_1` são as miniaturas pro mapa e pra faixa Golpes rolando (lista `thumbnails` do `layout.json`) |
| `Audio/` | Sons da cena (grana na sacola, porta), sintetizados por script |
| `HeistSceneArt.asset` | Referências que não são de um prédio: salas modulares, ícones, fonte, ladrão genérico, sons |

Se um golpe não tiver `buildingPrefab`, ou se as salas do `HeistDef` mudarem e o prefab ficar com outro número de salas, a cena monta o prédio sozinha com as salas modulares (`BuildingAssembler`): o golpe nunca fica sem cena.

## Mudou alguma coisa? Gere de novo

1. **Arte**: `python "Assets/_Project/HeistScene/Art/Source~/gerar_cena.py"` (precisa de `pip install pillow`). Lê as salas de `Data/Heists`, então mudou sala de golpe, rode de novo.
2. **Sons**: `python "Assets/_Project/HeistScene/Audio/Source~/gerar_sons.py"` (só Python padrão).
3. **Unity**: menu **A Firma > Cena do Golpe > Gerar tudo**. Refaz importação, prefabs e a cena sem quebrar referência.

Linha de comando: `Unity -batchmode -quit -projectPath . -executeMethod AFirma.HeistScene.EditorTools.HeistSceneBuilder.BuildAll`

## Pra quem for usar

- **Enzo (HUD):** ponha o prefab da HUD na cena `Heist` e **apague o GameObject "HUD provisória"** (é uma faixa OnGUI embaixo, só pra dar pra jogar). A cena não depende dela.
- **Dono de ladrão:** a cena usa o `ThiefTypeDef.animator` (estados `idle`, `andar`, `trabalhar`, `comemorar`). Sem animator, aparece o ladrão genérico com a roupa na `color` do tipo. O `actionSfx` toca quando o ladrão começa a trabalhar numa sala do tipo dele; o `ability.vfxPrefab` nasce na sala quando a habilidade é usada (exemplo: `Thieves/Gazua/VFX/VFX_PeDeCabra`).
- **Fonte pixel:** `Art/Fonte/FontePixel.asset` tem maiúscula, minúscula, número e acento. A UI pode usar (`PixelFont.Get`).
- **Paleta:** 32 cores em `Art/Paleta.png` e no `docs/ASSETS.md`.
