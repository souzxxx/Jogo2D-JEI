# Gazua

Gazua (Miguel): sprites, animações, retrato, habilidade, eventos, bico.

## Personalidade

Ex-chaveiro de plantão da Zona Norte. Abriu a primeira fechadura aos 9 anos, a do porquinho da irmã, e devolveu as moedas. Fala pouco e fala **com as fechaduras**, como se fossem gente: "minha filha", "teimosa", "essa é das boas". Anda com um molho de duzentas chaves e jura que sabe de cor de que porta é cada uma. Mania: lubrifica tudo com óleo de máquina de costura. Único medo: fechadura eletrônica de teclado ("não tem nem buraco pra conversar").

## O que tem aqui

| Asset | O que é |
|---|---|
| `ThiefType_Gazua` | especialista em Fechadura, 8 bordões, retrato, Animator e som de ação |
| `Ability_Gazua` (`PeDeCabraAbility`) | Pé-de-cabra: abre na marra a sala de Fechadura em que a crew está (limpa na hora), com +15 de alerta. Diz o motivo quando não dá: "Só serve em sala de Fechadura." / "Espera a crew entrar na sala." |
| `SideJob_Gazua` | Chaveiro 24h: rende o dobro com um Gazua |
| `Events/` | 3 eventos em que ele brilha: Tranca tetra (sala de Fechadura, Lotérica+), Cofrinho atrás do quadro (Joalheria+), Porta corta-fogo (Câmbio+) |
| `Scripts/` | `PeDeCabraAbility` (habilidade própria, só mexe no golpe pelo `IHeistControl`) |
| `Tests/EditMode` | Testes da habilidade: limpa e faz barulho, motivo fora de sala de Fechadura, na porta e no evento |
| `Icons/` | Ícone do Pé-de-cabra e do Chaveiro 24h, 32×32 |
| `Audio/` | `SFX_Gazua_Fechadura` (gazua nos pinos + estalo da tranca, toca quando ele começa a trabalhar) e `SFX_PeDeCabra` (madeira rachando + metal), sintetizados por `HeistScene/Audio/Source~/gerar_sons.py` |
| `VFX/VFX_PeDeCabra` | Faíscas, lascas de madeira, poeira e um "CREC!" de gibi. A Cena do Golpe instancia na sala quando a habilidade é usada |

## Arte

Arrombador de gorro de lã, lupa de relojoeiro no olho, bigodão grisalho, jaqueta de trabalho cinza-azulada (a `color` do tipo), cinto de ferramentas e o molho de chaves pendurado. De lado, olhando pra direita, como a crew anda no prédio em corte. No retrato ele encara a câmera com a lupa no olho.

| Asset | O que é |
|---|---|
| `Art/Gazua_sheet.png` | Sheet 32×32 fatiado em 20 sprites (`Gazua_sheet_<animação>_<n>`), pivô no pé (centro de baixo) |
| `Art/Gazua_retrato.png` | Retrato 64×64, foto 3x4 do dossiê |
| `Art/Gazua.controller` | Animator com os estados `idle` (padrão), `andar`, `trabalhar` e `comemorar` |
| `Art/Gazua_*.anim` | Um clipe por estado. `comemorar` não repete e para no último frame |

| Estado | Frames | FPS | O que faz |
|---|---|---|---|
| `idle` | 4 | 4 | Respira, o molho de chaves balança |
| `andar` | 6 | 10 | Passada com as chaves sacudindo |
| `trabalhar` | 6 | 8 | Ajoelhado na fechadura com gazua e tensor, faísca nos frames 2 e 5 |
| `comemorar` | 4 | 8 | Pula erguendo o molho de chaves |

**Pra mudar a arte:** edite e rode `python "Assets/_Project/Thieves/Gazua/Art/Source~/gerar_gazua.py"` na raiz do repositório (usa a paleta e o boneco de `HeistScene/Art/Source~`). Depois, no Unity: **A Firma > Cena do Golpe > Gerar tudo** refaz fatiamento, clipes e Animator sem perder referência.
