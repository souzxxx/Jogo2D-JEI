"""Gera toda a arte da Cena do Golpe, pixel a pixel, na paleta do jogo, e o layout dos prédios.

Uso (da raiz do repositório):  python "Assets/_Project/HeistScene/Art/Source~/gerar_cena.py" [--previas]
Precisa de Pillow (pip install pillow). A pasta Source~ é ignorada pelo Unity.

Saída em Assets/_Project/HeistScene/Art/:
  Salas/        fundo de cada sala (um por sala de cada golpe + um padrão por RoomKind)
  Aderecos/     o obstáculo de cada sala, fechado e aberto (grade, cofre, painel, CFTV...)
  Personagens/  NPCs (guardas, Seu Zé, crupiê...) e o ladrão genérico (placeholder de quem ainda não tem arte)
  Predios/      casca de cada prédio: fachada, paredes, laje, fundação, rua e porta de cada sala
  Rua/          poste, orelhão, banca, van da firma, viatura
  Fundo/        céu e camadas do parallax da cidade
  Icones/       ícone do obstáculo, anel de progresso, balão, alerta, sacola, partículas
  Fonte/        fonte pixel (com acento) usada nos balões e no "+Cr$"
  layout.json   posições de tudo, lido pelo HeistSceneBuilder (A Firma > Cena do Golpe)

Os prédios saem das salas de cada HeistDef (Data/Heists): mudou a lista de salas, rode de novo.
"""
import json
import random
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

from paleta import (Px, P, rgba, shade, draw_text, text_width, draw_small, font_atlas, palette_image,  # noqa: E402
                    CELL_H)
from boneco import spec, npc_frames, thief_frames, sheet, frame  # noqa: E402

ART = HERE.parent
REPO = HERE.parents[4]
HEISTS_DIR = REPO / "Assets/_Project/Data/Heists"
# prévias ampliadas (pra conferir a arte) só com --previas; ficam fora do Git
PREVIEW = HERE / "previas" if "--previas" in sys.argv else None

# ---------------------------------------------------------------- Medidas (px, 32 px = 1 unidade)

ROOM_W, ROOM_H, WALL = 192, 96, 8
CELL_W = ROOM_W + WALL
SLAB = 8
FLOOR_H = ROOM_H + SLAB
FOUND = 48
STREET_UG = 136          # nível da rua nos prédios com golpe no subsolo (cassino e banco)
MARGIN = 288             # rua que a casca do prédio desenha de cada lado
DOOR_H = 40
FEET = 3                 # onde fica o pé dos personagens dentro da faixa do piso
KINDS = ["Entrance", "Corridor", "Counter", "CameraRoom", "Office", "Vault", "Tunnel", "Garage"]
LOCK, SYSTEM, PERSON = 0, 1, 2

rng = random.Random(1992)


def unescape(s):
    s = s.strip()
    if s.startswith('"') and s.endswith('"'):
        s = s[1:-1]
    s = re.sub(r"\\x([0-9A-Fa-f]{2})", lambda m: chr(int(m.group(1), 16)), s)
    s = re.sub(r"\\u([0-9A-Fa-f]{4})", lambda m: chr(int(m.group(1), 16)), s)
    return s.replace('\\"', '"')


def load_heists():
    heists = []
    for path in sorted(HEISTS_DIR.glob("*.asset")):
        text = path.read_text(encoding="utf-8")
        h = {"id": re.search(r"^  id: (.+)$", text, re.M).group(1).strip(),
             "name": unescape(re.search(r"^  displayName: (.+)$", text, re.M).group(1)),
             "tier": int(re.search(r"^  tier: (\d+)$", text, re.M).group(1)), "rooms": []}
        for m in re.finditer(r"  - name: (.+)\n    kind: (\d+)\n    obstacle: (\d+)", text):
            h["rooms"].append({"name": unescape(m.group(1)), "kind": int(m.group(2)), "obstacle": int(m.group(3))})
        heists.append(h)
    heists.sort(key=lambda h: h["tier"])
    return heists


# ---------------------------------------------------------------- Temas

THEMES = {
    "padrao": dict(wall="mist", pattern="plain", trim="steel", floor="slate", floor2="dusk", lamp="lamp",
                   sep="slate", door="madeira", wood="wood"),
    "loterica": dict(wall="mint", pattern="stripes", trim="green", floor="pale", floor2="mist", lamp="lamp",
                     sep="green", door="madeira", wood="wood"),
    "joalheria": dict(wall="wine", pattern="damask", trim="winedark", floor="winedark", floor2="purple",
                      lamp="lamp", sep="winedark", door="madeira", wood="brown"),
    "cambio": dict(wall="pale", pattern="tiles", trim="steel", floor="slate", floor2="steel", lamp="cyan",
                   sep="steel", door="vidro", wood="tan"),
    "cassino": dict(wall="purple", pattern="damask", trim="winedark", floor="winedark", floor2="wine",
                    lamp="neon", sep="winedark", door="metal", wood="brown"),
    "banco": dict(wall="slate", pattern="panels", trim="dusk", floor="steel", floor2="slate", lamp="cyan",
                  sep="dusk", door="metal", wood="tan"),
    "tunel": dict(wall="brown", pattern="earth", trim="umber", floor="umber", floor2="brown", lamp="lamp",
                  sep="umber", door="tabuas", wood="wood"),
    "garagem": dict(wall="steel", pattern="concrete", trim="slate", floor="slate", floor2="dusk", lamp="pale",
                    sep="slate", door="metal", wood="tan"),
    "pastelaria": dict(wall="cream", pattern="tiles", trim="red", floor="mist", floor2="pale", lamp="lamp",
                       sep="red", door="madeira", wood="wood"),
    "marmore": dict(wall="pale", pattern="marble", trim="mist", floor="pale", floor2="mist", lamp="lamp",
                    sep="steel", door="metal", wood="tan"),
}

# Luz ambiente (Light2D global) de cada prédio: cor e intensidade.
AMBIENT = {
    "loterica": ("lamp", 0.62), "joalheria": ("paper", 0.55), "cambio": ("pale", 0.58),
    "cassino": ("neon", 0.5), "banco": ("cyan", 0.5),
}


def room_theme(heist_id, room, index):
    kind = KINDS[room["kind"]]
    if kind == "Tunnel":
        return "tunel"
    if heist_id == "banco-central":
        return "garagem" if kind == "Garage" else ("marmore" if kind in ("Office", "Vault") else "banco")
    if heist_id == "cassino" and index == 0:
        return "pastelaria"
    if kind == "Garage":
        return "garagem"
    return heist_id if heist_id in THEMES else "padrao"


# ---------------------------------------------------------------- Peças de cenário

def outline_rect(px, x0, y0, x1, y1, fill, line="ink"):
    px.rect(x0, y0, x1, y1, fill)
    px.frame(x0, y0, x1, y1, line)


