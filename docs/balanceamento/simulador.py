"""
Simulador de balanceamento d'A Firma (issue #33).

Lê os números direto dos assets (golpes, upgrades, ladrões, GameConfig) e simula um jogador
guloso do zero até concluir o Banco Central. Mostra em que minuto cada golpe é concluído.

    python docs/balanceamento/simulador.py            # números atuais dos assets
    python docs/balanceamento/simulador.py --log      # golpe a golpe
    python docs/balanceamento/simulador.py --sem-habilidades
    python docs/balanceamento/simulador.py --extras   # quanto cada golpe extra rende perto da escada
    python docs/balanceamento/simulador.py --ciclos 3  # prestígio: ciclos seguidos com Procurado e bônus de Fama

O que o jogador simulado faz:
- recruta o especialista que falta pro próximo golpe (e um Pé-na-Tábua a partir do Câmbio);
- compra Van quando tem mais ladrão que vaga e, com o resto, o upgrade mais barato (Contatos entra na conta);
- troca quem ficou pra trás por um recruta do nível mais alto que os Contatos dão (+ Lenda), pagando a troca de
  candidatos até sair o tipo certo nesse nível (--sem-contatos desliga: só recruta nível 1, como a v1);
- escolhe a crew e o golpe: o próximo da escada se der pra concluir, senão o que rende mais Cr$/s
  (os golpes extras entram aqui, como farm; nunca são o "próximo da escada");
- mete o pé na porta antes da sirene, ou segue com sirene se a sala acaba antes da polícia
  (cofre limpo com sirene = concluído com 80% da sacola);
- usa as habilidades do jeito ideal (Lábia −25, Loop 15 s, Pé-de-cabra na sala de Fechadura mais cara).

No prestígio (--ciclos) ele soma a Fama do ciclo, compra sempre o bônus de Fama mais barato (no empate:
Esquema, Herança, Lenda, Sangue-frio, Rede, Reputação) e começa o ciclo seguinte com o Procurado +1.

É um jogador perfeito: não erra, não lê texto, não tem evento ruim. Jogador de verdade é mais lento,
então o tempo daqui é o piso. O playtest cronometrado (#43) é quem dá o número real.
"""
import argparse, itertools, math, pathlib, re, sys

LOCK, SYSTEM, PERSON = 0, 1, 2
DRIVER, LOCKSMITH, HACKER, CON = 0, 1, 2, 3
SPECIALTY = {LOCKSMITH: LOCK, HACKER: SYSTEM, CON: PERSON}

# Tempo de interface que o motor não conta: planejar, mapa, jornal (por golpe) e porta/eventos (por sala).
OVERHEAD_HEIST, OVERHEAD_ROOM, DOOR_SECONDS = 20, 4, 1.5

ROOT = pathlib.Path(__file__).resolve().parents[2] / "Assets" / "_Project"


def _fields(text):
    return {k: v.strip() for k, v in re.findall(r"^\s+(\w+): (.*)$", text, re.M)}


