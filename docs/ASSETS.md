# Assets e direção de arte

> Dono da direção de arte: **Miguel** (cenário) e **Enzo** (UI). Mudanças aqui são combinadas no grupo.

## Regras

1. **Fonte principal: [craftpix.net](https://craftpix.net)**, com acesso pela faculdade. Complementos gratuitos (itch.io, Kenney, OpenGameArt) só quando o craftpix não tiver.
2. **Antes de baixar um pack, coloque ele na tabela abaixo** com status "proposto". Assim ninguém mistura estilos.
3. Tudo vai pra `Assets/ThirdParty/<NomeDoPack>/`. Não espalhe asset de terceiro pelas pastas do projeto: crie prefabs/sprites derivados na sua pasta.
4. Todo pack usado entra nos **Créditos do README** no mesmo PR.
5. **IA é permitida como ferramenta** (ex.: modelar no Blender e renderizar como pixel art, gerar base de sprite, música). Registre a ferramenta nos créditos e o que foi feito com ela.
6. Recolorir pra paleta do jogo é obrigatório: o jogo tem que parecer um só, não colagem de packs.

## Padrões técnicos (pixel art)

| Item | Padrão |
|---|---|
| Pixels por unidade (PPU) | **32** |
| Tamanho base dos ladrões | 32×32 |
| Retrato da UI | 64×64 (foto 3x4 do "dossiê") |
| Import de sprite | Filter Mode **Point**, Compression **None**, PPU 32 |
| Câmera | Pixel Perfect Camera (URP), referência 640×360, upscale pra 1280×720 |
| Paleta | _a definir pelo Miguel na Sprint 0_ (colar a imagem da paleta aqui) |
| Fonte da UI | _a definir pelo Enzo na Sprint 0_ (pixel, legível, com acentos) |

## Packs do craftpix: NÃO vão pro GitHub

A licença do craftpix (<https://craftpix.net/file-licenses/>) permite usar no jogo, mas **não permite redistribuir os arquivos**, e o repositório é público. Por isso `Assets/ThirdParty/Craftpix/` está no `.gitignore`.

**Como pegar os packs:**
1. Baixe o `AFirma-ThirdParty-Craftpix.zip` do Drive do grupo.
2. Extraia **na raiz do repositório**. Ele já cria `Assets/ThirdParty/Craftpix/...`.
3. Abra o Unity.

O zip tem os arquivos `.meta`. **Não importe os packs por conta própria**: cada importação gera GUIDs novos, e prefab/cena que usar sprite do pack quebra na máquina dos outros.

**Pack novo?**
1. Combine no grupo.
2. Coloque em `Assets/ThirdParty/Craftpix/<Categoria>/<Pack>/` e abra o Unity uma vez pra gerar os `.meta`.
3. Gere o zip de novo e suba no Drive.
4. Adicione o pack na tabela abaixo e nos créditos do README.

## Packs

| Pack (craftpix.net) | ID | Pasta em `Assets/ThirdParty/Craftpix/` | Ideia de uso | Quem trouxe | Status |
|---|---|---|---|---|---|
| Bandits Pixel Art Character Sprite Sheets Pack | 979751 | `Personagens/Bandits` | base pros ladrões da crew | Leonardo | baixado |
| Gangster Pixel Character Sprite Sheets Pack 2 | 326415 | `Personagens/Gangsters2` | base pros ladrões / capangas do cassino | Leonardo | baixado |
| City Man Pixel Character Sprite Pack 2 | 389916 | `Personagens/CityMan2` | civis, clientes, Seu Zé | Leonardo | baixado |
| Free Homeless Character Sprite Sheets | 724171 | `Personagens/Homeless` | figurantes de rua, novato da Dona Cida | Leonardo | baixado |
| Police Character Sprites Pixel Art | 642886 | `Personagens/Police` | policiais e Delegado Bastos | Leonardo | baixado |
| SWAT 2D Pixel Art Character Sprite Pack | 879529 | `Personagens/SWAT` | polícia de choque (Procurado alto) | Leonardo | baixado |
| Free Soldier Sprite Sheets | 507107 | `Personagens/Soldiers` | seguranças do Banco Central | Leonardo | baixado |
| Urban Animals Sprite Sheet Pixel Art Pack | 222276 | `Personagens/UrbanAnimals` | ambiente (gato, cachorro, rato) | Leonardo | baixado |
| Free SWAT Dialog Portraits | 163543 | `Retratos/SWATPortraits` | retratos de diálogo / jornal | Leonardo | baixado |
| Free Pixel Art Street 2D Backgrounds | 891123 | `Cenario/StreetBackgrounds` | fundo parallax da cidade | Leonardo | baixado |
| Night City Street Game Background Tiles | 891154 | `Cenario/NightCityTiles` | paredes, janelas, decoração dos prédios e rua | Leonardo | baixado |
| Free Currency Loot Vector Icons | 482753 | `UI/CurrencyLootIcons` | ícones de dinheiro/loot na UI | Leonardo | baixado |

Status: proposto → baixado → aprovado (direção de arte) → em uso.

## Outros assets (licença livre, ficam no repositório)

| Asset | Fonte | Licença | Onde está | Quem trouxe |
|---|---|---|---|---|
| Car engine start 01 | looneybits, [OpenGameArt](https://opengameart.org/node/93658) | CC0 | `Thieves/PeNaTabua/Audio/SFX_PeNaTabua_Partida.wav` | Leonardo |
| Car Tire Skid Squealing (loop) | Mike Koenig (Soundbible), editado por qubodup, [OpenGameArt](https://opengameart.org/node/5634) | CC-BY 3.0 (crédito obrigatório) | `Thieves/PeNaTabua/Audio/SFX_CavaloDePau.wav` | Leonardo |
Importados com o padrão de pixel art (Point, sem compressão, PPU 32) pelo `PixelArtImporter`. Os sprite sheets ainda precisam ser fatiados (Sprite Mode Multiple) por quem for usar.

## Animações de cada ladrão (dono do ladrão faz)

| Animação | Frames sugeridos | Uso |
|---|---|---|
| idle | 2–4 | parado na sala / esconderijo |
| andar | 4–6 | indo pra próxima sala |
| trabalhar | 4–6 | resolvendo o obstáculo (cada tipo tem a sua) |
| comemorar | 2–4 | sala limpa / golpe concluído |
| retrato | 1 (64×64) | UI, ficha, jornal |