def wall_pattern(px, t, y0=8, y1=ROOM_H - 1):
    wall, pat = t["wall"], t["pattern"]
    px.rect(0, y0, ROOM_W - 1, y1, wall)
    dark = shade(wall, -1)
    if pat == "stripes":
        for x in range(0, ROOM_W, 8):
            px.rect(x, y0, x + 1, y1, shade(wall, 1) if wall != "cream" else "pale")
    elif pat == "damask":
        for y in range(y0 + 4, y1, 10):
            for x in range((y // 10) % 2 * 6, ROOM_W, 12):
                px.dot(x, y, dark); px.dot(x - 1, y - 1, dark); px.dot(x + 1, y - 1, dark); px.dot(x, y - 2, dark)
    elif pat == "tiles":
        for y in range(y0, y1 + 1, 10):
            px.hline(0, ROOM_W - 1, y, shade(wall, -1))
        for y in range(y0, y1 + 1, 10):
            for x in range((y // 10) % 2 * 5, ROOM_W, 10):
                px.vline(x, y, min(y + 9, y1), shade(wall, -1))
    elif pat == "panels":
        for x in range(0, ROOM_W, 24):
            px.vline(x, y0, y1, "ink")
            px.vline(x + 1, y0, y1, shade(wall, 1))
        px.hline(0, ROOM_W - 1, y0 + 44, shade(wall, -1))
    elif pat == "marble":
        px.dither(0, y0, ROOM_W - 1, y1, wall, "cream", 0.25)
        for _ in range(14):
            x, y = rng.randrange(ROOM_W), rng.randrange(y0, y1)
            for i in range(rng.randrange(6, 16)):
                px.dot(x + i, y + (i // 3) * rng.choice((1, -1)) // 2, "mist")
        for x in range(0, ROOM_W, 32):
            px.vline(x, y0, y1, "mist")
    elif pat == "concrete":
        px.dither(0, y0, ROOM_W - 1, y1, wall, shade(wall, -1), 0.18)
        for y in range(y0 + 22, y1, 24):
            px.hline(0, ROOM_W - 1, y, shade(wall, -1))
        for x in range(10, ROOM_W, 40):
            px.dot(x, y0 + 10, "slate"); px.dot(x, y0 + 34, "slate")
    elif pat == "earth":
        px.dither(0, y0, ROOM_W - 1, y1, "brown", "umber", 0.35)
        for _ in range(40):
            x, y = rng.randrange(ROOM_W), rng.randrange(y0, y1)
            px.rect(x, y, x + rng.randrange(1, 3), y + 1, rng.choice(("wood", "umber", "slate")))


def floor_strip(px, t):
    f, f2 = t["floor"], t["floor2"]
    px.rect(0, 0, ROOM_W - 1, 7, f)
    if t["pattern"] in ("earth",):
        px.dither(0, 0, ROOM_W - 1, 7, "umber", "brown", 0.4)
    elif t["floor"] in ("winedark",):
        px.dither(0, 0, ROOM_W - 1, 7, f, f2, 0.25)
    else:
        for x in range(0, ROOM_W, 8):
            px.rect(x, 0, x + 3, 3, f2)
            px.rect(x + 4, 4, x + 7, 7, f2)
    px.hline(0, ROOM_W - 1, 7, shade(f, 1))


def wainscot(px, t, top=26):
    if t["pattern"] in ("earth", "concrete", "marble"):
        return
    px.rect(0, 8, ROOM_W - 1, top, t["trim"])
    px.hline(0, ROOM_W - 1, top, shade(t["trim"], 1))
    px.hline(0, ROOM_W - 1, top + 1, shade(t["trim"], -1))
    px.hline(0, ROOM_W - 1, 8, shade(t["trim"], -1))
    for x in range(6, ROOM_W, 24):
        px.frame(x, 11, x + 17, top - 3, shade(t["trim"], -1))


def ceiling(px, t):
    px.rect(0, ROOM_H - 4, ROOM_W - 1, ROOM_H - 1, shade(t["wall"], -2))
    px.hline(0, ROOM_W - 1, ROOM_H - 5, shade(t["wall"], -1))


def lamp_hanging(px, x, t, cord=6):
    px.vline(x, ROOM_H - 4 - cord, ROOM_H - 5, "ink")
    y = ROOM_H - 5 - cord
    px.rect(x - 4, y - 2, x + 4, y, "ink")
    px.rect(x - 3, y - 2, x + 3, y - 1, shade(t["trim"], 0) if t["trim"] != "ink" else "slate")
    px.rect(x - 2, y - 3, x + 2, y - 3, t["lamp"])


def tube_lamp(px, x, t):
    px.rect(x - 12, ROOM_H - 6, x + 12, ROOM_H - 5, "ink")
    px.rect(x - 11, ROOM_H - 6, x + 11, ROOM_H - 6, "pale")


def clock(px, x, y):
    px.disc(x, y, 5, "ink")
    px.disc(x, y, 4, "cream")
    px.line(x, y, x, y + 3, "ink")
    px.line(x, y, x + 2, y, "ink")


def frame_picture(px, x, y, w, h, scene="landscape"):
    outline_rect(px, x, y, x + w, y + h, "bronze")
    px.rect(x + 2, y + 2, x + w - 2, y + h - 2, "blue")
    if scene == "landscape":
        px.rect(x + 2, y + 2, x + w - 2, y + h // 2, "green")
        px.disc(x + w - 6, y + h - 6, 2, "lamp")
        for i in range(x + 3, x + w - 3):
            px.dot(i, y + h // 2 + (i % 5 == 0), "money")
    elif scene == "portrait":
        px.rect(x + 2, y + 2, x + w - 2, y + h - 2, "winedark")
        px.disc(x + w // 2, y + h // 2 + 3, 3, "skin")
        px.rect(x + w // 2 - 4, y + 2, x + w // 2 + 4, y + h // 2, "ink")


def poster(px, x, y, w, h, bg, fg, lines):
    outline_rect(px, x, y, x + w, y + h, bg)
    yy = y + h - 9
    for line in lines:
        tw = text_width(line)
        draw_text(px, x + (w - tw) // 2 + 1, yy, line, fg)
        yy -= 9


def small_poster(px, x, y, w, h, bg, fg, lines):
    outline_rect(px, x, y, x + w, y + h, bg)
    yy = y + h - 7
    for line in lines:
        tw = len(line) * 4 - 1
        draw_small(px, x + (w - tw) // 2 + 1, yy, line, fg)
        yy -= 7


def plant(px, x):
    px.rect(x - 4, 4, x + 4, 12, "brown")
    px.frame(x - 4, 4, x + 4, 12, "ink")
    for i, (dx, dy) in enumerate([(-6, 22), (-3, 26), (0, 28), (3, 25), (6, 21), (-5, 16), (5, 17)]):
        px.line(x, 12, x + dx, dy, "green" if i % 2 else "money", 2)


def bench(px, x, w, t):
    px.rect(x, 12, x + w, 14, t["wood"])
    px.frame(x, 12, x + w, 14, "ink")
    px.rect(x + 2, 4, x + 3, 11, "ink")
    px.rect(x + w - 3, 4, x + w - 2, 11, "ink")
    px.rect(x, 15, x + w, 22, shade(t["wood"], -1))
    px.frame(x, 15, x + w, 22, "ink")


def counter(px, x0, x1, top, body, edge="ink"):
    px.rect(x0, 4, x1, 30, body)
    px.frame(x0, 4, x1, 30, edge)
    px.rect(x0 - 2, 30, x1 + 2, 33, top)
    px.frame(x0 - 2, 30, x1 + 2, 33, edge)
    for x in range(x0 + 6, x1 - 4, 14):
        px.frame(x, 8, x + 9, 26, shade(body, -1))


def cash_register(px, x, y):
    outline_rect(px, x, y, x + 14, y + 9, "steel")
    px.rect(x + 2, y + 6, x + 12, y + 8, "money")
    for i in range(3):
        px.rect(x + 2 + i * 4, y + 2, x + 4 + i * 4, y + 3, "pale")
    outline_rect(px, x + 3, y + 10, x + 11, y + 13, "slate")
    draw_small(px, x + 4, y + 10, "00", "mint")


def shelf(px, x, y, w, items):
    px.rect(x, y, x + w, y + 1, "ink")
    cx = x + 2
    for kind in items:
        if kind == "book":
            c = rng.choice(("wine", "police", "green", "bronze", "slate"))
            h = rng.randrange(6, 9)
            px.rect(cx, y + 2, cx + 1, y + 1 + h, c)
            cx += 3
        elif kind == "bag":
            px.disc(cx + 3, y + 5, 3, "kraft")
            px.rect(cx + 2, y + 8, cx + 4, y + 9, "tan")
            draw_small(px, cx + 2, y + 3, "$", "bronze") if False else px.dot(cx + 3, y + 5, "bronze")
            cx += 8
        elif kind == "gold":
            px.rect(cx, y + 2, cx + 6, y + 4, "gold")
            px.rect(cx + 1, y + 5, cx + 5, y + 6, "lamp")
            px.frame(cx, y + 2, cx + 6, y + 4, "bronze")
            cx += 8
        elif kind == "box":
            outline_rect(px, cx, y + 2, cx + 8, y + 8, "kraft")
            cx += 10
        elif kind == "notes":
            px.rect(cx, y + 2, cx + 7, y + 6, "money")
            px.frame(cx, y + 2, cx + 7, y + 6, "green")
            cx += 9


def desk(px, x, w, t, top_item="typewriter"):
    px.rect(x, 22, x + w, 24, t["wood"])
    px.frame(x, 22, x + w, 24, "ink")
    px.rect(x + 1, 4, x + 4, 21, shade(t["wood"], -1))
    px.rect(x + w - 12, 4, x + w - 1, 21, shade(t["wood"], -1))
    px.frame(x + w - 12, 4, x + w - 1, 21, "ink")
    px.hline(x + w - 11, x + w - 2, 13, "ink")
    px.dot(x + w - 6, 17, "gold")
    px.dot(x + w - 6, 9, "gold")
    if top_item == "typewriter":
        outline_rect(px, x + 6, 25, x + 20, 30, "slate")
        px.rect(x + 8, 31, x + 18, 35, "cream")
    elif top_item == "phone":
        outline_rect(px, x + 6, 25, x + 13, 28, "red")
        px.rect(x + 5, 29, x + 14, 30, "red")
    elif top_item == "lamp":
        px.rect(x + 8, 25, x + 12, 25, "ink")
        px.line(x + 10, 25, x + 13, 33, "green")
        px.rect(x + 11, 33, x + 17, 36, "green")
    elif top_item == "computer":
        outline_rect(px, x + 6, 25, x + 22, 38, "pale")
        px.rect(x + 8, 28, x + 20, 36, "deepgreen")
        px.hline(x + 9, x + 15, 33, "money")
        px.hline(x + 9, x + 18, 31, "money")


def chair(px, x, t, facing=1):
    px.rect(x - 4, 14, x + 4, 15, "ink")
    px.rect(x - 4, 16, x + 4, 16, shade(t["wood"], -1))
    bx = x - 4 if facing > 0 else x + 4
    px.rect(bx, 16, bx, 28, "ink")
    px.rect(x, 4, x, 13, "ink")
    px.rect(x - 4, 4, x + 4, 4, "ink")


def filing_cabinet(px, x):
    outline_rect(px, x, 4, x + 14, 40, "steel")
    for y in (8, 18, 28):
        px.frame(x + 2, y, x + 12, y + 8, "slate")
        px.rect(x + 6, y + 5, x + 8, y + 5, "pale")


def extinguisher(px, x):
    px.rect(x, 18, x + 4, 30, "red")
    px.frame(x, 18, x + 4, 30, "ink")
    px.rect(x + 1, 31, x + 3, 32, "ink")
    px.line(x + 3, 32, x + 6, 28, "ink")


def back_door(px, x, t, sign=None):
    outline_rect(px, x, 4, x + 22, 50, shade(t["wood"], 0))
    px.frame(x + 3, 8, x + 19, 26, shade(t["wood"], -1))
    px.frame(x + 3, 29, x + 19, 46, shade(t["wood"], -1))
    px.dot(x + 18, 27, "gold")
    if sign:
        tw = len(sign) * 4 - 1
        outline_rect(px, x + 11 - tw // 2 - 2, 53, x + 11 + tw // 2 + 2, 61, "cream")
        draw_small(px, x + 11 - tw // 2, 55, sign, "ink")


def window_back(px, x, y, w, h, lit=False, blinds=True):
    outline_rect(px, x, y, x + w, y + h, "night")
    if lit:
        px.rect(x + 1, y + 1, x + w - 1, y + h - 1, "dusk")
    for i in range(x + 2, x + w - 1, 5):
        px.dot(i, y + h - 4, "pale")  # estrela/luz de longe
    if blinds:
        for yy in range(y + h - 2, y + 1, -3):
            px.hline(x + 1, x + w - 1, yy, "slate")
    px.vline(x + w // 2, y, y + h, "ink")


def money_bags(px, x, n=3):
    for i in range(n):
        bx = x + i * 9
        px.disc(bx + 4, 9, 5, "kraft")
        px.rect(bx + 3, 14, bx + 5, 16, "tan")
        px.rect(bx + 2, 16, bx + 6, 16, "bronze")
        draw_small(px, bx + 3, 6, "$", "bronze")
    px.outline() if False else None


def gold_stack(px, x, rows=3):
    for r in range(rows):
        for i in range(rows - r):
            gx = x + i * 8 + r * 4
            gy = 4 + r * 4
            px.rect(gx, gy, gx + 7, gy + 3, "gold")
            px.rect(gx + 1, gy + 3, gx + 6, gy + 3, "lamp")
            px.frame(gx, gy, gx + 7, gy + 3, "bronze")


def slot_machine(px, x):
    outline_rect(px, x, 4, x + 16, 40, "red")
    outline_rect(px, x + 2, 24, x + 14, 34, "cream")
    for i, c in enumerate(("gold", "neon", "money")):
        px.rect(x + 4 + i * 4, 27, x + 5 + i * 4, 31, c)
    px.rect(x + 17, 26, x + 18, 36, "steel")
    px.disc(x + 18, 37, 2, "red")
    px.rect(x + 2, 6, x + 14, 14, "winedark")
    px.rect(x + 3, 38, x + 13, 40, "lamp")


def roulette_table(px, x):
    px.rect(x, 18, x + 44, 22, "deepgreen")
    px.frame(x, 18, x + 44, 22, "ink")
    px.rect(x - 1, 23, x + 45, 24, "wood")
    px.rect(x + 3, 4, x + 6, 17, "brown")
    px.rect(x + 38, 4, x + 41, 17, "brown")
    px.disc(x + 12, 27, 6, "wood")
    px.ring(x + 12, 27, 5, "red")
    px.disc(x + 12, 27, 2, "gold")
    for i in range(4):
        px.rect(x + 24 + i * 5, 25, x + 27 + i * 5, 26 + (i % 2), rng.choice(("red", "ink", "cream", "gold")))


def jewel_case(px, x, w=34):
    px.rect(x, 4, x + w, 20, "winedark")
    px.frame(x, 4, x + w, 20, "ink")
    px.rect(x, 21, x + w, 34, "pale")
    px.frame(x, 21, x + w, 34, "mist")
    for i in range(x + 3, x + w - 2, 6):
        c = rng.choice(("gold", "cyan", "lamp", "coral", "cream"))
        px.dot(i, 23, c)
        px.dot(i + 1, 24, c)
        px.dot(i, 25, "gold")
    px.line(x + 2, 33, x + 8, 27, "cream")


def rates_board(px, x, y):
    outline_rect(px, x, y, x + 46, y + 30, "ink")
    draw_small(px, x + 3, y + 23, "US$", "lamp")
    draw_small(px, x + 22, y + 23, "3.250", "mint")
    draw_small(px, x + 3, y + 15, "DM", "lamp")
    draw_small(px, x + 22, y + 15, "1.980", "mint")
    draw_small(px, x + 3, y + 7, "Cr$", "lamp")
    draw_small(px, x + 22, y + 7, "1.000", "coral")


def monitors_small(px, x, y):
    for i in range(2):
        outline_rect(px, x + i * 14, y, x + i * 14 + 12, y + 10, "slate")
        px.rect(x + i * 14 + 2, y + 2, x + i * 14 + 10, y + 8, "deepgreen")


def beam(px, x):
    px.rect(x, 0, x + 5, ROOM_H - 1, "wood")
    px.vline(x, 0, ROOM_H - 1, "brown")
    px.vline(x + 5, 0, ROOM_H - 1, "umber")
    px.rect(x - 8, ROOM_H - 8, x + 13, ROOM_H - 3, "wood")
    px.frame(x - 8, ROOM_H - 8, x + 13, ROOM_H - 3, "umber")


def bulb_string(px):
    for x in range(0, ROOM_W, 2):
        y = ROOM_H - 14 + round(3 * abs(((x % 48) - 24) / 24))
        px.dot(x, y, "ink")
        if x % 48 == 24:
            px.rect(x - 1, y - 3, x + 1, y - 1, "lamp")


def rails(px):
    px.hline(0, ROOM_W - 1, 2, "steel")
    px.hline(0, ROOM_W - 1, 5, "steel")
    for x in range(4, ROOM_W, 10):
        px.rect(x, 1, x + 3, 6, "wood")
    px.hline(0, ROOM_W - 1, 2, "steel")
    px.hline(0, ROOM_W - 1, 5, "steel")


def rocks(px, x, n=4):
    for i in range(n):
        rx = x + i * 5
        px.disc(rx, 5, 2 + (i % 2), rng.choice(("slate", "steel", "umber")))


def tools_tunnel(px, x):
    px.line(x, 4, x + 10, 22, "wood", 1)
    px.line(x + 8, 22, x + 14, 18, "steel", 2)  # picareta
    px.line(x + 16, 4, x + 18, 24, "wood", 1)
    px.rect(x + 16, 2, x + 21, 6, "steel")      # pá
    outline_rect(px, x + 24, 4, x + 34, 12, "slate")  # caixote
    px.rect(x + 26, 13, x + 32, 14, "kraft")


def pillar(px, x, w=10):
    px.rect(x, 0, x + w, ROOM_H - 1, "steel")
    px.vline(x, 0, ROOM_H - 1, "slate")
    px.vline(x + w, 0, ROOM_H - 1, "slate")
    for y in range(8, 30, 6):
        px.rect(x, y, x + w, y + 2, "lamp" if (y // 6) % 2 else "ink")


def armored_car(px, x):
    outline_rect(px, x, 6, x + 62, 34, "mist")
    outline_rect(px, x + 44, 20, x + 58, 32, "night")
    px.rect(x + 4, 14, x + 40, 18, "slate")
    draw_small(px, x + 8, 22, "VALORES", "police") if False else draw_small(px, x + 6, 22, "FORTE", "police")
    for wx in (x + 10, x + 48):
        px.disc(wx, 6, 5, "ink")
        px.disc(wx, 6, 2, "steel")


def hazard(px, x0, x1, y):
    for x in range(x0, x1):
        px.rect(x, y, x, y + 3, "lamp" if (x // 4) % 2 else "ink")


def scanner_arch(px, x):
    outline_rect(px, x, 4, x + 4, 58, "slate")
    outline_rect(px, x + 26, 4, x + 30, 58, "slate")
    outline_rect(px, x, 56, x + 30, 62, "slate")
    px.rect(x + 12, 58, x + 18, 59, "money")


def tv_set(px, x, y):
    outline_rect(px, x, y, x + 18, y + 14, "wood")
    px.rect(x + 2, y + 2, x + 13, y + 12, "police")
    px.rect(x + 3, y + 3, x + 12, y + 6, "blue")
    px.dot(x + 16, y + 10, "gold")
    px.dot(x + 16, y + 6, "gold")
    px.line(x + 6, y + 15, x + 3, y + 21, "ink")
    px.line(x + 12, y + 15, x + 15, y + 21, "ink")


def pastel_counter(px, x0, x1):
    counter(px, x0, x1, "red", "cream")
    outline_rect(px, x0 + 4, 34, x0 + 40, 48, "pale")  # estufa de pastel
    for i in range(4):
        px.rect(x0 + 7 + i * 8, 36, x0 + 12 + i * 8, 39, "gold")
        px.dot(x0 + 9 + i * 8, 39, "kraft")
    outline_rect(px, x0 + 50, 34, x0 + 62, 50, "money")  # garapeira
    px.rect(x0 + 52, 38, x0 + 60, 46, "mint")


# ---------------------------------------------------------------- Fundo das salas

def room_background(kind, theme_name, heist_id, room_name, obstacle, index):
    t = THEMES[theme_name]
    px = Px(ROOM_W, ROOM_H)
    wall_pattern(px, t)
    floor_strip(px, t)
    if kind != "Tunnel":
        wainscot(px, t)
        ceiling(px, t)
    k = kind
    if k == "Tunnel":
        beam(px, 30)
        beam(px, 110)
        bulb_string(px)
        rails(px)
        rocks(px, 60)
        if index == 0:
            tools_tunnel(px, 140)
        else:
            rocks(px, 150, 6)
            px.rect(175, 4, 191, 70, "slate")       # concreto do banco aparecendo no fim do túnel
            px.dither(175, 4, 191, 70, "slate", "steel", 0.2)
    elif k == "Garage":
        pillar(px, 46)
        pillar(px, 150)
        hazard(px, 0, ROOM_W, 40)
        armored_car(px, 70)
        tube_lamp(px, 40, t)
        tube_lamp(px, 150, t)
    elif k == "Entrance":
        lamp_hanging(px, 96, t)
        if heist_id == "loterica":
            poster(px, 20, 40, 44, 30, "money", "cream", ["LOTO", "FEZINHA"])
            small_poster(px, 70, 50, 30, 22, "lamp", "ink", ["MEGA", "45 MI"])
            bench(px, 26, 28, t)
            clock(px, 120, 74)
            px.rect(76, 4, 77, 26, "slate")                      # fila com cordinha
            px.rect(106, 4, 107, 26, "slate")
            px.line(77, 24, 106, 22, "red")
        elif heist_id == "joalheria":
            jewel_case(px, 18)
            jewel_case(px, 62)
            outline_rect(px, 112, 40, 130, 80, "bronze")         # espelho
            px.rect(114, 42, 128, 78, "pale")
            px.line(116, 50, 124, 70, "cream")
            poster(px, 24, 52, 70, 22, "ink", "gold", ["BRILHO", "ETERNO"])
        elif heist_id == "cambio":
            rates_board(px, 20, 44)
            bench(px, 76, 26, t)
            window_back(px, 112, 40, 26, 30)
            clock(px, 160, 80)
        elif heist_id == "cassino":   # a pastelaria (fachada do cassino)
            pastel_counter(px, 64, 136)
            poster(px, 70, 60, 64, 22, "red", "cream", ["PASTEL", "Cr$ 900"])
            tv_set(px, 150, 56)
        else:
            back_door(px, 20, t, "SAIDA")
            bench(px, 60, 30, t)
            clock(px, 120, 74)
        if heist_id != "cassino":
            plant(px, 182)
    elif k == "Counter":
        lamp_hanging(px, 60, t)
        lamp_hanging(px, 150, t)
        if heist_id == "loterica":
            counter(px, 96, 184, "green", "mint")
            outline_rect(px, 118, 34, 140, 54, "pale")       # vidro do caixa
            px.line(122, 38, 128, 50, "cream")
            cash_register(px, 150, 34)
            poster(px, 20, 44, 52, 28, "lamp", "ink", ["APOSTE", "AQUI"])
            shelf(px, 106, 66, 70, ["box", "box", "book", "book", "box"])
        elif heist_id == "cambio":
            counter(px, 90, 186, "steel", "slate")
            for x in (98, 132, 166):
                outline_rect(px, x, 34, x + 18, 58, "pale")
                px.rect(x + 2, 36, x + 16, 39, "cyan")
                draw_small(px, x + 3, 50, "US$", "police")
            rates_board(px, 24, 46)
        elif heist_id == "cassino":
            roulette_table(px, 20)
            slot_machine(px, 82)
            slot_machine(px, 104)
            poster(px, 130, 56, 50, 22, "neon", "cream", ["SORTE", "GRANDE"])
        else:
            counter(px, 100, 186, "steel", t["wood"])
            cash_register(px, 150, 34)
            bench(px, 24, 30, t)
    elif k == "Corridor":
        tube_lamp(px, 50, t)
        tube_lamp(px, 140, t)
        if heist_id == "banco-central":
            scanner_arch(px, 34)
            for x in (90, 140):
                back_door(px, x, THEMES["banco"])
            hazard(px, 0, ROOM_W, 30)
        else:
            back_door(px, 24, t, "WC" if heist_id == "cambio" else None)
            back_door(px, 84, t)
            extinguisher(px, 62)
            frame_picture(px, 120, 46, 24, 18)
            plant(px, 180)
    elif k == "CameraRoom":
        tube_lamp(px, 96, t)
        desk(px, 30, 44, t, "phone")
        chair(px, 60, t)
        filing_cabinet(px, 10)
        px.rect(84, 26, 86, 29, "cream")                         # cafezinho
        px.dot(85, 30, "pale")
        small_poster(px, 30, 54, 36, 22, "paper", "ink", ["TURNO", "22-06"])
    elif k == "Office":
        lamp_hanging(px, 96, t)
        if theme_name == "marmore":                            # antecâmara do banco
            for x in (20, 60, 100):
                outline_rect(px, x, 4, x + 12, 88, "pale")
                px.rect(x - 2, 84, x + 14, 88, "mist")
                px.rect(x - 2, 4, x + 14, 8, "mist")
            poster(px, 20, 60, 92, 14, "slate", "gold", ["COFRE CENTRAL"]) if False else None
        elif heist_id == "cassino":                            # escritório do bicheiro
            desk(px, 20, 52, t, "phone")
            chair(px, 50, t)
            for i, animal in enumerate(["LEAO", "VACA", "AGUIA"]):
                small_poster(px, 22 + i * 26, 52, 22, 18, "paper", "wine", [animal[:4]])
            frame_picture(px, 96, 50, 22, 26, "portrait")
            px.rect(140, 70, 144, 80, "steel")                   # ventilador
            px.disc(142, 78, 4, "slate")
        elif heist_id == "cambio":
            desk(px, 40, 56, t, "computer")
            chair(px, 70, t)
            filing_cabinet(px, 14)
            window_back(px, 104, 40, 28, 34)
            frame_picture(px, 140, 54, 20, 16)
        else:
            desk(px, 24, 56, t, "typewriter")
            chair(px, 54, t)
            shelf(px, 96, 60, 40, ["book"] * 10)
            shelf(px, 96, 44, 40, ["book"] * 8 + ["box"])
            frame_picture(px, 30, 54, 24, 18)
    elif k == "Vault":
        tube_lamp(px, 60, t) if theme_name != "loterica" else lamp_hanging(px, 60, t)
        if heist_id == "loterica":
            shelf(px, 20, 44, 50, ["box", "box", "notes", "box"])
            shelf(px, 20, 64, 50, ["book"] * 6 + ["box"])
            money_bags(px, 80, 2)
            px.rect(160, 4, 186, 6, "steel") if False else None
        elif heist_id == "banco-central":
            gold_stack(px, 14, 4)
            gold_stack(px, 52, 3)
            shelf(px, 16, 62, 70, ["gold", "gold", "gold", "gold", "gold", "gold", "gold", "gold"])
            money_bags(px, 86, 3)
        else:
            shelf(px, 16, 48, 60, ["notes", "bag", "notes", "gold", "bag"])
            shelf(px, 16, 68, 60, ["gold", "gold", "notes", "notes", "bag"])
            money_bags(px, 30, 3)
            gold_stack(px, 74, 2)
    return px


# ---------------------------------------------------------------- Aderecos do obstáculo (fechado / aberto)

def prop_grade(style="ferro"):
    """Grade de ferro com cadeado. 36x56."""
    out = {}
    bar = {"ferro": "slate", "enferrujada": "bronze", "dourada": "gold"}[style]
    for state in ("fechado", "aberto"):
        px = Px(36, 56)
        px.rect(0, 0, 35, 1, "ink")
        px.rect(0, 54, 35, 55, "ink")
        if state == "fechado":
            for x in range(1, 35, 5):
                px.rect(x, 2, x + 1, 53, bar)
                px.vline(x, 2, 53, shade(bar, 1))
            px.rect(0, 20, 35, 22, bar)
            px.rect(0, 40, 35, 42, bar)
            outline_rect(px, 14, 24, 21, 31, "gold")             # cadeado
            px.ring(17, 33, 2, "steel")
            px.dot(17, 27, "ink")
        else:
            for x in range(1, 9, 2):
                px.rect(x, 2, x + 1, 53, bar)
            px.rect(0, 20, 9, 22, bar)
            px.rect(0, 40, 9, 42, bar)
            outline_rect(px, 12, 2, 18, 8, "gold")               # cadeado aberto no chão
            px.ring(15, 10, 2, "steel")
        out[state] = px
    return out


def prop_cofrinho():
    out = {}
    for state in ("fechado", "aberto"):
        px = Px(26, 24)
        outline_rect(px, 1, 1, 24, 22, "slate")
        px.rect(2, 2, 23, 3, "dusk")
        if state == "fechado":
            outline_rect(px, 5, 6, 20, 19, "steel")
            px.disc(12, 12, 4, "slate")
            px.ring(12, 12, 4, "ink")
            px.line(12, 12, 14, 15, "lamp")
            px.dot(18, 9, "gold")
        else:
            px.rect(3, 4, 22, 20, "night")
            for i in range(6):
                px.rect(5 + i * 3, 5, 6 + i * 3, 7 + (i % 3), "gold")
            px.rect(6, 9, 13, 12, "money")
            px.rect(14, 9, 20, 11, "money")
            outline_rect(px, 22, 4, 25, 19, "steel")             # porta aberta
        out[state] = px
    return out


def prop_cofre(size=56, keypad=False):
    out = {}
    r = size // 2 - 3
    for state in ("fechado", "aberto"):
        px = Px(size + 14, size)
        cx, cy = size // 2, size // 2
        px.disc(cx, cy, r + 2, "ink")
        px.disc(cx, cy, r + 1, "slate")
        if state == "fechado":
            px.disc(cx, cy, r - 1, "steel")
            px.ring(cx, cy, r - 4, "slate")
            px.disc(cx, cy, 3, "slate")
            for a in range(6):
                import math
                ang = a * math.pi / 3
                px.line(cx, cy, cx + round(math.cos(ang) * (r - 6)), cy + round(math.sin(ang) * (r - 6)), "pale", 1)
            for a in range(8):
                import math
                ang = a * math.pi / 4
                px.disc(cx + round(math.cos(ang) * (r - 2)), cy + round(math.sin(ang) * (r - 2)), 1, "mist")
            if keypad:
                outline_rect(px, size + 2, cy - 8, size + 12, cy + 8, "slate")
                for i in range(3):
                    for j in range(3):
                        px.dot(size + 4 + i * 3, cy - 5 + j * 3, "pale")
                px.rect(size + 4, cy + 5, size + 10, cy + 6, "red")
        else:
            px.disc(cx, cy, r - 1, "night")
            for i in range(8):
                gx = cx - r + 6 + (i % 4) * 7
                gy = cy - r + 6 + (i // 4) * 5
                px.rect(gx, gy, gx + 5, gy + 3, "gold")
                px.hline(gx + 1, gx + 4, gy + 3, "lamp")
            px.rect(cx - 8, cy + 2, cx + 6, cy + 6, "money")
            # porta girada pro lado
            for y in range(cy - r, cy + r):
                w = 2 if abs(y - cy) < r - 3 else 1
                px.rect(size - 2, y, size - 2 + w + 2, y, "steel")
            px.vline(size + 2, cy - r + 2, cy + r - 2, "ink")
            if keypad:
                outline_rect(px, size + 4, cy - 8, size + 12, cy + 8, "slate")
                px.rect(size + 6, cy + 5, size + 10, cy + 6, "money")
        out[state] = px
    return out


def prop_arquivo():
    out = {}
    for state in ("fechado", "aberto"):
        px = Px(30, 48)
        outline_rect(px, 4, 1, 22, 46, "steel")
        for i, y in enumerate((4, 18, 32)):
            if state == "aberto" and i == 1:
                outline_rect(px, 4, y, 29, y + 11, "slate")
                px.rect(6, y + 8, 27, y + 13, "cream")
                px.rect(8, y + 9, 12, y + 15, "money")
            else:
                px.frame(6, y, 20, y + 11, "slate")
                px.rect(11, y + 7, 15, y + 7, "pale")
        if state == "fechado":
            px.disc(13, 44, 1, "gold")
            outline_rect(px, 10, 39, 16, 42, "gold")
        out[state] = px
    return out


def prop_painel():
    out = {}
    for state in ("fechado", "aberto"):
        px = Px(18, 26)
        outline_rect(px, 1, 1, 16, 24, "pale")
        px.rect(3, 15, 14, 21, "night")
        draw_small(px, 4, 16, "ON" if state == "fechado" else "OK", "red" if state == "fechado" else "money")
        for i in range(3):
            for j in range(3):
                px.dot(4 + i * 4, 4 + j * 3, "slate")
        px.rect(13, 3, 14, 4, "red" if state == "fechado" else "money")
        out[state] = px
    return out


def prop_terminal():
    out = {}
    for state in ("fechado", "aberto"):
        px = Px(34, 34)
        outline_rect(px, 2, 1, 31, 4, "steel")
        outline_rect(px, 4, 5, 29, 30, "pale")
        px.rect(7, 9, 26, 27, "night")
        if state == "fechado":
            draw_small(px, 8, 20, "SENHA", "red")
            px.hline(8, 22, 14, "red")
        else:
            draw_small(px, 9, 20, "OK", "money")
            px.hline(8, 24, 14, "money")
            px.hline(8, 18, 11, "money")
        out[state] = px
    return out


def prop_cftv():
    out = {}
    for state in ("fechado", "aberto"):
        px = Px(66, 44)
        outline_rect(px, 0, 0, 65, 43, "slate")
        for i in range(3):
            for j in range(2):
                x, y = 3 + i * 21, 3 + j * 20
                outline_rect(px, x, y, x + 18, y + 17, "ink")
                if state == "fechado":
                    px.rect(x + 1, y + 1, x + 17, y + 16, "police")
                    px.rect(x + 2, y + 2, x + 16, y + 6, "blue")
                    px.rect(x + 6 + i * 2, y + 6, x + 8 + i * 2, y + 11, "cyan")  # alguém na imagem
                    px.dot(x + 15, y + 14, "red")
                else:
                    for yy in range(y + 1, y + 17):
                        for xx in range(x + 1, x + 18):
                            if rng.random() < 0.45:
                                px.dot(xx, yy, rng.choice(("mist", "pale", "steel")))
                    draw_small(px, x + 3, y + 6, "LOOP", "money") if (i + j) % 2 == 0 else None
        out[state] = px
    return out


def prop_sensor():
    out = {}
    for state in ("fechado", "aberto"):
        px = Px(16, 22)
        px.rect(7, 0, 8, 6, "ink")
        outline_rect(px, 1, 6, 14, 20, "slate")
        px.rect(4, 9, 11, 14, "night")
        px.line(4, 11, 11, 12 if state == "fechado" else 11, "red" if state == "fechado" else "money")
        px.dot(12, 18, "red" if state == "fechado" else "money")
        out[state] = px
    return out


# NPCs ------------------------------------------------------------

NPCS = {
    "seguranca": spec(skin="skin2", hair="ink", hat="cap", hat_color="police", shirt="police", pants="night",
                      item="flashlight", badge="gold", mustache=True),
    "vigia": spec(skin="skin", hair="brown", hat="cap", hat_color="slate", shirt="steel", pants="slate",
                  item="flashlight", badge="lamp"),
    "leao": spec(skin="skin2", hair="ink", hair_style="bald", shirt="night", jacket="ink", tie="wine",
                 pants="ink", glasses=True, item="flashlight", item_color="slate"),
    "seu_ze": spec(skin="skin", hair="pale", hair_style="bald", glasses=True, mustache=True, shirt="cream",
                   tie=None, pants="brown", belt="umber", collar=None),
    "freguesa": spec(skin="skin2", hair="ink", hair_style="curly", shirt="coral", pants="wine", female=True,
                     item="purse", item_color="kraft"),
    "vendedora": spec(skin="skin", hair="brown", hair_style="bun", shirt="cream", jacket="winedark",
                      pants="ink", female=True),
    "caixa": spec(skin="skin", hair="brown", hat="visor", shirt="cream", jacket=None, tie="police",
                  pants="slate", glasses=True),
    "gerente": spec(skin="skin2", hair="ink", mustache=True, shirt="pale", jacket="slate", tie="red",
                    pants="slate", item="newspaper"),
    "pasteleiro": spec(skin="skin", hair="ink", hat="paper", hat_color="cream", shirt="cream", apron="pale",
                       pants="slate", mustache=True),
    "crupie": spec(skin="skin2", hair="ink", shirt="cream", jacket="winedark", tie="ink", pants="ink"),
    "bicheiro": spec(skin="skin", hair="ink", hat="fedora", hat_color="cream", shirt="lamp", jacket="cream",
                     pants="cream", shoes="wine", chain=True, mustache=True),
}

GENERIC_THIEF_BASE = spec(skin="skin", hair="ink", hat="beanie", hat_color="ink", shirt="ink", pants="night",
                          shoes="ink", belt="ink")


# ---------------------------------------------------------------- Casca do prédio (paredes, laje, fachada, rua)

def sign_board(px, cx, y, text, bg, fg, border="ink", pad=4, icon=None, scale=2):
    """Placa com o texto em escala 2 (cada pixel da fonte vira 2x2). Devolve (x0, largura)."""
    from PIL import Image as _Image
    tw = text_width(text)
    txt = Px(tw + 2, CELL_H + 2)
    draw_text(txt, 0, 3, text, fg, shadow=shade(bg, -2))
    big = txt.img.resize((txt.w * scale, txt.h * scale), _Image.NEAREST)
    icon_w = 14 * scale // 2 + 4 if icon else 0
    w = big.width + pad * 2 + icon_w
    h = big.height + 2
    x0 = cx - w // 2
    outline_rect(px, x0, y, x0 + w, y + h, bg, border)
    px.hline(x0 + 1, x0 + w - 1, y + h - 1, shade(bg, 1))
    px.hline(x0 + 1, x0 + w - 1, y + 1, shade(bg, -1))
    px.img.alpha_composite(big, (x0 + pad + icon_w, px.h - (y + 1) - big.height))
    d = x0 + pad + 6
    cy = y + h // 2
    if icon == "diamond":
        for i in range(6):
            px.hline(d - i, d + i, cy + 5 - i, "cyan")
            px.hline(d - i, d + i, cy - 6 + i, "cyan")
        px.dot(d - 1, cy + 3, "cream")
        px.dot(d, cy + 4, "cream")
    elif icon == "clover":
        for dx, dy in ((0, 3), (-3, 0), (3, 0), (0, -3)):
            px.disc(d + dx, cy + dy, 2, "money")
        px.line(d, cy - 2, d + 3, cy - 8, "green")
    elif icon == "dollar":
        px.rect(d - 3, cy - 5, d + 3, cy + 5, "lamp")
        draw_text(px, d - 2, cy - 3, "$", "ink")
    return x0, w


class Shell:
    """Casca do prédio num canvas grande. Coordenadas do prédio: (0, 0) = canto de baixo à esquerda da parede externa,
    no nível do piso das salas."""

    def __init__(self, width_px, top_px):
        self.ox, self.oy = MARGIN, FOUND
        self.w = width_px
        self.px = Px(width_px + 2 * MARGIN, top_px + FOUND)

    def dot(self, x, y, c, a=255):
        self.px.dot(x + self.ox, y + self.oy, c, a)

    def rect(self, x0, y0, x1, y1, c):
        self.px.rect(x0 + self.ox, y0 + self.oy, x1 + self.ox, y1 + self.oy, c)

    def frame(self, x0, y0, x1, y1, c):
        self.px.frame(x0 + self.ox, y0 + self.oy, x1 + self.ox, y1 + self.oy, c)

    def dither(self, x0, y0, x1, y1, c1, c2, d):
        self.px.dither(x0 + self.ox, y0 + self.oy, x1 + self.ox, y1 + self.oy, c1, c2, d)

    def text(self, x, y, s, c, shadow=None):
        return draw_text(self.px, x + self.ox, y + self.oy, s, c, shadow=shadow)

    def sub(self, x, y, w, h):
        """Px temporária pra desenhar um pedaço e colar depois."""
        return Px(w, h), (x, y)

    def paste(self, part, x, y):
        self.px.paste(part, x + self.ox, y + self.oy)


def ground(shell, street_y, x0, x1):
    """Terra embaixo e calçada no nível da rua."""
    shell.dither(x0, -FOUND, x1, street_y - 5, "brown", "umber", 0.45)
    for _ in range((x1 - x0) // 6):
        x, y = rng.randrange(x0, x1), rng.randrange(-FOUND, street_y - 6)
        shell.rect(x, y, x + 1, y, rng.choice(("umber", "slate", "wood")))
    shell.rect(x0, street_y - 4, x1, street_y - 1, "steel")
    shell.rect(x0, street_y - 1, x1, street_y - 1, "mist")
    for x in range(x0, x1, 16):
        shell.rect(x, street_y - 4, x, street_y - 2, "slate")


def facade_floor(shell, x0, x1, y, style, rows_seed, sign=None):
    """Um andar de fachada (lado de fora) de x0 a x1, base em y, altura FLOOR_H."""
    s = style
    wall, trim = s["wall"], s["trim"]
    shell.rect(x0, y, x1, y + FLOOR_H - 1, wall)
    tex = s.get("texture", "plaster")
    if tex == "plaster":
        for _ in range((x1 - x0) // 5):
            xx, yy = rng.randrange(x0, x1), rng.randrange(y, y + FLOOR_H)
            shell.dot(xx, yy, shade(wall, -1))
        for _ in range(3):
            xx, yy = rng.randrange(x0 + 4, x1 - 4), rng.randrange(y + 8, y + FLOOR_H - 8)
            for i in range(rng.randrange(4, 10)):
                shell.dot(xx + i // 2, yy - i, shade(wall, -1))
    elif tex == "bricks":
        for yy in range(y, y + FLOOR_H, 6):
            shell.rect(x0, yy, x1, yy, shade(wall, -1))
            off = 0 if (yy // 6) % 2 else 6
            for xx in range(x0 + off, x1, 12):
                shell.rect(xx, yy, xx, yy + 5, shade(wall, -1))
    elif tex == "glass":
        for yy in range(y + 10, y + FLOOR_H - 12, 4):
            pass
    shell.rect(x0, y + FLOOR_H - 6, x1, y + FLOOR_H - 1, trim)
    shell.rect(x0, y + FLOOR_H - 6, x1, y + FLOOR_H - 6, shade(trim, -1))
    win_w, win_h, gap = s.get("win", (22, 34, 42))
    n = max(1, (x1 - x0 - 16) // gap)
    start = x0 + ((x1 - x0) - (n - 1) * gap - win_w) // 2
    r = random.Random(rows_seed)
    for i in range(n):
        wx = start + i * gap
        wy = y + 30
        if tex == "glass":
            shell.rect(wx - 6, wy - 4, wx + win_w + 6, wy + win_h + 4, "police")
            shell.rect(wx - 6, wy + win_h, wx + win_w + 6, wy + win_h + 4, "blue")
        lit = r.random() < 0.45
        shell.rect(wx - 2, wy - 3, wx + win_w + 2, wy - 1, shade(trim, 1))     # peitoril
        shell.frame(wx - 1, wy - 1, wx + win_w + 1, wy + win_h + 1, "ink")
        shell.rect(wx, wy, wx + win_w, wy + win_h, "lamp" if lit else "night")
        if lit:
            shell.rect(wx, wy + win_h - 6, wx + win_w, wy + win_h, "gold")
            c = r.choice(("coral", "neon", "money", "wine"))
            shell.rect(wx, wy, wx + 4, wy + win_h, c)                         # cortina
            shell.rect(wx + win_w - 4, wy, wx + win_w, wy + win_h, c)
            if r.random() < 0.5:
                shell.rect(wx + 9, wy, wx + 13, wy + 12, "ink")                # alguém vendo novela
                shell.rect(wx + 10, wy + 13, wx + 12, wy + 15, "ink")
        else:
            shell.dot(wx + 3, wy + win_h - 3, "dusk")
            shell.rect(wx + 1, wy + win_h - 8, wx + 6, wy + win_h - 8, "dusk")
        shell.rect(wx + win_w // 2, wy, wx + win_w // 2, wy + win_h, "ink")
        shell.rect(wx, wy + win_h // 2, wx + win_w, wy + win_h // 2, "ink")
        if s.get("ac") and r.random() < 0.4:
            shell.rect(wx + win_w + 4, wy - 2, wx + win_w + 14, wy + 6, "pale")
            shell.frame(wx + win_w + 4, wy - 2, wx + win_w + 14, wy + 6, "ink")
            for k in range(3):
                shell.rect(wx + win_w + 6, wy + k * 2, wx + win_w + 12, wy + k * 2, "steel")
        if s.get("balcony") and r.random() < 0.5:
            shell.rect(wx - 4, wy - 6, wx + win_w + 4, wy - 4, "ink")
            for k in range(wx - 4, wx + win_w + 5, 3):
                shell.rect(k, wy - 4, k, wy + 6, "ink")
            shell.rect(wx - 4, wy + 6, wx + win_w + 4, wy + 6, "ink")
    if sign:
        pass


def roof(shell, x0, x1, y, style, water_tank=True, antenna=True):
    shell.rect(x0 - 2, y, x1 + 2, y + 5, style["trim"])
    shell.frame(x0 - 2, y, x1 + 2, y + 5, "ink")
    if water_tank:
        tx = x0 + (x1 - x0) // 3
        shell.rect(tx, y + 6, tx + 24, y + 18, "blue")
        shell.frame(tx, y + 6, tx + 24, y + 18, "ink")
        shell.rect(tx + 2, y + 18, tx + 22, y + 20, "police")
        shell.frame(tx + 2, y + 18, tx + 22, y + 20, "ink")
    if antenna:
        ax = x0 + 2 * (x1 - x0) // 3
        shell.rect(ax, y + 6, ax, y + 34, "ink")
        for k in range(4):
            shell.rect(ax - 8 + k, y + 18 + k * 4, ax + 8 - k, y + 18 + k * 4, "ink")


def clothesline(shell, x0, x1, y):
    for x in range(x0, x1):
        shell.dot(x, y + round(2 * abs(((x - x0) % 40 - 20) / 20)), "ink")
    for i, x in enumerate(range(x0 + 6, x1 - 6, 11)):
        c = ["coral", "blue", "lamp", "money", "cream"][i % 5]
        shell.rect(x, y - 8, x + 6, y, c)
        shell.frame(x, y - 8, x + 6, y, "ink")


def separators_and_slab(shell, n_rooms, theme_for_room, door_styles, slab_color="slate", tunnel_rooms=()):
    W = WALL + n_rooms * CELL_W
    # laje do teto e piso
    for i in range(n_rooms):
        x0 = WALL + i * CELL_W
        if i in tunnel_rooms:
            shell.dither(x0 - WALL, ROOM_H, x0 + ROOM_W + WALL - 1, ROOM_H + SLAB - 1, "brown", "umber", 0.4)
            shell.dither(x0 - WALL, -SLAB, x0 + ROOM_W + WALL - 1, -1, "umber", "brown", 0.3)
        else:
            shell.rect(x0 - WALL, ROOM_H, x0 + ROOM_W + WALL - 1, ROOM_H + SLAB - 1, slab_color)
            shell.rect(x0 - WALL, ROOM_H, x0 + ROOM_W + WALL - 1, ROOM_H, "ink")
            shell.rect(x0 - WALL, ROOM_H + SLAB - 1, x0 + ROOM_W + WALL - 1, ROOM_H + SLAB - 1, shade(slab_color, 1))
            shell.rect(x0 - WALL, -SLAB, x0 + ROOM_W + WALL - 1, -1, slab_color)
            shell.rect(x0 - WALL, -1, x0 + ROOM_W + WALL - 1, -1, "ink")
    # paredes entre salas, com vão de porta
    for i in range(n_rooms - 1):
        x = WALL + i * CELL_W + ROOM_W
        style = door_styles[i]
        if style in ("tabuas", "buraco"):
            shell.rect(x, 0, x + WALL - 1, ROOM_H - 1, "wood" if style == "tabuas" else "slate")
            shell.rect(x + 1, 0, x + WALL - 2, ROOM_H - 1, "brown" if style == "tabuas" else "steel")
        else:
            c = THEMES[theme_for_room(i)]["sep"]
            shell.rect(x, 0, x + WALL - 1, ROOM_H - 1, c)
            shell.rect(x, 0, x, ROOM_H - 1, shade(c, -1))
            shell.rect(x + WALL - 1, 0, x + WALL - 1, ROOM_H - 1, shade(c, -1))
        shell.rect(x, 0, x + WALL - 1, DOOR_H - 1, "ink")
        shell.rect(x + 1, 0, x + WALL - 2, DOOR_H - 2, "night")
    return W


def ext_walls(shell, W, color, entrance=True, height=ROOM_H + SLAB):
    for x0 in (0, W - WALL):
        shell.rect(x0, -SLAB, x0 + WALL - 1, height - 1, color)
        shell.rect(x0, -SLAB, x0, height - 1, shade(color, -1))
        shell.rect(x0 + WALL - 1, -SLAB, x0 + WALL - 1, height - 1, "ink")
    if entrance:
        shell.rect(0, 0, WALL - 1, DOOR_H + 1, "ink")
        shell.rect(1, 0, WALL - 2, DOOR_H, "night")


def door_sprites(style):
    """Porta entre salas, vista de lado no vão da parede (8x40): fechada e aberta."""
    out = {}
    colors = {"madeira": ("wood", "tan"), "metal": ("steel", "mist"), "vidro": ("cyan", "pale"),
              "tabuas": ("wood", "brown"), "buraco": ("slate", "steel")}[style]
    for state in ("fechada", "aberta"):
        px = Px(WALL, DOOR_H)
        if state == "fechada":
            if style == "tabuas":
                for y in range(0, DOOR_H, 7):
                    px.rect(0, y, WALL - 1, y + 5, colors[0])
                    px.hline(0, WALL - 1, y + 5, "umber")
                    px.dot(2, y + 2, "steel")
            elif style == "buraco":
                px.rect(0, 0, WALL - 1, DOOR_H - 1, colors[0])
                px.dither(0, 0, WALL - 1, DOOR_H - 1, "slate", "steel", 0.4)
            else:
                px.rect(1, 0, WALL - 2, DOOR_H - 1, colors[0])
                px.vline(1, 0, DOOR_H - 1, shade(colors[0], -1))
                px.vline(WALL - 2, 0, DOOR_H - 1, colors[1])
                px.dot(WALL - 3, 18, "gold")
                if style == "vidro":
                    px.rect(2, 8, WALL - 3, DOOR_H - 6, "pale")
        else:
            if style == "tabuas":
                px.rect(0, 0, 2, 5, "wood")                           # tábuas caídas
                px.rect(4, 0, 7, 2, "wood")
            elif style == "buraco":
                px.rect(0, 0, 1, 3, "slate")
                px.rect(5, 0, 7, 2, "steel")
            else:
                px.rect(WALL - 2, 0, WALL - 1, DOOR_H - 1, colors[0])
        out[state] = px
    return out


# ---------------------------------------------------------------- Rua e fundo

def street_props():
    out = {}
    # poste de luz (sódio)
    px = Px(28, 96)
    px.rect(4, 0, 6, 88, "slate")
    px.vline(4, 0, 88, "dusk")
    px.rect(2, 0, 8, 4, "dusk")
    px.rect(6, 86, 20, 88, "slate")
    px.rect(16, 82, 26, 86, "dusk")
    px.rect(17, 81, 25, 81, "lamp")
    px.outline()
    out["Poste"] = px
    # orelhão
    px = Px(22, 40)
    px.rect(10, 0, 11, 24, "slate")
    for y in range(22, 38):
        half = round(9 * (1 - ((y - 30) / 9) ** 2) ** 0.5) if abs(y - 30) < 9 else 1
        px.rect(11 - half, y, 11 + half, y, "coral")
    px.rect(5, 23, 17, 33, "red")
    px.rect(8, 25, 14, 31, "cream")
    px.rect(9, 28, 10, 30, "ink")
    px.rect(11, 26, 13, 27, "ink")
    px.outline()
    out["Orelhao"] = px
    # lixeira
    px = Px(12, 16)
    px.rect(2, 0, 9, 11, "money")
    px.rect(1, 11, 10, 12, "green")
    px.rect(5, 12, 6, 14, "slate")
    px.outline()
    out["Lixeira"] = px
    # banca de jornal
    px = Px(52, 40)
    px.rect(2, 0, 49, 28, "deepgreen")
    px.rect(0, 28, 51, 33, "green")
    draw_small(px, 14, 30, "BANCA", "cream")
    for i in range(6):
        c = rng.choice(("coral", "lamp", "cyan", "cream", "neon"))
        px.rect(5 + i * 7, 12, 10 + i * 7, 22, c)
        px.rect(6 + i * 7, 18, 9 + i * 7, 18, "ink")
    px.rect(4, 0, 47, 8, "dusk")
    px.outline()
    out["Banca"] = px
    # bueiro (tampa no chão)
    px = Px(16, 3)
    px.rect(0, 0, 15, 2, "dusk")
    px.rect(2, 1, 13, 1, "slate")
    out["Bueiro"] = px
    return out


def van_sprites():
    """Kombi da firma (60x30): 2 frames de roda."""
    out = []
    for f in range(2):
        px = Px(60, 30)
        px.rect(2, 6, 57, 26, "cream")
        px.rect(2, 6, 57, 13, "kraft")
        px.rect(4, 27, 52, 28, "cream")
        px.rect(44, 15, 55, 24, "night")         # para-brisa
        px.rect(45, 21, 54, 23, "dusk")
        for x in (8, 20, 32):
            px.rect(x, 17, x + 9, 24, "night")
        px.rect(2, 13, 57, 14, "wine")
        draw_small(px, 9, 8, "AUTO CIDA", "wine")
        px.rect(56, 8, 58, 10, "lamp")
        px.rect(1, 8, 2, 10, "red")
        for wx in (12, 46):
            px.disc(wx, 5, 5, "ink")
            px.disc(wx, 5, 2, "mist")
            px.dot(wx + (1 if f else -1), 5 + (1 if f else -1), "ink")
        px.outline()
        out.append(px)
    return out


def police_sprites():
    """Viatura (60x30): 2 frames do giroflex."""
    out = []
    for f in range(2):
        px = Px(60, 30)
        px.rect(2, 6, 57, 16, "cream")
        px.rect(12, 16, 46, 24, "cream")
        px.rect(15, 17, 27, 23, "night")
        px.rect(30, 17, 43, 23, "night")
        px.rect(2, 10, 57, 12, "police")
        draw_small(px, 18, 6, "POLICIA", "police") if False else draw_small(px, 6, 6, "POLICIA", "police")
        px.rect(25, 25, 33, 27, "ink")
        px.rect(25, 25, 28, 27, "red" if f == 0 else "winedark")
        px.rect(30, 25, 33, 27, "police" if f == 0 else "blue")
        px.rect(56, 8, 58, 10, "lamp")
        for wx in (12, 46):
            px.disc(wx, 5, 5, "ink")
            px.disc(wx, 5, 2, "mist")
        px.outline()
        out.append(px)
    return out


def sky():
    px = Px(640, 360)
    px.vgradient(0, 0, 639, 359, "dusk", "night")
    px.dither(0, 0, 639, 90, "dusk", "slate", 0.2)
    r = random.Random(7)
    for _ in range(140):
        x, y = r.randrange(640), r.randrange(140, 360)
        px.dot(x, y, r.choice(("pale", "mist", "cream")))
    px.disc(520, 300, 16, "paper")
    px.disc(526, 304, 13, "night")                    # lua minguante
    px.disc(520, 300, 16, "paper") if False else None
    return px


def skyline_layer(height, color, lit, seed, min_h, max_h, neon=False, hills=False):
    px = Px(640, height)
    r = random.Random(seed)
    if hills:
        for x in range(640):
            import math
            h = int(height * 0.55 + 22 * math.sin(x / 640 * math.tau * 2 + 1) + 12 * math.sin(x / 640 * math.tau * 5))
            px.rect(x, 0, x, h, color)
        for _ in range(260):
            x = r.randrange(640)
            import math
            top = int(height * 0.55 + 22 * math.sin(x / 640 * math.tau * 2 + 1) + 12 * math.sin(x / 640 * math.tau * 5))
            y = r.randrange(4, max(5, top - 4))
            px.dot(x, y, r.choice(("lamp", "gold", "paper", "coral")))
        # torre de antena com luz vermelha
        px.rect(400, 0, 401, height - 6, shade(color, -1))
        px.dot(400, height - 5, "red")
        return px
    x = 0
    while x < 640:
        w = r.randrange(24, 64)
        h = r.randrange(min_h, max_h)
        px.rect(x, 0, min(639, x + w - 1), h, color)
        if r.random() < 0.3:
            px.rect(x + w // 2, h, x + w // 2, h + r.randrange(6, 16), color)
        for wy in range(8, h - 6, 8):
            for wx in range(x + 4, x + w - 4, 6):
                if r.random() < lit:
                    px.rect(wx, wy, wx + 1, wy + 2, r.choice(("lamp", "gold", "paper")))
        if neon and r.random() < 0.35 and h > 40:
            word = r.choice(["HOTEL", "BAR", "LOJA", "VIDEO", "FARMACIA", "CINE"])
            if word == "BAR":
                word = "LANCHES"
            tw = len(word) * 4
            if tw < w - 4:
                draw_small(px, x + (w - tw) // 2, h - 12, word, r.choice(("neon", "cyan", "coral", "lamp")))
        x += w + r.randrange(0, 6)
    return px


# ---------------------------------------------------------------- Ícones, anel, partículas, balão

def icons():
    out = {}
    px = Px(16, 16)                                   # cadeado
    px.rect(4, 1, 11, 8, "gold")
    px.rect(4, 1, 11, 1, "bronze")
    px.ring(8, 9, 3, "mist")
    px.rect(4, 9, 4, 10, "mist"); px.rect(11, 9, 11, 10, "mist")
    px.rect(7, 3, 8, 6, "ink")
    px.outline()
    out["Icone_Fechadura"] = px
    px = Px(16, 16)                                   # monitor
    px.rect(2, 5, 13, 14, "pale")
    px.rect(3, 6, 12, 13, "night")
    px.rect(4, 11, 9, 11, "cyan"); px.rect(4, 9, 11, 9, "cyan"); px.rect(4, 7, 7, 7, "cyan")
    px.rect(6, 2, 9, 4, "steel"); px.rect(4, 1, 11, 1, "steel")
    px.outline()
    out["Icone_Sistema"] = px
    px = Px(16, 16)                                   # pessoa
    px.disc(8, 11, 3, "skin")
    px.rect(6, 13, 10, 14, "brown")
    px.rect(3, 1, 12, 6, "kraft")
    px.rect(4, 7, 11, 7, "kraft")
    px.rect(7, 2, 8, 6, "cream")
    px.outline()
    out["Icone_Pessoa"] = px
    px = Px(16, 16)                                   # check
    px.line(3, 8, 6, 4, "money", 2); px.line(6, 4, 12, 12, "money", 2)
    px.outline()
    out["Icone_Check"] = px
    px = Px(16, 16)                                   # x
    px.line(3, 3, 12, 12, "red", 2); px.line(3, 12, 12, 3, "red", 2)
    px.outline()
    out["Icone_X"] = px
    px = Px(8, 16)                                    # !
    px.rect(3, 5, 4, 14, "red"); px.rect(3, 1, 4, 2, "red")
    px.outline()
    out["Icone_Alerta"] = px
    px = Px(8, 16)                                    # ?
    px.rect(2, 12, 5, 13, "lamp"); px.rect(5, 9, 6, 12, "lamp"); px.rect(3, 6, 4, 8, "lamp"); px.rect(3, 1, 4, 2, "lamp")
    px.rect(1, 11, 2, 12, "lamp")
    px.outline()
    out["Icone_Duvida"] = px
    px = Px(16, 12)                                   # zZ
    for (x, y, s) in ((1, 1, 4), (8, 5, 5)):
        px.rect(x, y + s, x + s, y + s, "pale"); px.line(x + s, y + s, x, y, "pale"); px.rect(x, y, x + s, y, "pale")
    px.outline()
    out["Icone_Zz"] = px
    px = Px(18, 10)                                   # algemas
    px.ring(4, 4, 3, "mist"); px.ring(13, 4, 3, "mist"); px.rect(6, 6, 11, 6, "steel")
    px.outline()
    out["Icone_Algemas"] = px
    px = Px(12, 12)                                   # sem especialista
    for y in range(1, 11):
        half = (11 - y) // 2
        px.rect(6 - half - 0, y, 5 + half + 0, y, "red")
    px.rect(5, 5, 6, 8, "cream"); px.rect(5, 2, 6, 3, "cream")
    px.outline()
    out["Icone_SemEspecialista"] = px
    px = Px(16, 16)                                   # evento "!?"
    px.rect(1, 4, 14, 14, "cream"); px.rect(3, 1, 5, 4, "cream")
    px.rect(4, 7, 5, 12, "red"); px.rect(4, 5, 5, 5, "red")
    px.rect(8, 11, 11, 12, "police"); px.rect(11, 9, 12, 11, "police"); px.rect(9, 7, 10, 8, "police"); px.rect(9, 5, 10, 5, "police")
    px.outline()
    out["Icone_Evento"] = px
    px = Px(12, 12)                                   # relógio (atraso)
    px.disc(6, 6, 5, "cream"); px.ring(6, 6, 5, "ink"); px.line(6, 6, 6, 9, "ink"); px.line(6, 6, 8, 6, "ink")
    out["Icone_Relogio"] = px
    for name, size in (("Sacola_P", 10), ("Sacola_M", 13), ("Sacola_G", 16)):
        px = Px(size + 2, size + 2)
        r = size // 2
        px.disc(r + 1, r, r, "kraft")
        px.rect(r - 1, size - 1, r + 3, size, "tan")
        px.rect(r - 2, size - 2, r + 4, size - 2, "bronze")
        draw_small(px, r - 1, r - 3, "$", "bronze") if size >= 13 else px.dot(r + 1, r, "bronze")
        px.outline()
        out[name] = px
    px = Px(9, 6)                                     # cédula
    px.rect(0, 0, 8, 5, "money"); px.frame(0, 0, 8, 5, "green"); px.rect(3, 2, 5, 3, "mint")
    out["Cedula"] = px
    px = Px(5, 5)                                     # moeda
    px.disc(2, 2, 2, "gold"); px.dot(2, 2, "lamp"); px.dot(1, 1, "bronze")
    out["Moeda"] = px
    px = Px(3, 3)
    px.rect(0, 1, 2, 1, "lamp"); px.rect(1, 0, 1, 2, "lamp"); px.dot(1, 1, "cream")
    out["Faisca"] = px
    px = Px(2, 2)
    px.rect(0, 0, 1, 1, "kraft")
    out["Poeira"] = px
    px = Px(8, 8)
    px.disc(4, 4, 3, "mist"); px.disc(3, 5, 2, "pale")
    out["Fumaca"] = px
    px = Px(4, 2)
    px.rect(0, 0, 3, 1, "tan"); px.dot(3, 1, "wood")
    out["Lasca"] = px
    px = Px(8, 6)                                     # giroflex de alarme
    px.rect(1, 0, 6, 1, "ink"); px.rect(2, 2, 5, 5, "red"); px.rect(3, 4, 4, 5, "coral")
    out["Giroflex"] = px
    px = Px(14, 10)                                   # câmera de segurança (olhando pra direita)
    px.rect(0, 6, 2, 9, "slate")
    px.rect(2, 7, 4, 7, "ink")
    px.rect(4, 3, 12, 8, "pale"); px.rect(4, 3, 12, 3, "mist"); px.rect(12, 4, 13, 7, "ink")
    px.dot(6, 6, "red")
    px.outline()
    out["Camera"] = px
    px = Px(64, 32)                                   # cone de luz (branco, transparente nas bordas)
    for x in range(64):
        half = x * 15 // 63
        for y in range(16 - half, 16 + half + 1):
            edge = abs(y - 16) / max(half, 1)
            a = int(120 * (1 - x / 70) * (1 - edge * 0.5))
            if (x + y) % 2 == 0 or edge < 0.7:
                px.dot(x, y, "cream", max(0, a))
    out["Cone"] = px
    return out


def ring_frames(n=16, size=26, r=11):
    import math
    frames = []
    for k in range(n + 1):
        px = Px(size, size)
        c = size // 2
        for i in range(n):
            ang = math.pi / 2 - (i + 0.5) / n * math.tau
            x = c + round(math.cos(ang) * r)
            y = c + round(math.sin(ang) * r) - 1
            on = i < k
            px.rect(x - 1, y, x + 1, y + 1, "gold" if on else "night")
            if on:
                px.dot(x, y + 1, "lamp")
        frames.append(px)
    sheet_px = Px(size * (n + 1), size)
    for i, f in enumerate(frames):
        sheet_px.paste(f, i * size, 0)
    return sheet_px


def bubble():
    px = Px(16, 16)
    px.rect(1, 0, 14, 15, "cream")
    px.rect(0, 1, 15, 14, "cream")
    px.rect(1, 0, 14, 0, "ink"); px.rect(1, 15, 14, 15, "ink"); px.rect(0, 1, 0, 14, "ink"); px.rect(15, 1, 15, 14, "ink")
    tail = Px(7, 5)
    for y in range(5):
        tail.rect(1, y, 5 - y, y, "cream")
    tail.rect(0, 1, 0, 4, "ink")
    for y in range(5):
        tail.dot(6 - y, y, "ink")
    tail.rect(1, 4, 1, 4, "cream")
    return px, tail


def stairs_cassino(height):
    """Escada da pastelaria pro porão do cassino: desce da esquerda pra direita."""
    w = 56
    px = Px(w, height)
    steps = height // 6
    for i in range(steps):
        y = height - 1 - i * 6
        x = 4 + i * (w - 12) // steps
        px.rect(x, y - 5, x + 10, y - 4, "tan")
        px.rect(x, y - 3, x + 10, y, "wood")
        px.rect(x + 10, y - 5, x + 10, y, "brown")
    px.line(2, height - 2, w - 6, 18, "brown", 2)          # corrimão
    return px


def ladder(height):
    px = Px(14, height)
    px.rect(1, 0, 2, height - 1, "wood")
    px.rect(11, 0, 12, height - 1, "wood")
    for y in range(4, height, 7):
        px.rect(1, y, 12, y + 1, "tan")
    px.outline()
    return px


# ---------------------------------------------------------------- Prédios

STREET_LEVEL_STYLE = {
    "loterica": dict(wall="paper", trim="kraft", texture="plaster", floors=1, ac=True, balcony=True,
                     sign=("LOTÉRICA DO SEU ZÉ", "green", "cream", "clover"), win=(22, 32, 46), ext="kraft"),
    "joalheria": dict(wall="deepgreen", trim="bronze", texture="bricks", floors=2, ac=False, balcony=True,
                      sign=("JOALHERIA BRILHO ETERNO", "ink", "gold", "diamond"), win=(20, 38, 40), ext="green"),
    "cambio": dict(wall="mist", trim="steel", texture="glass", floors=3, ac=True, balcony=False,
                   sign=("CÂMBIO DÓLAR FÁCIL", "police", "cream", "dollar"), win=(24, 30, 44), ext="steel"),
}


def door_style_for(heist_id, i, rooms):
    a, b = KINDS[rooms[i]["kind"]], KINDS[rooms[i + 1]["kind"]]
    if a == "Tunnel" and b == "Tunnel":
        return "tabuas"
    if a == "Tunnel":
        return "buraco"
    if b == "Vault":
        return "metal"
    return THEMES[room_theme(heist_id, rooms[i + 1], i + 1)]["door"]


def build_street_level(h, out, layout):
    rooms = h["rooms"]
    n = len(rooms)
    st = STREET_LEVEL_STYLE[h["id"]]
    W = WALL + n * CELL_W
    top = FLOOR_H + st["floors"] * FLOOR_H + 64
    shell = Shell(W, top)
    ground(shell, 0, -MARGIN, W + MARGIN - 1)
    shell.rect(0, -SLAB - 6, W - 1, -SLAB - 1, "slate")                # sapata de concreto
    shell.rect(0, -SLAB - 6, W - 1, -SLAB - 6, "ink")
    doors = [door_style_for(h["id"], i, rooms) for i in range(n - 1)]
    separators_and_slab(shell, n, lambda i: room_theme(h["id"], rooms[i + 1], i + 1), doors)
    ext_walls(shell, W, st["ext"])
    y = FLOOR_H
    for f in range(st["floors"]):
        facade_floor(shell, 0, W - 1, y, st, seed_for(h["id"], f))
        y += FLOOR_H
    roof(shell, 0, W - 1, y, st, water_tank=True, antenna=h["id"] != "cambio")
    if h["id"] == "cambio":   # outdoor no telhado
        bx = W // 2 - 70
        shell.rect(bx + 20, y + 6, bx + 22, y + 26, "ink")
        shell.rect(bx + 118, y + 6, bx + 120, y + 26, "ink")
        shell.rect(bx, y + 26, bx + 140, y + 54, "cream")
        shell.frame(bx, y + 26, bx + 140, y + 54, "ink")
        shell.text(bx + 8, y + 42, "FITA K7 VIRGEM", "wine")
        shell.text(bx + 8, y + 32, "3 POR Cr$ 2.000", "police")
    if h["id"] == "loterica":
        clothesline(shell, 40, W - 60, FLOOR_H + 92)
    text, bg, fg, icon = st["sign"]
    tmp = Px(text_width(text) * 2 + 60, 32)
    x0, w = sign_board(tmp, tmp.w // 2, 2, text, bg, fg, icon=icon)
    sign_y = FLOOR_H + 4 if h["id"] != "cambio" else FLOOR_H + 6
    shell.paste(tmp, W // 2 - tmp.w // 2, sign_y)
    path = f"Predios/Predio_{h['id']}.png"
    shell.px.save(ART / path, PREVIEW, 1)
    return dict(shell=path, shellX=-MARGIN, shellY=-FOUND, width=W, streetY=0, doors=doors,
                entryPath=[dict(x=-60, y=FEET), dict(x=4, y=FEET), dict(x=WALL + 14, y=FEET)],
                van=dict(x=-150, y=0), police=dict(x=-262, y=0),
                props=[dict(sprite="Rua/Poste.png", x=-96, y=0, light=True), dict(sprite="Rua/Orelhao.png", x=-30, y=0),
                       dict(sprite="Rua/Lixeira.png", x=W + 30, y=0), dict(sprite="Rua/Poste.png", x=W + 60, y=0, light=True),
                       dict(sprite="Rua/Banca.png", x=W + 150, y=0) if h["id"] != "loterica" else dict(sprite="Rua/Banca.png", x=-250, y=0)],
                cameraY=FLOOR_H + 20)


def seed_for(hid, f):
    return sum(ord(c) for c in hid) * 10 + f


def build_underground(h, out, layout):
    rooms = h["rooms"]
    n = len(rooms)
    W = WALL + n * CELL_W
    S = STREET_UG
    floors_above = 3 if h["id"] == "banco-central" else 2
    top = S + floors_above * FLOOR_H + 72
    shell = Shell(W, top)
    ground(shell, S, -MARGIN, W + MARGIN - 1)
    tunnel = tuple(i for i, r in enumerate(rooms) if KINDS[r["kind"]] == "Tunnel")
    doors = [door_style_for(h["id"], i, rooms) for i in range(n - 1)]
    separators_and_slab(shell, n, lambda i: room_theme(h["id"], rooms[i + 1], i + 1), doors, "steel", tunnel)
    # paredes externas do subsolo (concreto), sem porta: a crew desce pelo poço da esquerda
    first_tunnel = 0 in tunnel
    if not first_tunnel:
        shell.rect(0, -SLAB, WALL - 1, ROOM_H + SLAB - 1, "steel")
        shell.rect(WALL - 1, -SLAB, WALL - 1, ROOM_H + SLAB - 1, "ink")
    shell.rect(W - WALL, -SLAB, W - 1, ROOM_H + SLAB - 1, "steel")
    shell.rect(W - WALL, -SLAB, W - WALL, ROOM_H + SLAB - 1, "ink")
    # poço de entrada (escada ou escada de mão) da rua até o piso da primeira sala
    shaft_x0, shaft_x1 = (WALL, WALL + 48) if h["id"] == "cassino" else (WALL, WALL + 30)
    shell.rect(shaft_x0, ROOM_H, shaft_x1, S - 5, "night")
    shell.rect(shaft_x0, ROOM_H, shaft_x0, S - 5, "ink")
    shell.rect(shaft_x1, ROOM_H, shaft_x1, S - 5, "ink")
    # o que tem em cima, no nível da rua
    if h["id"] == "cassino":
        blocks = [(0, 2 * CELL_W + WALL - 1, "pastelaria"), (2 * CELL_W + WALL, 4 * CELL_W + WALL - 1, "video"),
                  (4 * CELL_W + WALL, W - 1, "farmacia")]
    else:
        blocks = [(0, CELL_W + WALL - 1, "material"), (CELL_W + WALL, 2 * CELL_W + WALL - 1, "rua"),
                  (2 * CELL_W + WALL, W - 1, "banco")]
    for (x0, x1, kind) in blocks:
        shop_front(shell, x0, x1, S, kind, floors_above, h["id"])
    # porta da loja por cima do poço
    path = f"Predios/Predio_{h['id']}.png"
    shell.px.save(ART / path, PREVIEW, 1)
    if h["id"] == "cassino":
        stairs = stairs_cassino(S + 2)
        stairs.save(ART / "Predios/Escada_cassino.png", PREVIEW, 2)
        entry = [dict(x=-60, y=S + FEET), dict(x=WALL + 4, y=S + FEET), dict(x=WALL + 46, y=FEET + 2),
                 dict(x=WALL + 56, y=FEET)]
        extra = [dict(sprite="Predios/Escada_cassino.png", x=WALL, y=0, order=-4)]
    else:
        lad = ladder(S + 6)
        lad.save(ART / "Predios/Escada_banco.png", PREVIEW, 2)
        entry = [dict(x=-60, y=S + FEET), dict(x=WALL + 14, y=S + FEET), dict(x=WALL + 14, y=FEET),
                 dict(x=WALL + 34, y=FEET)]
        extra = [dict(sprite="Predios/Escada_banco.png", x=WALL + 8, y=0, order=-4)]
    return dict(shell=path, shellX=-MARGIN, shellY=-FOUND, width=W, streetY=S, doors=doors, entryPath=entry,
                van=dict(x=-150, y=S), police=dict(x=-262, y=S),
                props=extra + [dict(sprite="Rua/Poste.png", x=-96, y=S, light=True), dict(sprite="Rua/Orelhao.png", x=-30, y=S),
                               dict(sprite="Rua/Poste.png", x=W + 60, y=S, light=True), dict(sprite="Rua/Lixeira.png", x=W + 26, y=S),
                               dict(sprite="Rua/Bueiro.png", x=CELL_W + 90, y=S - 2) if h["id"] == "banco-central" else dict(sprite="Rua/Banca.png", x=W + 150, y=S)],
                cameraY=ROOM_H + 30)


def shop_front(shell, x0, x1, S, kind, floors, hid):
    """Prédio no nível da rua: térreo com vitrine e placa, andares em cima."""
    styles = {
        "pastelaria": dict(wall="cream", trim="red", texture="plaster", ac=True, balcony=True, win=(20, 30, 44)),
        "video": dict(wall="kraft", trim="wood", texture="bricks", ac=False, balcony=True, win=(20, 30, 40)),
        "farmacia": dict(wall="pale", trim="money", texture="plaster", ac=True, balcony=False, win=(22, 30, 42)),
        "material": dict(wall="mist", trim="slate", texture="bricks", ac=False, balcony=False, win=(20, 30, 44)),
        "banco": dict(wall="pale", trim="mist", texture="plaster", ac=False, balcony=False, win=(14, 46, 34)),
    }
    if kind == "rua":
        # trecho de rua sem prédio: poste e calçada, prédios do fundo aparecem
        return
    st = styles[kind]
    y = S
    shell.rect(x0, y, x1, y + FLOOR_H - 1, st["wall"])
    if kind == "banco":
        # colunas, frontão e porta de bronze
        shell.rect(x0, y, x1, y + 6, "mist")
        for cx in range(x0 + 30, x1 - 20, 64):
            shell.rect(cx, y + 6, cx + 14, y + 86, "cream")
            shell.rect(cx + 2, y + 6, cx + 2, y + 86, "mist")
            shell.rect(cx + 12, y + 6, cx + 12, y + 86, "mist")
            shell.rect(cx - 3, y + 86, cx + 17, y + 90, "mist")
            shell.rect(cx - 3, y + 6, cx + 17, y + 9, "mist")
        mid = (x0 + x1) // 2
        shell.rect(mid - 20, y + 6, mid + 20, y + 60, "bronze")
        shell.frame(mid - 20, y + 6, mid + 20, y + 60, "ink")
        shell.rect(mid, y + 6, mid, y + 60, "ink")
        shell.rect(x0, y + 90, x1, y + FLOOR_H - 1, "mist")
        tmp = Px(text_width("BANCO CENTRAL") * 2 + 40, 32)
        sign_board(tmp, tmp.w // 2, 2, "BANCO CENTRAL", "slate", "gold")
        shell.paste(tmp, mid - tmp.w // 2, y + FLOOR_H - 8)
        for f in range(1, floors):
            facade_floor(shell, x0, x1, y + f * FLOOR_H, st, seed_for(hid, f + 7))
        roof(shell, x0, x1, y + floors * FLOOR_H, st, water_tank=False, antenna=False)
        # frontão triangular
        top = y + floors * FLOOR_H + 6
        for i in range(0, 30):
            shell.rect(mid - 120 + i * 4, top + i, mid + 120 - i * 4, top + i, "pale")
        shell.rect(mid - 120, top, mid + 120, top, "ink")
        return
    # vitrine e porta
    shell.rect(x0, y, x1, y + 6, shade(st["wall"], -1))
    gx0, gx1 = x0 + 50, x1 - 30
    shell.rect(gx0, y + 10, gx1, y + 60, "night")
    shell.frame(gx0, y + 10, gx1, y + 60, "ink")
    shell.rect(gx0 + 1, y + 11, gx1 - 1, y + 59, "dusk")
    for gx in range(gx0 + 40, gx1, 40):
        shell.rect(gx, y + 10, gx, y + 60, "ink")
    door_x = x0 + WALL + 4
    shell.rect(door_x, y, door_x + 24, y + 50, "night")
    shell.frame(door_x, y, door_x + 24, y + 50, "ink")
    if kind == "pastelaria":
        for i in range(gx0 + 4, gx1 - 4, 10):
            shell.rect(i, y + 12, i + 6, y + 15, "gold")
        for k in range(x0, x1, 12):   # toldo listrado
            shell.rect(k, y + 66, k + 5, y + 74, "red")
            shell.rect(k + 6, y + 66, k + 11, y + 74, "cream")
        shell.rect(x0, y + 65, x1, y + 65, "ink")
        label, bg, fg = "PASTELARIA", "red", "cream"
    elif kind == "video":
        for i in range(gx0 + 4, gx1 - 6, 8):
            shell.rect(i, y + 12, i + 5, y + 22, rng.choice(("coral", "cyan", "lamp", "neon")))
        label, bg, fg = "VIDEOLOCADORA", "ink", "cyan"
    elif kind == "farmacia":
        label, bg, fg = "FARMÁCIA", "money", "cream"
        shell.rect(gx0 + 10, y + 30, gx0 + 20, y + 34, "red"); shell.rect(gx0 + 13, y + 27, gx0 + 17, y + 37, "red")
    else:  # material de construção
        for i in range(gx0 + 6, gx1 - 10, 14):
            shell.rect(i, y + 10, i + 10, y + 18, "pale")      # sacos de cimento
            shell.frame(i, y + 10, i + 10, y + 18, "mist")
        label, bg, fg = "MATERIAL DE CONSTRUÇÃO", "lamp", "ink"
    tmp = Px(text_width(label) * 2 + 40, 32)
    sign_board(tmp, tmp.w // 2, 2, label, bg, fg)
    shell.paste(tmp, (x0 + x1) // 2 - tmp.w // 2, y + 74)
    for f in range(1, floors):
        facade_floor(shell, x0, x1, y + f * FLOOR_H, st, seed_for(hid + kind, f))
    roof(shell, x0, x1, y + floors * FLOOR_H, st, water_tank=kind != "video", antenna=kind == "pastelaria")


def structure_modules():
    """Peças genéricas pra montar um prédio sem prefab próprio (salas modulares): parede com vão de porta,
    laje, piso, um andar de fachada e fundação. Cada sala liga as suas quando é montada sozinha."""
    out = {}
    px = Px(WALL, FLOOR_H)
    px.rect(0, 0, WALL - 1, FLOOR_H - 1, "slate")
    px.rect(0, 0, 0, FLOOR_H - 1, "dusk")
    px.rect(WALL - 1, 0, WALL - 1, FLOOR_H - 1, "ink")
    px.rect(0, 0, WALL - 1, DOOR_H + 1, "ink")
    px.rect(1, 0, WALL - 2, DOOR_H, "night")
    out["Modulo_parede"] = px
    px = Px(CELL_W + WALL, SLAB)
    px.rect(0, 0, CELL_W + WALL - 1, SLAB - 1, "slate")
    px.hline(0, CELL_W + WALL - 1, 0, "ink")
    px.hline(0, CELL_W + WALL - 1, SLAB - 1, "steel")
    out["Modulo_laje"] = px
    out["Modulo_piso"] = px
    shell = Shell(CELL_W + WALL, FLOOR_H + 8)
    facade_floor(shell, 0, CELL_W + WALL - 1, 0, dict(wall="mist", trim="steel", texture="plaster", ac=True,
                                                     balcony=False, win=(22, 32, 60)), 5)
    shell.rect(0, FLOOR_H, CELL_W + WALL - 1, FLOOR_H + 5, "steel")
    shell.frame(0, FLOOR_H, CELL_W + WALL - 1, FLOOR_H + 5, "ink")
    part = Px(CELL_W + WALL, FLOOR_H + 8)
    part.img = shell.px.img.crop((MARGIN, 0, MARGIN + CELL_W + WALL, FLOOR_H + 8))
    part.px = part.img.load()
    out["Modulo_fachada"] = part
    px = Px(CELL_W + WALL, FOUND)
    px.dither(0, 0, CELL_W + WALL - 1, FOUND - 1, "brown", "umber", 0.45)
    px.rect(0, FOUND - 6, CELL_W + WALL - 1, FOUND - 1, "slate")
    px.hline(0, CELL_W + WALL - 1, FOUND - 6, "ink")
    out["Modulo_fundacao"] = px
    return out


def ui_bits():
    out = {}
    px = Px(4, 4)
    px.rect(0, 0, 3, 3, "cream")
    out["Branco"] = px
    px = Px(24, 24)                       # placa escura atrás do ícone do obstáculo
    px.disc(11, 11, 10, "night")
    px.ring(11, 11, 10, "ink")
    out["Placa"] = px
    px = Px(12, DOOR_H + 4)               # contorno que pisca na porta durante a decisão
    px.frame(0, 0, 11, DOOR_H + 3, "lamp")
    px.frame(1, 1, 10, DOOR_H + 2, "gold")
    out["Porta_destaque"] = px
    return out


# ---------------------------------------------------------------- Montagem do layout de cada sala

def obstacle_prop(heist_id, room, index):
    kind, obs = KINDS[room["kind"]], room["obstacle"]
    if obs == PERSON:
        table = {("loterica", 0): "freguesa", ("loterica", 1): "seu_ze", ("joalheria", 0): "vendedora",
                 ("cambio", 1): "caixa", ("cambio", 3): "gerente", ("cassino", 0): "pasteleiro",
                 ("cassino", 2): "crupie", ("cassino", 4): "bicheiro", ("banco-central", 2): "vigia"}
        return "npc:" + table.get((heist_id, index), "seguranca")
    if obs == LOCK:
        if kind == "Vault":
            return "Cofrinho" if heist_id == "loterica" else ("CofreCentral" if heist_id == "banco-central" else "Cofre")
        if kind == "Office":
            return "GradeDourada" if heist_id == "banco-central" else "Arquivo"
        if kind == "Tunnel":
            return "GradeEnferrujada"
        return "Grade"
    if kind == "CameraRoom":
        return "Cftv"
    if kind == "Vault":
        return "CofreEletronico"
    if kind == "Tunnel":
        return "Sensor"
    if kind == "Office":
        return "Terminal"
    return "Painel"


PROP_SIZE = {}


def prop_geometry(prop):
    """(x do centro do adereço, y da base, x de trabalho, na parede do fundo?)"""
    if prop.startswith("npc:"):
        return 150, FEET, 124, False
    back_wall = prop in ("Cofre", "CofreEletronico", "CofreCentral", "Cftv", "Painel", "Sensor")
    if prop in ("Cofre", "CofreEletronico"):
        return 150, 22, 128, True
    if prop == "CofreCentral":
        return 140, 12, 112, True
    if prop == "Cftv":
        return 140, 30, 128, True
    if prop in ("Painel", "Sensor"):
        return 150, 40, 138, True
    if prop == "Terminal":
        return 152, 22, 130, False
    if prop == "Cofrinho":
        return 156, 4, 136, False
    if prop == "Arquivo":
        return 154, 4, 134, False
    return 156, 4, 132, False   # grades


GUARDS = {  # golpe -> {índice da sala: variante do guarda que patrulha}
    "joalheria": {2: "seguranca", 3: "seguranca"},
    "cambio": {2: "seguranca", 4: "seguranca", 1: "seguranca"},
    "cassino": {1: "leao", 3: "leao", 5: "leao"},
    "banco-central": {3: "vigia", 4: "vigia", 5: "vigia", 6: "vigia"},
}
CAMERAS = {  # golpe -> salas com câmera de segurança
    "joalheria": (0, 1, 3), "cambio": (1, 2, 4), "cassino": (2, 3, 5), "banco-central": (3, 4, 5, 6),
}


def room_layout(h, i, room, extra):
    kind = KINDS[room["kind"]]
    theme = room_theme(h["id"], room, i)
    t = THEMES[theme]
    prop = obstacle_prop(h["id"], room, i)
    px_, py_, wx, back = prop_geometry(prop)
    x0 = WALL + i * CELL_W
    guards = []
    if i in GUARDS.get(h["id"], {}):
        guards.append(dict(variant=GUARDS[h["id"]][i], minX=x0 + 24, maxX=x0 + 104))
    cams = []
    if i in CAMERAS.get(h["id"], ()):
        cams.append(dict(x=x0 + 20, y=ROOM_H - 14, facing=1))
    lamp = t["lamp"]
    if theme in ("tunel",):
        lights = [dict(x=x0 + 24, y=ROOM_H - 18, color=P["lamp"], radius=2.6, intensity=0.9),
                  dict(x=x0 + 120, y=ROOM_H - 18, color=P["lamp"], radius=2.6, intensity=0.9)]
    else:
        lights = [dict(x=x0 + 96, y=ROOM_H - 16, color=P[lamp], radius=5.0, intensity=0.85)]
    entry_x = x0 + 14 if not (i == 0 and h["id"] in ("cassino", "banco-central")) else x0 + 40
    return dict(index=i, name=room["name"], kind=room["kind"], obstacle=room["obstacle"], x=x0,
                background=f"Salas/Sala_{h['id']}_{i}.png", prop=prop, propX=x0 + px_, propY=py_, backWall=back,
                workX=x0 + wx, entryX=entry_x, exitX=x0 + ROOM_W - 6, iconX=x0 + px_, iconY=ROOM_H - 24,
                guards=guards, cameras=cams, lights=lights, alarmX=x0 + 60, alarmY=ROOM_H - 6,
                door=(extra["doors"][i] if i < len(extra["doors"]) else ""), dust=theme == "tunel")


def color_obj(name, a=1.0):
    c = P[name]
    return dict(r=c[0] / 255, g=c[1] / 255, b=c[2] / 255, a=a)


def main():
    if PREVIEW is not None:
        PREVIEW.mkdir(exist_ok=True)
    heists = load_heists()
    layout = dict(ppu=32, roomW=ROOM_W, roomH=ROOM_H, wall=WALL, cellW=CELL_W, feet=FEET, buildings=[],
                  defaults=[], npcs=[], props=[], doors=[], font=[])

    # salas padrão (uma por RoomKind) pros prefabs modulares
    for k, kind in enumerate(KINDS):
        theme = "tunel" if kind == "Tunnel" else ("garagem" if kind == "Garage" else "padrao")
        bg = room_background(kind, theme, "padrao", kind, LOCK, 1)
        bg.save(ART / f"Salas/Sala_Padrao_{kind}.png", PREVIEW, 2)
        layout["defaults"].append(dict(kind=k, background=f"Salas/Sala_Padrao_{kind}.png",
                                       door=THEMES[theme]["door"], lightColor=color_obj(THEMES[theme]["lamp"])))

    for h in heists:
        for i, room in enumerate(h["rooms"]):
            theme = room_theme(h["id"], room, i)
            bg = room_background(KINDS[room["kind"]], theme, h["id"], room["name"], room["obstacle"], i)
            bg.save(ART / f"Salas/Sala_{h['id']}_{i}.png", PREVIEW, 2)
        extra = build_underground(h, None, layout) if h["id"] in ("cassino", "banco-central") else build_street_level(h, None, layout)
        amb = AMBIENT.get(h["id"], ("pale", 0.6))
        b = dict(heistId=h["id"], name=h["name"], tier=h["tier"], ambient=color_obj(amb[0]), ambientIntensity=amb[1],
                 underground=h["id"] in ("cassino", "banco-central"), **{k: v for k, v in extra.items() if k != "doors"})
        b["rooms"] = [room_layout(h, i, r, extra) for i, r in enumerate(h["rooms"])]
        layout["buildings"].append(b)

    # adereços
    props = {"Grade": prop_grade("ferro"), "GradeEnferrujada": prop_grade("enferrujada"), "GradeDourada": prop_grade("dourada"),
             "Cofrinho": prop_cofrinho(), "Cofre": prop_cofre(56), "CofreEletronico": prop_cofre(56, keypad=True),
             "CofreCentral": prop_cofre(80), "Arquivo": prop_arquivo(), "Painel": prop_painel(), "Terminal": prop_terminal(),
             "Cftv": prop_cftv(), "Sensor": prop_sensor()}
    for name, states in props.items():
        for state, px in states.items():
            if not name.startswith(("Cofre", "Cftv", "Painel", "Sensor")):
                px.outline() if False else None
            px.save(ART / f"Aderecos/{name}_{state}.png", PREVIEW, 3)
        layout["props"].append(dict(name=name, closed=f"Aderecos/{name}_fechado.png", open=f"Aderecos/{name}_aberto.png"))

    # portas
    for style in ("madeira", "metal", "vidro", "tabuas", "buraco"):
        d = door_sprites(style)
        for state, px in d.items():
            px.save(ART / f"Predios/Porta_{style}_{state}.png", PREVIEW, 4)
        layout["doors"].append(dict(style=style, closed=f"Predios/Porta_{style}_fechada.png", open=f"Predios/Porta_{style}_aberta.png"))

    # NPCs
    for name, s in NPCS.items():
        anims = npc_frames(s)
        sh, order = sheet(anims, ["idle", "andar", "alerta", "distraido"])
        sh.save(ART / f"Personagens/NPC_{name}.png", PREVIEW, 4)
        layout["npcs"].append(dict(name=name, sheet=f"Personagens/NPC_{name}.png",
                                   anims=[dict(name=a, frames=n) for a, n in order]))

    # ladrão genérico em duas camadas: base e roupa (branca, tingida com a cor do tipo)
    base = thief_frames(GENERIC_THIEF_BASE)
    sh, order = sheet(base, ["idle", "andar", "trabalhar", "comemorar"])
    sh.save(ART / "Personagens/Ladrao_generico_base.png", PREVIEW, 4)
    roupa_spec = dict(GENERIC_THIEF_BASE)
    roupa_spec.update(shirt="cream", jacket="cream")
    full = thief_frames(roupa_spec)
    mask = {}
    for anim, frames_ in full.items():
        mask[anim] = []
        for fb, ff in zip(base[anim], frames_):
            m = Px(32, 32)
            for yy in range(32):
                for xx in range(32):
                    a, b2 = fb.get(xx, yy), ff.get(xx, yy)
                    if b2[3] and b2[:3] != a[:3] and b2[:3] in (P["cream"], P["pale"], P["mist"]):
                        m.dot(xx, yy, b2[:3])
            mask[anim].append(m)
    sh2, _ = sheet(mask, ["idle", "andar", "trabalhar", "comemorar"])
    sh2.save(ART / "Personagens/Ladrao_generico_roupa.png", PREVIEW, 4)
    layout["genericThief"] = dict(base="Personagens/Ladrao_generico_base.png", cloth="Personagens/Ladrao_generico_roupa.png",
                                  anims=[dict(name=a, frames=n) for a, n in order])

    for name, px in structure_modules().items():
        px.save(ART / f"Predios/{name}.png", PREVIEW, 2)
    for name, px in ui_bits().items():
        px.save(ART / f"Icones/{name}.png", PREVIEW, 4)

    # rua, veículos, fundo
    for name, px in street_props().items():
        px.save(ART / f"Rua/{name}.png", PREVIEW, 3)
    for i, px in enumerate(van_sprites()):
        px.save(ART / f"Rua/Van_{i}.png", PREVIEW, 3)
    for i, px in enumerate(police_sprites()):
        px.save(ART / f"Rua/Viatura_{i}.png", PREVIEW, 3)
    sky().save(ART / "Fundo/Ceu.png", PREVIEW, 1)
    skyline_layer(120, "deepgreen", 0.0, 3, 0, 0, hills=True).save(ART / "Fundo/Morro.png", PREVIEW, 1)
    skyline_layer(170, "dusk", 0.12, 4, 60, 160).save(ART / "Fundo/Predios_longe.png", PREVIEW, 1)
    skyline_layer(120, "night", 0.2, 5, 40, 110, neon=True).save(ART / "Fundo/Predios_perto.png", PREVIEW, 1)
    layout["backdrop"] = [dict(sprite="Fundo/Morro.png", parallax=0.08, y=10, order=-90),
                          dict(sprite="Fundo/Predios_longe.png", parallax=0.18, y=0, order=-85),
                          dict(sprite="Fundo/Predios_perto.png", parallax=0.35, y=-4, order=-80)]

    # ícones, anel, balão
    for name, px in icons().items():
        px.save(ART / f"Icones/{name}.png", PREVIEW, 6)
    ring_frames().save(ART / "Icones/Anel.png", PREVIEW, 4)
    b, tail = bubble()
    b.save(ART / "Icones/Balao.png", PREVIEW, 6)
    tail.save(ART / "Icones/Balao_ponta.png", PREVIEW, 6)

    # fonte
    atlas, table = font_atlas()
    atlas.save(ART / "Fonte/Fonte_pixel.png", PREVIEW, 4)
    layout["font"] = [dict(c=ch, x=x, y=y, w=w) for (ch, x, y, w) in table]
    layout["fontCellH"] = CELL_H

    # paleta
    palette_image(8).save(ART / "Paleta.png")
    palette_image(32).save(REPO / "docs/img/paleta.png")

    (HERE / "layout.json").write_text(json.dumps(layout, ensure_ascii=False, indent=1), encoding="utf-8")
    print("ok:", len(heists), "prédios,", sum(len(h["rooms"]) for h in heists), "salas")


if __name__ == "__main__":
    main()
