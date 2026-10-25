"""Gera a arte do 171 pixel a pixel: sprite sheet 32x32 (idle, andar, trabalhar, comemorar) e retrato 64x64.

Uso (da raiz do repositório):  python "Assets/_Project/Thieves/171/Art/Source~/gerar_171.py"
Saída: Assets/_Project/Thieves/171/Art/171_sheet.png e 171_retrato.png (e prévias ampliadas nesta pasta).
A pasta Source~ é ignorada pelo Unity.

O 171 é o vigarista: chapéu panamá, blazer vinho (a cor do tipo), camisa branca, gravata dourada,
bigodinho e sapato bicolor. Visto de lado, olhando pra direita, como a crew anda no prédio em corte.
"""
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
OUT_DIR = HERE.parent

CELL = 32
ANIMS = [  # (nome, frames); cada animação é uma linha do sheet, nesta ordem
    ("idle", 4),
    ("andar", 6),
    ("trabalhar", 6),
    ("comemorar", 4),
]

OUTLINE = (27, 20, 32)
SKIN, SKIN_D = (214, 160, 120), (176, 120, 88)
HAIR = (40, 28, 24)
HAT, HAT_D, BAND = (232, 220, 180), (196, 180, 140), (140, 30, 45)
BLAZER, BLAZER_D, BLAZER_L = (217, 77, 89), (160, 48, 62), (236, 118, 120)
SHIRT = (240, 236, 224)
TIE = (232, 184, 64)
PANTS, PANTS_D = (52, 52, 72), (36, 36, 52)
SHOE_W, SHOE_B = (240, 236, 224), (110, 60, 40)
BUBBLE = (250, 250, 245)


class Canvas:
    def __init__(self, size, offset_y=0):
        self.size = size
        self.offset_y = offset_y
        self.px = {}

    def dot(self, x, y, c):
        y += self.offset_y
        if 0 <= x < self.size and 0 <= y < self.size:
            self.px[(x, y)] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, c)

    def line(self, x0, y0, x1, y1, c, width=2):
        steps = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(steps + 1):
            x = round(x0 + (x1 - x0) * i / steps)
            y = round(y0 + (y1 - y0) * i / steps)
            self.rect(x, y, x + width - 1, y + width - 1, c)

    def outline(self, color=OUTLINE):
        filled = set(self.px)
        for (x, y) in list(filled):
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if (nx, ny) not in filled and 0 <= nx < self.size and 0 <= ny < self.size:
                    self.px[(nx, ny)] = color

    def image(self):
        img = Image.new("RGBA", (self.size, self.size), (0, 0, 0, 0))
        for (x, y), c in self.px.items():
            img.putpixel((x, y), c + (255,))
        return img


# ---------------- Partes do corpo (de lado, olhando pra direita) ----------------

def leg(cv, hip_x, foot_dx, lift, dy, color):
    top, foot_y = 21 + dy, 28 + dy - lift
    for y in range(top, foot_y):
        x = hip_x + round(foot_dx * (y - top) / max(foot_y - top, 1))
        cv.rect(x, y, x + 1, y, color)
    fx = hip_x + foot_dx
    cv.rect(fx, foot_y, fx + 2, foot_y, SHOE_W)
    cv.dot(fx, foot_y, SHOE_B)
    cv.rect(fx, foot_y + 1, fx + 2, foot_y + 1, SHOE_B)


def hat(cv, x, y):
    """x, y = canto esquerdo da aba."""
    cv.rect(x + 2, y - 4, x + 8, y - 1, HAT)
    cv.rect(x + 2, y - 4, x + 8, y - 4, HAT_D)
    cv.dot(x + 5, y - 4, OUTLINE)  # vinco da copa
    cv.rect(x + 2, y - 1, x + 8, y - 1, BAND)
    cv.rect(x, y, x + 10, y, HAT_D)


def head(cv, dy, mouth_open, with_hat=True):
    y = 6 + dy
    cv.rect(13, y, 19, y + 5, SKIN)
    cv.rect(13, y, 14, y + 3, HAIR)        # nuca
    cv.rect(13, y, 19, y, HAIR)            # cabelo (aparece sem chapéu)
    cv.dot(15, y + 2, SKIN_D)              # orelha
    cv.dot(15, y + 3, SKIN_D)
    cv.dot(20, y + 3, SKIN)                # nariz
    cv.dot(18, y + 2, OUTLINE)             # olho
    cv.dot(17, y + 1, HAIR)                # sobrancelha
    cv.dot(18, y + 1, HAIR)
    cv.rect(17, y + 4, 19, y + 4, HAIR)    # bigodinho
    cv.dot(19, y + 5, OUTLINE if mouth_open else SKIN_D)
    cv.rect(14, y + 5, 16, y + 5, SKIN_D)  # queixo/sombra
    if with_hat:
        hat(cv, 11, y - 1)


