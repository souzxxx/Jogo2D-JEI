"""Gera a arte do Disquete pixel a pixel: sprite sheet 32x32 (idle, andar, trabalhar, comemorar), retrato 64x64
e os ícones do Loop na câmera e do Gatonet.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/Disquete/Art/Source~/gerar_disquete.py"
Precisa de Pillow (pip install pillow). Usa a paleta e o boneco da Cena do Golpe (HeistScene/Art/Source~).
Saída: Assets/_Project/Thieves/Disquete/Art/Disquete_sheet.png, Disquete_retrato.png e Icons/*.png (e prévias nesta pasta).
A pasta Source~ é ignorada pelo Unity.

O Disquete é o hacker (ver ../../LEIA-ME.md): cabelo cacheado, óculos fundo de garrafa, camiseta verde (a cor do
tipo), pochete neon e tênis branco. Trabalha ajoelhado num micro em cima de um caixote, com a tela piscando, e
comemora erguendo um disquete 5¼. De lado, olhando pra direita.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parent
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, shade  # noqa: E402
from boneco import spec, frame, sheet, WALK6  # noqa: E402

DISQUETE = spec(skin="skin", hair="umber", hair_style="curly", glasses=True, shirt="money", pants="slate",
                shoes="pale", belt=None)


def details(px, d):
    """Pochete neon na cintura e um disquete no bolso de trás."""
    px.rect(17, 9 + d, 20, 11 + d, "neon")
    px.hline(13, 16, 11 + d, "purple")
    px.rect(11, 6 + d, 12, 8 + d, "night")
    px.dot(12, 8 + d, "pale")


def floppy(px, x, y):
    """Disquete 5¼ de 6x6 com o canto de baixo à esquerda em (x, y)."""
    px.rect(x, y, x + 5, y + 5, "night")
    px.rect(x + 1, y + 3, x + 4, y + 4, "cream")
    px.dot(x + 2, y + 1, "mist")


def disquete_frame(d_extra=None, **kw):
    def extra(px, d):
        details(px, d)
        if d_extra:
            d_extra(px, d)
    return frame(DISQUETE, extra=extra, **kw)


def frames_idle():
    return [
        disquete_frame(),
        disquete_frame(),
        disquete_frame(body_dy=-1, front_hand=(16, 10)),
        disquete_frame(body_dy=-1, front_hand=(16, 10), blink=True),
    ]


def frames_andar():
    return [disquete_frame(body_dy=1 if i in (1, 4) else 0, legs=(b, f), back_hand=bh, front_hand=fh)
            for i, (b, f, bh, fh) in enumerate(WALK6)]


def frames_trabalhar():
    """Ajoelhado no micro em cima de um caixote: as mãos batem no teclado e a tela pisca nos frames 2 e 5."""
    out = []
    hands = [((21, 8), (19, 8)), ((22, 9), (20, 7)), ((21, 7), (19, 8)),
             ((22, 8), (20, 9)), ((21, 9), (19, 7)), ((22, 7), (20, 8))]
    for i, (front, back) in enumerate(hands):
        def micro(px, d, i=i):
            px.rect(23, 1, 30, 6, "wood")                       # caixote
            px.hline(23, 30, 4, "brown")
            px.rect(20, 7, 26, 7, "mist")                       # teclado
            px.rect(25, 8, 30, 14, "pale")                      # monitor
            px.rect(26, 9, 29, 13, "money" if i in (1, 4) else "ink")
            if i not in (1, 4):
                for row in range(3):
                    px.hline(26, 26 + (i + row * 2) % 4, 12 - row, "money")
        out.append(disquete_frame(d_extra=micro, kneel=True, body_dy=-4, front_hand=front, back_hand=back))
    return out


def frames_comemorar():
    def raised(px, d, k):
        floppy(px, 19, 26 + d + k)
    return [
        disquete_frame(body_dy=-1, front_hand=(19, 13), legs=((-1, 0), (1, 0))),
        disquete_frame(d_extra=lambda px, d: raised(px, d, 0), jump=1, front_hand=(20, 24), mouth_open=True,
                       back_hand=(12, 13), legs=((-1, 1), (1, 1))),
        disquete_frame(d_extra=lambda px, d: raised(px, d, 1), jump=1, front_hand=(20, 25), mouth_open=True,
                       back_hand=(12, 14), legs=((-1, 1), (1, 1))),
        disquete_frame(front_hand=(19, 20), back_hand=(13, 9)),
    ]


def retrato():
    """Foto 3x4 do dossiê, de frente: cachos, óculos fundo de garrafa, camiseta verde com disquete no bolso."""
    S = 64
    px = Px(S, S)
    px.rect(0, 0, S - 1, S - 1, "mist")
    for y in range(0, S, 4):
        px.hline(0, S - 1, y, "pale")
    # ombros e camiseta
    px.rect(6, 0, 57, 12, "money")
    for y in range(12, 20):
        px.rect(6 + (y - 12), y, 57 - (y - 12), y, "money")
    px.rect(6, 0, 9, 12, shade("money", -1))
    px.rect(25, 17, 38, 19, shade("money", -1))         # gola
    px.rect(40, 3, 50, 11, shade("money", -1))          # bolso com disquete
    px.rect(42, 8, 48, 14, "night")
    px.rect(43, 11, 47, 12, "cream")
    px.line(14, 12, 20, 0, "neon", 2)                   # alça da pochete
    # pescoço e rosto
    px.rect(26, 16, 37, 24, "skin")
    px.rect(26, 16, 37, 17, shade("skin", -1))
    for y in range(20, 46):
        half = 13 if 24 < y < 42 else (11 if y >= 42 else 10)
        px.rect(32 - half, y, 31 + half, y, "skin")
    px.rect(17, 28, 19, 34, shade("skin", -1))          # orelhas
    px.rect(44, 28, 46, 34, shade("skin", -1))
    # óculos fundo de garrafa
    for cx in (26, 38):
        px.disc(cx, 33, 5, "pale")
        px.ring(cx, 33, 5, "ink")
        px.rect(cx - 1, 32, cx, 33, "ink")
        px.dot(cx + 2, 35, "cream")
    px.rect(31, 34, 33, 34, "ink")
    px.rect(20, 35, 21, 35, "ink")
    px.rect(43, 35, 44, 35, "ink")
    # nariz e sorriso de quem achou a senha
    px.rect(30, 26, 33, 29, shade("skin", -1))
    px.rect(26, 22, 37, 22, "winedark")
    px.dot(25, 23, "winedark")
    px.dot(38, 23, "winedark")
    # cachos
    for (cx, cy) in ((20, 44), (26, 48), (32, 50), (38, 48), (44, 44), (18, 38), (46, 38), (23, 53), (32, 55),
                     (41, 53), (28, 43), (36, 43)):
        px.disc(cx, cy, 5, "umber")
    for (cx, cy) in ((24, 50), (34, 52), (42, 47), (19, 42)):
        px.ring(cx, cy, 2, "brown")
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


def icone_loop_na_camera():
    """Câmera de segurança dentro de uma seta em círculo: a imagem gravada fica rodando em loop."""
    px = Px(32, 32)
    px.ring(16, 16, 13, "money", 2)
    px.rect(25, 24, 29, 26, "money")                    # ponta da seta
    px.rect(27, 22, 29, 28, "money")
    px.rect(8, 13, 21, 19, "pale")                      # corpo da câmera
    px.rect(8, 13, 21, 13, "mist")
    px.rect(21, 14, 24, 18, "slate")                    # lente
    px.rect(23, 15, 24, 17, "cyan")
    px.rect(12, 10, 14, 12, "slate")                    # suporte
    px.dot(10, 17, "red")
    px.outline()
    return px


def icone_gatonet():
    """TV de tubo com o cabo puxado no gato (emenda com fita isolante)."""
    px = Px(32, 32)
    px.rect(3, 6, 22, 23, "wood")
    px.rect(5, 8, 18, 21, "cyan")
    for y in range(9, 21, 3):
        px.hline(5, 18, y, "blue")
    px.rect(19, 9, 21, 20, "brown")
    px.dot(20, 18, "lamp")
    px.dot(20, 15, "lamp")
    px.rect(6, 3, 8, 5, "umber")                        # pés
    px.rect(17, 3, 19, 5, "umber")
    px.line(22, 12, 27, 12, "night", 2)                 # cabo
    px.line(27, 12, 27, 28, "night", 2)
    px.rect(25, 18, 29, 21, "ink")                      # fita isolante na emenda
    for (x, y, c) in ((30, 22, "lamp"), (24, 23, "gold"), (30, 17, "cream")):
        px.dot(x, y, c)
    px.outline()
    return px


def main():
    anims = {"idle": frames_idle(), "andar": frames_andar(), "trabalhar": frames_trabalhar(),
             "comemorar": frames_comemorar()}
    sh, order = sheet(anims, ["idle", "andar", "trabalhar", "comemorar"])
    sh.save(OUT_DIR / "Disquete_sheet.png")
    portrait = retrato()
    portrait.save(OUT_DIR / "Disquete_retrato.png")
    icons = OUT_DIR.parent / "Icons"
    icons.mkdir(exist_ok=True)
    icone_loop_na_camera().save(icons / "Icon_LoopNaCamera.png")
    icone_gatonet().save(icons / "Icon_Gatonet.png")

    from PIL import Image
    sh.img.resize((sh.w * 8, sh.h * 8), Image.NEAREST).save(HERE / "previa_sheet.png")
    portrait.img.resize((64 * 6, 64 * 6), Image.NEAREST).save(HERE / "previa_retrato.png")
    print("ok:", OUT_DIR / "Disquete_sheet.png", sh.img.size, order)


if __name__ == "__main__":
    main()
