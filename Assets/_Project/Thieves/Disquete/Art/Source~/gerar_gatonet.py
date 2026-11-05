"""Gera a cena do bico Gatonet (ESCOPO 6.7): 96x64, 8 quadros a 8 fps e a cena vazia.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/Disquete/Art/Source~/gerar_gatonet.py"
Precisa de Pillow. Usa a paleta da Cena do Golpe e o boneco do Disquete (gerar_disquete.py).
Saída: Assets/_Project/Thieves/Disquete/Bico/gatonet_0..7.png e gatonet_vazio.png (e a prévia nesta pasta).
Depois, no Unity: A Firma › Bicos › Ligar as cenas dos bicos.

Com gente: de noite, o Disquete na escada encostada no poste puxa cabo do ninho de fios (que solta faísca) e as TVs
do prédio vão acendendo janela por janela. Vazia: a escada parada com o papelão "VOLTO JÁ", janelas apagadas.
"""
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parents[1] / "Bico"
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, draw_text, text_width, rgba  # noqa: E402
from gerar_disquete import disquete_frame  # noqa: E402

W, H = 96, 64
FRAMES = 8
ID = "gatonet"

POLE_X = 46
NEST = (47, 50)                                      # centro do ninho de fios
WINDOWS = [(x, y) for y in (13, 24, 35, 46) for x in (64, 75, 86)]
ORDER = random.Random(5).sample(range(len(WINDOWS)), len(WINDOWS))   # ordem em que as TVs acendem


def sag(px, x0, y0, x1, y1, drop, color):
    """Fio caído entre dois pontos (parábola)."""
    steps = max(1, abs(x1 - x0))
    for k in range(steps + 1):
        t = k / steps
        px.dot(round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t - drop * 4 * t * (1 - t)), color)


def street(px):
    """Céu de noite, prédio à direita, calçada e o poste com o ninho de fios."""
    px.vgradient(0, 7, W - 1, H - 1, "dusk", "night")
    for (x, y) in ((6, 58), (22, 61), (35, 55), (57, 60), (14, 50)):
        px.dot(x, y, "pale")
    px.disc(9, 44, 3, "pale")                                   # lua
    px.disc(10, 45, 2, "cream")
    px.rect(60, 7, W - 1, 57, "brown")                          # prédio
    px.hline(60, W - 1, 57, "wood")
    for (x, y) in WINDOWS:
        px.rect(x - 1, y - 1, x + 7, y + 7, "umber")
    px.rect(0, 0, W - 1, 6, "slate")                            # calçada
    px.hline(0, W - 1, 6, "mist")
    for x in range(4, W, 10):
        px.vline(x, 0, 5, "dusk")
    # poste de concreto com a cruzeta e os isoladores
    px.rect(POLE_X - 1, 6, POLE_X + 1, 62, "mist")
    px.vline(POLE_X + 1, 6, 62, "steel")
    px.rect(38, 58, 56, 59, "steel")
    for x in (39, 47, 55):
        px.rect(x, 60, x + 1, 61, "cream")
    # fios da rua e os puxados pro prédio, saindo do ninho
    sag(px, 0, 57, 39, 59, 3, "ink")
    sag(px, 56, 59, W - 1, 57, 2, "ink")
    for (wy, drop) in ((52, 3), (44, 4), (36, 5)):
        sag(px, NEST[0] + 3, NEST[1] - 1, 60, wy, drop, "ink")
    # o ninho: bolo de fio enrolado
    rnd = random.Random(2)
    for _ in range(26):
        x, y = NEST[0] + rnd.randint(-5, 5), NEST[1] + rnd.randint(-3, 3)
        px.line(x, y, x + rnd.randint(-3, 3), y + rnd.randint(-2, 2), rnd.choice(["ink", "night", "slate", "steel"]))


def ladder(px):
    """Escada de madeira encostada no poste."""
    x0, x1, y0, y1 = 28, POLE_X - 3, 6, 46
    for dx in (0, 4):
        px.line(x0 + dx, y0, x1 + dx - 4, y1, "wood")
    for y in range(y0 + 4, y1, 5):
        x = x0 + (x1 - 4 - x0) * (y - y0) / (y1 - y0)
        px.hline(round(x), round(x) + 4, y, "tan")


def window(px, x, y, lit, f):
    if not lit:
        px.rect(x, y, x + 6, y + 6, "night")
        return
    px.rect(x, y, x + 6, y + 6, "police")
    px.rect(x + 1, y + 1, x + 5, y + 4, "blue")                 # a luz da TV na sala
    px.rect(x + 2, y + 1, x + 4, y + 3, "cyan" if (x + y + f) % 3 else "pale")
    px.hline(x, x + 6, y + 6, "slate")                          # cortina


def frame(f):
    px = Px(W, H)
    street(px)
    lit = set(ORDER[:4 + f])                                    # uma TV nova a cada quadro
    for i, (x, y) in enumerate(WINDOWS):
        window(px, x, y, i in lit, f)
    ladder(px)
    pull = f % 2                                                # puxa o cabo e solta
    hand = (21, 22 - pull * 2)
    disquete = disquete_frame(body_dy=-pull, front_hand=hand, back_hand=(18, 14))
    px.paste(disquete, 19, 25)
    hx, hy = 19 + hand[0], 25 + hand[1] - pull
    px.line(hx, hy, NEST[0] - 2, NEST[1], "ink")                 # o cabo na mão dele
    sag(px, hx, hy, 32, 8, 2, "ink")                            # o resto do rolo caindo até o chão
    px.disc(32, 8, 2, "night")
    if f in (2, 5):                                             # faísca no ninho
        for (dx, dy, c) in ((-6, 3, "lamp"), (-7, 5, "gold"), (-4, 6, "lamp"), (-8, 2, "cream")):
            px.dot(NEST[0] + dx, NEST[1] + dy, c)
    return px


def empty():
    """Parada e apagada: a escada sozinha com o papelão "VOLTO JÁ", nenhuma TV acesa."""
    px = Px(W, H)
    street(px)
    for (x, y) in WINDOWS:
        window(px, x, y, False, 0)
    ladder(px)
    sign = "VOLTO JÁ"
    w = text_width(sign)
    px.rect(14, 17, 14 + w + 5, 28, "kraft")
    px.frame(14, 17, 14 + w + 5, 28, "wood")
    px.line(14 + w + 5, 28, 33, 24, "ink")                      # amarrado na escada
    draw_text(px, 17, 20, sign, "umber")
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
    strip.resize((strip.width * 4, H * 4), Image.NEAREST).save(HERE / "previa_gatonet.png")
    print("ok:", OUT_DIR, FRAMES, "quadros + vazia")


if __name__ == "__main__":
    main()
