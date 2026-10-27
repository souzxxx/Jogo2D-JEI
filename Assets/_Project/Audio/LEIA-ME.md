# Audio

Áudio (Enzo): AudioService, músicas e SFX. Todo mundo toca som por `Services.Audio`, nunca por AudioSource solto.

## Como funciona

- `Runtime/AudioService.cs` substitui o mock no começo do jogo (`AudioInstaller`).
- **Música**: `Services.Audio.PlayMusic(MusicTrack.X)` faz crossfade de 1,5 s. O próprio serviço troca sozinho no golpe: começou → `Heist`, sirene → `Siren`, acabou → `Hideout`. Menu e esconderijo chamam a deles.
- **Camadas de tensão**: `Resources/Musica/Heist_1` (agudos: pratos e caixa) e `Heist_2` (graves: bumbo e baixo) entram quando o alerta passa de 35% e 70%. Saem da própria Heavy Heist (`Source~/camadas.sh`), então têm o mesmo tamanho e nunca saem do tempo. Sem esses arquivos, toca só a base.
- **Volumes**: canais `master`, `music` e `sfx`, salvos em PlayerPrefs (`afirma.volume.*`). Sem AudioMixer: o WebGL não tem mixer, então o volume vai direto nas fontes.

## Músicas (`Resources/Musica`, o nome do arquivo é o nome da faixa)

| Faixa | Música | Autor | Licença |
|---|---|---|---|
| Menu | [8bit Bossa](https://opengameart.org/content/bossa-nova) | Joth | CC0 |
| Hideout | [Two Left Socks](https://opengameart.org/content/two-left-socks) | congusbongus | CC0 |
| Heist | [Heavy Heist](https://opengameart.org/content/heavy-heist) (cortado o silêncio do fim) | bertsz | CC0 |
| Siren | [Chase - Diamond Dust](https://opengameart.org/node/181066) | NBJDLukasAbsolute | CC0 |

## Efeitos sonoros (`Resources/Sons`)

Todos sintetizados por `Source~/gerar_sfx.py` (só matemática, sem sample). Importam sem Normalize: o volume relativo vem do script.

| Som | Quando toca | Quem toca |
|---|---|---|
| UI_Click / UI_Hover | clique e mouse em cima de botão | `Ui.OnClick` |
| UI_Buy / UI_Error | compra feita / não deu (upgrade, bônus, fiança, contratar) | painéis da UI |
| UI_Bip | bip da Dona Cida no pager | `Notices` |
| UI_Typewriter | capa do jornal no fim do golpe | `HeistHud` |
| UI_Stamp | confirmou ação irreversível (carimbo) | `Ui.Confirm` |
| UI_Paper | abriu aba, janela, porta ou evento do golpe | `Ui.Modal`, `HideoutScreen`, `HeistHud` |
| Passos | crew entrando numa sala (`RoomEntered`) | `AudioService` |
| Alarme + Sirene | começou a contagem da polícia (`SirenStarted`) | `AudioService` |
| Viatura | ladrão preso (`ThiefJailed`), uma vez pra crew presa junta | `AudioService` |

Porta abrindo e grana na sacola são da Cena do Golpe (`HeistScene/Audio`); o som de cada habilidade fica com o dono do ladrão (`actionSfx`/`sfx`).

**Volumes**: as faixas importam com Normalize (pico em 0 dB) e ficam entre −14,5 e −16,5 LUFS no jogo. A Siren tinha −9 LUFS e gritava por cima de tudo: o arquivo está 4,5 dB mais baixo e importa sem Normalize (fica em −14, um pouco acima das outras de propósito). As camadas também importam sem Normalize, uns 6 dB abaixo da base.
