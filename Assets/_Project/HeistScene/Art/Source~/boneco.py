"""Bonecos 32x32 de lado, olhando pra direita, no mesmo padrão do 171: pé na linha de baixo (y = 1).

Um boneco é descrito por um dicionário (pele, cabelo, chapéu, roupa...) e cada pose vira um frame.
Usado pelos NPCs da cena (guardas, Seu Zé, crupiê...), pelo ladrão genérico e pelo Gazua.
"""
from paleta import Px, shade

CELL = 32

DEFAULT = dict(
    skin="skin", hair="brown", hair_style="short", hat=None, hat_color="slate",
    shirt="pale", jacket=None, tie=None, apron=None, pants="dusk", shoes="umber",
    glasses=False, mustache=False, beard=False, chain=False, belt="umber", item=None, item_color="steel",
    female=False, badge=None, collar=None,
)


def spec(**kw):
    s = dict(DEFAULT)
    s.update(kw)
    return s


# ---------------------------------------------------------------- partes (y pra cima)

def leg(px, hip_x, foot_dx, lift, dy, color, shoe):
    top, foot_y = 8 + dy, 2 + lift
    for y in range(foot_y, top + 1):
        t = (top - y) / max(top - foot_y, 1)
        x = hip_x + round(foot_dx * t)
        px.rect(x, y, x + 1, y, color)
    fx = hip_x + foot_dx
    px.rect(fx, foot_y - 1, fx + 2, foot_y - 1 + (1 if lift == 0 else 1), shoe)


def torso(px, s, dy):
    y0 = 9 + dy
    body = s["jacket"] or s["shirt"]
    px.rect(13, y0, 19, y0 + 7, body)
    px.rect(13, y0, 13, y0 + 7, shade(body, -1))              # costas na sombra
    if s["female"]:
        px.rect(13, y0 - 2, 19, y0, body)                        # saia/barra do vestido
    if s["jacket"]:
        px.rect(18, y0 + 3, 19, y0 + 7, s["shirt"])               # camisa aparecendo na frente
        px.dot(18, y0 + 2, shade(s["jacket"], -1))
    if s["tie"]:
        px.rect(19, y0 + 2, 19, y0 + 6, s["tie"])
    if s["apron"]:
        px.rect(17, y0 - 1, 20, y0 + 5, s["apron"])
        px.dot(20, y0 + 5, shade(s["apron"], -1))
    if s["chain"]:
        px.dot(19, y0 + 5, "gold")
        px.dot(18, y0 + 4, "gold")
    if s["badge"]:
        px.dot(18, y0 + 5, s["badge"])
    if s["collar"]:
        px.rect(17, y0 + 7, 19, y0 + 7, s["collar"])
    if s["belt"] and not s["female"]:
        px.rect(13, y0, 19, y0, s["belt"])
    px.rect(15, y0 + 8, 17, y0 + 8, s["skin"])                    # pescoço