def load_project(root=ROOT):
    cfg = _fields((root / "Data" / "Config" / "GameConfig.asset").read_text(encoding="utf-8"))
    cfg = {k: float(v) for k, v in cfg.items() if re.fullmatch(r"-?[\d.]+", v)}

    heists = []
    for path in (root / "Data" / "Heists").glob("Heist_*.asset"):
        text = path.read_text(encoding="utf-8")
        head = text.split("\n  rooms:")[0]
        rooms = []
        for block in re.split(r"\n\s+- name: ", text)[1:]:
            f = _fields("\n" + block)
            rooms.append((int(f["obstacle"]), float(f["difficulty"]), float(f["loot"]), float(f["alertPerSecond"])))
        f = _fields(head)
        name = f["displayName"].strip('"').encode("latin-1").decode("unicode_escape")  # YAML: "Lot\xE9rica"
        guid = re.search(r"guid: (\w+)", path.with_name(path.name + ".meta").read_text(encoding="utf-8")).group(1)
        unlocked_by = re.search(r"unlockedBy: \{fileID: \d+(?:, guid: (\w+))?", head).group(1)
        heists.append({"name": name, "rooms": rooms, "guid": guid, "unlockedBy": unlocked_by, "tier": int(f["tier"]),
                       "default": f.get("unlockedByDefault") == "1", "extra": f.get("extra") == "1"})
    heists.sort(key=lambda h: (h["extra"], h["tier"]))  # a escada primeiro, em ordem; os extras depois

    upgrades = {}
    for path in (root / "Data").rglob("Upgrade_*.asset"):
        f = _fields(path.read_text(encoding="utf-8"))
        upgrades[f["id"]] = (float(f["baseCost"]), float(f["costGrowth"]), int(f["maxLevel"]), float(f["valuePerLevel"]))

    thieves = {}
    for path in root.rglob("ThiefType_*.asset"):
        f = _fields(path.read_text(encoding="utf-8"))
        thieves[int(f["type"])] = {k: float(f[k]) for k in (
            "baseSkill", "baseStealth", "skillPerLevel", "stealthPerLevel", "policeCountdownBonus", "escapeLootBonus")}

    perks = {}
    for path in (root / "Data" / "Progression" / "FamePerks").glob("FamePerk_*.asset"):
        f = _fields(path.read_text(encoding="utf-8"))
        perks[f["id"]] = {"effect": int(f["effect"]), "base": float(f["baseCost"]), "growth": float(f["costGrowth"]),
                          "max": int(f["maxLevel"]), "value": float(f["valuePerLevel"])}

    side_job = max(float(_fields(p.read_text(encoding="utf-8"))["incomePerSecond"])
                   for p in root.rglob("SideJob_*.asset") if "CatarLatinha" not in p.name)
    match = float(_fields(next(root.rglob("SideJob_171.asset")).read_text(encoding="utf-8"))["matchMultiplier"])
    return {"cfg": cfg, "heists": heists, "upgrades": upgrades, "thieves": thieves, "perks": perks,
            "sideJob": side_job, "sideMatch": match}


# FamePerkEffect, na ordem do enum
REPUTACAO, HERANCA, LENDA, ESQUEMA, REDE, SANGUE_FRIO = range(6)
PERK_PRIORITY = ["esquema", "heranca", "lenda", "sangue-frio-da-firma", "rede", "reputacao"]


