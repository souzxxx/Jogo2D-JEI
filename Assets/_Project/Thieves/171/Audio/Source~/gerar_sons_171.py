"""Sintetiza os sons do 171, sem sample de terceiro (só matemática), como os outros ladrões.

Uso (da raiz do repositório):  python3 -B "Assets/_Project/Thieves/171/Audio/Source~/gerar_sons_171.py"
Só usa a biblioteca padrão. Reaproveita as funções do gerador da Cena do Golpe (HeistScene/Audio/Source~).
Saída em Thieves/171/Audio:
- SFX_171_Conversa: o som de ação (toca quando ele começa a trabalhar): conversa mole abafada, sílaba atrás de sílaba.
- SFX_Labia: a Lábia. Três "blá" de voz macia descendo e o plim de quem convenceu (o alerta caindo).
"""
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
OUT = HERE.parent
sys.path.insert(0, str(HERE.parents[3] / "HeistScene/Audio/Source~"))

from gerar_sons import RATE, mix, noise, rng, save, silence, tone  # noqa: E402

# Formantes de um "a" aberto: o que faz a onda soar como voz e não como apito.
VOWEL_A = ((750.0, 260.0, 1.0), (1250.0, 380.0, 0.55), (2600.0, 500.0, 0.2))


def syllable(f0, seconds, glide=0.0, vibrato=0.04):
    """Uma sílaba de voz: harmônicos de f0 pesados pelos formantes do "a", com um tremidinho e a pontinha suave."""
    out, phase = [], 0.0
    n = int(seconds * RATE)
    harmonics = []
    for h in range(1, 18):
        amp = sum(g * math.exp(-((h * f0 - fc) / bw) ** 2) for fc, bw, g in VOWEL_A)
        if amp > 0.01:
            harmonics.append((h, amp))
    for i in range(n):
        k = i / n
        t = i / RATE
        f = f0 * (1 + glide * k) * (1 + vibrato * math.sin(2 * math.pi * 5.5 * t))
        phase += f / RATE
        v = sum(a * math.sin(2 * math.pi * h * phase) for h, a in harmonics)
        out.append(v * min(1.0, k * 14, (1 - k) * 6))
    return out


def conversa():
    """Conversa mole abafada: sílabas curtas subindo e descendo, como quem enrola o vigia."""
    s = silence(0.95)
    at = 0.0
    for f0, length in ((175, 0.11), (205, 0.09), (160, 0.14), (190, 0.08), (220, 0.12), (170, 0.16)):
        mix(s, syllable(f0 * (0.97 + 0.06 * rng.random()), length, glide=-0.08), at, 0.7)
        at += length + 0.03 + 0.03 * rng.random()
    mix(s, noise(0.95, decay=1.0, lowpass=0.08), 0.0, 0.05)  # o ar da sala
    return s


def labia():
    """Três "blá" macios descendo (conversa de quem já ganhou) e o plim descendo: alerta caiu."""
    s = silence(1.15)
    for i, (f0, at) in enumerate(((210, 0.0), (190, 0.2), (165, 0.4))):
        mix(s, syllable(f0, 0.17 if i < 2 else 0.26, glide=-0.12, vibrato=0.05), at, 0.85)
    mix(s, tone([(1568, 0.5), (3136, 0.15)], 0.4, decay=7.0), 0.66, 0.5)   # sol
    mix(s, tone([(1175, 0.5), (2350, 0.15)], 0.45, decay=6.0), 0.78, 0.5)  # ré: o alerta descendo
    return s


def main():
    save(conversa(), OUT / "SFX_171_Conversa.wav", 0.7)
    save(labia(), OUT / "SFX_Labia.wav", 0.8)


if __name__ == "__main__":
    main()
