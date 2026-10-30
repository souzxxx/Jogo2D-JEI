"""Gera a cena do bico Lotação pirata (ESCOPO 6.7): 96x64, 8 quadros a 8 fps e a cena vazia.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/PeNaTabua/Art/Source~/gerar_lotacao.py"
Precisa de Pillow. Usa a paleta da Cena do Golpe e o boneco do Pé-na-Tábua (gerar_penatabua.py).
Saída: Assets/_Project/Thieves/PeNaTabua/Bico/lotacao-pirata_0..7.png e lotacao-pirata_vazio.png (e a prévia nesta pasta).
Depois, no Unity: A Firma › Bicos › Ligar as cenas dos bicos.

Com gente: a Kombi lotada no ponto, motor ligado (ela treme e solta fumaça), mala no bagageiro e o Pé-na-Tábua
de cobrador, pendurado na porta e gritando "CENTRO!". Vazia: a Kombi parada, porta fechada, "VOLTO JÁ" no vidro,
tudo apagado.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parents[1] / "Bico"
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, shade, draw_text, text_width, rgba  # noqa: E402
from gerar_penatabua import pe_frame  # noqa: E402

W, H = 96, 64
FRAMES = 8
ID = "lotacao-pirata"

# Kombi: traseira à esquerda, frente à direita (sai do ponto pra direita)
BODY_X0, BODY_X1 = 28, 86
WHEELS = (38, 76)
DOOR_X0, DOOR_X1 = 44, 59


def street(px):
    """Céu de fim de tarde, prédios do centro, calçada com o ponto e o asfalto."""
    px.vgradient(0, 13, W - 1, H - 1, "paper", "kraft")
    for x0, x1, top, wall in ((0, 17, 52, "wood"), (18, 33, 58, "brown"), (60, 79, 55, "wood"), (80, 95, 49, "brown")):
        px.rect(x0, 13, x1, top, wall)
        for wx in range(x0 + 2, x1 - 1, 4):
            for wy in range(top - 5, 16, -6):
                px.rect(wx, wy, wx + 1, wy + 2, "lamp" if (wx + wy) % 3 else shade(wall, -1))
    px.rect(0, 9, W - 1, 12, "mist")                      # calçada
    for x in range(3, W, 9):
        px.vline(x, 9, 12, "steel")
    px.hline(0, W - 1, 8, "steel")                        # meio-fio
    px.rect(0, 0, W - 1, 7, "dusk")                       # asfalto
    for x in range(0, W, 10):
        px.hline(x, x + 4, 3, "gold")
    # placa do ponto
    px.vline(12, 9, 41, "steel")
    px.vline(13, 9, 41, "slate")
    px.rect(1, 41, 33, 51, "blue")
    px.frame(1, 41, 33, 51, "police")
    draw_text(px, 3, 45, "PONTO", "cream")


def wheel(px, cx, cy):
    px.disc(cx, cy, 4, "ink")
    px.disc(cx, cy, 2, "slate")
    px.dot(cx, cy, "mist")


def kombi(px, dy, crowded, door_open):
    """Kombi saia-e-blusa (azul embaixo, creme em cima). dy é o tremido do motor ligado."""
    px.rect(BODY_X0, 6 + dy, BODY_X1, 20 + dy, "blue")
    px.rect(BODY_X0 + 1, 21 + dy, BODY_X1 - 1, 36 + dy, "cream")
    px.hline(BODY_X0 + 3, BODY_X1 - 3, 37 + dy, "cream")
    px.hline(BODY_X0, BODY_X1, 20 + dy, "police")         # friso
    px.hline(BODY_X0, BODY_X1, 6 + dy, shade("blue", -1))
    for cx in WHEELS:                                     # caixa de roda e a roda (a roda não treme)
        for x in range(cx - 6, cx + 7):
            for y in range(6 + dy, 13):
                if (x - cx) ** 2 + (y - 5) ** 2 <= 40:
                    px.dot(x, y, "night")
        wheel(px, cx, 5)
    # para-choques, farol, lanterna
    px.rect(BODY_X0 - 2, 7 + dy, BODY_X0, 9 + dy, "slate")
    px.rect(BODY_X1, 7 + dy, BODY_X1 + 2, 9 + dy, "slate")
    px.rect(BODY_X1 - 1, 14 + dy, BODY_X1, 16 + dy, "lamp")
    px.rect(BODY_X0, 14 + dy, BODY_X0 + 1, 17 + dy, "red")
    # janelas
    for x0, x1 in ((31, 40), (61, 69), (71, 78)):
        px.rect(x0, 25 + dy, x1, 33 + dy, "dusk")
        px.hline(x0, x1, 33 + dy, "steel")
    px.rect(80, 23 + dy, 84, 34 + dy, "cyan")             # para-brisa
    px.line(80, 34 + dy, 84, 30 + dy, "pale")
    if crowded:                                           # passageiros espremidos
        for i, x in enumerate((32, 36, 62, 66, 73)):
            hair = ("umber", "brown", "ink", "wood", "umber")[i]
            skin = "skin" if i % 2 else "skin2"
            hy = 26 + dy + (i % 2)
            px.rect(x, hy, x + 3, hy + 3, skin)
            px.rect(x, hy + 3, x + 3, hy + 4, hair)
    # porta corrediça
    if door_open:
        px.rect(DOOR_X0, 7 + dy, DOOR_X1, 34 + dy, "night")
        px.hline(DOOR_X0, DOOR_X1, 34 + dy, "slate")       # barra de segurar
        px.rect(DOOR_X0 + 1, 3 + dy, DOOR_X1 - 1, 5 + dy, "slate")  # degrau
    else:
        px.vline(DOOR_X0, 7 + dy, 35 + dy, shade("blue", -1))
        px.vline(DOOR_X1, 7 + dy, 35 + dy, shade("blue", -1))
        px.rect(DOOR_X0 + 2, 25 + dy, DOOR_X1 - 2, 33 + dy, "dusk")
    # bagageiro com as malas da freguesia
    px.hline(32, 58, 39 + dy, "slate")
    if crowded:
        px.rect(33, 40 + dy, 41, 44 + dy, "wood")
        px.rect(43, 40 + dy, 49, 43 + dy, "red")
        px.rect(51, 40 + dy, 56, 42 + dy, "green")
        px.hline(33, 41, 42 + dy, "tan")


def smoke(px, f):
    """Fumaça do escapamento saindo pra trás, em loop."""
    for k in range(3):
        t = (f + k * 3) % FRAMES
        x = BODY_X0 - 3 - t * 2
        y = 6 + t // 2
        r = 2 + t // 4
        px.disc(x, y, r, "pale" if t < 4 else "mist")
        px.dot(x - 1, y + r - 1, "cream")


def balloon(px, f):
    """ "CENTRO!" saindo da boca do cobrador; some nos dois últimos quadros (respira) e treme quando grita."""
    if f >= 6:
        return
    text = "CENTRO!"
    w = text_width(text)
    x0, y0 = W - w - 8 + (f % 2), 49 + (f % 2)
    px.rect(x0, y0, x0 + w + 4, y0 + 11, "cream")
    px.frame(x0, y0, x0 + w + 4, y0 + 11, "ink")
    px.line(x0 + 2, y0, DOOR_X1 - 2, 38, "ink")           # rabinho até a boca
    px.line(x0 + 3, y0, DOOR_X1 - 1, 38, "cream")
    draw_text(px, x0 + 3, y0 + 3, text, "red")


def frame(f):
    px = Px(W, H)
    street(px)
    dy = f % 2
    kombi(px, dy, crowded=True, door_open=True)
    shouting = f < 6
    cobrador = pe_frame(body_dy=0, front_hand=(24, 15 + (f % 2)), back_hand=(12, 26), mouth_open=shouting and f % 2 == 0)
    px.paste(cobrador, DOOR_X0 - 9, 4 + dy)
    smoke(px, f)
    balloon(px, f)
    return px


def empty():
    """Parada e apagada: porta fechada, ninguém dentro, papelão "VOLTO JÁ" no vidro."""
    px = Px(W, H)
    street(px)
    kombi(px, 0, crowded=False, door_open=False)
    sign = "VOLTO JÁ"
    w = text_width(sign)
    px.rect(31, 23, 31 + w + 5, 34, "kraft")
    px.frame(31, 23, 31 + w + 5, 34, "wood")
    draw_text(px, 34, 26, sign, "umber")
    # apaga tudo: meia-luz azulada por cima
    shade_layer = Px(W, H)
    shade_layer.rect(0, 0, W - 1, H - 1, "night", 150)
    px.img.alpha_composite(shade_layer.img)
    return px


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    frames = [frame(f) for f in range(FRAMES)]
    for i, px in enumerate(frames):
        px.img.save(OUT_DIR / f"{ID}_{i}.png")
    vazio = empty()
    vazio.img.save(OUT_DIR / f"{ID}_vazio.png")

    from PIL import Image
    strip = Image.new("RGBA", (W * (FRAMES + 1) + FRAMES * 2, H), rgba("ink"))
    for i, px in enumerate(frames + [vazio]):
        strip.alpha_composite(px.img, (i * (W + 2), 0))
    strip.resize((strip.width * 4, H * 4), Image.NEAREST).save(HERE / "previa_lotacao.png")
    print("ok:", OUT_DIR, FRAMES, "quadros + vazia")


if __name__ == "__main__":
    main()