def simulate(P, abilities=True, limit_minutes=120, wanted=0, perk_levels=None, smart_recruits=True):
    """Um ciclo, do zero até o Banco Central. wanted e perk_levels vêm do prestígio (ciclo 1: 0 e nenhum)."""
    cfg, T = P["cfg"], P["thieves"]
    perk_levels = perk_levels or {}

    def perk(effect):
        return sum(perk_levels.get(k, 0) * v["value"] for k, v in P["perks"].items() if v["effect"] == effect)

    w_difficulty = cfg.get("wantedDifficultyMult", 1.25) ** wanted
    w_loot = cfg.get("wantedLootMult", 1.5) ** wanted
    w_alert = cfg.get("wantedAlertMult", 1.1) ** wanted
    discount = 1 - perk(REPUTACAO) / 100
    crew, up = [], {k: 0 for k in P["upgrades"]}
    money, t, completed, milestones, log = cfg.get("startingMoney", 0) + perk(HERANCA), 0.0, set(), {}, []
    cycle_loot = 0.0
    rates = []  # (minuto, próximo da escada, {golpe: melhor Cr$/s}) a cada decisão, pro relatório dos extras

    def recruit_price(rolled):
        """Recruta de nível sorteado `rolled`: preço dele + as trocas pagas esperadas até aparecer o tipo certo nesse nível."""
        price = round(cfg["recruitBaseCost"] * cfg["recruitCostGrowthPerLevel"] ** (rolled - 1) * discount)
        if rolled <= 1: return price
        contacts = total("contatos")
        q = 0.25 / (1 + contacts)                                   # tipo certo e nível máximo, por candidato
        refreshes = 1 / (1 - (1 - q) ** cfg["recruitCandidates"]) - 1
        # campos que o GameConfig.asset não lista usam o padrão do GameConfig.cs
        refresh = round(cfg.get("recruitRefreshCost", 25) * max(0, 1 - cfg.get("recruitRefreshDiscountPerContacts", 0.1) * contacts))
        return price + refreshes * refresh

    def best_rolled():
        return int(1 + total("contatos")) if smart_recruits else 1

    def recruit(kind, rolled=1):
        lvl = int(min(cfg["maxLevel"], rolled + perk(LENDA)))
        crew.append({"type": kind, "lvl": lvl, "xp": 0,
                     "skill": T[kind]["baseSkill"] + T[kind]["skillPerLevel"] * (lvl - 1),
                     "stealth": T[kind]["baseStealth"] + T[kind]["stealthPerLevel"] * (lvl - 1)})

    def cost(k):
        base, growth, max_level, _ = P["upgrades"][k]
        return None if up[k] >= max_level else round(base * growth ** up[k])

    def total(k): return up[k] * P["upgrades"][k][3] if k in up else 0
    def skill(x): return x["skill"] * (1 + total("ferramentas") / 100)
    def stealth(x): return x["stealth"] * (1 + total("tenis-de-molinha") / 100)

    def run(h, team, push_siren):
        rooms = P["heists"][h]["rooms"]
        avg_stealth = sum(stealth(x) for x in team) / len(team)
        types = {x["type"] for x in team}
        countdown = cfg["policeCountdownSeconds"] + total("radio-da-policia") + max(T[x["type"]]["policeCountdownBonus"] for x in team)
        keep_siren = min(1, cfg["hastyEscapeLootKeep"] + max(T[x["type"]]["escapeLootBonus"] for x in team))
        plan = []
        for obstacle, difficulty, loot, alert_rate in rooms:
            specialists = [skill(x) for x in team if SPECIALTY.get(x["type"]) == obstacle]
            power = max(specialists) if specialists else cfg["noSpecialistPowerFactor"] * max(skill(x) for x in team)
            seconds = max(0.5, difficulty * w_difficulty / max(1, power))
            rate = alert_rate * w_alert * (1 - perk(SANGUE_FRIO) / 100) * (1 - cfg["stealthAlertFactor"] * min(1, avg_stealth / 100))
            if not specialists: rate *= cfg["noSpecialistAlertMult"]
            plan.append([obstacle, seconds, rate, loot * w_loot, 0.0])
        budget = 100
        if abilities:
            locks = [p for p in plan if p[0] == LOCK]
            if LOCKSMITH in types and locks:
                p = max(locks, key=lambda p: p[1] * p[2]); p[1], p[2], p[4] = 0.0, 0.0, 15.0
            if HACKER in types:
                p = max(plan, key=lambda p: p[2] * min(15, p[1]))
                if p[1] > 0: p[2] *= max(0.0, 1 - 15 / p[1])
            if CON in types: budget += 25
        alert, secs, bag, done, siren, police = 0.0, 0.0, 0.0, 0, False, 0.0
        for _, seconds, rate, loot, fixed in plan:
            if not siren and alert + rate * seconds + fixed >= budget:
                if not push_siren: break
                to_siren = (budget - alert - fixed) / rate if rate > 0 else 0
                if seconds - to_siren >= countdown: break
                siren, police, alert = True, countdown - (seconds - to_siren), budget
            elif siren:
                if seconds + DOOR_SECONDS >= police: break
                police -= seconds + DOOR_SECONDS
            else:
                alert += rate * seconds + fixed
            secs += seconds + OVERHEAD_ROOM; bag += loot; done += 1
        keep = keep_siren if siren else 1
        return {"secs": secs + OVERHEAD_HEIST, "loot": bag * keep * (1 + total("lavanderia") / 100) * (1 + perk(ESQUEMA) / 100),
                "done": done, "complete": done == len(rooms), "alert": alert, "siren": siren}

    def passive(dt, team):
        idle = [x for x in crew if x not in team]
        income = cfg["scavengeIncomePerSecond"] + sum(
            P["sideJob"] * (1 + cfg.get("sideJobLevelBonus", 0.1) * (x["lvl"] - 1)) * P["sideMatch"] for x in idle)
        return income * (1 + total("ponto-de-bico") / 100) * (1 + perk(REDE) / 100) * dt

    starting = [CON, LOCKSMITH]  # GameConfig.startingCrew
    for kind in starting: recruit(kind)
    H = P["heists"]
    by_guid = {h["guid"]: i for i, h in enumerate(H)}
    ladder = [i for i, h in enumerate(H) if not h["extra"]]
    last = ladder[-1]
    while t < limit_minutes * 60 and last not in completed:
        slots = int(cfg["startingHeistSlots"] + total("van"))
        unlocked = [i for i, h in enumerate(H) if h["default"] or by_guid.get(h["unlockedBy"]) in completed]
        target = max(i for i in unlocked if i in ladder)
        need = {room[0] for room in H[target]["rooms"]}
        have = {SPECIALTY.get(x["type"]) for x in crew}
        wanted = [[k for k, v in SPECIALTY.items() if v == o][0] for o in sorted(need - have)]
        if target >= 2 and not any(x["type"] == DRIVER for x in crew): wanted.append(DRIVER)
        for kind in wanted:
            if len(crew) >= cfg["startingCrewSlots"] + total("quartos"):
                c = cost("quartos")
                if c is None or money < c: continue
                money -= c; up["quartos"] += 1
            rolled = next((r for r in range(best_rolled(), 0, -1) if money >= recruit_price(r)), None)
            if rolled is not None:
                money -= recruit_price(rolled); recruit(kind, rolled)
                log.append(f"{t / 60:5.1f} min  recruta {['Pé-na-Tábua', 'Gazua', 'Disquete', '171'][kind]} nível {crew[-1]['lvl']}")
        if len(crew) > slots and cost("van") is not None and money >= cost("van"):
            money -= cost("van"); up["van"] += 1; slots += 1
        buyable = ("ferramentas", "tenis-de-molinha", "lavanderia", "ponto-de-bico", "radio-da-policia") + (("contatos",) if smart_recruits else ())
        while True:
            options = sorted((cost(k), k) for k in buyable if k in up and cost(k) is not None)
            if not options or money < options[0][0]: break
            money -= options[0][0]; up[options[0][1]] += 1
        if smart_recruits:
            rolled = best_rolled()
            for i, x in enumerate(crew):
                new_lvl = int(min(cfg["maxLevel"], rolled + perk(LENDA)))
                if new_lvl >= x["lvl"] + 2 and money >= recruit_price(rolled):
                    money -= recruit_price(rolled)
                    kind = x["type"]; crew.pop(i); recruit(kind, rolled); crew.insert(i, crew.pop())
                    log.append(f"{t / 60:5.1f} min  troca o {['Pé-na-Tábua', 'Gazua', 'Disquete', '171'][kind]} nível {x['lvl']} por um nível {new_lvl}")

        best, rate_by_heist = None, {}
        for h in unlocked:
            for n in range(1, min(slots, len(crew)) + 1):
                for team in itertools.combinations(crew, n):
                    for push in (False, True):
                        r = run(h, list(team), push)
                        if r["done"] == 0: continue
                        rate_by_heist[h] = max(rate_by_heist.get(h, 0), r["loot"] / r["secs"])
                        score = (h == target and r["complete"] and h not in completed, r["loot"] / r["secs"])
                        if best is None or score > best[0]: best = (score, h, list(team), r)
        rates.append((t / 60, target, rate_by_heist))
        if best is None:
            t += 30; money += passive(30, []); continue
        _, h, team, r = best
        t += r["secs"]; money += r["loot"] + passive(r["secs"], team); cycle_loot += r["loot"]
        for x in team:
            x["xp"] += cfg["xpPerRoom"] * r["done"]
            while x["lvl"] < cfg["maxLevel"] and x["xp"] >= cfg["xpPerLevelFactor"] * x["lvl"]:
                x["xp"] -= cfg["xpPerLevelFactor"] * x["lvl"]; x["lvl"] += 1
                x["skill"] += T[x["type"]]["skillPerLevel"]; x["stealth"] += T[x["type"]]["stealthPerLevel"]
        if r["complete"] and h not in completed:
            completed.add(h); milestones[H[h]["name"]] = round(t / 60, 1)
        log.append(f"{t / 60:5.1f} min  {H[h]['name']:<34} salas {r['done']}/{len(H[h]['rooms'])}"
                   f"{' sirene' if r['siren'] else '       '}  +Cr$ {r['loot']:>9,.0f}  caixa Cr$ {money:>11,.0f}"
                   f"  níveis {[x['lvl'] for x in crew]}")
    return {"milestones": milestones, "minutes": round(t / 60, 1), "upgrades": up, "log": log, "rates": rates,
            "cycleLoot": cycle_loot, "done": last in completed}


