"""
Simulador de balanceamento d'A Firma (issue #33).

Lê os números direto dos assets (golpes, upgrades, ladrões, GameConfig) e simula um jogador
guloso do zero até concluir o Banco Central. Mostra em que minuto cada golpe é concluído.

    python docs/balanceamento/simulador.py            # números atuais dos assets
    python docs/balanceamento/simulador.py --log      # golpe a golpe
    python docs/balanceamento/simulador.py --sem-habilidades

O que o jogador simulado faz:
- recruta o especialista que falta pro próximo golpe (e um Pé-na-Tábua a partir do Câmbio);
- compra Van quando tem mais ladrão que vaga e, com o resto, o upgrade mais barato;
- escolhe a crew e o golpe: o próximo da escada se der pra concluir, senão o que rende mais Cr$/s;
- mete o pé na porta antes da sirene, ou segue com sirene se a sala acaba antes da polícia
  (cofre limpo com sirene = concluído com 80% da sacola);
- usa as habilidades do jeito ideal (Lábia −25, Loop 15 s, Pé-de-cabra na sala de Fechadura mais cara).

É um jogador perfeito: não erra, não lê texto, não tem evento ruim. Jogador de verdade é mais lento,
então o tempo daqui é o piso. O playtest cronometrado (#43) é quem dá o número real.
"""
import argparse, itertools, pathlib, re, sys

LOCK, SYSTEM, PERSON = 0, 1, 2
DRIVER, LOCKSMITH, HACKER, CON = 0, 1, 2, 3
SPECIALTY = {LOCKSMITH: LOCK, HACKER: SYSTEM, CON: PERSON}

# Tempo de interface que o motor não conta: planejar, mapa, jornal (por golpe) e porta/eventos (por sala).
OVERHEAD_HEIST, OVERHEAD_ROOM, DOOR_SECONDS = 20, 4, 1.5
# Bônus de renda do bico por nível do ladrão (hoje no MockEconomyService, vira dado na #37).
SIDE_JOB_PER_LEVEL = 0.1

ROOT = pathlib.Path(__file__).resolve().parents[2] / "Assets" / "_Project"


def _fields(text):
    return {k: v.strip() for k, v in re.findall(r"^\s+(\w+): (.*)$", text, re.M)}


def load_project(root=ROOT):
    cfg = _fields((root / "Data" / "Config" / "GameConfig.asset").read_text(encoding="utf-8"))
    cfg = {k: float(v) for k, v in cfg.items() if re.fullmatch(r"-?[\d.]+", v)}

    heists = []
    for path in sorted((root / "Data" / "Heists").glob("Heist_*.asset")):
        text = path.read_text(encoding="utf-8")
        rooms = []
        for block in re.split(r"\n\s+- name: ", text)[1:]:
            f = _fields("\n" + block)
            rooms.append((int(f["obstacle"]), float(f["difficulty"]), float(f["loot"]), float(f["alertPerSecond"])))
        name = _fields(text)["displayName"].strip('"').encode("latin-1").decode("unicode_escape")  # YAML: "Lot\xE9rica"
        heists.append((name, rooms))

    upgrades = {}
    for path in (root / "Data").rglob("Upgrade_*.asset"):
        f = _fields(path.read_text(encoding="utf-8"))
        upgrades[f["id"]] = (float(f["baseCost"]), float(f["costGrowth"]), int(f["maxLevel"]), float(f["valuePerLevel"]))

    thieves = {}
    for path in root.rglob("ThiefType_*.asset"):
        f = _fields(path.read_text(encoding="utf-8"))
        thieves[int(f["type"])] = {k: float(f[k]) for k in (
            "baseSkill", "baseStealth", "skillPerLevel", "stealthPerLevel", "policeCountdownBonus", "escapeLootBonus")}

    side_job = max(float(_fields(p.read_text(encoding="utf-8"))["incomePerSecond"])
                   for p in root.rglob("SideJob_*.asset") if "CatarLatinha" not in p.name)
    match = float(_fields(next(root.rglob("SideJob_171.asset")).read_text(encoding="utf-8"))["matchMultiplier"])
    return {"cfg": cfg, "heists": heists, "upgrades": upgrades, "thieves": thieves, "sideJob": side_job, "sideMatch": match}


