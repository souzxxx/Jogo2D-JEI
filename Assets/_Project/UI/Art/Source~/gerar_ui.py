"""Gera a arte das telas da UI pixel a pixel, na paleta da Cena do Golpe:

- Esconderijo.png (640x360): o porão da "Auto Elétrica da Cida", fundo do hub.
- Mapa.png (400x230): o mapa de papel de Nova Guanabara do painel Mapa (os pinos dos golpes a UI põe por cima).

Uso (da raiz do repositório):  python3 -B "Assets/_Project/UI/Art/Source~/gerar_ui.py"
Precisa de Pillow (pip install pillow). Saída em Assets/_Project/UI/Art (e prévias nesta pasta).
"""
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parent
sys.path.insert(0, str(HERE.parents[2] / "HeistScene/Art/Source~"))

from paleta import Px, draw_text, text_width  # noqa: E402


def esconderijo():
    W, H = 640, 360
    px = Px(W, H)
    rnd = random.Random(7)

    # parede de tijolo, mais escura nos cantos
    px.rect(0, 0, W - 1, H - 1, "umber")
    for row, y in enumerate(range(40, H, 9)):
        offset = 0 if row % 2 else 9
        for x in range(-offset, W, 18):
            c = rnd.choice(["brown", "brown", "brown", "wood"])
            px.rect(x + 1, y + 1, x + 16, y + 7, c)
            px.hline(x + 1, x + 16, y + 7, "wood" if c == "brown" else "tan")
    for x0, x1 in ((0, 90), (550, W - 1)):
        for x in range(x0, x1 + 1):
            t = (x1 - x) / (x1 - x0) if x0 == 0 else (x - x0) / (x1 - x0)
            for y in range(40, H):
                if px.get(x, y)[:3] != (42, 28, 26) and ((x * 7 + y * 3) % 16) < t * 12:
                    px.dot(x, y, "umber")

    # luz da lâmpada na parede
    cx, cy = 330, 292
    for y in range(120, H):
        for x in range(170, 490):
            d = ((x - cx) ** 2 + ((y - cy) * 1.4) ** 2) ** 0.5
            if d < 150 and ((x * 5 + y * 11) % 16) < (1 - d / 150) * 10 and px.get(x, y)[:3] == (75, 49, 40):
                px.dot(x, y, "wood")

    # janelinha no nível da rua (com grade)
    px.rect(36, 286, 124, 340, "wood")
    px.rect(40, 290, 120, 336, "night")
    px.vgradient(40, 290, 120, 306, "dusk", "night")
    px.rect(40, 290, 120, 293, "slate")                         # calçada
    px.disc(96, 318, 3, "lamp")
    for x in range(48, 120, 12):
        px.rect(x, 290, x + 1, 336, "slate")

    # quadro de cortiça com mapa, fotos e barbante
    px.rect(196, 146, 474, 284, "wood")
    px.dither(200, 150, 470, 280, "tan", "kraft", 0.35)
    px.rect(230, 170, 380, 264, "paper")
    px.frame(230, 170, 380, 264, "kraft")
    for x in (280, 330):
        px.vline(x, 171, 263, "kraft")
    px.hline(231, 379, 217, "kraft")
    px.rect(232, 172, 378, 190, "blue")                         # mar
    px.dither(232, 191, 378, 196, "paper", "blue", 0.5)
    for (x, y) in ((250, 240), (300, 236), (350, 244)):
        px.disc(x, y, 9, "green")
    for x in range(236, 378, 14):
        px.vline(x, 197, 262, "tan")
    photos = [(396, 236), (404, 182), (206, 236), (214, 172)]
    pins = [(262, 222), (318, 248), (352, 206), (300, 200)]
    for (fx, fy), (pxx, pyy) in zip(photos, pins):
        px.line(fx + 16, fy + 20, pxx, pyy, "red")
    for (fx, fy) in photos:
        px.rect(fx, fy, fx + 32, fy + 40, "cream")
        px.rect(fx + 3, fy + 10, fx + 29, fy + 37, "slate")
        px.disc(fx + 16, fy + 26, 6, "mist")
        px.rect(fx + 8, fy + 10, fx + 24, fy + 17, "mist")
        px.dot(fx + 16, fy + 39, "red")
    for (x, y) in pins:
        px.disc(x, y, 2, "red")

    # lâmpada pendurada
    px.vline(330, 300, H - 1, "ink")
    px.rect(326, 298, 334, 302, "slate")
    px.disc(330, 292, 6, "lamp")
    px.disc(328, 294, 2, "cream")

    # chão de cimento
    px.rect(0, 0, W - 1, 40, "dusk")
    px.dither(0, 0, W - 1, 40, "dusk", "slate", 0.25)
    px.hline(0, W - 1, 40, "night")
    for (x, y, r) in ((420, 18, 14), (180, 12, 9)):
        for yy in range(y - r // 2, y + r // 2 + 1):
            for xx in range(x - r, x + r + 1):
                if ((xx - x) / r) ** 2 + ((yy - y) / (r / 2)) ** 2 <= 1:
                    px.dot(xx, yy, "night")                      # mancha de óleo

    # bancada com bateria de carro, caixa de ferramentas e rádio
    px.rect(150, 96, 520, 106, "wood")
    px.hline(150, 520, 106, "tan")
    px.hline(150, 520, 96, "brown")
    for x in (158, 330, 506):
        px.rect(x, 24, x + 8, 95, "brown")
    px.rect(150, 46, 520, 50, "brown")
    px.rect(180, 107, 230, 134, "ink")                          # bateria
    px.rect(180, 130, 230, 134, "night")
    px.rect(188, 135, 194, 139, "red")
    px.rect(216, 135, 222, 139, "slate")
    draw_text(px, 192, 115, "12V", "lamp")
    px.rect(260, 107, 320, 128, "red")                          # caixa de ferramentas
    px.rect(260, 122, 320, 124, "wine")
    px.rect(282, 129, 298, 132, "slate")
    px.rect(400, 107, 452, 140, "wood")                         # rádio
    px.rect(404, 112, 428, 136, "brown")
    for y in range(114, 136, 3):
        px.hline(405, 427, y, "umber")
    px.rect(434, 126, 448, 134, "lamp")
    px.disc(441, 117, 3, "pale")
    px.line(448, 140, 466, 176, "steel")                        # antena

    # pilha de pneus (Auto Elétrica) e escada
    for (x, y) in ((22, 0), (22, 22), (22, 44), (70, 0)):
        px.rect(x, y + 2, x + 40, y + 20, "ink")
        px.rect(x + 2, y + 4, x + 38, y + 18, "night")
        px.rect(x + 12, y + 8, x + 28, y + 14, "ink")
        px.hline(x + 2, x + 38, y + 18, "dusk")
    for i in range(12):
        x0 = 548 + i * 8
        y0 = 40 + i * 26
        px.rect(x0, y0, W - 1, y0 + 6, "wood")
        px.hline(x0, W - 1, y0 + 6, "tan")
        px.rect(x0, y0 - 20, x0 + 1, y0, "brown")
    px.line(560, 80, W - 1, 340, "slate", 2)                     # corrimão

    # placa da oficina pendurada perto da escada
    px.rect(470, 300, 540, 330, "lamp")
    px.frame(470, 300, 540, 330, "bronze")
    draw_text(px, 476, 318, "AUTO", "umber")
    draw_text(px, 476, 306, "ELÉTRICA", "red")
    return px


def mapa():
    """Os pinos dos golpes ficam entre y = 90 e 160 (mapPosition), sempre em terra: o mar fica abaixo de y = 75."""
    import math
    W, H = 400, 230
    px = Px(W, H)
    rnd = random.Random(3)
    px.rect(0, 0, W - 1, H - 1, "paper")

    def coast(x):
        y = 32 + 9 * math.sin(x / 28) + x * 0.08
        return int(y if x < 378 else y + (x - 378) * 3)

    def is_land(x, y):
        return y > coast(x) and px.get(x, y)[:3] != (46, 106, 70)

    for x in range(W):                                           # mar com hachura de onda
        for y in range(0, coast(x)):
            px.dot(x, y, "blue" if (x + y * 3) % 23 else "cyan")
        px.dot(x, coast(x), "police")
    for (x, y, r) in ((36, 196, 18), (78, 204, 20), (340, 200, 18)):  # morros
        px.disc(x, y, r, "green")
        px.disc(x - r // 3, y + r // 3, r // 2, "money")
    for y in range(coast(0), 186, 14):                           # quarteirões
        for x in range(8, W - 8, 18):
            if all(is_land(xx, yy) for xx, yy in ((x, y), (x + 13, y), (x, y + 9), (x + 13, y + 9))):
                px.rect(x, y, x + 13, y + 9, rnd.choice(["cream", "cream", "pale"]))
    for (x0, y0, x1, y1, c, w) in ((0, 120, W - 1, 140, "tan", 3), (150, 0, 186, H - 1, "tan", 3),
                                   (0, 76, W - 1, 104, "kraft", 2)):  # avenidas, só em terra
        steps = max(abs(x1 - x0), abs(y1 - y0))
        for k in range(steps + 1):
            x, y = round(x0 + (x1 - x0) * k / steps), round(y0 + (y1 - y0) * k / steps)
            for dx in range(w):
                for dy in range(w):
                    if is_land(x + dx, y + dy):
                        px.dot(x + dx, y + dy, c)
    for x in (133, 266):                                         # dobras do papel
        px.vline(x, 0, H - 1, "kraft")
    px.hline(0, W - 1, 115, "kraft")
    for (x, label) in ((10, "ZONA NORTE"), (156, "CENTRO"), (300, "PORTO")):
        draw_text(px, x, coast(x + 20) + 8, label, "umber")
    title = "NOVA GUANABARA"
    tw = text_width(title)
    px.rect((W - tw) // 2 - 6, 202, (W + tw) // 2 + 6, 220, "cream")
    px.frame((W - tw) // 2 - 6, 202, (W + tw) // 2 + 6, 220, "wine")
    draw_text(px, (W - tw) // 2, 208, title, "wine")
    px.frame(0, 0, W - 1, H - 1, "umber")
    return px


def main():
    for name, img in (("Esconderijo", esconderijo()), ("Mapa", mapa())):
        img.save(OUT_DIR / f"{name}.png", HERE, 2)
        print("ok:", OUT_DIR / f"{name}.png", img.img.size)


if __name__ == "__main__":
    main()
