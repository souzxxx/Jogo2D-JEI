"""Gera a cena do bico Chaveiro 24h (ESCOPO 6.7): 96x64, 8 quadros a 8 fps e a cena vazia.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/Gazua/Art/Source~/gerar_chaveiro.py"
Precisa de Pillow. Usa a paleta da Cena do Golpe e o boneco do Gazua (gerar_gazua.py).
Saída: Assets/_Project/Thieves/Gazua/Bico/chaveiro-24h_0..7.png e chaveiro-24h_vazio.png (e a prévia nesta pasta).
Depois, no Unity: A Firma › Bicos › Ligar as cenas dos bicos.

Com gente: de noite, a banquinha acesa na esquina, com o quadro de chaves brilhando, o orelhão do lado (quem perdeu
a chave liga dali) e o Gazua no balcão copiando chave: o disco da máquina gira e solta faísca. Vazia: porta de aço
baixada, papelão "VOLTO JÁ" e a lâmpada apagada.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parents[1] / "Bico"
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, shade, draw_text, text_width, rgba  # noqa: E402
from gerar_gazua import gazua_frame  # noqa: E402

W, H = 96, 64
FRAMES = 8
ID = "chaveiro-24h"

# banquinha
K_X0, K_X1 = 26, 93          # caixa da banca
COUNTER_Y = 21               # tampo do balcão
ROOF_Y = 45                  # toldo
MACHINE_X = 66               # máquina de copiar chave, em cima do balcão
WHEEL = (MACHINE_X + 9, COUNTER_Y + 9)


def street(px):
    """Noite: céu azul-escuro com estrela, prédios apagados com uma janela ou outra, calçada e asfalto."""
    px.vgradient(0, 13, W - 1, H - 1, "dusk", "night")
    for x, y in ((6, 58), (19, 61), (47, 60), (88, 62), (71, 59)):
        px.dot(x, y, "pale")
    for x0, x1, top, wall in ((0, 14, 50, "night"), (15, 30, 56, "ink"), (78, 95, 53, "night")):
        px.rect(x0, 13, x1, top, wall)
        for wx in range(x0 + 2, x1 - 1, 4):
            for wy in range(top - 5, 16, -6):
                lit = (wx * 7 + wy * 3) % 11 == 0
                px.rect(wx, wy, wx + 1, wy + 2, "lamp" if lit else shade(wall, 1))
    px.rect(0, 9, W - 1, 12, "slate")                     # calçada
    for x in range(3, W, 9):
        px.vline(x, 9, 12, "dusk")
    px.hline(0, W - 1, 8, "steel")                        # meio-fio
    px.rect(0, 0, W - 1, 7, "night")                      # asfalto
    for x in range(0, W, 10):
        px.hline(x, x + 4, 3, "bronze")


def orelhao(px, lit):
    """Orelhão laranja da Telerj na calçada, à esquerda da banca."""
    px.vline(11, 9, 30, "slate")
    px.vline(12, 9, 30, "steel")
    shell = "coral" if lit else shade("coral", -2)
    for y in range(30, 43):                               # concha
        half = 7 - abs(y - 37) // 2
        px.hline(11 - half, 12 + half, y, shell)
    px.hline(5, 18, 30, shade(shell, -1))
    px.rect(9, 32, 14, 37, "pale" if lit else "slate")    # aparelho
    px.rect(10, 35, 13, 36, "ink")
    px.dot(14, 33, "ink")
    px.line(14, 33, 15, 31, "ink")                        # fio


def kiosk(px, lit):
    """Caixa da banca (verde-escuro), toldo listrado e a placa."""
    wall = "green" if lit else "deepgreen"
    px.rect(K_X0, 9, K_X1, ROOF_Y - 1, wall)
    px.frame(K_X0, 9, K_X1, ROOF_Y - 1, "ink")
    # vão do balcão (onde o Gazua atende)
    inside = "brown" if lit else "umber"
    px.rect(K_X0 + 3, COUNTER_Y + 1, K_X1 - 3, ROOF_Y - 3, inside)
    # frente do balcão com uma chave pintada
    px.rect(K_X0 + 1, 10, K_X1 - 1, COUNTER_Y - 1, shade(wall, -1))
    px.rect(K_X0 + 3, COUNTER_Y, K_X1 - 3, COUNTER_Y, "tan" if lit else "wood")
    px.hline(K_X0 + 3, K_X1 - 3, COUNTER_Y - 1, "ink")
    key = "gold" if lit else "bronze"
    px.ring(36, 15, 3, key, 2)
    px.rect(39, 14, 52, 16, key)
    px.rect(46, 11, 47, 13, key)
    px.rect(50, 12, 51, 13, key)
    # toldo listrado vermelho e creme
    for i, x in enumerate(range(K_X0 - 2, K_X1 + 3, 4)):
        c = ("red" if i % 2 == 0 else "cream") if lit else ("wine" if i % 2 == 0 else "mist")
        px.rect(x, ROOF_Y, x + 3, ROOF_Y + 4, c)
        px.dot(x + 1, ROOF_Y - 1, c)                      # franja
        px.dot(x + 2, ROOF_Y - 1, c)
    px.hline(K_X0 - 2, K_X1 + 2, ROOF_Y + 5, "ink")
    # placa
    text = "CHAVEIRO 24H"
    w = text_width(text)
    x0 = (K_X0 + K_X1) // 2 - w // 2 - 3
    px.rect(x0, ROOF_Y + 6, x0 + w + 5, ROOF_Y + 16, "lamp" if lit else "bronze")
    px.frame(x0, ROOF_Y + 6, x0 + w + 5, ROOF_Y + 16, "ink")
    draw_text(px, x0 + 3, ROOF_Y + 9, text, "ink" if lit else "umber")


def key_board(px, f, lit):
    """Quadro de chaves no fundo da banca, com uma chave piscando por quadro."""
    x0, x1, y0, y1 = K_X0 + 6, K_X0 + 30, COUNTER_Y + 4, ROOF_Y - 6
    px.rect(x0, y0, x1, y1, "kraft" if lit else "wood")
    px.frame(x0, y0, x1, y1, "umber")
    k = 0
    for row, y in enumerate(range(y1 - 3, y0 + 1, -5)):
        for x in range(x0 + 2, x1 - 1, 4):
            px.dot(x, y + 1, "umber")                     # preguinho
            color = ("gold", "mist", "lamp", "pale")[(x + row) % 4] if lit else "slate"
            px.vline(x, y - 2, y, color)
            px.dot(x - 1, y, color)
            if lit and k % 8 == f:
                px.dot(x, y - 1, "cream")                 # brilho
                px.dot(x + 1, y, "cream")
            k += 1


def lamp_glow(px):
    """Luz quente da lâmpada no fundo da banca: dois halos em volta dela, só dentro do vão."""
    for r, c in ((9, "wood"), (5, "tan")):
        for y in range(COUNTER_Y + 1, ROOF_Y - 2):
            for x in range(K_X0 + 3, K_X1 - 2):
                if (x - 60) ** 2 + (y - (ROOF_Y - 7)) ** 2 <= r * r + r * 0.6:
                    px.dot(x, y, c)


def lamp(px, lit):
    """Lâmpada pendurada no toldo."""
    px.vline(60, ROOF_Y - 5, ROOF_Y - 1, "ink")
    px.rect(59, ROOF_Y - 8, 61, ROOF_Y - 6, "lamp" if lit else "slate")
    if lit:
        px.dot(60, ROOF_Y - 7, "cream")


def machine(px, f):
    """Máquina de copiar chave no balcão: base, disco girando e a chave presa na morsa."""
    bx = MACHINE_X
    y = COUNTER_Y + 1
    px.rect(bx, y, bx + 14, y + 4, "slate")
    px.frame(bx, y, bx + 14, y + 4, "ink")
    px.rect(bx + 2, y + 5, bx + 5, y + 8, "steel")        # morsa
    px.rect(bx + 1, y + 7, bx + 7, y + 7, "gold")         # chave sendo copiada
    px.dot(bx + 8, y + 7, "gold")
    cx, cy = WHEEL
    px.disc(cx, cy, 4, "mist")
    px.ring(cx, cy, 4, "ink")
    spokes = [(1, 0), (1, 1), (0, 1), (-1, 1)]
    dx, dy = spokes[f % 4]
    px.line(cx - 3 * dx, cy - 3 * dy, cx + 3 * dx, cy + 3 * dy, "steel")
    px.dot(cx, cy, "ink")
    px.rect(cx + 4, y, cx + 5, cy, "slate")               # motor


def sparks(px, f):
    """Faísca saindo do disco pra direita e pra cima, em leque, mudando a cada quadro."""
    import math
    import random
    cx, cy = WHEEL
    r = random.Random(f * 31 + 7)
    for _ in range(9):
        ang = r.uniform(-0.5, 1.3)
        dist = r.uniform(5, 13)
        x = round(cx + math.cos(ang) * dist)
        y = round(cy + math.sin(ang) * dist * 0.8)
        c = r.choice(("lamp", "cream", "gold", "coral"))
        px.dot(x, y, c)
        if dist > 9:                                      # rastro
            px.dot(round(cx + math.cos(ang) * (dist - 1.5)), round(cy + math.sin(ang) * (dist - 1.5) * 0.8), "gold")
    px.dot(cx + 4, cy + 1, "cream")


def gazua(px, f):
    """O Gazua atrás do balcão, de frente pra máquina, mão indo e voltando na chave."""
    hand = ((21, 14), (22, 15), (22, 14), (21, 15))[f % 4]
    g = gazua_frame(front_hand=hand, back_hand=(18, 12), body_dy=-(f % 2 == 1), keys_swing=f % 3 - 1, blink=f == 5)
    px.paste(g, MACHINE_X - 24, COUNTER_Y - 13)
    # o balcão tapa as pernas: redesenha o tampo por cima
    px.rect(K_X0 + 3, COUNTER_Y, K_X1 - 3, COUNTER_Y, "tan")
    px.hline(K_X0 + 3, K_X1 - 3, COUNTER_Y - 1, "ink")


def frame(f):
    px = Px(W, H)
    street(px)
    orelhao(px, True)
    kiosk(px, True)
    lamp_glow(px)
    key_board(px, f, True)
    lamp(px, True)
    gazua(px, f)
    # tapa o que o boneco desenhou abaixo do balcão
    wall = "green"
    sub = Px(W, H)
    sub.rect(K_X0 + 1, 10, K_X1 - 1, COUNTER_Y - 2, shade(wall, -1))
    key = "gold"
    sub.ring(36, 15, 3, key, 2)
    sub.rect(39, 14, 52, 16, key)
    sub.rect(46, 11, 47, 13, key)
    sub.rect(50, 12, 51, 13, key)
    px.img.alpha_composite(sub.img)
    machine(px, f)
    sparks(px, f)
    return px


def empty():
    """Fechada: porta de aço baixada, papelão "VOLTO JÁ", lâmpada apagada, tudo na meia-luz."""
    px = Px(W, H)
    street(px)
    orelhao(px, False)
    kiosk(px, False)
    lamp(px, False)
    # porta de aço de enrolar fechando o vão do balcão
    x0, x1, y0, y1 = K_X0 + 3, K_X1 - 3, 10, ROOF_Y - 3
    px.rect(x0, y0, x1, y1, "steel")
    for y in range(y0 + 2, y1, 3):
        px.hline(x0, x1, y, "slate")
    px.rect((x0 + x1) // 2 - 3, y0 + 1, (x0 + x1) // 2 + 3, y0 + 2, "ink")   # puxador
    sign = "VOLTO JÁ"
    w = text_width(sign)
    sx = (x0 + x1) // 2 - w // 2 - 3
    px.rect(sx, 24, sx + w + 5, 35, "kraft")
    px.frame(sx, 24, sx + w + 5, 35, "wood")
    px.dot(sx + 1, 34, "pale")                            # durex
    px.dot(sx + w + 4, 34, "pale")
    draw_text(px, sx + 3, 27, sign, "umber")
    shade_layer = Px(W, H)
    shade_layer.rect(0, 0, W - 1, H - 1, "night", 140)
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
    strip.resize((strip.width * 4, H * 4), Image.NEAREST).save(HERE / "previa_chaveiro.png")
    print("ok:", OUT_DIR, FRAMES, "quadros + vazia")


if __name__ == "__main__":
    main()