def head(px, s, dy, mouth_open=False, blink=False, look_back=False):
    y = 18 + dy
    skin = s["skin"]
    px.rect(13, y, 19, y + 6, skin)
    px.dot(20, y + 3, skin)                                       # nariz
    px.dot(15, y + 3, shade(skin, -1))                            # orelha
    px.dot(15, y + 4, shade(skin, -1))
    eye_x = 18 if not look_back else 16
    if blink:
        px.rect(eye_x - 1, y + 4, eye_x, y + 4, "ink")
    else:
        px.dot(eye_x, y + 4, "ink")
    if s["glasses"]:
        px.rect(17, y + 4, 19, y + 4, "ink")
        px.dot(18, y + 4, "cyan")
    px.dot(19, y + 1, "ink" if mouth_open else shade(skin, -1))  # boca
    if mouth_open:
        px.dot(19, y + 2, "winedark")
    if s["mustache"]:
        px.rect(17, y + 2, 19, y + 2, s["hair"])
    if s["beard"]:
        px.rect(14, y, 19, y + 1, s["hair"])
    style = s["hair_style"]
    hair = s["hair"]
    if style == "short":
        px.rect(13, y + 6, 19, y + 6, hair)
        px.rect(13, y + 3, 14, y + 6, hair)
    elif style == "bald":
        px.rect(13, y + 3, 13, y + 4, hair)
    elif style == "bun":
        px.rect(13, y + 5, 19, y + 6, hair)
        px.rect(13, y + 2, 14, y + 6, hair)
        px.rect(11, y + 4, 12, y + 6, hair)                       # coque
    elif style == "long":
        px.rect(13, y + 5, 19, y + 6, hair)
        px.rect(12, y - 1, 14, y + 6, hair)
    elif style == "curly":
        px.rect(12, y + 5, 19, y + 7, hair)
        px.rect(12, y + 2, 14, y + 7, hair)
        px.dot(20, y + 6, hair)
    hat = s["hat"]
    hc = s["hat_color"]
    if hat == "cap":
        px.rect(13, y + 6, 19, y + 8, hc)
        px.rect(19, y + 6, 22, y + 6, shade(hc, -1))               # pala
        if s["badge"]:
            px.dot(17, y + 7, s["badge"])
    elif hat == "paper":
        px.rect(13, y + 6, 19, y + 9, hc)
        px.rect(14, y + 10, 18, y + 10, hc)
    elif hat == "fedora":
        px.rect(14, y + 7, 19, y + 9, hc)
        px.rect(12, y + 6, 21, y + 6, shade(hc, -1))
        px.rect(14, y + 7, 19, y + 7, "wine")
    elif hat == "visor":
        px.rect(13, y + 6, 19, y + 6, hair)
        px.rect(17, y + 6, 22, y + 6, "money")
    elif hat == "beanie":
        px.rect(13, y + 5, 19, y + 8, hc)
        px.rect(13, y + 5, 19, y + 5, shade(hc, -1))
        px.dot(16, y + 9, hc)


def arm(px, s, dy, hand, back=False):
    sx, sy = (14, 16 + dy) if back else (16, 16 + dy)
    sleeve = s["jacket"] or s["shirt"]
    color = shade(sleeve, -1) if back else sleeve
    hx, hy = hand
    px.line(sx, sy, hx, hy + 1, color, 2)
    px.rect(hx, hy, hx + 1, hy + 1, shade(s["skin"], -1) if back else s["skin"])


def item(px, s, hand, kind=None):
    kind = kind or s["item"]
    hx, hy = hand
    if kind == "flashlight":
        px.rect(hx + 1, hy, hx + 4, hy + 1, s["item_color"])
        px.dot(hx + 5, hy, "lamp")
        px.dot(hx + 5, hy + 1, "lamp")
    elif kind == "purse":
        px.rect(hx - 1, hy - 4, hx + 2, hy - 1, s["item_color"])
        px.line(hx, hy, hx - 1, hy - 1, "ink")
    elif kind == "newspaper":
        px.rect(hx + 1, hy - 1, hx + 5, hy + 5, "cream")
        px.hline(hx + 2, hx + 4, hy + 3, "slate")
        px.hline(hx + 2, hx + 4, hy + 1, "slate")
    elif kind == "tray":
        px.rect(hx - 1, hy + 2, hx + 5, hy + 2, "mist")
        px.rect(hx + 1, hy + 3, hx + 3, hy + 4, "kraft")


# ---------------------------------------------------------------- frame

def frame(s, body_dy=0, jump=0, legs=((0, 0), (0, 0)), back_hand=(14, 9), front_hand=(16, 9),
          mouth_open=False, blink=False, item_kind=None, kneel=False, extra=None, look_back=False):
    px = Px(CELL, CELL)
    d = body_dy - jump
    (bdx, blift), (fdx, flift) = legs
    if kneel:
        # ajoelhado: joelho de trás no chão, perna da frente dobrada
        px.rect(13, 2, 15, 3, shade(s["pants"], -1))
        px.rect(13, 1, 13, 2, s["shoes"])
        px.rect(17, 2, 18, 5, s["pants"])
        px.rect(17, 5, 20, 6, s["pants"])
        px.rect(19, 1, 21, 1, s["shoes"])
        px.rect(19, 2, 20, 4, s["pants"])
    else:
        leg(px, 14, bdx, blift, -jump, shade(s["pants"], -1), s["shoes"])
    arm(px, s, d, (back_hand[0], back_hand[1] + d), back=True)
    torso(px, s, d)
    head(px, s, d, mouth_open, blink, look_back)
    if not kneel:
        leg(px, 16, fdx, flift, -jump, s["pants"], s["shoes"])
    hand = (front_hand[0], front_hand[1] + d)
    arm(px, s, d, hand)
    if item_kind or s["item"]:
        item(px, s, hand, item_kind)
    if extra:
        extra(px, d)
    px.outline()
    return px


