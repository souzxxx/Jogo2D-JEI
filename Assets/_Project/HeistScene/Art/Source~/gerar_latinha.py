"""Gera a cena do bico Catar latinha (ESCOPO 6.7): 96x64, 8 quadros a 8 fps. Sem cena vazia: sempre tem gente.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/HeistScene/Art/Source~/gerar_latinha.py"
Precisa de Pillow. Usa a paleta e o boneco da Cena do Golpe.
Saída: Assets/_Project/Data/Economy/SideJobs/Bico/catar-latinha_0..7.png (ao lado do SideJob_CatarLatinha.asset,
onde o menu A Firma › Bicos › Ligar as cenas dos bicos procura) e a prévia nesta pasta.

Manhã cedo na calçada: um catador de boné empurra o carrinho de supermercado com o saco de latinhas transbordando.
Ele anda no lugar e a calçada corre por baixo (loop de 8 quadros), as rodinhas giram e as latinhas brilham.
"""
import math
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parents[2] / "Data/Economy/SideJobs/Bico"
sys.path.insert(0, str(HERE))

from paleta import Px, shade, rgba  # noqa: E402
from boneco import spec, frame as boneco_frame, WALK4  # noqa: E402

W, H = 96, 64
FRAMES = 8
ID = "catar-latinha"
SCROLL = 2               # px por quadro que a calçada corre (16 px em 8 quadros: fecha o loop)

CATADOR = spec(skin="skin2", hair="ink", hat="cap", hat_color="red", shirt="lamp", pants="wood", shoes="ink",
               belt=None, mustache=True)

CART_X0, CART_X1 = 46, 78    # cesta do carrinho
CART_Y0 = 10                 # fundo da cesta
CART_TOP = 22                # borda de cima da cesta
CANS = ("red", "blue", "money", "gold", "pale", "coral", "cyan")


def scenery(px, f):
    """Céu de manhã, prédios com varal e padaria, a calçada de pedra portuguesa correndo e o asfalto."""
    px.vgradient(0, 13, W - 1, H - 1, "paper", "cyan")
    px.disc(84, 54, 4, "lamp")                            # sol nascendo
    px.disc(84, 54, 2, "cream")
    for x0, x1, top, wall in ((0, 20, 50, "mist"), (21, 40, 44, "kraft"), (41, 66, 53, "pale"), (67, 95, 46, "tan")):
        px.rect(x0, 13, x1, top, wall)
        px.hline(x0, x1, top, shade(wall, -1))
        for wx in range(x0 + 3, x1 - 2, 6):
            for wy in range(top - 7, 20, -8):
                px.rect(wx, wy, wx + 2, wy + 3, "dusk")
                px.hline(wx, wx + 2, wy + 3, shade(wall, -2))
    # padaria no térreo do prédio claro
    px.rect(43, 13, 64, 20, "wine")
    px.rect(45, 21, 62, 24, "cream")
    from paleta import draw_small
    draw_small(px, 46, 21, "PAO", "wine")
    # calçada de pedra portuguesa (ondinhas pretas e brancas) correndo pra esquerda
    px.rect(0, 9, W - 1, 12, "cream")
    off = (f * SCROLL) % 16
    for x in range(-16, W + 16, 16):
        bx = x - off
        for i in range(8):
            y = 10 + round(math.sin((i / 8) * math.pi * 2) * 1.2)
            px.dot(bx + i * 2, y, "ink")
            px.dot(bx + i * 2 + 1, y, "ink")
    px.hline(0, W - 1, 8, "steel")                        # meio-fio
    px.rect(0, 0, W - 1, 7, "slate")                      # asfalto
    for x in range(-16, W + 16, 16):
        bx = x - off * 2 % 16                             # faixa corre mais rápido (mais perto)
        px.hline(bx, bx + 7, 3, "cream")


def wheel(px, cx, cy, f):
    px.disc(cx, cy, 2, "ink")
    px.dot(cx + (1 if f % 2 else -1), cy + (1 if f % 4 < 2 else -1), "steel")


def basket_left(y, bob):
    t = (y - CART_Y0 - bob) / max(CART_TOP - CART_Y0, 1)
    return CART_X0 + 3 - round(t * 3)


def can(px, x, y, c):
    """Latinha em pé: 2x3 com a tampa de alumínio."""
    px.rect(x, y, x + 1, y + 2, c)
    px.dot(x + 1, y + 1, shade(c, -1))
    px.hline(x, x + 1, y + 3, "pale")


