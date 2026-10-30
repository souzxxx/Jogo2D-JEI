# Thieves

Uma pasta por ladrão, cada uma do dono dele (ver `docs/ARQUITETURA.md`). O que cada um entrega: sprite, animações, retrato, habilidade, 3 eventos em que ele brilha, bordões, SFX de ação e o bico, com a cena dele.

## Cena do bico (ESCOPO 6.7 e 9)

O cartão do bico vira uma vitrine animada. Todo mundo desenha igual:

| | Spec |
|---|---|
| Tamanho | **96×64 px**, PPU 32 (o `PixelArtImporter` já importa assim) |
| Quadros | **4 a 8**, em loop, a **~8 fps** (`sceneFps` no `SideJobDef`) |
| Paleta | a de 32 cores da Cena do Golpe (`HeistScene/Art/Source~/paleta.py`), gerada por script como o resto da arte |
| Ladrão | o **sprite 32×32 do tipo dele** (mesmo boneco do sheet), trabalhando na cena |
| Com gente | a cena viva, com o ladrão trabalhando. O "+Cr$" subindo é da UI, não desenhe |
| Vazia | **um quadro**, parado e apagado, com a placa "volto já" |

Arquivos, ao lado do `SideJob_X.asset`, na pasta `Bico/`:

```
Thieves/PeNaTabua/Bico/lotacao-pirata_0.png … lotacao-pirata_5.png   (quadros, o nome é o id do bico)
Thieves/PeNaTabua/Bico/lotacao-pirata_vazio.png                      (cena vazia)
```

Depois, no Unity, **A Firma › Bicos › Ligar as cenas dos bicos** preenche `sceneFrames` e `sceneEmpty` de todos os bicos. O `DataValidationTests` confere o tamanho, os quadros e a cena vazia.

Na UI, `SideJobDef.SceneFrameAt(segundos)` devolve o quadro da hora; sem cena (`HasScene` falso) a vitrine mostra o ícone.

| Bico | Cena | Dono |
|---|---|---|
| Lotação pirata | Kombi lotada saindo do ponto, cobrador pendurado na porta gritando "Centro! Centro!" | Leonardo |
| Chaveiro 24h | Banquinha com o quadro de chaves e a máquina de copiar soltando faísca | Miguel |
| Gatonet | Poste com um ninho de fios, o ladrão na escada puxando cabo, TVs acendendo nas janelas | Enzo |
| Bilhete premiado | Calçada do centro, o 171 abanando o bilhete pra um otário de terno | Nicolas |
| Catar latinha (`Data/Economy/SideJobs`) | Carrinho de supermercado com o saco de latinhas, sempre rodando (não tem cena vazia: sempre tem gente) | Miguel |