WALK4 = [  # (perna de trás, perna da frente, mão de trás, mão da frente)
    ((-2, 0), (2, 0), (17, 9), (13, 9)),
    ((0, 1), (0, 0), (15, 9), (15, 9)),
    ((2, 0), (-2, 0), (13, 9), (17, 9)),
    ((0, 0), (0, 1), (15, 9), (15, 9)),
]

WALK6 = [
    ((-3, 0), (3, 0), (18, 10), (12, 10)),
    ((-1, 0), (1, 2), (17, 9), (13, 9)),
    ((1, 2), (-1, 0), (15, 9), (16, 9)),
    ((3, 0), (-3, 0), (12, 10), (18, 10)),
    ((1, 0), (-1, 2), (13, 9), (17, 9)),
    ((-1, 2), (1, 0), (16, 9), (15, 9)),
]


def npc_frames(s):
    """Sheet de NPC: idle (2), andar (4), alerta (2), distraido (2)."""
    hold = s["item"] in ("flashlight",)
    fh = (18, 11) if hold else (16, 9)
    idle = [frame(s, front_hand=fh), frame(s, body_dy=-1 if False else 0, front_hand=fh, blink=True)]
    idle[1] = frame(s, body_dy=0, front_hand=fh, blink=True)
    walk = []
    for i, (b, f, bh, fhw) in enumerate(WALK4):
        walk.append(frame(s, body_dy=1 if i in (1, 3) else 0, legs=(b, f), back_hand=bh,
                          front_hand=(18, 11) if hold else fhw))
    alert = [
        frame(s, front_hand=(19, 15) if not hold else (19, 13), mouth_open=True),
        frame(s, body_dy=1, front_hand=(19, 16) if not hold else (19, 14), mouth_open=True),
    ]
    def zz(px, d):
        pass
    distracted = [
        frame(s, body_dy=-1, front_hand=(16, 9), blink=True, item_kind="none"),
        frame(s, body_dy=-2, front_hand=(16, 9), blink=True, item_kind="none"),
    ]
    return {"idle": idle, "andar": walk, "alerta": alert, "distraido": distracted}


def thief_frames(s, work_extra=None, celebrate_item=None):
    """Sheet de ladrão no padrão do 171: idle (4), andar (6), trabalhar (6), comemorar (4)."""
    idle = [frame(s), frame(s), frame(s, body_dy=-1, front_hand=(16, 10)), frame(s, body_dy=-1, front_hand=(16, 10), blink=True)]
    walk = []
    for i, (b, f, bh, fh) in enumerate(WALK6):
        walk.append(frame(s, body_dy=1 if i in (1, 4) else 0, legs=(b, f), back_hand=bh, front_hand=fh))
    work = []
    hands = [(21, 12), (22, 13), (21, 12), (22, 11), (21, 13), (22, 12)]
    for i, h in enumerate(hands):
        work.append(frame(s, body_dy=-1 if i % 2 else 0, front_hand=h, back_hand=(19, 11),
                          extra=(lambda px, d, i=i: work_extra(px, d, i)) if work_extra else None))
    celebrate = [
        frame(s, body_dy=-1, front_hand=(19, 13), legs=((-1, 0), (1, 0))),
        frame(s, jump=1, front_hand=(20, 24), mouth_open=True, back_hand=(12, 13), legs=((-1, 1), (1, 1)),
              extra=(lambda px, d: celebrate_item(px, d, 0)) if celebrate_item else None),
        frame(s, jump=1, front_hand=(20, 25), mouth_open=True, back_hand=(12, 14), legs=((-1, 1), (1, 1)),
              extra=(lambda px, d: celebrate_item(px, d, 1)) if celebrate_item else None),
        frame(s, front_hand=(19, 20), back_hand=(13, 9)),
    ]
    return {"idle": idle, "andar": walk, "trabalhar": work, "comemorar": celebrate}


def sheet(anims, order):
    """Junta as animações num sheet: uma linha por animação, na ordem dada."""
    cols = max(len(anims[name]) for name in order)
    out = Px(cols * CELL, len(order) * CELL)
    for row, name in enumerate(order):
        for col, f in enumerate(anims[name]):
            out.paste(f, col * CELL, (len(order) - 1 - row) * CELL)
    return out, [(name, len(anims[name])) for name in order]