def perk_cost(P, perk_id, level):
    v = P["perks"][perk_id]
    return math.ceil(v["base"] * v["growth"] ** level - 1e-6)


def simulate_cycles(P, cycles, abilities=True, smart_recruits=True):
    """Ciclos seguidos: no fim de cada um, Fama = √(loot do ciclo ÷ divisor), compra bônus e Procurado +1."""
    cfg = P["cfg"]
    fame, wanted, levels, out = 0, 0, {}, []
    for _ in range(cycles):
        result = simulate(P, abilities, wanted=wanted, perk_levels=dict(levels), smart_recruits=smart_recruits)
        gained = math.floor(math.sqrt(max(0, result["cycleLoot"]) / cfg.get("fameDivisor", 10000)))
        fame += gained
        bought = []
        while True:
            options = [(perk_cost(P, k, levels.get(k, 0)), PERK_PRIORITY.index(k) if k in PERK_PRIORITY else 99, k)
                       for k, v in P["perks"].items() if levels.get(k, 0) < v["max"]]
            if not options: break
            price, _, k = min(options)
            if price > fame: break
            fame -= price; levels[k] = levels.get(k, 0) + 1; bought.append(k)
        out.append({"wanted": wanted, "result": result, "fameGained": gained, "bought": bought,
                    "levels": dict(levels), "fameLeft": fame})
        if not result["done"]: break
        wanted += 1
    return out


