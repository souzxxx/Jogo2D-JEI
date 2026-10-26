"""Paleta, fonte pixel e utilitários de desenho da arte de A Firma.

Usado pelos geradores da Cena do Golpe (gerar_cena.py) e do Gazua (Thieves/Gazua/Art/Source~/gerar_gazua.py).
Precisa de Pillow: pip install pillow

A paleta tem 32 cores: noite azulada de Nova Guanabara, luz quente de interior, kraft do dossiê,
verde de dinheiro, vermelho de alarme, azul de viatura e neon de cassino.
"""
from pathlib import Path

from PIL import Image, ImageDraw

# ---------------------------------------------------------------- Paleta (32)

PALETTE = {
    "ink": (14, 12, 21),
    "night": (28, 27, 46),
    "dusk": (43, 46, 74),
    "slate": (64, 72, 105),
    "steel": (95, 109, 143),
    "mist": (142, 157, 184),
    "pale": (205, 213, 227),
    "cream": (247, 243, 232),
    "umber": (42, 28, 26),
    "brown": (75, 49, 40),
    "wood": (113, 73, 47),
    "tan": (160, 109, 66),
    "kraft": (207, 157, 99),
    "paper": (236, 210, 160),
    "deepgreen": (29, 58, 47),
    "green": (46, 106, 70),
    "money": (90, 168, 100),
    "mint": (180, 220, 140),
    "winedark": (61, 20, 36),
    "wine": (124, 30, 47),
    "red": (204, 51, 56),
    "coral": (242, 115, 79),
    "bronze": (138, 90, 22),
    "gold": (219, 161, 43),
    "lamp": (247, 212, 104),
    "police": (27, 58, 120),
    "blue": (49, 132, 214),
    "cyan": (116, 214, 234),
    "purple": (94, 43, 114),
    "neon": (197, 70, 143),
    "skin": (238, 180, 138),
    "skin2": (176, 116, 80),
}
P = PALETTE
NAMES = list(PALETTE)


def rgba(color, alpha=255):
    if isinstance(color, str):
        color = PALETTE[color]
    return tuple(color[:3]) + (alpha,)


