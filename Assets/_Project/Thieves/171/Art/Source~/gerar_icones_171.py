"""Gera os ícones e o efeito visual do 171 pixel a pixel: ícone da Lábia, ícone do Bilhete premiado e o balão
"BLÁ BLÁ" que sobe da sala quando ele usa a Lábia.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/171/Art/Source~/gerar_icones_171.py"
Precisa de Pillow (pip install pillow). Usa a paleta e a fonte da Cena do Golpe (HeistScene/Art/Source~),
como os ícones dos outros ladrões. O sprite e o retrato do 171 continuam no gerar_171.py.
Saída: Assets/_Project/Thieves/171/Icons/*.png e VFX/Labia.png (e prévias nesta pasta).
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
THIEF = HERE.parents[1]
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Art/Source~"))

from paleta import Px, draw_text, text_width  # noqa: E402


def balloon(px, x0, y0, x1, y1, tail_x):
    """Balão de fala creme com a pontinha embaixo, apontando pro 171."""
    px.rect(x0 + 1, y0, x1 - 1, y1, "cream")
    px.rect(x0, y0 + 1, x1, y1 - 1, "cream")
    px.rect(tail_x, y0 - 3, tail_x + 2, y0, "cream")
    px.rect(tail_x, y0 - 4, tail_x, y0 - 3, "cream")


def icone_labia():
    """Balão de conversa mole ("BLÁ") e o alerta descendo: a Lábia baixa 25 de alerta na hora."""
    px = Px(32, 32)
    balloon(px, 2, 13, 25, 29, 5)
    draw_text(px, 5, 18, "BLÁ", "wine")
    px.rect(27, 6, 29, 20, "red")                       # barra de alerta
    px.rect(27, 6, 29, 12, "money")                     # o que sobrou depois da conversa
    px.rect(20, 7, 22, 11, "money")                     # seta pra baixo
    px.rect(18, 5, 24, 6, "money")
    px.rect(19, 4, 23, 4, "money")
    px.rect(20, 3, 22, 3, "money")
    px.dot(21, 2, "money")
    px.outline()
    return px


def icone_bilhete_premiado():
    """O bilhete premiado de mentira: papel com os números, a estrela dourada e o carimbo PREMIADO."""
    px = Px(32, 32)
    px.rect(3, 6, 28, 25, "paper")                      # bilhete
    px.rect(3, 6, 28, 7, "kraft")
    for x in range(4, 28, 3):                           # picote do canhoto
        px.dot(x, 22, "tan")
    for i, x in enumerate(range(6, 24, 5)):             # números da loteria
        px.rect(x, 14, x + 3, 18, "wine" if i % 2 else "slate")
    px.rect(5, 9, 22, 11, "red")                        # carimbo
    px.hline(6, 21, 10, "cream")
    px.dot(25, 28, "gold")                              # estrela de "premiado", na ponta do bilhete
    px.hline(23, 27, 27, "gold")
    px.hline(24, 26, 26, "gold")
    px.dot(24, 25, "gold")
    px.dot(26, 25, "gold")
    px.dot(25, 27, "lamp")
    px.outline()
    return px


def labia_vfx():
    """Balão grande "BLÁ BLÁ" com os corações da conversa mole: sobe da sala quando o 171 usa a Lábia."""
    text = "BLÁ BLÁ"
    w = text_width(text) + 10
    W, H = w + 7, 22
    px = Px(W, H)
    balloon(px, 0, 5, w, 21, 4)
    draw_text(px, 5, 10, text, "wine")
    for cx, cy in ((w + 3, 17), (w + 4, 9)):            # coraçõezinhos
        px.dot(cx - 1, cy, "neon"); px.dot(cx + 1, cy, "neon")
        px.rect(cx - 1, cy - 1, cx + 1, cy - 1, "neon")
        px.dot(cx, cy - 2, "neon")
    px.outline()
    return px


def main():
    icons = THIEF / "Icons"
    vfx = THIEF / "VFX"
    icons.mkdir(exist_ok=True)
    vfx.mkdir(exist_ok=True)
    outputs = {
        icons / "Icon_Labia.png": icone_labia(),
        icons / "Icon_BilhetePremiado.png": icone_bilhete_premiado(),
        vfx / "Labia.png": labia_vfx(),
    }
    from PIL import Image
    for path, px in outputs.items():
        px.img.save(path)
        px.img.resize((px.w * 8, px.h * 8), Image.NEAREST).save(HERE / f"previa_{path.stem.lower()}.png")
        print("ok:", path.relative_to(THIEF.parents[2]), px.img.size)


if __name__ == "__main__":
    main()