def torso(cv, dy):
    y = 12 + dy
    cv.rect(15, y, 17, y, SKIN)                 # pescoço
    cv.rect(13, y + 1, 19, y + 7, BLAZER)
    cv.rect(13, y + 1, 13, y + 7, BLAZER_D)     # costas na sombra
    cv.rect(18, y, 19, y + 1, SHIRT)            # colarinho
    cv.rect(19, y + 2, 19, y + 5, TIE)
    cv.dot(18, y + 2, SHIRT)
    cv.dot(18, y + 7, BLAZER_D)                 # barra do paletó
    cv.rect(13, y + 8, 19, y + 8, OUTLINE)      # cinto
    cv.dot(18, y + 8, TIE)                      # fivela


def arm(cv, dy, hand_x, hand_y, color=BLAZER_L, hand=SKIN):
    sx, sy = 15, 13 + dy
    cv.line(sx, sy, hand_x, hand_y - 1, color)
    cv.rect(hand_x, hand_y, hand_x + 1, hand_y + 1, hand)


def bubble(cv, x, y, dots):
    """Balãozinho de fala com 1 a 3 pontinhos, desenhado por cima do contorno."""
    cv.rect(x, y, x + 6, y + 3, BUBBLE)
    cv.dot(x, y + 4, BUBBLE)
    for i in range(dots):
        cv.dot(x + 1 + i * 2, y + 2, OUTLINE)


# ---------------- Poses ----------------

def frame(body_dy=0, jump=0, legs=((0, 0), (0, 0)), back_hand=(14, 20), front_hand=(16, 20),
          mouth_open=False, hat_in_hand=False, talk=0):
    cv = Canvas(CELL, offset_y=1)  # pé na linha 30: sobra 1 px em cima pro pulo
    d = body_dy - jump
    (bdx, blift), (fdx, flift) = legs
    leg(cv, 14, bdx, blift, -jump, PANTS_D)                         # perna de trás
    arm(cv, d, back_hand[0], back_hand[1] + d, BLAZER_D, SKIN_D)    # braço de trás
    torso(cv, d)
    head(cv, d, mouth_open, with_hat=not hat_in_hand)
    leg(cv, 17, fdx, flift, -jump, PANTS)                           # perna da frente
    hx, hy = front_hand[0], front_hand[1] + d
    arm(cv, d, hx, hy)
    if hat_in_hand:
        hat(cv, hx - 4, hy - 1)
    cv.outline()
    if talk:
        bubble(cv, 22, 1 + d, talk)
    return cv.image()


def frames_idle():
    return [
        frame(),
        frame(),
        frame(body_dy=1, front_hand=(16, 19)),
        frame(body_dy=1, front_hand=(16, 19)),
    ]


def frames_andar():
    # passada: pé da frente vai e volta, braço balança ao contrário
    poses = [
        ((-3, 0), (3, 0), (18, 19), (13, 19)),
        ((-1, 0), (1, 2), (17, 20), (14, 20)),
        ((1, 2), (-1, 0), (15, 20), (16, 20)),
        ((3, 0), (-3, 0), (13, 19), (18, 19)),
        ((1, 0), (-1, 2), (14, 20), (17, 20)),
        ((-1, 2), (1, 0), (16, 20), (15, 20)),
    ]
    out = []
    for i, (back, front, bh, fh) in enumerate(poses):
        out.append(frame(body_dy=-1 if i in (1, 4) else 0, legs=(back, front), back_hand=bh, front_hand=fh))
    return out


def frames_trabalhar():
    # conversa mole: gesticula com a mão da frente, boca abrindo e fechando, balão de "..."
    hands = [(21, 15), (22, 12), (21, 14), (22, 16), (21, 11), (21, 15)]
    out = []
    for i, h in enumerate(hands):
        out.append(frame(front_hand=h, back_hand=(14, 19), mouth_open=i % 2 == 0,
                         body_dy=1 if i in (2, 3) else 0, talk=1 + i % 3))
    return out


def frames_comemorar():
    # agacha e dá um pulinho com o punho pra cima
    return [
        frame(body_dy=1, front_hand=(19, 16), legs=((-1, 0), (1, 0))),
        frame(jump=1, front_hand=(20, 6), mouth_open=True, back_hand=(12, 17), legs=((-1, 1), (1, 1))),
        frame(jump=1, front_hand=(20, 5), mouth_open=True, back_hand=(12, 16), legs=((-1, 1), (1, 1))),
        frame(front_hand=(19, 10), back_hand=(13, 19)),
    ]