def cycles_report(P, cycles):
    names = [h["name"] for h in P["heists"] if not h["extra"]]
    lines = ["Ciclos de prestígio (min de jogo ativo até concluir cada golpe da escada):",
             "  " + f"{'ciclo':<6}{'proc.':<6}" + "".join(f"{n.split()[0][:10]:>11}" for n in names) + "   Fama  bônus depois do ciclo"]
    for i, c in enumerate(cycles, 1):
        m = c["result"]["milestones"]
        row = "".join(f"{m.get(n, float('nan')):>11.1f}" for n in names)
        perks = ", ".join(f"{k} {v}" for k, v in sorted(c["levels"].items()))
        lines.append(f"  {i:<6}{c['wanted']:<6}{row}   +{c['fameGained']:<4} {perks}")
        if not c["result"]["done"]:
            lines.append(f"  (o ciclo {i} parou em {c['result']['minutes']} min sem concluir o Banco Central)")
    return "\n".join(lines)


def extras_report(P, result):
    """
    Pra cada golpe extra, na faixa dele (de quando libera até concluir o degrau da escada que estava na mira):
    Cr$/s do extra ÷ Cr$/s do melhor golpe da escada liberado, decisão a decisão.
    """
    H = P["heists"]
    lines = ["Golpes extras na faixa deles (Cr$/s do extra ÷ melhor da escada; abaixo de 100% não fura a escada):"]
    for i, h in enumerate(H):
        if not h["extra"]: continue
        seen = [(minute, target, r) for minute, target, r in result["rates"] if i in r]
        if not seen:
            lines.append(f"  {h['name']:<34} não liberou")
            continue
        band = [r[i] / max(v for j, v in r.items() if not H[j]["extra"]) for _, target, r in seen if target == seen[0][1]]
        values = sorted(band)
        lines.append(f"  {h['name']:<34} libera em {seen[0][0]:5.1f} min · faixa até {H[seen[0][1]]['name'][:12]:<12} · "
                     f"mín {values[0]:4.0%} · mediana {values[len(values) // 2]:4.0%} · máx {values[-1]:4.0%}")
    return "\n".join(lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--log", action="store_true", help="mostra golpe a golpe")
    parser.add_argument("--sem-habilidades", action="store_true", help="simula sem usar as habilidades ativas")
    parser.add_argument("--extras", action="store_true", help="mostra quanto cada golpe extra rende perto da escada")
    parser.add_argument("--ciclos", type=int, default=0, help="simula N ciclos seguidos de prestígio")
    parser.add_argument("--sem-contatos", action="store_true", help="o jogador só recruta nível 1 (não usa Contatos)")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    project = load_project()
    result = simulate(project, abilities=not args.sem_habilidades, smart_recruits=not args.sem_contatos)
    if args.log: print("\n".join(result["log"]))
    print("Golpe concluído em (min de jogo ativo):")
    for name, minute in result["milestones"].items(): print(f"  {name:<34} {minute:5.1f}")
    if not any(name.startswith("Banco Central") for name in result["milestones"]):
        print(f"  (parou em {result['minutes']} min sem concluir o último golpe)")
    if args.extras: print(extras_report(project, result))
    if args.ciclos: print(cycles_report(project, simulate_cycles(project, args.ciclos, not args.sem_habilidades, not args.sem_contatos)))
