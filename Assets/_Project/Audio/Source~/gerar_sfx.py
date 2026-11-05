"""Sintetiza os efeitos sonoros da interface e do golpe, sem sample de terceiro (só matemática).

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Audio/Source~/gerar_sfx.py"
Só usa a biblioteca padrão. Reaproveita as funções do gerador da Cena do Golpe (HeistScene/Audio/Source~).
Saída em Audio/Resources/Sons: UI_* (o UiSons da UI aponta pra eles) e os do golpe, que o AudioService toca
sozinho pelos GameEvents (Passos, Alarme, Sirene, Viatura, e Kombi, Cofre e Algemas da abertura). Porta e Grana já
são da Cena do Golpe.
E os do Disquete em Thieves/Disquete/Audio: o som de ação (teclado e drive) e o do Loop na câmera (fita rebobinando).
"""
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT = HERE.parent / "Resources/Sons"
DISQUETE = HERE.parents[1] / "Thieves/Disquete/Audio"
sys.path.insert(0, str(HERE.parents[1] / "HeistScene/Audio/Source~"))

from gerar_sons import RATE, click, creak, mix, noise, rng, save, silence, tone  # noqa: E402


def square(f, seconds, decay=0.0, harmonics=(1, 3, 5, 7)):
    """Onda quadrada (só harmônicos ímpares): som de aparelho, bip de pager e buzina de erro."""
    return tone([(f * h, 1.0 / h) for h in harmonics], seconds, decay=decay)


def sweep(f0, f1, seconds, shape=lambda k: k):
    """Tom que desliza de f0 a f1 (sirene, viatura), com as pontas suaves."""
    out, phase = [], 0.0
    n = int(seconds * RATE)
    for i in range(n):
        k = i / n
        phase += (f0 + (f1 - f0) * shape(k)) / RATE
        tri = 4 * abs(phase % 1.0 - 0.5) - 1            # triângulo: mais encorpado que seno
        out.append(tri * min(1.0, k * 20, (1 - k) * 20))
    return out


# ---------------------------------------------------------------- interface

def ui_click():
    return click(2600.0, 0.035)


def ui_hover():
    return tone([(5200, 0.6), (7800, 0.2)], 0.02, decay=260.0)


def ui_buy():
    """Caixa registradora: a gaveta batendo e o plim."""
    s = silence(0.55)
    mix(s, click(3400.0, 0.03), 0.0, 0.8)
    mix(s, noise(0.05, decay=70.0, lowpass=0.5), 0.02, 0.5)
    mix(s, tone([(2093, 0.5), (4186, 0.2)], 0.5, decay=8.0), 0.06, 0.7)
    mix(s, tone([(2637, 0.5), (5274, 0.2)], 0.45, decay=8.0), 0.13, 0.7)
    return s


def ui_error():
    s = silence(0.24)
    mix(s, square(150, 0.09, decay=6.0), 0.0, 0.6)
    mix(s, square(118, 0.11, decay=6.0), 0.12, 0.6)
    return s


def ui_bip():
    """Pager: dois bipes agudos."""
    s = silence(0.3)
    for at in (0.0, 0.14):
        mix(s, square(2700, 0.09, harmonics=(1, 3)), at, 0.6)
    return s


def ui_typewriter():
    """Máquina de escrever: teclas em ritmo irregular e o sininho do fim da linha."""
    s = silence(1.0)
    at = 0.0
    for _ in range(7):
        mix(s, click(rng.uniform(1300, 1900), 0.04), at, 0.8)
        mix(s, tone([(110, 0.6)], 0.05, decay=60.0), at, 0.5)          # a haste batendo no rolo
        at += rng.uniform(0.07, 0.12)
    mix(s, tone([(3520, 0.5), (5280, 0.25)], 0.35, decay=10.0), at + 0.05, 0.5)
    return s


def ui_stamp():
    """Carimbo: pancada surda no papel."""
    s = silence(0.25)
    mix(s, tone([(85, 0.9), (140, 0.3)], 0.2, decay=22.0), 0.0, 1.0)
    mix(s, noise(0.08, decay=50.0, lowpass=0.35), 0.0, 0.7)
    return s


def ui_paper():
    """Papel da pasta deslizando."""
    s = noise(0.28, decay=9.0, lowpass=0.6, attack=0.06)
    return mix(s, noise(0.12, decay=30.0, lowpass=0.85), 0.12, 0.4)


# ---------------------------------------------------------------- golpe

def passos():
    """Quatro passos de tênis (a crew andando pra próxima sala)."""
    s = silence(0.9)
    for k in range(4):
        at = k * 0.2
        mix(s, tone([(70 + 12 * (k % 2), 0.8)], 0.08, decay=45.0), at, 0.9)
        mix(s, noise(0.05, decay=60.0, lowpass=0.3), at + 0.005, 0.5)
    return s


def alarme():
    """Campainha elétrica de alarme: o martelinho batendo no sino 18 vezes por segundo."""
    seconds, rate = 1.6, 18.0
    out = []
    for i in range(int(seconds * RATE)):
        t = i / RATE
        hit = math.exp(-(t * rate % 1.0) * 6)                         # cada batida decai até a próxima
        bell = sum(math.sin(2 * math.pi * f * t) * a for f, a in ((1180, 0.6), (2950, 0.3), (4400, 0.15)))
        out.append(bell * hit * min(1.0, (seconds - t) * 6))
    return out


