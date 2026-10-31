"""Gera a arte do Esconderijo vivo (ESCOPO 9 e pedido do professor): o que aparece no porão da Cida conforme a firma
cresce. Cada upgrade vira um objeto no porão (com níveis), os bicos passam nos monitores e quem está preso vira foto.

Uso (da raiz do repositório):  python "Assets/_Project/HeistScene/Art/Source~/gerar_esconderijo.py" [--previas]
Precisa de Pillow. Desenhado na mesma escala do fundo do esconderijo (UI/Art/Esconderijo.png, 640x360): 1 px aqui
é 1 px da tela de referência. Saída em Assets/_Project/HeistScene/Art/Esconderijo/.
"""
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

from paleta import Px, draw_text, shade, text_width  # noqa: E402

ART = HERE.parent / "Esconderijo"
PREVIEW = HERE / "previas" if "--previas" in sys.argv else None
rng = random.Random(1992)


def save(px, name, scale=4):
    px.save(ART / f"{name}.png", PREVIEW, scale)


# ---------------------------------------------------------------- monitores dos bicos

def monitor(on):
    """TV de tubo 56x40 com a tela (48x32) vazada em (4, 5): a cena do bico aparece por trás."""
    px = Px(56, 40)
    px.rect(0, 2, 55, 39, "slate")
    px.rect(1, 3, 54, 38, "steel")
    px.frame(0, 2, 55, 39, "ink")
    px.rect(2, 4, 53, 4, shade("steel", -1))
    for x in range(4, 52):                 # some a tela: fica só a moldura
        for y in range(5, 37):
            px.dot(x, y, "ink", 0)
    px.frame(3, 4, 52, 37, "ink")
    px.rect(44, 0, 50, 1, "ink")            # pezinho
    px.rect(5, 0, 11, 1, "ink")
    px.dot(50, 3, "money" if on else "red")  # LED
    return px


def shelf(w=344):
    px = Px(w, 8)
    px.rect(0, 3, w - 1, 7, "wood")
    px.hline(0, w - 1, 7, "tan")
    px.hline(0, w - 1, 3, "brown")
    for x in (10, w - 16):                  # mão-francesa
        px.rect(x, 0, x + 5, 2, "slate")
    return px


def neon(lit):
    text = "PONTO DE BICO"
    w = text_width(text) + 10
    px = Px(w, 15)
    px.rect(0, 0, w - 1, 14, "night")
    px.frame(0, 0, w - 1, 14, "ink")
    color = "neon" if lit else "purple"
    draw_text(px, 5, 5, text, color, shadow="winedark" if lit else None)
    if lit:
        px.frame(1, 1, w - 2, 13, "purple")
    return px


# ---------------------------------------------------------------- Van: a Kombi da firma

