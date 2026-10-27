"""Gera a arte do Pé-na-Tábua pixel a pixel: sprite sheet 32x32 (idle, andar, trabalhar, comemorar), retrato 64x64
e o "VRUUM!" do efeito visual do Cavalo de pau.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/PeNaTabua/Art/Source~/gerar_penatabua.py"
Precisa de Pillow (pip install pillow). Usa a paleta e o boneco da Cena do Golpe (HeistScene/Art/Source~).
Saída: Assets/_Project/Thieves/PeNaTabua/Art/PeNaTabua_sheet.png, PeNaTabua_retrato.png e VFX/Vruum.png (e prévias nesta pasta).
A pasta Source~ é ignorada pelo Unity.

O Pé-na-Tábua é o motorista (ver ../../LEIA-ME.md): boné vermelho, óculos escuros, bigode, palito no canto da boca,
jaqueta laranja (a cor do tipo) por cima da camiseta branca e o chaveiro da van na cintura. Trabalha ao volante
(um volante solto na mão, esterçando pra lá e pra cá) e comemora sacudindo o chaveiro. De lado, olhando pra direita.
"""
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parent
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, shade  # noqa: E402
from boneco import spec, frame, sheet, WALK6  # noqa: E402

PE_NA_TABUA = spec(skin="skin2", hair="umber", hat="cap", hat_color="red", shirt="cream", jacket="gold",
                   pants="police", shoes="umber", mustache=True, belt="umber")


def details(px, d):
    """Óculos escuros por cima do olho, palito no canto da boca e o chaveiro pendurado no cinto."""
    y = 18 + d
    px.rect(17, y + 4, 19, y + 4, "ink")
    px.dot(19, y + 4, "slate")
    px.dot(20, y + 1, "tan")
    px.dot(21, y, "wood")
    px.dot(19, 8 + d, "gold")
    px.dot(19, 7 + d, "lamp")


def wheel(px, cx, cy, turn, hands):
    """Volante de 9x9 com o centro em (cx, cy); turn é o ângulo das hastes em graus. As mãos vão por cima do aro."""
    px.ring(cx, cy, 4, "night")
    for k in range(3):
        a = math.radians(turn + 90 + 120 * k)
        for r in (1, 2, 3):
            px.dot(cx + round(r * math.cos(a)), cy + round(r * math.sin(a)), "slate")
    px.dot(cx, cy, "red")
    for (hx, hy), skin in zip(hands, ("skin2", shade("skin2", -1))):
        px.rect(hx, hy, hx + 1, hy + 1, skin)


def keys(px, x, y, k):
    """Chaveiro com duas chaves; k alterna o balanço."""
    px.ring(x, y, 1, "gold")
    px.line(x + 1, y + 1, x + 3, y + 3 - k, "lamp")
    px.line(x - 1, y + 1, x - 2, y + 3 + k, "mist")


def pe_frame(d_extra=None, **kw):
    def extra(px, d):
        details(px, d)
        if d_extra:
            d_extra(px, d)
    return frame(PE_NA_TABUA, extra=extra, **kw)


def frames_idle():
    return [
        pe_frame(),
        pe_frame(),
        pe_frame(body_dy=-1, front_hand=(16, 10)),
        pe_frame(body_dy=-1, front_hand=(16, 10), blink=True),
    ]


def frames_andar():
    return [pe_frame(body_dy=1 if i in (1, 4) else 0, legs=(b, f), back_hand=bh, front_hand=fh)
            for i, (b, f, bh, fh) in enumerate(WALK6)]


def frames_trabalhar():
    """Ao volante: as duas mãos no aro e o volante esterçando pra direita e pra esquerda ("segura!")."""
    cx, cy = 23, 13
    out = []
    for i, turn in enumerate((0, 25, 45, 25, 0, -25)):
        a_front = math.radians(turn + 45)
        a_back = math.radians(turn + 135)
        front = (cx + round(4 * math.cos(a_front)) - 1, cy + round(4 * math.sin(a_front)) - 1)
        back = (cx + round(4 * math.cos(a_back)) - 1, cy + round(4 * math.sin(a_back)) - 1)
        def ao_volante(px, d, t=turn, f=front, b=back):
            wheel(px, cx, cy + d, t, [(f[0], f[1] + d), (b[0], b[1] + d)])
        out.append(pe_frame(d_extra=ao_volante, body_dy=-1 if i % 2 else 0,
                            front_hand=front, back_hand=back, mouth_open=i in (2, 5)))
    return out


def frames_comemorar():
    def raised(px, d, k):
        keys(px, 21, 27 + d + k, k)
    return [
        pe_frame(body_dy=-1, front_hand=(19, 13), legs=((-1, 0), (1, 0))),
        pe_frame(d_extra=lambda px, d: raised(px, d, 0), jump=1, front_hand=(20, 24), mouth_open=True,
                 back_hand=(12, 13), legs=((-1, 1), (1, 1))),
        pe_frame(d_extra=lambda px, d: raised(px, d, 1), jump=1, front_hand=(20, 25), mouth_open=True,
                 back_hand=(12, 14), legs=((-1, 1), (1, 1))),
        pe_frame(front_hand=(19, 20), back_hand=(13, 9)),
    ]