def cart(px, f, bob):
    """Carrinho de supermercado de lado: chassi, rodinhas e a cesta cheia (o saco de latinhas por dentro)."""
    y0 = CART_Y0 + bob
    top = CART_TOP + bob
    px.line(CART_X0 + 3, y0 - 1, CART_X0 + 4, 4, "steel")
    px.line(CART_X1 - 2, y0 - 1, CART_X1 - 2, 4, "steel")
    px.hline(CART_X0 + 3, CART_X1 - 1, 6, "steel")
    wheel(px, CART_X0 + 4, 3, f)
    wheel(px, CART_X1 - 2, 3, f + 1)
    r = random.Random(7)
    for y in range(y0 + 1, top):
        px.hline(basket_left(y, bob) + 1, CART_X1 - 1, y, "pale")
    for y in range(y0 + 1, top - 2, 4):
        for x in range(basket_left(y, bob) + 2, CART_X1 - 2, 3):
            can(px, x + r.randint(0, 1), y, r.choice(CANS))
    for y in range(y0, top + 1):                          # grade de arame por cima
        px.dot(basket_left(y, bob), y, "mist")
        px.dot(CART_X1, y, "mist")
        if (y - y0) % 4 == 0:
            px.hline(basket_left(y, bob), CART_X1, y, "steel")
    for x in range(CART_X0 + 6, CART_X1, 5):
        px.vline(x, y0, top, "steel")
    px.hline(CART_X0, CART_X1, top, "mist")
    px.line(CART_X0, top, CART_X0 - 4, top + 1, "mist")   # alça, onde o catador segura
    px.rect(CART_X0 - 6, top + 1, CART_X0 - 4, top + 2, "red")


def sack(px, f, bob):
    """O saco transbordando por cima da cesta: monte de latinha, nó em cima, e algumas brilhando."""
    base = CART_TOP + bob
    x0, x1 = CART_X0 + 1, CART_X1 - 1
    cx = (x0 + x1) // 2
    heights = [round(13 * math.sin(math.pi * (x - x0 + 1) / (x1 - x0 + 2)) ** 0.4) for x in range(x0, x1 + 1)]
    for i, x in enumerate(range(x0, x1 + 1)):
        h = heights[i]
        if h <= 0:
            continue
        px.vline(x, base + 1, base + h, "pale")
        if x > cx + 4:
            px.vline(x, base + 1, base + max(1, h // 3), "mist")   # sombra do plástico
    r = random.Random(42)
    cans = []
    for i, x in enumerate(range(x0 + 1, x1 - 1, 3)):
        h = heights[i * 3 + 1]
        for y in range(base + 1, base + h - 4, 4):
            yy = y + r.randint(0, 1)
            can(px, x + r.randint(0, 1), yy, r.choice(CANS))
            cans.append((x, yy))
    for i, x in enumerate(range(x0, x1 + 1)):             # contorno de cima
        if heights[i] > 0:
            px.dot(x, base + heights[i], "steel")
    top = base + max(heights)
    px.rect(cx - 1, top, cx + 1, top + 2, "pale")         # nó
    px.dot(cx - 2, top + 3, "steel")
    px.dot(cx + 2, top + 3, "steel")
    for k in (f, f + 4):                                  # brilho: duas latinhas por quadro
        x, y = cans[(k * 5) % len(cans)]
        px.dot(x, y + 2, "cream")
        px.dot(x + 1, y + 3, "cream")
    sx = CART_X1 + 2 + (f % 4 == 1) - (f % 4 == 3)        # sacola azul amarrada na frente, balançando
    px.line(CART_X1, base - 1, sx + 2, base - 4, "steel")
    px.rect(sx, base - 12, sx + 6, base - 4, "blue")
    px.rect(sx + 1, base - 11, sx + 5, base - 5, "police")
    px.dot(sx + 2, base - 8, "red")
    px.dot(sx + 4, base - 7, "gold")


def catador(px, f):
    """Anda no lugar empurrando: as duas mãos na alça."""
    b, fr, _, _ = WALK4[f % 4]
    bob = 1 if f % 2 else 0
    hand_y = CART_TOP + 1 - 3 - bob
    g = boneco_frame(CATADOR, body_dy=-bob, legs=(b, fr), back_hand=(19, 18 - bob), front_hand=(21, 19 - bob),
                     blink=f == 6)
    px.paste(g, CART_X0 - 27, 3)
    return hand_y


def frame(f):
    px = Px(W, H)
    scenery(px, f)
    bob = 1 if f % 4 in (1, 2) else 0                      # o carrinho sacoleja na calçada
    catador(px, f)
    cart(px, f, bob)
    sack(px, f, bob)
    return px


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    frames = [frame(f) for f in range(FRAMES)]
    for i, px in enumerate(frames):
        px.img.save(OUT_DIR / f"{ID}_{i}.png")

    from PIL import Image
    strip = Image.new("RGBA", (W * FRAMES + (FRAMES - 1) * 2, H), rgba("ink"))
    for i, px in enumerate(frames):
        strip.alpha_composite(px.img, (i * (W + 2), 0))
    strip.resize((strip.width * 4, H * 4), Image.NEAREST).save(HERE / "previa_latinha.png")
    print("ok:", OUT_DIR, FRAMES, "quadros")


if __name__ == "__main__":
    main()