def simulate(P, abilities=True, limit_minutes=120):
    cfg, T = P["cfg"], P["thieves"]
    crew, up = [], {k: 0 for k in P["upgrades"]}
    money, t, completed, milestones, log = cfg.get("startingMoney", 0), 0.0, set(), {}, []

    def recruit(kind):
        crew.append({"type": kind, "lvl": 1, "xp": 0, "skill": T[kind]["baseSkill"], "stealth": T[kind]["baseStealth"]})

    def cost(k):
        base, growth, max_level, _ = P["upgrades"][k]
        return None if up[k] >= max_level else round(base * growth ** up[k])

    def total(k): return up[k] * P["upgrades"][k][3] if k in up else 0
    def skill(x): return x["skill"] * (1 + total("ferramentas") / 100)
    def stealth(x): return x["stealth"] * (1 + total("tenis-de-molinha") / 100)

    def run(h, team, push_siren):
        rooms = P["heists"][h][1]
        avg_stealth = sum(stealth(x) for x in team) / len(team)
        types = {x["type"] for x in team}
        countdown = cfg["policeCountdownSeconds"] + total("radio-da-policia") + max(T[x["type"]]["policeCountdownBonus"] for x in team)
        keep_siren = min(1, cfg["hastyEscapeLootKeep"] + max(T[x["type"]]["escapeLootBonus"] for x in team))
        plan = []
        for obstacle, difficulty, loot, alert_rate in rooms:
            specialists = [skill(x) for x in team if SPECIALTY.get(x["type"]) == obstacle]
            power = max(specialists) if specialists else cfg["noSpecialistPowerFactor"] * max(skill(x) for x in team)
            seconds = max(0.5, difficulty / max(1, power))
            rate = alert_rate * (1 - cfg["stealthAlertFactor"] * min(1, avg_stealth / 100))
            if not specialists: rate *= cfg["noSpecialistAlertMult"]
            plan.append([obstacle, seconds, rate, loot, 0.0])
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
        return {"secs": secs + OVERHEAD_HEIST, "loot": bag * keep * (1 + total("lavanderia") / 100),
                "done": done, "complete": done == len(rooms), "alert": alert, "siren": siren}

    def passive(dt, team):
        idle = [x for x in crew if x not in team]
        income = cfg["scavengeIncomePerSecond"] + sum(
            P["sideJob"] * (1 + SIDE_JOB_PER_LEVEL * (x["lvl"] - 1)) * P["sideMatch"] for x in idle)
        return income * (1 + total("ponto-de-bico") / 100) * dt

    starting = [CON, LOCKSMITH]  # GameConfig.startingCrew
    for kind in starting: recruit(kind)
    last = len(P["heists"]) - 1
    while t < limit_minutes * 60 and last not in completed:
        slots = int(cfg["startingHeistSlots"] + total("van"))
        unlocked = [i for i in range(len(P["heists"])) if i == 0 or (i - 1) in completed]
        target = max(unlocked)
        need = {room[0] for room in P["heists"][target][1]}
        have = {SPECIALTY.get(x["type"]) for x in crew}
        wanted = [[k for k, v in SPECIALTY.items() if v == o][0] for o in sorted(need - have)]
        if target >= 2 and not any(x["type"] == DRIVER for x in crew): wanted.append(DRIVER)
        for kind in wanted:
            if len(crew) >= cfg["startingCrewSlots"] + total("quartos"):
                c = cost("quartos")
                if c is None or money < c: continue
                money -= c; up["quartos"] += 1
            if money >= cfg["recruitBaseCost"]:
                money -= cfg["recruitBaseCost"]; recruit(kind)
                log.append(f"{t / 60:5.1f} min  recruta {['Pé-na-Tábua', 'Gazua', 'Disquete', '171'][kind]}")
        if len(crew) > slots and cost("van") is not None and money >= cost("van"):
            money -= cost("van"); up["van"] += 1; slots += 1
        while True:
            options = sorted((cost(k), k) for k in ("ferramentas", "tenis-de-molinha", "lavanderia", "ponto-de-bico", "radio-da-policia")
                             if k in up and cost(k) is not None)
            if not options or money < options[0][0]: break
            money -= options[0][0]; up[options[0][1]] += 1

        best = None
        for h in unlocked:
            for n in range(1, min(slots, len(crew)) + 1):
                for team in itertools.combinations(crew, n):
                    for push in (False, True):
                        r = run(h, list(team), push)
                        if r["done"] == 0: continue
                        score = (h == target and r["complete"] and h not in completed, r["loot"] / r["secs"])
                        if best is None or score > best[0]: best = (score, h, list(team), r)
        if best is None:
            t += 30; money += passive(30, []); continue
        _, h, team, r = best
        t += r["secs"]; money += r["loot"] + passive(r["secs"], team)
        for x in team:
            x["xp"] += cfg["xpPerRoom"] * r["done"]
            while x["lvl"] < cfg["maxLevel"] and x["xp"] >= cfg["xpPerLevelFactor"] * x["lvl"]:
                x["xp"] -= cfg["xpPerLevelFactor"] * x["lvl"]; x["lvl"] += 1
                x["skill"] += T[x["type"]]["skillPerLevel"]; x["stealth"] += T[x["type"]]["stealthPerLevel"]
        if r["complete"] and h not in completed:
            completed.add(h); milestones[P["heists"][h][0]] = round(t / 60, 1)
        log.append(f"{t / 60:5.1f} min  {P['heists'][h][0]:<34} salas {r['done']}/{len(P['heists'][h][1])}"
                   f"{' sirene' if r['siren'] else '       '}  +Cr$ {r['loot']:>9,.0f}  caixa Cr$ {money:>11,.0f}"
                   f"  níveis {[x['lvl'] for x in crew]}")
    return {"milestones": milestones, "minutes": round(t / 60, 1), "upgrades": up, "log": log}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--log", action="store_true", help="mostra golpe a golpe")
    parser.add_argument("--sem-habilidades", action="store_true", help="simula sem usar as habilidades ativas")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    result = simulate(load_project(), abilities=not args.sem_habilidades)
    if args.log: print("\n".join(result["log"]))
    print("Golpe concluído em (min de jogo ativo):")
    for name, minute in result["milestones"].items(): print(f"  {name:<34} {minute:5.1f}")
    if len(result["milestones"]) < len(load_project()["heists"]):
        print(f"  (parou em {result['minutes']} min sem concluir o último golpe)")