def palette_image(cell=16, cols=8):
    rows = (len(NAMES) + cols - 1) // cols
    img = Image.new("RGBA", (cols * cell, rows * cell), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for i, name in enumerate(NAMES):
        x, y = (i % cols) * cell, (i // cols) * cell
        d.rectangle((x, y, x + cell - 1, y + cell - 1), fill=rgba(name))
    return img


# ---------------------------------------------------------------- Desenho

class Px:
    """Imagem RGBA com y crescendo pra cima (como no Unity): (0, 0) é o canto de baixo à esquerda."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.img.load()

    def dot(self, x, y, c, a=255):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[x, self.h - 1 - y] = rgba(c, a)

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[x, self.h - 1 - y]
        return (0, 0, 0, 0)

    def rect(self, x0, y0, x1, y1, c, a=255):
        """Retângulo cheio com cantos inclusivos."""
        for y in range(min(y0, y1), max(y0, y1) + 1):
            for x in range(min(x0, x1), max(x0, x1) + 1):
                self.dot(x, y, c, a)

    def frame(self, x0, y0, x1, y1, c):
        self.rect(x0, y0, x1, y0, c)
        self.rect(x0, y1, x1, y1, c)
        self.rect(x0, y0, x0, y1, c)
        self.rect(x1, y0, x1, y1, c)

    def hline(self, x0, x1, y, c):
        self.rect(x0, y, x1, y, c)

    def vline(self, x, y0, y1, c):
        self.rect(x, y0, x, y1, c)

    def line(self, x0, y0, x1, y1, c, width=1):
        steps = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(steps + 1):
            x = round(x0 + (x1 - x0) * i / steps)
            y = round(y0 + (y1 - y0) * i / steps)
            self.rect(x, y, x + width - 1, y + width - 1, c)

    def disc(self, cx, cy, r, c):
        for y in range(cy - r, cy + r + 1):
            for x in range(cx - r, cx + r + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r + r * 0.6:
                    self.dot(x, y, c)

    def ring(self, cx, cy, r, c, thick=1):
        for y in range(cy - r - 1, cy + r + 2):
            for x in range(cx - r - 1, cx + r + 2):
                d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
                if r - thick + 0.5 <= d <= r + 0.5:
                    self.dot(x, y, c)

    def dither(self, x0, y0, x1, y1, c1, c2, density=0.5):
        """Mistura duas cores em padrão xadrez/Bayer (density = fração de c2)."""
        bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, c2 if bayer[y % 4][x % 4] < density * 16 else c1)

    def vgradient(self, x0, y0, x1, y1, c_bottom, c_top, steps=None):
        """Degradê vertical em faixas dithered entre duas cores."""
        h = y1 - y0
        for y in range(y0, y1 + 1):
            t = (y - y0) / max(h, 1)
            self.dither(x0, y, x1, y, c_bottom, c_top, t)

    def outline(self, color="ink", only_empty=True):
        """Contorno de 1 px ao redor do que é opaco (como os ladrões)."""
        filled = {(x, y) for y in range(self.h) for x in range(self.w) if self.get(x, y)[3] > 0}
        for (x, y) in list(filled):
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if (nx, ny) not in filled and 0 <= nx < self.w and 0 <= ny < self.h:
                    self.dot(nx, ny, color)

    def paste(self, other, x, y):
        """Cola outra Px com o canto de baixo à esquerda em (x, y)."""
        self.img.alpha_composite(other.img, (x, self.h - y - other.h))

    def flipped(self):
        out = Px(self.w, self.h)
        out.img = self.img.transpose(Image.FLIP_LEFT_RIGHT)
        out.px = out.img.load()
        return out

    def save(self, path, preview_dir=None, scale=4):
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        self.img.save(path)
        if preview_dir is not None:
            Path(preview_dir).mkdir(parents=True, exist_ok=True)
            self.img.resize((self.w * scale, self.h * scale), Image.NEAREST).save(Path(preview_dir) / ("previa_" + path.name))


def shade(color, steps):
    """Escurece (steps < 0) ou clareia (steps > 0) uma cor andando pela rampa da paleta."""
    ramps = [
        ["ink", "night", "dusk", "slate", "steel", "mist", "pale", "cream"],
        ["ink", "umber", "brown", "wood", "tan", "kraft", "paper", "cream"],
        ["ink", "deepgreen", "green", "money", "mint", "cream"],
        ["ink", "winedark", "wine", "red", "coral", "paper"],
        ["umber", "bronze", "gold", "lamp", "cream"],
        ["ink", "night", "police", "blue", "cyan", "pale"],
        ["ink", "winedark", "purple", "neon", "coral"],
        ["umber", "brown", "skin2", "skin", "paper"],
    ]
    for ramp in ramps:
        if color in ramp:
            i = max(0, min(len(ramp) - 1, ramp.index(color) + steps))
            return ramp[i]
    return color


# ---------------------------------------------------------------- Fonte pixel
# Cada glifo tem 7 linhas na caixa (linha 0 = topo da maiúscula) e até 2 de descendente.
# Célula final: 2 linhas pra acento de maiúscula + 7 + 2 de descendente = 11 de altura.

GLYPHS = {
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "B": ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
    "C": [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
    "D": ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "F": ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
    "G": [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###."],
    "H": ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "I": ["###", ".#.", ".#.", ".#.", ".#.", ".#.", "###"],
    "J": ["..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.."],
    "K": ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"],
    "L": ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
    "M": ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"],
    "N": ["#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "P": ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
    "Q": [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"],
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
    "U": ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "V": ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."],
    "W": ["#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#."],
    "X": ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
    "Y": ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."],
    "Z": ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"],
    "a": [".....", ".....", ".###.", "....#", ".####", "#...#", ".####"],
    "b": ["#....", "#....", "####.", "#...#", "#...#", "#...#", "####."],
    "c": ["....", "....", ".###", "#...", "#...", "#...", ".###"],
    "d": ["....#", "....#", ".####", "#...#", "#...#", "#...#", ".####"],
    "e": [".....", ".....", ".###.", "#...#", "#####", "#....", ".###."],
    "f": ["..##", ".#..", "###.", ".#..", ".#..", ".#..", ".#.."],
    "g": [".....", ".....", ".####", "#...#", "#...#", "#...#", ".####", "....#", ".###."],
    "h": ["#....", "#....", "####.", "#...#", "#...#", "#...#", "#...#"],
    "i": ["#", ".", "#", "#", "#", "#", "#"],
    "j": ["..#", "...", ".##", "..#", "..#", "..#", "..#", "#.#", ".#."],
    "k": ["#...", "#...", "#..#", "#.#.", "##..", "#.#.", "#..#"],
    "l": ["##.", ".#.", ".#.", ".#.", ".#.", ".#.", ".##"],
    "m": [".....", ".....", "##.#.", "#.#.#", "#.#.#", "#.#.#", "#.#.#"],
    "n": [".....", ".....", "####.", "#...#", "#...#", "#...#", "#...#"],
    "o": [".....", ".....", ".###.", "#...#", "#...#", "#...#", ".###."],
    "p": [".....", ".....", "####.", "#...#", "#...#", "#...#", "####.", "#....", "#...."],
    "q": [".....", ".....", ".####", "#...#", "#...#", "#...#", ".####", "....#", "....#"],
    "r": ["....", "....", "#.##", "##..", "#...", "#...", "#..."],
    "s": [".....", ".....", ".####", "#....", ".###.", "....#", "####."],
    "t": [".#..", ".#..", "###.", ".#..", ".#..", ".#..", "..##"],
    "u": [".....", ".....", "#...#", "#...#", "#...#", "#..##", ".##.#"],
    "v": [".....", ".....", "#...#", "#...#", "#...#", ".#.#.", "..#.."],
    "w": [".....", ".....", "#...#", "#...#", "#.#.#", "#.#.#", ".#.#."],
    "x": [".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#"],
    "y": [".....", ".....", "#...#", "#...#", "#...#", "#...#", ".####", "....#", ".###."],
    "z": [".....", ".....", "#####", "...#.", "..#..", ".#...", "#####"],
    "0": [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."],
    "1": [".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###"],
    "2": [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
    "3": ["####.", "....#", "....#", ".###.", "....#", "....#", "####."],
    "4": ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
    "5": ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
    "6": [".###.", "#....", "#....", "####.", "#...#", "#...#", ".###."],
    "7": ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
    "8": [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
    "9": [".###.", "#...#", "#...#", ".####", "....#", "....#", ".###."],
    "$": ["..#..", ".####", "#.#..", ".###.", "..#.#", "####.", "..#.."],
    ".": [".", ".", ".", ".", ".", ".", "#"],
    ",": ["..", "..", "..", "..", "..", "..", ".#", "#."],
    ":": [".", ".", "#", ".", ".", ".", "#"],
    ";": ["..", "..", ".#", "..", "..", "..", ".#", "#."],
    "!": ["#", "#", "#", "#", "#", ".", "#"],
    "?": [".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."],
    "-": ["...", "...", "...", "###", "...", "...", "..."],
    "+": [".....", "..#..", "..#..", "#####", "..#..", "..#..", "....."],
    "'": ["#", "#", ".", ".", ".", ".", "."],
    '"': ["#.#", "#.#", "...", "...", "...", "...", "..."],
    "/": ["....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...."],
    "(": [".#", "#.", "#.", "#.", "#.", "#.", ".#"],
    ")": ["#.", ".#", ".#", ".#", ".#", ".#", "#."],
    "%": ["##..#", "##.#.", "...#.", "..#..", ".#...", ".#.##", "#..##"],
    "*": [".....", "#.#.#", ".###.", "#####", ".###.", "#.#.#", "....."],
    "x": [".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#"],
}

ACCENTS = {
    "acute": ["..#", ".#."],
    "grave": ["#..", ".#."],
    "circ": [".#.", "#.#"],
    "tilde": [".#.#", "#.#."],
}

ACCENTED = {
    "Á": ("A", "acute"), "À": ("A", "grave"), "Â": ("A", "circ"), "Ã": ("A", "tilde"),
    "É": ("E", "acute"), "Ê": ("E", "circ"), "Í": ("I", "acute"), "Ó": ("O", "acute"),
    "Ô": ("O", "circ"), "Õ": ("O", "tilde"), "Ú": ("U", "acute"),
    "á": ("a", "acute"), "à": ("a", "grave"), "â": ("a", "circ"), "ã": ("a", "tilde"),
    "é": ("e", "acute"), "ê": ("e", "circ"), "í": ("i", "acute"), "ó": ("o", "acute"),
    "ô": ("o", "circ"), "õ": ("o", "tilde"), "ú": ("u", "acute"),
}

CELL_H = 11      # 2 de acento + 7 + 2 de descendente
BASELINE = 2     # linhas abaixo da base (descendente)
SPACE_W = 3


def glyph_bitmap(ch):
    """Devolve (largura, set de pixels (x, y) com y pra cima, 0 = linha de baixo da descendente)."""
    pixels = set()
    if ch == " ":
        return SPACE_W, pixels
    if ch in ("Ç", "ç"):
        base_w, base = glyph_bitmap("C" if ch == "Ç" else "c")
        cx = base_w // 2
        return base_w, base | {(cx, 1), (cx, 0), (cx - 1, 0)}
    if ch in ACCENTED:
        base_ch, mark = ACCENTED[ch]
        base_w, base = glyph_bitmap(base_ch)
        rows = ACCENTS[mark]
        upper = base_ch.isupper()
        top = CELL_H - 1 if upper else CELL_H - 3   # minúscula usa as linhas livres acima do x-height
        mw = len(rows[0])
        ox = max(0, (base_w - mw + 1) // 2)
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    pixels.add((ox + c, top - r))
        if base_ch in ("i",):  # i com acento perde o pingo
            base = {(x, y) for (x, y) in base if y != CELL_H - 3}
        return max(base_w, ox + mw), base | pixels
    rows = GLYPHS[ch]
    w = max(len(r) for r in rows)
    for r, row in enumerate(rows):
        y = CELL_H - 3 - r          # linha 0 da caixa fica 2 abaixo do topo da célula
        for c, v in enumerate(row):
            if v == "#":
                pixels.add((c, y))
    return w, pixels


FONT_CHARS = (
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    "$.,:;!?-+'\"/()%*" "ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç "
)


def text_width(text, spacing=1):
    return sum(glyph_bitmap(ch if ch in FONT_CHARS else "?")[0] + spacing for ch in text) - spacing


def draw_text(px, x, y, text, color, spacing=1, shadow=None):
    """Escreve com a base das letras em y (descendente fica abaixo)."""
    cx = x
    for ch in text:
        if ch not in FONT_CHARS:
            ch = "?"
        w, pixels = glyph_bitmap(ch)
        for (gx, gy) in pixels:
            if shadow:
                px.dot(cx + gx + 1, y + gy - BASELINE - 1, shadow)
            px.dot(cx + gx, y + gy - BASELINE, color)
        cx += w + spacing
    return cx - x - spacing


def font_atlas(cols=16, cell_w=6):
    """Atlas da fonte: cada glifo numa célula cell_w x CELL_H, em branco. Volta (imagem, lista de (char, x, y, w))."""
    rows = (len(FONT_CHARS) + cols - 1) // cols
    atlas = Px(cols * cell_w, rows * CELL_H)
    table = []
    for i, ch in enumerate(FONT_CHARS):
        col, row = i % cols, i // cols
        ox, oy = col * cell_w, (rows - 1 - row) * CELL_H
        w, pixels = glyph_bitmap(ch)
        for (gx, gy) in pixels:
            atlas.dot(ox + gx, oy + gy, "cream")
        table.append((ch, ox, oy, w))
    return atlas, table


# ---------------------------------------------------------------- Fonte pequena (só números, 3x5)

SMALL = {
    "0": ["###", "#.#", "#.#", "#.#", "###"], "1": [".#.", "##.", ".#.", ".#.", "###"],
    "2": ["###", "..#", "###", "#..", "###"], "3": ["###", "..#", ".##", "..#", "###"],
    "4": ["#.#", "#.#", "###", "..#", "..#"], "5": ["###", "#..", "###", "..#", "###"],
    "6": ["###", "#..", "###", "#.#", "###"], "7": ["###", "..#", "..#", ".#.", ".#."],
    "8": ["###", "#.#", "###", "#.#", "###"], "9": ["###", "#.#", "###", "..#", "###"],
    ".": ["...", "...", "...", "...", ".#."], "$": [".##", "##.", ".#.", ".##", "##."],
    "-": ["...", "...", "###", "...", "..."], "/": ["..#", "..#", ".#.", "#..", "#.."],
    "x": ["...", "#.#", ".#.", "#.#", "..."], "H": ["#.#", "#.#", "###", "#.#", "#.#"],
    "U": ["#.#", "#.#", "#.#", "#.#", "###"], "S": [".##", "#..", ".#.", "..#", "##."],
    "D": ["##.", "#.#", "#.#", "#.#", "##."], "M": ["#.#", "###", "###", "#.#", "#.#"],
    "C": [".##", "#..", "#..", "#..", ".##"], "r": ["...", "...", "#.#", "##.", "#.."],
    "A": [".#.", "#.#", "###", "#.#", "#.#"], "B": ["##.", "#.#", "##.", "#.#", "##."],
    "E": ["###", "#..", "##.", "#..", "###"], "L": ["#..", "#..", "#..", "#..", "###"],
    "O": ["###", "#.#", "#.#", "#.#", "###"], "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "I": ["###", ".#.", ".#.", ".#.", "###"], "N": ["#.#", "###", "###", "###", "#.#"],
    "P": ["##.", "#.#", "##.", "#..", "#.."], "R": ["##.", "#.#", "##.", "#.#", "#.#"],
    "Z": ["###", "..#", ".#.", "#..", "###"], "F": ["###", "#..", "##.", "#..", "#.."],
    "G": [".##", "#..", "#.#", "#.#", ".##"], "K": ["#.#", "#.#", "##.", "#.#", "#.#"],
    "V": ["#.#", "#.#", "#.#", "#.#", ".#."], "W": ["#.#", "#.#", "###", "###", "#.#"],
    "Y": ["#.#", "#.#", ".#.", ".#.", ".#."], " ": ["...", "...", "...", "...", "..."],
}


def draw_small(px, x, y, text, color):
    """Texto 3x5 com a base em y."""
    cx = x
    for ch in text:
        rows = SMALL.get(ch, SMALL[" "])
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    px.dot(cx + c, y + 4 - r, color)
        cx += 4
    return cx - x - 1
