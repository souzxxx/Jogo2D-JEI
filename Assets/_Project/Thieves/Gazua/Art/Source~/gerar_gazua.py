"""Gera a arte do Gazua pixel a pixel: sprite sheet 32x32 (idle, andar, trabalhar, comemorar), retrato 64x64
e os ícones do Pé-de-cabra e do Chaveiro 24h.

Uso (da raiz do repositório):  python "Assets/_Project/Thieves/Gazua/Art/Source~/gerar_gazua.py"
Precisa de Pillow (pip install pillow). Usa a paleta e o boneco da Cena do Golpe (HeistScene/Art/Source~).
Saída: Assets/_Project/Thieves/Gazua/Art/Gazua_sheet.png, Gazua_retrato.png e Icons/*.png (e prévias nesta pasta).
A pasta Source~ é ignorada pelo Unity.

O Gazua é o arrombador: gorro de lã, lupa de relojoeiro no olho, bigodão, jaqueta de trabalho cinza-azulada
(a cor do tipo), cinto de ferramentas e um molho de chaves que não larga. De lado, olhando pra direita.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parent
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, shade  # noqa: E402
from boneco import spec, frame, sheet, WALK6  # noqa: E402

GAZUA = spec(skin="skin2", hair="mist", hat="beanie", hat_color="slate", shirt="dusk", jacket="steel",
             pants="night", shoes="umber", belt="tan", mustache=True)


def details(px, d, keys_swing=0):
    """Lupa de relojoeiro, bigodão e molho de chaves no cinto (por cima do boneco base, antes do contorno)."""
    y = 18 + d
    px.rect(19, y + 3, 21, y + 5, "slate")      # lupa no olho
    px.dot(21, y + 4, "cyan")
    px.rect(17, y + 1, 19, y + 1, "steel")      # bigodão grisalho caindo no canto
    px.rect(13, y + 3, 14, y + 4, "mist")       # costeleta
    kx = 13 + keys_swing                        # molho de chaves pendurado no cinto, do lado
    px.dot(kx, 8 + d, "gold")
    px.dot(kx - 1, 7 + d, "gold")
    px.dot(kx + 1, 7 + d, "gold")
    px.dot(kx, 6 + d, "lamp")
    px.dot(kx - 1, 5 + d, "mist")


def gazua_frame(d_extra=None, keys_swing=0, **kw):
    def extra(px, d):
        details(px, d, keys_swing)
        if d_extra:
            d_extra(px, d)
    return frame(GAZUA, extra=extra, **kw)


def frames_idle():
    return [
        gazua_frame(),
        gazua_frame(keys_swing=1),
        gazua_frame(body_dy=-1, front_hand=(16, 10)),
        gazua_frame(body_dy=-1, front_hand=(16, 10), blink=True),
    ]


def frames_andar():
    out = []
    for i, (b, f, bh, fh) in enumerate(WALK6):
        out.append(gazua_frame(body_dy=1 if i in (1, 4) else 0, legs=(b, f), back_hand=bh, front_hand=fh,
                               keys_swing=(i % 3) - 1))
    return out


def frames_trabalhar():
    """Ajoelhado na fechadura: gazua e tensor na mão, faísca nos frames 2 e 5."""
    out = []
    picks = [(22, 12), (22, 13), (23, 12), (22, 11), (22, 12), (23, 13)]
    for i, hand in enumerate(picks):
        def tool(px, d, i=i, hand=hand):
            hx, hy = hand[0], hand[1] + d
            px.line(hx + 2, hy + 1, hx + 6, hy + 2 + (i % 2), "pale")     # gazua
            px.rect(hx + 1, hy - 1, hx + 4, hy - 1, "steel")              # tensor
            if i in (1, 4):
                for sx, sy, c in ((hx + 7, hy + 4, "lamp"), (hx + 8, hy + 2, "cream"), (hx + 6, hy + 5, "gold"),
                                  (hx + 9, hy + 5, "lamp")):
                    px.dot(sx, sy, c)
        out.append(gazua_frame(d_extra=tool, kneel=True, body_dy=-4 + (i % 2 == 1) * 0, front_hand=hand,
                               back_hand=(19, 10), keys_swing=0))
    return out


def frames_comemorar():
    def keyring(px, d, k):
        hx, hy = 20, 25 + d + k
        px.ring(hx + 1, hy + 4, 3, "gold")
        for j in range(4):
            kx = hx - 2 + j * 2 + k
            px.rect(kx, hy + 6 + (j % 2), kx, hy + 9 + (j % 2), "mist" if j % 2 else "lamp")
    return [
        gazua_frame(body_dy=-1, front_hand=(19, 13), legs=((-1, 0), (1, 0))),
        gazua_frame(d_extra=lambda px, d: keyring(px, d, 0), jump=1, front_hand=(20, 24), mouth_open=True,
                    back_hand=(12, 13), legs=((-1, 1), (1, 1))),
        gazua_frame(d_extra=lambda px, d: keyring(px, d, 1), jump=1, front_hand=(20, 25), mouth_open=True,
                    back_hand=(12, 14), legs=((-1, 1), (1, 1))),
        gazua_frame(front_hand=(19, 20), back_hand=(13, 9)),
    ]


def retrato():
    """Foto 3x4 do dossiê, de frente: gorro, lupa no olho direito, bigodão grisalho."""
    S = 64
    px = Px(S, S)
    px.rect(0, 0, S - 1, S - 1, "mist")
    for y in range(0, S, 4):
        px.hline(0, S - 1, y, "pale")
    # ombros e jaqueta
    px.rect(6, 0, 57, 12, "steel")
    for y in range(12, 20):
        px.rect(6 + (y - 12), y, 57 - (y - 12), y, "steel")
    px.rect(26, 0, 37, 18, "dusk")                 # camiseta por baixo
    px.line(24, 19, 30, 2, "slate", 2)             # gola
    px.line(39, 19, 33, 2, "slate", 2)
    px.rect(44, 4, 50, 10, "slate")                # bolso
    px.rect(46, 9, 47, 13, "pale")                 # chave de fenda no bolso
    # pescoço e rosto
    px.rect(26, 16, 37, 24, "skin2")
    px.rect(26, 16, 37, 17, shade("skin2", -1))
    for y in range(20, 46):
        half = 13 if 24 < y < 42 else (11 if y >= 42 else 10)
        px.rect(32 - half, y, 31 + half, y, "skin2")
    px.rect(17, 28, 19, 34, shade("skin2", -1))    # orelhas
    px.rect(44, 28, 46, 34, shade("skin2", -1))
    # olho esquerdo e sobrancelhas grossas
    px.rect(23, 37, 29, 38, "mist")
    px.rect(35, 37, 41, 38, "mist")
    px.rect(25, 33, 27, 34, "ink")
    # lupa no olho direito
    px.disc(38, 33, 5, "slate")
    px.disc(38, 33, 3, "cyan")
    px.dot(37, 34, "cream")
    px.rect(43, 33, 46, 34, "ink")                 # elástico
    # nariz, bigodão e barba por fazer
    px.rect(30, 27, 33, 32, shade("skin2", -1))
    px.rect(24, 24, 39, 26, "mist")                # bigodão grisalho
    px.rect(24, 24, 39, 24, "steel")
    px.rect(22, 21, 24, 24, "mist")
    px.rect(39, 21, 41, 24, "mist")
    px.rect(28, 22, 35, 22, "winedark")            # boca fechada
    for x in range(21, 43, 3):
        px.dot(x, 20 + (x % 2), "brown")           # barba por fazer
    # gorro
    px.rect(17, 41, 46, 54, "slate")
    px.rect(17, 41, 46, 44, "dusk")
    for x in range(19, 45, 3):
        px.vline(x, 45, 53, "dusk")
    px.disc(32, 56, 3, "slate")
    # contorno
    bg = {(142, 157, 184, 255), (205, 213, 227, 255)}
    edge = []
    for y in range(S):
        for x in range(S):
            if px.get(x, y) in bg:
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < S and 0 <= ny < S and px.get(nx, ny) not in bg:
                        edge.append((x, y))
                        break
    for (x, y) in edge:
        px.dot(x, y, "ink")
    px.frame(0, 0, S - 1, S - 1, "ink")
    return px


def icone_pe_de_cabra():
    px = Px(32, 32)
    px.line(6, 5, 22, 25, "red", 3)               # cabo
    px.line(7, 5, 22, 24, "coral", 1)
    px.line(22, 25, 26, 25, "steel", 3)           # ponta curva
    px.rect(25, 22, 27, 24, "steel")
    px.line(5, 4, 3, 7, "steel", 3)               # unha
    for (x, y, c) in ((28, 28, "lamp"), (30, 26, "cream"), (27, 30, "gold"), (30, 30, "lamp"), (29, 24, "gold")):
        px.dot(x, y, c)
    px.outline()
    return px


def icone_chaveiro():
    px = Px(32, 32)
    px.rect(2, 18, 29, 29, "lamp")                # plaquinha "24H"
    px.frame(2, 18, 29, 29, "bronze")
    from paleta import draw_text
    draw_text(px, 6, 21, "24H", "ink")
    px.ring(9, 9, 5, "gold", 2)                   # chave
    px.rect(13, 8, 28, 10, "gold")
    px.rect(22, 4, 23, 8, "gold")
    px.rect(26, 5, 27, 8, "gold")
    px.dot(9, 9, "bronze")
    px.outline()
    return px


def crec():
    """Onomatopeia do pé-de-cabra, estilo gibi (pro efeito visual da habilidade)."""
    from paleta import draw_text, text_width
    from PIL import Image
    w = text_width("CREC!") + 4
    txt = Px(w, 12)
    draw_text(txt, 1, 3, "CREC!", "lamp")
    big = txt.img.resize((w * 2, 24), Image.NEAREST)
    px = Px(w * 2 + 8, 30)
    # estrela de impacto atrás
    for i in range(px.w):
        for j in range(px.h):
            dx, dy = (i - px.w / 2) / (px.w / 2), (j - px.h / 2) / (px.h / 2)
            if dx * dx + dy * dy < 0.9 and (abs(dx) < 0.25 or abs(dy) < 0.3 or abs(abs(dx) - abs(dy)) < 0.18):
                px.dot(i, j, "red")
    px.img.alpha_composite(big, (4, 3))
    px.outline()
    return px


def main():
    anims = {"idle": frames_idle(), "andar": frames_andar(), "trabalhar": frames_trabalhar(),
             "comemorar": frames_comemorar()}
    sh, order = sheet(anims, ["idle", "andar", "trabalhar", "comemorar"])
    sh.save(OUT_DIR / "Gazua_sheet.png")
    portrait = retrato()
    portrait.save(OUT_DIR / "Gazua_retrato.png")
    icons = OUT_DIR.parent / "Icons"
    icone_pe_de_cabra().save(icons / "Icon_PeDeCabra.png")
    icone_chaveiro().save(icons / "Icon_Chaveiro24h.png")
    crec().save(OUT_DIR.parent / "VFX/Crec.png")

    from PIL import Image
    sh.img.resize((sh.w * 8, sh.h * 8), Image.NEAREST).save(HERE / "previa_sheet.png")
    portrait.img.resize((64 * 6, 64 * 6), Image.NEAREST).save(HERE / "previa_retrato.png")
    print("ok:", OUT_DIR / "Gazua_sheet.png", sh.img.size, order)


if __name__ == "__main__":
    main()
