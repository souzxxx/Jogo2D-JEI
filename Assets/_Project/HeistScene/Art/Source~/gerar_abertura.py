"""Gera a arte da abertura no Banco Central (ESCOPO 5): o Delegado Bastos, as placas da porta e a foto do jornal.

Uso (da raiz do repositório):  python "Assets/_Project/HeistScene/Art/Source~/gerar_abertura.py" [--previas]
Precisa de Pillow. Saída em Assets/_Project/HeistScene/Art/Abertura/:
  Bastos.png              sheet 32x32 do delegado: idle (2), andar (4), apontar (2), algemar (2)
  Placa_meter_o_pe.png    placa verde "<- METER O PÉ", pendurada sobre a porta
  Placa_so_mais_uma.png   placa vermelha "SÓ MAIS UMA ->"
  Bastos_foto.png         foto P&B da capa do jornal: Bastos sorrindo na frente da firma algemada
"""
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

from boneco import CELL, frame, sheet, spec, WALK4  # noqa: E402
from paleta import P, Px, draw_text, shade, text_width  # noqa: E402

ART = HERE.parent / "Abertura"
PREVIEW = HERE / "previas" if "--previas" in sys.argv else None

# Delegado Bastos: chapéu, sobretudo bege, gravata vermelha, distintivo dourado, bigodão e óculos escuros.
BASTOS = spec(skin="skin", hair="ink", hat="fedora", hat_color="brown", shirt="pale", jacket="kraft", tie="red",
              pants="umber", shoes="ink", belt="umber", mustache=True, glasses=True, badge="gold")


def bastos_frames():
    idle = [frame(BASTOS), frame(BASTOS, blink=True)]
    walk = [frame(BASTOS, body_dy=1 if i in (1, 3) else 0, legs=(b, f), back_hand=bh, front_hand=fh)
            for i, (b, f, bh, fh) in enumerate(WALK4)]
    # apontando pra crew ("perdeu!") e algemando (mão na frente, algema brilhando)
    point = [frame(BASTOS, front_hand=(23, 16), mouth_open=True), frame(BASTOS, front_hand=(23, 17), mouth_open=False)]

    def cuffs(px, d):
        px.ring(23, 12 + d, 1, "mist")
        px.dot(23, 12 + d, "cream")
    cuff = [frame(BASTOS, front_hand=(21, 12), extra=cuffs), frame(BASTOS, body_dy=1, front_hand=(22, 12), extra=cuffs)]
    return {"idle": idle, "andar": walk, "apontar": point, "algemar": cuff}


def arrow(px, x, y, left, color):
    """Seta de 7 px de altura com a ponta em (x, y)."""
    d = 1 if left else -1
    for i in range(4):
        px.rect(x + d * i, y - i, x + d * i, y + i, color)
    px.rect(x + d * 3, y - 1, x + d * 9, y + 1, color)


def sign(text, bg, fg, left):
    """Placa pendurada por duas correntes, com o texto em escala 1 e uma seta."""
    tw = text_width(text)
    w, h = tw + 26, 17
    chain = 6
    px = Px(w, h + chain)
    for cx in (5, w - 6):
        for y in range(h, h + chain, 2):
            px.dot(cx, y, "steel")
            px.dot(cx, y + 1, "mist")
    px.rect(0, 0, w - 1, h - 1, bg)
    px.frame(0, 0, w - 1, h - 1, "ink")
    px.rect(1, h - 2, w - 2, h - 2, shade(bg, 1))
    px.rect(1, 1, w - 2, 1, shade(bg, -1))
    for (bx, by) in ((2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3)):
        px.dot(bx, by, shade(bg, -2))
    if left:
        arrow(px, 4, h // 2, True, fg)
        draw_text(px, 16, 6, text, fg, shadow=shade(bg, -2))
    else:
        draw_text(px, 6, 6, text, fg, shadow=shade(bg, -2))
        arrow(px, w - 5, h // 2, False, fg)
    return px


GRAYS = [P[c] for c in ("ink", "night", "dusk", "slate", "steel", "mist", "pale", "cream")]


def newspaper_photo():
    """Foto da capa: o Bastos em primeiro plano mostrando a algema, a firma algemada atrás na frente do Banco Central.
    P&B com retícula, pra cara de foto de jornal."""
    W, H = 64, 44
    s = Px(W, H)
    s.rect(0, 0, W - 1, H - 1, "mist")
    # fachada clara do Banco Central: colunas
    for cx in range(2, W, 12):
        s.rect(cx, 6, cx + 5, H - 1, "pale")
        s.rect(cx, 6, cx, H - 1, "steel")
    s.rect(0, 0, W - 1, 5, "slate")                                   # calçada
    # a firma antiga algemada, encostada na parede (do lado esquerdo)
    crew = [spec(skin="skin", hair="ink", hat="beanie", hat_color="ink", shirt="red", pants="night"),
            spec(skin="skin2", hair="brown", shirt="money", pants="dusk", glasses=True),
            spec(skin="skin", hair="brown", hat="cap", hat_color="police", shirt="lamp", pants="night"),
            spec(skin="skin2", hair="ink", shirt="cream", jacket="wine", pants="ink", mustache=True)]

    def cuffs(px, d):
        px.ring(16, 9 + d, 1, "pale")

    for i, c in enumerate(crew):
        f = frame(c, back_hand=(14, 9), front_hand=(15, 9), extra=cuffs, blink=i % 2 == 0)
        s.paste(f, -10 + i * 8, 2)
    # Bastos da cintura pra cima, em escala 2, sorrindo e levantando a algema
    big = frame(BASTOS, front_hand=(22, 19), mouth_open=True,
                extra=lambda px, d: (px.ring(22, 21 + d, 1, "cream"), px.ring(24, 21 + d, 1, "cream")))
    bimg = big.img.crop((8, 0, 28, 22)).resize((40, 44), Image.NEAREST)   # da cabeça até a cintura
    s.img.paste(bimg, (W - 42, 0), bimg)

    # P&B: luminância -> 8 cinzas da paleta, com retícula (dither ordenado)
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    out = Px(W, H)
    for y in range(H):
        for x in range(W):
            r, g, b, a = s.get(x, y)
            lum = ((0.3 * r + 0.59 * g + 0.11 * b) / 255.0) ** 0.8
            v = lum * (len(GRAYS) - 1) + (bayer[y % 4][x % 4] / 16.0 - 0.5) * 0.45
            rgb = GRAYS[max(0, min(len(GRAYS) - 1, int(round(v))))]
            out.img.putpixel((x, H - 1 - y), (rgb[0], rgb[1], rgb[2], 255))
    out.frame(0, 0, W - 1, H - 1, "ink")
    return out


def main():
    anims = bastos_frames()
    sh, order = sheet(anims, ["idle", "andar", "apontar", "algemar"])
    sh.save(ART / "Bastos.png", PREVIEW, 4)
    sign("METER O PÉ", "green", "cream", left=True).save(ART / "Placa_meter_o_pe.png", PREVIEW, 6)
    sign("SÓ MAIS UMA", "red", "cream", left=False).save(ART / "Placa_so_mais_uma.png", PREVIEW, 6)
    newspaper_photo().save(ART / "Bastos_foto.png", PREVIEW, 6)
    print("ok: abertura", [f"{n}({c})" for n, c in order])


if __name__ == "__main__":
    main()