def retrato():
    """Foto 3x4 do dossiê, de frente: boné vermelho, óculos escuros, bigode, palito e flanela no ombro."""
    S = 64
    px = Px(S, S)
    px.rect(0, 0, S - 1, S - 1, "mist")
    for y in range(0, S, 4):
        px.hline(0, S - 1, y, "pale")
    # ombros e jaqueta laranja aberta por cima da camiseta branca
    px.rect(6, 0, 57, 12, "gold")
    for y in range(12, 20):
        px.rect(6 + (y - 12), y, 57 - (y - 12), y, "gold")
    px.rect(6, 0, 9, 12, shade("gold", -1))
    px.rect(26, 0, 37, 18, "cream")                 # camiseta
    px.line(24, 19, 30, 2, shade("gold", -1), 2)    # gola
    px.line(39, 19, 33, 2, shade("gold", -1), 2)
    px.rect(46, 0, 47, 8, "mist")                   # zíper
    px.rect(10, 9, 19, 15, "red")                   # flanela no ombro
    for x in range(11, 19, 3):
        px.vline(x, 9, 15, "coral")
    # pescoço e rosto
    px.rect(26, 16, 37, 24, "skin2")
    px.rect(26, 16, 37, 17, shade("skin2", -1))
    for y in range(20, 46):
        half = 13 if 24 < y < 42 else (11 if y >= 42 else 10)
        px.rect(32 - half, y, 31 + half, y, "skin2")
    px.rect(17, 28, 19, 34, shade("skin2", -1))     # orelhas
    px.rect(44, 28, 46, 34, shade("skin2", -1))
    px.rect(19, 35, 20, 41, "umber")                # costeletas
    px.rect(43, 35, 44, 41, "umber")
    # óculos escuros de motorista
    px.rect(21, 31, 29, 36, "ink")
    px.rect(34, 31, 42, 36, "ink")
    px.rect(29, 34, 34, 35, "ink")
    px.rect(18, 35, 21, 35, "ink")
    px.rect(42, 35, 45, 35, "ink")
    px.rect(22, 35, 24, 35, "steel")
    px.rect(35, 35, 37, 35, "steel")
    # nariz, bigode, boca e o palito
    px.rect(30, 27, 33, 30, shade("skin2", -1))
    px.rect(24, 23, 39, 25, "umber")
    px.rect(22, 21, 24, 23, "umber")
    px.rect(39, 21, 41, 23, "umber")
    px.rect(28, 21, 35, 21, "winedark")
    px.line(35, 21, 41, 18, "tan")
    px.line(36, 22, 41, 19, "wood")
    # boné vermelho com a aba pra frente e um volantinho bordado
    px.rect(17, 42, 46, 53, "red")
    px.rect(19, 54, 44, 55, "red")
    px.rect(17, 51, 46, 53, shade("red", 1))
    px.rect(14, 40, 49, 42, shade("red", -1))
    px.ring(32, 47, 3, "lamp")
    px.dot(32, 47, "lamp")
    px.disc(32, 56, 1, shade("red", -1))
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


def vruum():
    """Onomatopeia do Cavalo de pau, estilo gibi (pro efeito visual da habilidade), com marca de pneu embaixo."""
    from paleta import draw_text, text_width
    from PIL import Image
    w = text_width("VRUUM!") + 4
    txt = Px(w, 12)
    draw_text(txt, 1, 3, "VRUUM!", "red")
    big = txt.img.resize((w * 2, 24), Image.NEAREST)
    px = Px(w * 2 + 8, 30)
    # balão de velocidade atrás, esticado pros lados
    for i in range(px.w):
        for j in range(px.h):
            dx, dy = (i - px.w / 2) / (px.w / 2), (j - px.h / 2) / (px.h / 2)
            if dx * dx + dy * dy * 1.6 < 0.95 and (abs(dy) < 0.45 or (i + j) % 5 == 0):
                px.dot(i, j, "gold")
    for x in range(2, px.w - 2, 6):                     # marca de pneu
        px.rect(x, 1, x + 3, 2, "night")
    px.img.alpha_composite(big, (4, 4))
    px.outline()
    return px


def main():
    anims = {"idle": frames_idle(), "andar": frames_andar(), "trabalhar": frames_trabalhar(),
             "comemorar": frames_comemorar()}
    sh, order = sheet(anims, ["idle", "andar", "trabalhar", "comemorar"])
    sh.save(OUT_DIR / "PeNaTabua_sheet.png")
    portrait = retrato()
    portrait.save(OUT_DIR / "PeNaTabua_retrato.png")
    vfx = OUT_DIR.parent / "VFX"
    vfx.mkdir(exist_ok=True)
    vruum().save(vfx / "Vruum.png")

    from PIL import Image
    sh.img.resize((sh.w * 8, sh.h * 8), Image.NEAREST).save(HERE / "previa_sheet.png")
    portrait.img.resize((64 * 6, 64 * 6), Image.NEAREST).save(HERE / "previa_retrato.png")
    print("ok:", OUT_DIR / "PeNaTabua_sheet.png", sh.img.size, order)


if __name__ == "__main__":
    main()