def kombi(level):
    px = Px(124, 64)
    body_low, body_top = ("red", "cream") if level == 1 else ("police", "cream")
    px.rect(6, 10, 117, 30, body_low)
    px.rect(8, 30, 115, 50, body_top)
    px.rect(110, 12, 119, 44, body_top)                       # frente arredondada
    px.rect(108, 10, 119, 30, body_low)
    for x0 in (14, 34, 54, 74):                                # janelas
        px.rect(x0, 34, x0 + 16, 46, "dusk")
        px.rect(x0 + 1, 41, x0 + 4, 45, "steel")
    px.rect(96, 34, 116, 46, "dusk")                           # para-brisa
    px.line(96, 34, 104, 46, "steel")
    px.rect(60, 12, 62, 28, shade(body_low, -1))               # porta
    # V da frente da Kombi
    for i in range(8):
        px.dot(112 - i // 2, 30 - i, body_top)
        px.dot(116 + i // 3, 30 - i, body_top)
    px.disc(116, 20, 3, "lamp")                                # farol
    px.rect(4, 8, 119, 10, "ink")                              # para-choque
    for cx in (24, 96):                                        # rodas
        px.disc(cx, 8, 8, "ink")
        px.disc(cx, 8, 4, "steel")
        px.dot(cx, 8, "pale")
    px.frame(6, 10, 119, 50, "ink")
    if level >= 2:
        # turbinada: bagageiro com malas, faixa de corrida e rodas cromadas
        px.rect(12, 52, 108, 53, "ink")
        for x in (14, 60, 106):
            px.rect(x, 50, x + 1, 53, "ink")
        for (x0, w, c) in ((18, 22, "wood"), (42, 18, "kraft"), (62, 26, "green"), (90, 14, "wine")):
            px.rect(x0, 54, x0 + w, 54 + 6 + rng.randint(0, 4), c)
            px.frame(x0, 54, x0 + w, 60, "ink")
        px.rect(8, 24, 117, 26, "gold")
        draw_text(px, 66, 15, "TURBO", "cream")
        for cx in (24, 96):
            px.disc(cx, 8, 4, "pale")
    return px


# ---------------------------------------------------------------- Quartos: colchonetes enrolados

def sleeping_bag(color):
    px = Px(30, 14)
    px.rect(2, 1, 27, 12, color)
    px.disc(4, 7, 5, color)
    px.disc(25, 7, 5, color)
    px.disc(25, 7, 3, shade(color, -1))
    px.disc(25, 7, 1, shade(color, 1))
    for x in (9, 18):
        px.rect(x, 0, x + 1, 13, "umber")
    px.outline()
    return px


# ---------------------------------------------------------------- Tênis de molinha: sapateira

def shoe(px, x, y, color):
    px.rect(x, y, x + 9, y + 2, "cream")                       # solado com molinha
    px.dot(x + 2, y, "steel")
    px.dot(x + 6, y, "steel")
    px.rect(x, y + 3, x + 8, y + 6, color)
    px.rect(x, y + 7, x + 4, y + 8, color)
    px.dot(x + 6, y + 5, "cream")


def shoe_rack(pairs):
    colors = ["red", "blue", "money", "lamp", "coral"]
    px = Px(62, 34)
    for y in (0, 16):
        px.rect(0, y, 61, y + 2, "wood")
        px.hline(0, 61, y + 2, "tan")
    px.rect(0, 0, 2, 33, "wood")
    px.rect(59, 0, 61, 33, "wood")
    spots = [(4, 3), (32, 3), (4, 19), (32, 19), (18, 27)]
    for i in range(min(pairs, 5)):
        x, y = spots[i]
        shoe(px, x, y, colors[i])
        shoe(px, x + 12, y, colors[i])
    return px


# ---------------------------------------------------------------- Ferramentas: painel de ferramentas

def pegboard():
    px = Px(66, 100)
    px.rect(0, 0, 65, 99, "kraft")
    px.frame(0, 0, 65, 99, "wood")
    px.frame(1, 1, 64, 98, "tan")
    for y in range(6, 96, 8):
        for x in range(6, 62, 8):
            px.dot(x, y, "wood")
    return px


def tool(kind):
    px = Px(16, 28)
    if kind == "martelo":
        px.rect(7, 0, 8, 20, "wood"); px.rect(3, 20, 13, 25, "steel"); px.rect(3, 25, 5, 27, "steel")
    elif kind == "chave":
        px.rect(7, 0, 8, 22, "mist"); px.ring(8, 23, 3, "mist"); px.rect(7, 25, 9, 27, "ink", 0)
    elif kind == "serrote":
        px.rect(4, 0, 9, 7, "wood"); px.rect(5, 7, 11, 27, "pale")
        for y in range(8, 27, 2): px.dot(12, y, "mist")
    elif kind == "pe-de-cabra":
        px.rect(7, 0, 8, 24, "red"); px.rect(8, 24, 11, 25, "red"); px.rect(4, 0, 7, 1, "red")
    elif kind == "furadeira":
        px.rect(4, 0, 8, 9, "lamp"); px.rect(2, 9, 12, 16, "lamp"); px.rect(12, 12, 15, 13, "steel"); px.dot(5, 13, "ink")
    elif kind == "fenda":
        px.rect(6, 0, 9, 9, "red"); px.rect(7, 9, 8, 24, "mist")
    elif kind == "alicate":
        px.rect(5, 0, 6, 14, "blue"); px.rect(9, 0, 10, 14, "blue"); px.rect(6, 14, 9, 22, "steel")
    elif kind == "chaves":
        px.ring(8, 22, 4, "gold")
        for (x, h) in ((5, 12), (8, 9), (11, 14)): px.rect(x, 22 - h, x + 1, 20, "gold")
    elif kind == "lanterna":
        px.rect(6, 0, 10, 16, "ink"); px.rect(5, 16, 11, 20, "steel"); px.rect(6, 20, 10, 21, "lamp")
    elif kind == "pe-de-cabra-ouro":
        px.rect(7, 0, 8, 24, "gold"); px.rect(8, 24, 11, 25, "gold"); px.rect(4, 0, 7, 1, "gold"); px.dot(7, 12, "cream")
    px.outline()
    return px


TOOLS = ["martelo", "chave", "pe-de-cabra", "serrote", "fenda", "alicate", "furadeira", "lanterna", "chaves", "pe-de-cabra-ouro"]


# ---------------------------------------------------------------- Advogado: diploma na parede

def diploma(stars):
    px = Px(66, 50)
    px.rect(0, 0, 65, 49, "wood" if stars < 5 else "gold")
    px.rect(3, 3, 62, 46, "paper")
    px.frame(0, 0, 65, 49, "ink")
    draw_text(px, 33 - text_width("DR. ARLINDO") // 2, 34, "DR. ARLINDO", "ink")
    draw_text(px, 33 - text_width("ADVOGADO") // 2, 23, "ADVOGADO", "wine")
    px.hline(10, 55, 18, "kraft")
    px.disc(52, 10, 4, "red")                                  # selo
    px.dot(52, 10, "gold")
    for i in range(min(stars, 5)):                             # uma estrela por nível
        sx = 8 + i * 8
        px.dot(sx, 12, "gold"); px.rect(sx - 1, 9, sx + 1, 11, "gold"); px.dot(sx - 2, 10, "gold"); px.dot(sx + 2, 10, "gold")
    return px


# ---------------------------------------------------------------- Contatos: telefone e agenda

def phone():
    px = Px(28, 18)
    px.rect(2, 0, 25, 9, "red")
    px.rect(4, 9, 23, 11, "wine")
    px.rect(0, 12, 27, 15, "red")                              # fone no gancho
    px.rect(0, 12, 4, 17, "red"); px.rect(23, 12, 27, 17, "red")
    px.ring(13, 5, 3, "cream")
    px.outline()
    return px


def rolodex(level):
    px = Px(30, 24)
    px.rect(2, 0, 27, 3, "ink")
    px.rect(5, 3, 7, 8, "ink"); px.rect(22, 3, 24, 8, "ink")
    cards = 3 + level * 2
    for i in range(cards):
        y = 6 + i * 14 // max(cards, 1)
        px.rect(8 - i % 2, y, 21 + i % 2, y + 9, "cream" if i % 3 else "paper")
        px.hline(8, 21, y + 9, "mist")
    px.disc(14, 8, 2, "slate")
    return px


# ---------------------------------------------------------------- Lavanderia: máquina de lavar dinheiro

def washer(frame):
    px = Px(40, 44)
    dx = frame % 2                                             # tremendo
    px.rect(dx, 0, 37 + dx, 43, "pale")
    px.frame(dx, 0, 37 + dx, 43, "ink")
    px.rect(1 + dx, 34, 36 + dx, 42, "mist")
    px.rect(4 + dx, 37, 14 + dx, 40, "night")                  # painel
    px.dot(30 + dx, 38, "money" if frame % 2 else "lamp")
    px.dot(33 + dx, 38, "red")
    cx, cy = 18 + dx, 17
    px.disc(cx, cy, 13, "steel")
    px.disc(cx, cy, 11, "night")
    # notas girando dentro do tambor
    import math
    for k in range(5):
        a = frame * 0.9 + k * 1.25
        nx, ny = cx + round(math.cos(a) * 6), cy + round(math.sin(a) * 6)
        px.rect(nx - 2, ny - 1, nx + 2, ny + 1, "money")
        px.dot(nx, ny, "mint")
    px.disc(cx - 5, cy + 5, 2, "cyan", ) if frame % 4 == 0 else None
    px.ring(cx, cy, 12, "ink")
    return px


# ---------------------------------------------------------------- Rádio da polícia

def scanner(frame, level):
    px = Px(54, 26)
    px.rect(0, 0, 53, 19, "ink")
    px.rect(1, 1, 52, 18, "night")
    px.rect(4, 8, 30, 15, "deepgreen")
    draw_text(px, 6, 10, "190", "money" if frame % 2 == 0 else "mint")
    px.rect(34, 6, 38, 10, "red" if frame % 2 == 0 else "winedark")
    px.rect(42, 6, 46, 10, "blue" if frame % 2 == 1 else "police")
    for i in range(min(level, 5)):                             # barrinhas de sinal por nível
        px.rect(4 + i * 5, 3, 6 + i * 5, 4 + i, "lamp")
    px.rect(48, 19, 49, 25, "steel")                           # antena
    px.dot(49, 25, "red")
    return px


# ---------------------------------------------------------------- Orelhão (golpes no automático)

def orelhao(ringing):
    px = Px(40, 46)
    px.rect(2, 10, 37, 40, "coral")                            # concha laranja
    px.disc(19, 36, 18, "coral")
    for x in range(40):
        for y in range(40, 46):
            px.dot(x, y, "ink", 0)
    px.rect(8, 10, 31, 34, "kraft")
    px.rect(12, 14, 26, 30, "slate")                           # aparelho
    px.rect(14, 24, 24, 28, "ink")
    for (x, y) in ((14, 16), (18, 16), (22, 16), (14, 20), (18, 20), (22, 20)):
        px.dot(x, y, "pale")
    px.rect(27, 14, 29, 30, "ink")                             # fone
    px.outline()
    if ringing:
        for i, (x, y) in enumerate(((1, 30), (36, 30), (3, 36), (34, 36))):
            px.dot(x, y + i % 2, "lamp")
    return px


# ---------------------------------------------------------------- Mural: EM CANA

def stamp():
    text = "EM CANA"
    w = text_width(text) + 6
    px = Px(w, 11)
    px.frame(0, 0, w - 1, 10, "red")
    draw_text(px, 3, 3, text, "red")
    return px


def main():
    save(monitor(True), "Monitor_ligado")
    save(monitor(False), "Monitor_desligado")
    save(shelf(), "Prateleira", 2)
    save(neon(True), "Neon_aceso")
    save(neon(False), "Neon_apagado")
    for lv in (1, 2):
        save(kombi(lv), f"Kombi_{lv}", 3)
    for i, c in enumerate(("green", "wine", "police", "bronze", "purple")):
        save(sleeping_bag(c), f"Colchonete_{i}")
    for n in range(1, 6):
        save(shoe_rack(n), f"Sapateira_{n}")
    save(pegboard(), "Painel_ferramentas", 3)
    for i, t in enumerate(TOOLS):
        save(tool(t), f"Ferramenta_{i}")
    for n in range(1, 6):
        save(diploma(n), f"Diploma_{n}")
    save(phone(), "Telefone")
    for n in range(1, 6):
        save(rolodex(n), f"Agenda_{n}")
    for f in range(4):
        save(washer(f), f"Maquina_{f}")
    for lv in range(1, 6):
        for f in range(2):
            save(scanner(f, lv), f"Radio_{lv}_{f}")
    save(orelhao(False), "Orelhao")
    save(orelhao(True), "Orelhao_tocando")
    save(stamp(), "Carimbo_em_cana", 6)
    print("ok: esconderijo vivo")


if __name__ == "__main__":
    main()
