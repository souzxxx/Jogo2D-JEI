"""Gera a cena do bico Bilhete premiado (ESCOPO 6.7): 96x64, 8 quadros a 8 fps e a cena vazia.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/171/Art/Source~/gerar_bilhete.py"
Precisa de Pillow. Usa a paleta da Cena do Golpe, o boneco do 171 (gerar_171.py) e o boneco dos NPCs (boneco.py).
Saída: Assets/_Project/Thieves/171/Bico/bilhete-premiado_0..7.png e bilhete-premiado_vazio.png (e a prévia nesta pasta).
Depois, no Unity: A Firma › Bicos › Ligar as cenas dos bicos.

Com gente: de dia, na calçada de pedra portuguesa do Centro, na porta da lotérica fechada (a desculpa perfeita pra
"não poder descontar"), o 171 abana o bilhete brilhando pro otário de terno, que se debruça, desconfia e acaba
puxando a nota da carteira. Vazia: o caixote dele com o papelão "VOLTO JÁ" e a calçada sem ninguém.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parents[1] / "Bico"
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, shade, draw_text, text_width, rgba  # noqa: E402
import boneco  # noqa: E402
import gerar_171  # noqa: E402

W, H = 96, 64
FRAMES = 8
ID = "bilhete-premiado"

WALK_Y = 9                   # topo da calçada
SHOP_X0, SHOP_X1 = 34, 93    # fachada da lotérica

# O otário: terno cinza, gravata vermelha, óculos e pasta de executivo.
MARK = boneco.spec(skin="skin", hair="umber", hair_style="short", shirt="pale", jacket="steel", tie="red",
                   pants="slate", shoes="ink", glasses=True)


def as_px(image):
    """O 171 sai como imagem PIL (y pra baixo); embrulha numa Px pra colar na cena."""
    px = Px(image.width, image.height)
    px.img = image.copy()
    px.px = px.img.load()
    return px


def sky_and_city(px):
    """Céu de dia e os prédios do Centro ao fundo, com janela refletindo o céu."""
    px.vgradient(0, WALK_Y + 1, W - 1, H - 1, "cyan", "blue")
    for x0, x1, top, wall in ((0, 13, 58, "mist"), (12, 33, 63, "steel"), (88, 95, 60, "mist")):
        px.rect(x0, WALK_Y + 1, x1, top, wall)
        px.hline(x0, x1, top, shade(wall, 1))
        for wx in range(x0 + 2, x1 - 1, 4):
            for wy in range(top - 4, WALK_Y + 4, -5):
                glass = "cyan" if (wx * 5 + wy) % 7 == 0 else shade(wall, -1)
                px.rect(wx, wy - 2, wx + 1, wy, glass)
    # nuvenzinha
    for (x, y, r) in ((70, 57, 3), (74, 58, 4), (79, 57, 3)):
        px.disc(x, y, r, "pale")
    px.hline(66, 83, 55, "mist")


def lottery(px, f=None):
    """A lotérica fechada atrás deles: fachada verde-menta, a placa e a porta de aço baixada com o 'FECHADO'."""
    top = 52
    px.rect(SHOP_X0, WALK_Y + 1, SHOP_X1, top, "mint")
    px.frame(SHOP_X0, WALK_Y + 1, SHOP_X1, top, "green")
    # placa
    text = "LOTERIAS"
    w = text_width(text)
    x0 = (SHOP_X0 + SHOP_X1) // 2 - w // 2 - 3
    px.rect(x0, top - 10, x0 + w + 5, top - 2, "green")
    px.frame(x0, top - 10, x0 + w + 5, top - 2, "deepgreen")
    draw_text(px, x0 + 3, top - 8, text, "lamp")
    # porta de aço baixada
    d0, d1 = SHOP_X0 + 5, SHOP_X1 - 5
    px.rect(d0, WALK_Y + 1, d1, top - 13, "mist")
    for y in range(WALK_Y + 2, top - 13, 2):
        px.hline(d0, d1, y, "steel")
    px.frame(d0, WALK_Y + 1, d1, top - 13, "slate")
    # papel "FECHADO" colado na porta
    sign = "FECHADO"
    sw = text_width(sign)
    sx = SHOP_X1 - 4 - sw                                  # canto direito, longe da conversa
    px.rect(sx - 2, 15, sx + sw + 1, 23, "cream")
    px.frame(sx - 2, 15, sx + sw + 1, 23, "kraft")
    draw_text(px, sx, 17, sign, "red")


def sidewalk(px):
    """Pedra portuguesa em ondas (creme e preto), meio-fio e um pedaço do asfalto."""
    px.rect(0, 0, W - 1, 2, "night")                         # asfalto
    px.hline(0, W - 1, 3, "steel")                           # meio-fio
    for y in range(4, WALK_Y + 1):
        for x in range(W):
            wave = (x + 2 * ((y // 2) % 3)) % 12
            px.dot(x, y, "ink" if wave in (0, 1, 2) else "pale")
    px.hline(0, W - 1, WALK_Y, "mist")


def ticket(px, x, y, shine):
    """O bilhete 'premiado': papel creme com a faixa verde e os números; brilha quando ele abana."""
    px.rect(x, y, x + 5, y + 3, "paper")
    px.hline(x, x + 5, y + 3, "money")
    px.dot(x + 1, y + 1, "ink")
    px.dot(x + 3, y + 1, "ink")
    px.frame(x - 1, y - 1, x + 6, y + 4, "ink")
    if shine:
        for (dx, dy, c) in ((7, 5, "lamp"), (8, 6, "cream"), (-2, 5, "lamp"), (3, 6, "gold")):
            px.dot(x + dx, y + dy, c)


def bill(px, x, y):
    """A nota de Cr$ que o otário puxa."""
    px.rect(x, y, x + 5, y + 2, "money")
    px.frame(x - 1, y - 1, x + 6, y + 3, "ink")
    px.dot(x + 2, y + 1, "mint")


def briefcase(px, x, y):
    px.rect(x, y, x + 5, y + 4, "wood")
    px.frame(x - 1, y - 1, x + 6, y + 5, "ink")
    px.hline(x + 2, x + 3, y + 5, "ink")                     # alça
    px.dot(x + 2, y + 2, "gold")


def con_man(f):
    """O 171 gesticulando com o bilhete na mão da frente, boca abrindo e fechando, balão de conversa."""
    high = f % 2 == 0
    hand = (21, 10) if high else (22, 14)
    img = gerar_171.frame(front_hand=hand, back_hand=(14, 19), mouth_open=high, talk=1 + f % 3,
                          body_dy=1 if f in (3, 4) else 0)
    # mão no canvas do 171 (y pra baixo, offset 1) -> coordenada y pra cima dentro da célula
    return as_px(img), (hand[0], 31 - (hand[1] + 1 + (1 if f in (3, 4) else 0)))


def mark(f):
    """O otário: desconfia (0-2), se debruça pro bilhete (3-5) e puxa a nota (6-7). Olhando pra esquerda."""
    if f < 3:
        body = boneco.frame(MARK, back_hand=(14, 9), front_hand=(16, 9), blink=f == 2)
        hand = None
    elif f < 6:
        body = boneco.frame(MARK, body_dy=-1, back_hand=(14, 9), front_hand=(19, 12), mouth_open=f == 4)
        hand = None
    else:
        body = boneco.frame(MARK, back_hand=(14, 9), front_hand=(21, 12), mouth_open=f == 7)
        hand = (21, 12)
    return body.flipped(), hand


def scene(f):
    px = Px(W, H)
    sky_and_city(px)
    lottery(px)
    sidewalk(px)

    cx, cy = 4, WALK_Y - 2                                  # 171 (olha pra direita)
    mx, my = 28, WALK_Y - 2                                 # otário (olha pra esquerda)
    man, hand = con_man(f)
    other, mark_hand = mark(f)
    px.paste(other, mx, my)
    briefcase(px, mx + (31 - 14) - 3, my + 3)               # pasta na mão de trás dele
    px.paste(man, cx, cy)
    ticket(px, cx + hand[0] + 1, cy + hand[1] - 1, shine=f in (0, 2, 4, 6))
    if mark_hand:
        bill(px, mx + (31 - mark_hand[0]) - 6, my + mark_hand[1] + 1)
    if f in (6, 7):                                         # balão com "$" em cima do otário: fisgou
        bx, by = mx + 6, my + 31
        px.rect(bx, by, bx + 8, by + 10, "cream")
        px.frame(bx, by, bx + 8, by + 10, "ink")
        px.dot(bx + 6, by - 1, "ink")
        draw_text(px, bx + 2, by + 2, "$", "money" if f == 6 else "green")
    return px


def empty():
    """Ninguém na calçada: o caixote do 171 com o papelão 'VOLTO JÁ' na porta da lotérica."""
    px = Px(W, H)
    sky_and_city(px)
    lottery(px)
    sidewalk(px)
    # caixote de feira
    bx, by = 24, WALK_Y - 1
    px.rect(bx, by, bx + 13, by + 9, "tan")
    px.frame(bx, by, bx + 13, by + 9, "wood")
    for y in (by + 3, by + 6):
        px.hline(bx + 1, bx + 12, y, "wood")
    sign = "VOLTO JÁ"
    w = text_width(sign)
    sx, sy = 16, by + 10
    px.rect(sx, sy, sx + w + 5, sy + 10, "kraft")
    px.frame(sx, sy, sx + w + 5, sy + 10, "wood")
    draw_text(px, sx + 3, sy + 3, sign, "umber")
    shade_layer = Px(W, H)
    shade_layer.rect(0, 0, W - 1, H - 1, "night", 150)
    px.img.alpha_composite(shade_layer.img)
    return px


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    frames = [scene(f) for f in range(FRAMES)]
    for i, px in enumerate(frames):
        px.img.save(OUT_DIR / f"{ID}_{i}.png")
    vazio = empty()
    vazio.img.save(OUT_DIR / f"{ID}_vazio.png")

    from PIL import Image
    strip = Image.new("RGBA", (W * (FRAMES + 1) + FRAMES * 2, H), rgba("ink"))
    for i, px in enumerate(frames + [vazio]):
        strip.alpha_composite(px.img, (i * (W + 2), 0))
    strip.resize((strip.width * 4, H * 4), Image.NEAREST).save(HERE / "previa_bilhete.png")
    print("ok:", OUT_DIR, FRAMES, "quadros + vazia")


if __name__ == "__main__":
    main()
