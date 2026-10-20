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

## Packs

| Pack | Fonte | Usado pra | Quem trouxe | Status |
|---|---|---|---|---|
| _ex.: Interior pixel art tileset_ | craftpix | salas dos prédios | Miguel | proposto |

Status: proposto → aprovado → em uso.

## Animações de cada ladrão (dono do ladrão faz)

| Animação | Frames sugeridos | Uso |
|---|---|---|
| idle | 2–4 | parado na sala / esconderijo |
| andar | 4–6 | indo pra próxima sala |
| trabalhar | 4–6 | resolvendo o obstáculo (cada tipo tem a sua) |
| comemorar | 2–4 | sala limpa / golpe concluído |
| retrato | 1 (64×64) | UI, ficha, jornal |