def sirene():
    """Sirene da polícia subindo e descendo duas vezes."""
    s = silence(2.5)
    for k in range(2):
        mix(s, sweep(620, 1250, 1.2, lambda x: math.sin(math.pi * x)), k * 1.2, 0.7)
    return s


def viatura():
    """Viatura chegando: duas sirenadas curtas, freada e porta batendo."""
    s = silence(1.4)
    for at in (0.0, 0.35):
        mix(s, sweep(500, 1300, 0.3, lambda x: x ** 0.5), at, 0.6)
    mix(s, noise(0.35, decay=6.0, lowpass=0.8, attack=0.03), 0.65, 0.35)  # pneu
    mix(s, tone([(75, 0.9), (150, 0.3)], 0.2, decay=25.0), 1.1, 1.0)     # porta
    mix(s, noise(0.06, decay=60.0, lowpass=0.4), 1.1, 0.5)
    return s


# ---------------------------------------------------------------- Disquete

def disquete_teclado():
    """Digitando rápido e o drive de disquete lendo (o motorzinho de passo zumbindo)."""
    s = silence(1.0)
    at = 0.0
    for _ in range(9):
        mix(s, click(rng.uniform(2200, 3000), 0.03), at, 0.7)
        at += rng.uniform(0.04, 0.07)
    for k in range(5):                                                  # cabeça do drive andando
        mix(s, square(55 + 10 * (k % 2), 0.05, decay=30.0), at + 0.08 + k * 0.06, 0.5)
    return s


def loop_na_camera():
    """Videocassete: a fita rebobinando, o clunk do play e o bipe do REC."""
    s = silence(1.1)
    mix(s, sweep(180, 900, 0.6, lambda k: k * k), 0.0, 0.35)          # rebobinando
    mix(s, noise(0.6, decay=2.0, lowpass=0.7, attack=0.05), 0.0, 0.25)
    mix(s, tone([(90, 0.8), (180, 0.3)], 0.15, decay=30.0), 0.62, 0.9)  # clunk
    mix(s, noise(0.05, decay=60.0, lowpass=0.5), 0.62, 0.5)
    mix(s, square(1800, 0.12, harmonics=(1, 3)), 0.8, 0.35)            # bipe do REC
    return s


# ---------------------------------------------------------------- abertura

def kombi():
    """Kombi a ar chegando: o motor pipocando cada vez mais devagar, o freio chiando e o motor morrendo."""
    s = silence(2.0)
    at, gap = 0.0, 0.05
    while at < 1.5:
        mix(s, tone([(55, 0.8), (110, 0.4), (165, 0.15)], 0.07, decay=40.0), at, 0.8)
        mix(s, noise(0.04, decay=70.0, lowpass=0.35), at, 0.35)
        at += gap
        gap *= 1.04
    mix(s, sweep(1900, 1600, 0.4, lambda k: k), 0.9, 0.12)                # lona do freio
    mix(s, noise(0.4, decay=5.0, lowpass=0.85, attack=0.03), 0.9, 0.2)
    return s


def cofre():
    """Porta do cofre: o volante girando (catraca), a tranca soltando com um tum e a porta pesada rangendo."""
    s = silence(2.0)
    for k in range(6):
        mix(s, click(rng.uniform(900, 1200), 0.04), k * 0.09, 0.6)
    mix(s, tone([(60, 1.0), (120, 0.5), (240, 0.2)], 0.4, decay=10.0), 0.6, 1.0)
    mix(s, noise(0.08, decay=40.0, lowpass=0.5), 0.6, 0.6)
    mix(s, creak(1.0, 80.0, 50.0), 0.75, 0.8)
    return s


def algemas():
    """Duas algemas fechando: a catraca da argola (cliques metálicos rápidos) e a argola batendo."""
    s = silence(0.9)
    for at in (0.0, 0.42):
        for j in range(5):
            mix(s, click(rng.uniform(3000, 3800), 0.025), at + j * 0.035, 0.7)
        mix(s, tone([(2100, 0.4), (3300, 0.25)], 0.12, decay=35.0), at + 0.18, 0.6)
    return s


def main():
    for name, make, gain in (("UI_Click", ui_click, 0.8), ("UI_Hover", ui_hover, 0.6), ("UI_Buy", ui_buy, 0.8),
                             ("UI_Error", ui_error, 0.4), ("UI_Bip", ui_bip, 0.35), ("UI_Typewriter", ui_typewriter, 0.8),
                             ("UI_Stamp", ui_stamp, 0.9), ("UI_Paper", ui_paper, 0.6), ("Passos", passos, 0.8),
                             ("Alarme", alarme, 0.7), ("Sirene", sirene, 0.35), ("Viatura", viatura, 0.8)):
        save(make(), OUT / f"{name}.wav", gain)
    save(disquete_teclado(), DISQUETE / "SFX_Disquete_Teclado.wav", 0.7)
    save(loop_na_camera(), DISQUETE / "SFX_LoopNaCamera.wav", 0.8)
    for name, make, gain in (("Kombi", kombi, 0.8), ("Cofre", cofre, 0.8), ("Algemas", algemas, 0.7)):
        save(make(), OUT / f"{name}.wav", gain)


if __name__ == "__main__":
    main()