# ---------------- Retrato 3x4 do dossiê (de frente) ----------------

def retrato():
    S = 64
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, S - 1, S - 1), fill=(196, 200, 204))             # fundo de foto 3x4
    for y in range(0, S, 4):
        d.line((0, y, S - 1, y), fill=(188, 192, 198))                    # textura leve
    # ombros e blazer
    d.rectangle((8, 50, 55, 63), fill=BLAZER)
    d.polygon([(8, 52), (20, 44), (44, 44), (55, 52), (55, 63), (8, 63)], fill=BLAZER)
    d.polygon([(26, 44), (32, 58), (38, 44)], fill=SHIRT)                 # camisa em V
    d.polygon([(30, 47), (34, 47), (33, 60), (31, 60)], fill=TIE)         # gravata
    d.line((24, 45, 31, 60), fill=BLAZER_D)                               # lapelas
    d.line((40, 45, 33, 60), fill=BLAZER_D)
    d.point((46, 54), fill=TIE)                                           # alfinete
    # pescoço e rosto
    d.rectangle((27, 38, 37, 46), fill=SKIN_D)
    d.ellipse((20, 15, 44, 43), fill=SKIN)
    d.rectangle((19, 26, 21, 31), fill=SKIN_D)                            # orelhas
    d.rectangle((43, 26, 45, 31), fill=SKIN_D)
    d.rectangle((21, 19, 43, 22), fill=HAIR)                              # cabelo sob o chapéu
    d.rectangle((21, 19, 23, 28), fill=HAIR)                              # costeletas
    d.rectangle((41, 19, 43, 28), fill=HAIR)
    # olhos (um piscando: é o 171)
    d.rectangle((26, 26, 29, 26), fill=HAIR)                              # sobrancelhas
    d.rectangle((35, 25, 38, 25), fill=HAIR)
    d.rectangle((27, 28, 28, 29), fill=OUTLINE)
    d.line((35, 29, 38, 28), fill=OUTLINE)                                # piscadela
    d.rectangle((31, 30, 33, 34), fill=SKIN_D)                            # nariz
    d.rectangle((27, 36, 37, 37), fill=HAIR)                              # bigodinho
    d.line((29, 39, 36, 39), fill=(150, 70, 60))                          # sorriso de canto
    d.point((37, 38), fill=(150, 70, 60))
    # chapéu panamá
    d.rectangle((12, 16, 52, 19), fill=HAT_D)                             # aba
    d.rectangle((19, 4, 45, 16), fill=HAT)                                # copa
    d.rectangle((19, 4, 45, 5), fill=HAT_D)
    d.line((32, 4, 32, 9), fill=HAT_D)                                    # vinco
    d.rectangle((19, 12, 45, 15), fill=BAND)                              # fita
    # contorno da silhueta
    px = img.load()
    bg = {(196, 200, 204, 255), (188, 192, 198, 255)}
    edge = []
    for y in range(S):
        for x in range(S):
            if px[x, y] in bg:
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < S and 0 <= ny < S and px[nx, ny] not in bg:
                        edge.append((x, y))
                        break
    for p in edge:
        px[p] = OUTLINE + (255,)
    d.rectangle((0, 0, S - 1, S - 1), outline=OUTLINE)                    # borda da foto
    return img


def main():
    builders = {"idle": frames_idle, "andar": frames_andar, "trabalhar": frames_trabalhar,
                "comemorar": frames_comemorar}
    cols = max(n for _, n in ANIMS)
    sheet = Image.new("RGBA", (cols * CELL, len(ANIMS) * CELL), (0, 0, 0, 0))
    for row, (name, count) in enumerate(ANIMS):
        frames = builders[name]()
        assert len(frames) == count, name
        for col, f in enumerate(frames):
            sheet.paste(f, (col * CELL, row * CELL))
    sheet.save(OUT_DIR / "171_sheet.png")
    portrait = retrato()
    portrait.save(OUT_DIR / "171_retrato.png")

    scale = 8
    sheet.resize((sheet.width * scale, sheet.height * scale), Image.NEAREST).save(HERE / "previa_sheet.png")
    portrait.resize((64 * 6, 64 * 6), Image.NEAREST).save(HERE / "previa_retrato.png")
    print("ok:", OUT_DIR / "171_sheet.png", sheet.size)


if __name__ == "__main__":
    main()
