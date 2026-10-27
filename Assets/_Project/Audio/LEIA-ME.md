# Audio

Áudio (Enzo): AudioService, músicas e SFX. Todo mundo toca som por `Services.Audio`, nunca por AudioSource solto.

## Como funciona

- `Runtime/AudioService.cs` substitui o mock no começo do jogo (`AudioInstaller`).
- **Música**: `Services.Audio.PlayMusic(MusicTrack.X)` faz crossfade de 1,5 s. O próprio serviço troca sozinho no golpe: começou → `Heist`, sirene → `Siren`, acabou → `Hideout`. Menu e esconderijo chamam a deles.
- **Camadas de tensão**: `Resources/Musica/Heist_1` e `Heist_2` (mesmo tamanho da `Heist`) entram quando o alerta passa de 35% e 70%. Sem esses arquivos, toca só a base.
- **Volumes**: canais `master`, `music` e `sfx`, salvos em PlayerPrefs (`afirma.volume.*`). Sem AudioMixer: o WebGL não tem mixer, então o volume vai direto nas fontes.

## Músicas (`Resources/Musica`, o nome do arquivo é o nome da faixa)

| Faixa | Música | Autor | Licença |
|---|---|---|---|
| Menu | [8bit Bossa](https://opengameart.org/content/bossa-nova) | Joth | CC0 |
| Hideout | [Two Left Socks](https://opengameart.org/content/two-left-socks) | congusbongus | CC0 |
| Heist | [Heavy Heist](https://opengameart.org/content/heavy-heist) (cortado o silêncio do fim) | bertsz | CC0 |
| Siren | [Chase - Diamond Dust](https://opengameart.org/node/181066) | NBJDLukasAbsolute | CC0 |
