"""Gera a fonte da UI (Fonte/FirmaPixel.ttf) a partir dos glifos pixel da Cena do Golpe.

É a mesma letra dos balões e letreiros do Miguel (HeistScene/Art/Source~/paleta.py), virada TrueType pra
UI Toolkit usar, com alguns caracteres a mais que a interface precisa (&, _, …, —, #, =, < >, [ ], º, ª, ×).
Cada pixel vira um quadrado de 100 unidades e o em tem 11 pixels: em font-size 11px, 1 pixel da fonte = 1 pixel
da tela de referência (640x360). Use tamanhos múltiplos de 11 (11, 22, 33, 44).

Uso (da raiz do repositório):  python "Assets/_Project/UI/Art/Source~/gerar_fonte.py"
Precisa de fontTools (pip install fonttools).
"""
import sys
from pathlib import Path

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

HERE = Path(__file__).resolve().parent
OUT = HERE.parents[1] / "Fonte/FirmaPixel.ttf"
sys.path.insert(0, str(HERE.parents[2] / "HeistScene/Art/Source~"))

import paleta  # noqa: E402

EXTRA = {
    "&": [".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#"],
    "_": ["....", "....", "....", "....", "....", "....", "....", "####"],
    "…": [".....", ".....", ".....", ".....", ".....", ".....", "#.#.#"],
    "—": [".......", ".......", ".......", "#######", ".......", ".......", "......."],
    "–": ["....", "....", "....", "####", "....", "....", "...."],
    "#": [".#.#.", ".#.#.", "#####", ".#.#.", "#####", ".#.#.", ".#.#."],
    "=": ["....", "....", "####", "....", "####", "....", "...."],
    "<": ["...#", "..#.", ".#..", "#...", ".#..", "..#.", "...#"],
    ">": ["#...", ".#..", "..#.", "...#", "..#.", ".#..", "#..."],
    "[": ["##", "#.", "#.", "#.", "#.", "#.", "##"],
    "]": ["##", ".#", ".#", ".#", ".#", ".#", "##"],
    "{": ["..#", ".#.", ".#.", "#..", ".#.", ".#.", "..#"],
    "}": ["#..", ".#.", ".#.", "..#", ".#.", ".#.", "#.."],
    "|": ["#", "#", "#", "#", "#", "#", "#"],
    "·": [".", ".", ".", "#", ".", ".", "."],
    "º": ["###", "#.#", "###", "...", "###", "...", "..."],
    "ª": [".##", "#.#", ".##", "...", "###", "...", "..."],
    "°": ["###", "#.#", "###", "...", "...", "...", "..."],
    "×": ["...", "...", "#.#", ".#.", "#.#", "...", "..."],
    "“": ["#.#", "#.#", "...", "...", "...", "...", "..."],
    "”": ["#.#", "#.#", "...", "...", "...", "...", "..."],
    "‘": ["#", "#", ".", ".", ".", ".", "."],
    "’": ["#", "#", ".", ".", ".", ".", "."],
}

U = 100                         # unidades por pixel
CELL_H, BASE = paleta.CELL_H, paleta.BASELINE


def bitmap(ch):
    if ch in EXTRA:
        rows = EXTRA[ch]
        w = max(len(r) for r in rows)
        return w, {(c, CELL_H - 3 - r) for r, row in enumerate(rows) for c, v in enumerate(row) if v == "#"}
    return paleta.glyph_bitmap(ch)


def glyph(pixels):
    """Um retângulo por trecho contínuo de cada linha (sentido horário, como pede o TrueType)."""
    pen = TTGlyphPen(None)
    for y in sorted({y for _, y in pixels}):
        xs = sorted(x for x, yy in pixels if yy == y)
        start = prev = xs[0]
        for x in xs[1:] + [None]:
            if x == prev + 1:
                prev = x
                continue
            x0, x1, y0, y1 = start * U, (prev + 1) * U, (y - BASE) * U, (y - BASE + 1) * U
            pen.moveTo((x0, y0))
            pen.lineTo((x0, y1))
            pen.lineTo((x1, y1))
            pen.lineTo((x1, y0))
            pen.closePath()
            if x is not None:
                start = prev = x
    return pen.glyph()


def main():
    chars = sorted(set(paleta.FONT_CHARS) | set(EXTRA))
    names = [".notdef"] + [f"uni{ord(c):04X}" for c in chars]
    glyphs, metrics = {".notdef": TTGlyphPen(None).glyph()}, {".notdef": (4 * U, 0)}
    for ch, name in zip(chars, names[1:]):
        w, pixels = bitmap(ch)
        glyphs[name] = glyph(pixels) if pixels else TTGlyphPen(None).glyph()
        metrics[name] = ((w + 1) * U, min((x for x, _ in pixels), default=0) * U)

    ascent, descent = (CELL_H - BASE) * U, -BASE * U
    fb = FontBuilder(CELL_H * U, isTTF=True)
    fb.setupGlyphOrder(names)
    fb.setupCharacterMap({ord(c): n for c, n in zip(chars, names[1:])})
    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=ascent, descent=descent, lineGap=U)
    fb.setupNameTable({"familyName": "Firma Pixel", "styleName": "Regular"})
    fb.setupOS2(sTypoAscender=ascent, sTypoDescender=descent, sTypoLineGap=U,
                usWinAscent=ascent, usWinDescent=-descent)
    fb.setupPost()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    fb.save(str(OUT))
    print("ok:", OUT, len(chars), "caracteres")


if __name__ == "__main__":
    main()
