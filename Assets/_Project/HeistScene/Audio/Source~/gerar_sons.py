"""Sintetiza os efeitos sonoros da Cena do Golpe e do Gazua, sem sample de terceiro (só matemática).

Uso (da raiz do repositório):  python "Assets/_Project/HeistScene/Audio/Source~/gerar_sons.py"
Só usa a biblioteca padrão do Python. WAV mono 16 bits, 22.050 Hz.

Saída:
  HeistScene/Audio/SFX_Grana.wav        notas caindo na sacola (farfalhar de papel + moeda)
  HeistScene/Audio/SFX_Porta.wav        porta abrindo (rangido curto + batida)
  Thieves/Gazua/Audio/SFX_Gazua_Fechadura.wav   gazua trabalhando (cliques de pino + estalo da tranca)
  Thieves/Gazua/Audio/SFX_PeDeCabra.wav         pé-de-cabra (madeira rachando + metal batendo)
"""
import math
import random
import struct
import wave
from pathlib import Path

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
RATE = 22050
rng = random.Random(171)


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def mix(base, other, at=0.0, gain=1.0):
    start = int(at * RATE)
    need = start + len(other)
    if need > len(base):
        base.extend([0.0] * (need - len(base)))
    for i, v in enumerate(other):
        base[start + i] += v * gain
    return base


def tone(freqs, seconds, decay=8.0, attack=0.002):
    out = []
    n = int(seconds * RATE)
    for i in range(n):
        t = i / RATE
        env = min(1.0, t / attack) * math.exp(-decay * t)
        out.append(env * sum(math.sin(2 * math.pi * f * t) * a for f, a in freqs))
    return out


def noise(seconds, decay=20.0, lowpass=0.5, attack=0.001):
    out, prev = [], 0.0
    n = int(seconds * RATE)
    for i in range(n):
        t = i / RATE
        env = min(1.0, t / attack) * math.exp(-decay * t)
        prev = prev + lowpass * (rng.uniform(-1, 1) - prev)    # passa-baixa simples
        out.append(env * prev)
    return out


def creak(seconds, f0=95.0, f1=60.0, rough=0.35):
    """Rangido: dente-de-serra com o tom descendo e falhando (atrito)."""
    out, phase = [], 0.0
    n = int(seconds * RATE)
    for i in range(n):
        t = i / RATE
        k = t / seconds
        f = f0 + (f1 - f0) * k + 12 * math.sin(2 * math.pi * 23 * t)
        phase += f / RATE
        saw = 2 * (phase % 1.0) - 1
        stick = 1.0 if rng.random() > rough * 0.1 else 0.0
        env = math.sin(math.pi * min(1.0, k)) ** 0.6
        out.append(saw * env * stick * 0.5)
    return out


def save(samples, path, gain=0.9):
    peak = max(1e-6, max(abs(v) for v in samples))
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        frames = b"".join(struct.pack("<h", int(max(-1, min(1, v / peak * gain)) * 32767)) for v in samples)
        w.writeframes(frames)
    print("ok:", path.relative_to(PROJECT.parent.parent), f"{len(samples) / RATE:.2f}s")


def click(f=4200.0, length=0.03):
    return mix(tone([(f, 0.6), (f * 1.53, 0.3), (f * 2.4, 0.15)], length, decay=180.0), noise(length, decay=260.0, lowpass=0.9), 0, 0.5)


def gazua():
    s = silence(0.85)
    for at, f in ((0.02, 4300), (0.16, 3900), (0.27, 4600), (0.41, 4100), (0.5, 4400)):
        mix(s, click(f), at, 0.7)
    # estalo da tranca virando: mais grave e encorpado
    mix(s, tone([(950, 0.6), (1900, 0.25), (430, 0.4)], 0.18, decay=35.0), 0.64)
    mix(s, noise(0.08, decay=60.0, lowpass=0.6), 0.64, 0.6)
    return s


def pe_de_cabra():
    s = silence(1.1)
    mix(s, creak(0.32, 130, 70), 0.0, 0.8)                                         # madeira forçando
    mix(s, noise(0.25, decay=18.0, lowpass=0.75), 0.30, 1.0)                        # crec! rachando
    mix(s, tone([(520, 0.5), (1340, 0.35), (2210, 0.2), (3020, 0.12)], 0.75, decay=6.0), 0.32, 0.8)  # metal batendo
    mix(s, tone([(70, 0.8)], 0.25, decay=14.0), 0.31, 0.9)                          # tranco
    return s


def grana():
    s = silence(0.6)
    for k in range(3):
        mix(s, noise(0.09, decay=25.0, lowpass=0.85), k * 0.06, 0.35)             # notas farfalhando
    mix(s, tone([(1760, 0.5), (2640, 0.3), (3520, 0.2)], 0.45, decay=9.0), 0.12, 0.6)   # plim de moeda
    mix(s, tone([(2093, 0.5), (3140, 0.25)], 0.35, decay=11.0), 0.2, 0.4)
    return s


def porta():
    s = silence(0.45)
    mix(s, creak(0.28, 180, 140, rough=0.6), 0.0, 0.5)
    mix(s, tone([(85, 0.8), (170, 0.2)], 0.15, decay=25.0), 0.27, 0.7)
    mix(s, noise(0.06, decay=60.0, lowpass=0.4), 0.27, 0.4)
    return s


def main():
    save(grana(), HERE.parent / "SFX_Grana.wav", 0.7)
    save(porta(), HERE.parent / "SFX_Porta.wav", 0.6)
    gazua_dir = PROJECT / "Thieves/Gazua/Audio"
    save(gazua(), gazua_dir / "SFX_Gazua_Fechadura.wav", 0.8)
    save(pe_de_cabra(), gazua_dir / "SFX_PeDeCabra.wav", 0.9)


if __name__ == "__main__":
    main()
