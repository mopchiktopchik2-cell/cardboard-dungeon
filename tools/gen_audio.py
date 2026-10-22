# -*- coding: utf-8 -*-
"""Процедурный звук для «Картонного подземелья»: SFX и два музыкальных лупа."""
import math
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Audio"
OUT.mkdir(parents=True, exist_ok=True)
SR = 44100


def t_arr(dur):
    return np.arange(int(SR * dur)) / SR


def sine(freq, dur, phase=0.0):
    t = t_arr(dur)
    if np.isscalar(freq):
        return np.sin(2 * np.pi * freq * t + phase)
    freq = np.asarray(freq)
    return np.sin(2 * np.pi * np.cumsum(freq) / SR + phase)


def sine_n(freq, n, phase=0.0):
    t = np.arange(n) / SR
    return np.sin(2 * np.pi * freq * t + phase)


def saw(freq, dur):
    t = t_arr(dur)
    return 2 * ((t * freq) % 1.0) - 1


def noise(dur, seed=1):
    rng = np.random.default_rng(seed)
    return rng.uniform(-1, 1, int(SR * dur))


def env_ad(n, attack=0.005, decay=0.2, power=2.0):
    a = max(1, int(SR * attack))
    d = max(1, n - a)
    e = np.ones(n)
    e[:a] = np.linspace(0, 1, a) ** 0.5
    e[a:] = np.exp(-np.linspace(0, power * 4, d))
    return e


def env_exp(n, rate=8.0):
    return np.exp(-np.linspace(0, rate, n)) * (1 - np.exp(-np.linspace(0, 200, n)))


def lowpass(x, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a * (x[i] - acc)
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def delay(x, time, gain, repeats=3):
    out = x.copy()
    d = int(SR * time)
    for i in range(1, repeats + 1):
        g = gain ** i
        if d * i >= len(x):
            break
        out[d * i:] += x[:-d * i] * g
    return out


def soft(x, drive=1.0):
    x = x * drive
    return np.tanh(x * 1.4) / np.tanh(1.4)


def norm(x, peak=0.8):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


def save(name, x, peak=0.8):
    x = np.asarray(x, dtype=np.float64)
    x = norm(soft(x), peak)
    pcm = (x * 32767).astype(np.int16)
    import wave
    with wave.open(str(OUT / (name + ".wav")), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def click(name="ui_click", f0=1250, f1=760, dur=0.07, vol=0.35):
    n = int(SR * dur)
    freqs = np.linspace(f0, f1, n)
    x = sine(freqs, dur) * env_exp(n, 22) * vol
    x += noise(dur, 3) * env_exp(n, 60) * 0.08
    save(name, x, 0.55)


def thump(dur=0.18, f0=140, f1=55, vol=0.8):
    n = int(SR * dur)
    f = np.linspace(f0, f1, n)
    x = sine(f, dur) * env_exp(n, 18) * vol
    x += lowpass(noise(dur, 5), 900) * env_exp(n, 45) * 0.4
    return x


def sfx():
    click()
    click("ui_hover", 900, 700, 0.05, 0.20)

    # выстрел мага
    dur = 0.14
    n = int(SR * dur)
    x = sine(np.linspace(1100, 420, n), dur) * env_exp(n, 30) * 0.45
    x += highpass(noise(dur, 7), 1800) * env_exp(n, 60) * 0.25
    save("sfx_shoot", x, 0.55)

    # попадание
    x = thump(0.14, 190, 70, 0.7) + highpass(noise(0.14, 9), 900) * env_exp(int(SR * 0.14), 45) * 0.35
    save("sfx_hit", x, 0.7)

    x = thump(0.18, 110, 48, 0.8) + lowpass(noise(0.18, 11), 1400) * env_exp(int(SR * 0.18), 30) * 0.4
    save("sfx_enemy_hit", x, 0.65)

    # взрыв
    dur = 0.75
    n = int(SR * dur)
    nz = noise(dur, 13)
    low = lowpass(nz, 900) * env_exp(n, 7) * 0.9
    sub = sine(np.linspace(90, 35, n), dur) * env_exp(n, 9) * 0.8
    crack = highpass(nz, 3000) * env_exp(n, 40) * 0.25
    save("sfx_explosion", low + sub + crack, 0.95)

    # фаербол
    dur = 0.45
    n = int(SR * dur)
    nz = lowpass(noise(dur, 17), 1600) * env_exp(n, 8) * 0.6
    x = sine(np.linspace(300, 120, n), dur) * env_exp(n, 12) * 0.3 + nz
    save("sfx_fireball", x, 0.75)

    # лёд
    dur = 0.4
    n = int(SR * dur)
    parts = [sine(f, dur) * env_exp(n, 9 + i * 2) for i, f in enumerate((1760, 2217, 2637))]
    save("sfx_ice", sum(parts) / 3 + highpass(noise(dur, 19), 4000) * env_exp(n, 30) * 0.15, 0.55)

    # молния
    dur = 0.35
    n = int(SR * dur)
    x = highpass(noise(dur, 23), 2200) * env_exp(n, 18) * 0.7
    x += sine(np.linspace(900, 180, n), dur) * env_exp(n, 22) * 0.25
    save("sfx_lightning", x, 0.75)

    # лечение
    dur = 0.7
    n = int(SR * dur)
    x = np.zeros(n)
    for i, f in enumerate((523.25, 659.25, 783.99, 1046.5)):
        start = int(SR * i * 0.09)
        seg = sine_n(f, len(x) - start) * env_exp(len(x) - start, 15) * 0.22
        x[start:] += seg
    save("sfx_heal", x, 0.5)

    # щит
    dur = 0.5
    n = int(SR * dur)
    x = sine(np.linspace(180, 640, n), dur) * env_exp(n, 10) * 0.25
    x += highpass(noise(dur, 29), 2200) * env_exp(n, 18) * 0.2
    save("sfx_shield", x, 0.55)

    # рывок
    dur = 0.22
    n = int(SR * dur)
    x = lowpass(noise(dur, 31), 1800) * env_exp(n, 16) * 0.5
    x += sine(np.linspace(300, 120, n), dur) * env_exp(n, 20) * 0.15
    save("sfx_dash", x, 0.5)

    # смерть врага
    dur = 0.55
    n = int(SR * dur)
    x = sine(np.linspace(280, 60, n), dur) * env_exp(n, 8) * 0.5
    x += lowpass(noise(dur, 37), 700) * env_exp(n, 14) * 0.25
    save("sfx_death", x, 0.6)

    # спавн
    x = thump(0.4, 100, 38, 0.7) + lowpass(noise(0.4, 41), 600) * env_exp(int(SR * 0.4), 10) * 0.4
    save("sfx_spawn", x, 0.8)

    # дверь
    dur = 0.9
    n = int(SR * dur)
    creak = lowpass(noise(dur, 43), 380) * env_exp(n, 5) * 0.6
    creak *= (0.6 + 0.4 * np.sin(2 * np.pi * 9 * t_arr(dur)))
    knock = np.zeros(n)
    k = thump(0.2, 120, 50, 0.7)
    knock[:len(k)] += k
    save("sfx_door", creak + knock, 0.7)

    # подбор/перк
    dur = 0.5
    n = int(SR * dur)
    x = np.zeros(n)
    for i, f in enumerate((659.25, 880, 1318.5)):
        start = int(SR * i * 0.07)
        x[start:] += sine(f, dur - i * 0.07) * env_exp(len(x) - start, 12) * 0.22
    save("sfx_pickup", x, 0.5)

    # волна
    dur = 0.9
    n = int(SR * dur)
    x = (sine(110, dur) + 0.5 * sine(165, dur) + 0.3 * sine(220, dur)) * env_exp(n, 4) * 0.3
    save("sfx_wave", x, 0.65)

    # рёв босса
    dur = 1.3
    n = int(SR * dur)
    f = np.linspace(70, 42, n) * (1 + 0.06 * np.sin(2 * np.pi * 7 * t_arr(dur)))
    x = saw(1, dur) * 0
    ph = np.cumsum(f) / SR
    x = (np.sin(2 * np.pi * ph) + 0.4 * np.sin(4 * np.pi * ph) + 0.2 * np.sin(6 * np.pi * ph))
    x *= env_exp(n, 2.6) * 0.6
    x += lowpass(noise(dur, 47), 500) * env_exp(n, 4) * 0.35
    save("sfx_boss_roar", x, 0.85)

    # взрыв бомбера
    x = thump(0.6, 80, 28, 1.0) + lowpass(noise(0.6, 53), 700) * env_exp(int(SR * 0.6), 6) * 0.8
    save("sfx_bomb", x, 1.0)

    # яд
    dur = 0.45
    n = int(SR * dur)
    x = lowpass(noise(dur, 59), 500) * env_exp(n, 8) * 0.4
    for i in range(5):
        st = int(SR * (0.03 + i * 0.07))
        f = 200 + i * 45
        seg = sine(f, 0.08) * env_exp(int(SR * 0.08), 20) * 0.15
        if st + len(seg) < n:
            x[st:st + len(seg)] += seg
    save("sfx_poison", x, 0.55)

    # победа
    dur = 2.4
    n = int(SR * dur)
    x = np.zeros(n)
    notes = (392, 493.88, 587.33, 783.99, 987.77)
    for i, f in enumerate(notes):
        st = int(SR * i * 0.16)
        n2 = len(x) - st
        seg = (sine_n(f, n2) * 0.6 + sine_n(f * 2, n2) * 0.2)
        x[st:] += seg * env_exp(n2, 2.6) * 0.2
    save("sfx_victory", x, 0.7)

    # поражение
    dur = 2.2
    n = int(SR * dur)
    x = np.zeros(n)
    for i, f in enumerate((293.66, 261.63, 220, 146.83)):
        st = int(SR * i * 0.35)
        n2 = len(x) - st
        x[st:] += sine_n(f, n2) * env_exp(n2, 3) * 0.28
    save("sfx_defeat", x, 0.65)

    # шаг
    dur = 0.09
    x = lowpass(noise(dur, 61), 500) * env_exp(int(SR * dur), 40) * 0.3
    save("sfx_step", x, 0.3)

    # каст
    dur = 0.35
    n = int(SR * dur)
    x = sine(np.linspace(220, 880, n), dur) * env_exp(n, 9) * 0.3
    x += highpass(noise(dur, 67), 1500) * env_exp(n, 20) * 0.15
    save("sfx_cast", x, 0.55)


def pluck(freq, dur=1.2, amp=0.2):
    n = int(SR * dur)
    x = (sine(freq, dur) + 0.35 * sine(freq * 2, dur) + 0.15 * sine(freq * 3, dur))
    x *= env_exp(n, 5.5) * amp
    x += highpass(noise(dur, int(freq) % 1000 + 3), 3000) * env_exp(n, 90) * amp * 0.2
    return x


def music_dungeon():
    bpm = 88
    beat = 60 / bpm
    bars = 16
    dur = bars * 4 * beat
    n = int(SR * dur)
    x = np.zeros(n)

    roots = (73.42, 58.27, 87.31, 55.0)  # Dm, Bb, F, A
    for bar in range(bars):
        root = roots[(bar // 4) % 4]
        st = int(SR * bar * 4 * beat)
        seg_n = int(SR * 4 * beat)
        tt = np.arange(seg_n) / SR
        pad = (np.sin(2 * np.pi * root * tt) * 0.35 +
               np.sin(2 * np.pi * root * 1.5 * tt) * 0.22 +
               np.sin(2 * np.pi * root * 2 * tt) * 0.12)
        pad *= (0.6 + 0.4 * np.sin(2 * np.pi * 0.07 * tt + bar * 0.3))
        x[st:st + seg_n] += pad * 0.22

        # низкий удар на 1 и 3
        for b in (0, 2):
            k = thump(0.35, 85, 38, 0.5)
            kst = st + int(SR * b * beat)
            x[kst:kst + len(k)] += k * 0.24
        # шейкер
        for b in range(8):
            st2 = st + int(SR * b * beat / 2)
            sh = highpass(noise(0.05, 71 + b), 5000) * env_exp(int(SR * 0.05), 30) * 0.08
            x[st2:st2 + len(sh)] += sh

    # мелодия
    scale = (293.66, 349.23, 392.0, 440.0, 523.25, 587.33, 698.46)
    rng = np.random.default_rng(7)
    for bar in range(bars):
        for b in range(4):
            if rng.random() < 0.45:
                continue
            f = scale[int(rng.integers(0, len(scale)))]
            p = pluck(f, 1.4, 0.22)
            st = int(SR * (bar * 4 + b) * beat)
            if st + len(p) < n:
                x[st:st + len(p)] += p
    # эхо
    wet = delay(x, 0.37, 0.25, 3)
    x = x * 0.75 + wet * 0.3
    # шов лупа
    fade = int(SR * 0.02)
    x[:fade] *= np.linspace(0, 1, fade)
    x[-fade:] *= np.linspace(1, 0, fade)
    save("music_dungeon", x, 0.55)


def music_boss():
    bpm = 112
    beat = 60 / bpm
    bars = 16
    dur = bars * 4 * beat
    n = int(SR * dur)
    x = np.zeros(n)
    roots = (73.42, 69.3, 73.42, 92.5)
    for bar in range(bars):
        root = roots[(bar // 4) % 4]
        st = int(SR * bar * 4 * beat)
        seg_n = int(SR * 4 * beat)
        tt = np.arange(seg_n) / SR
        pad = (np.sin(2 * np.pi * root * tt) * 0.4 +
               np.sin(2 * np.pi * root * 1.41 * tt) * 0.2 +
               np.sin(2 * np.pi * root * 2 * tt) * 0.12)
        pad *= (0.7 + 0.3 * np.sin(2 * np.pi * 0.25 * tt))
        x[st:st + seg_n] += pad * 0.2
        for b in range(4):
            k = thump(0.3, 110, 42, 0.8)
            kst = st + int(SR * b * beat)
            x[kst:kst + len(k)] += k * (0.3 if b % 2 == 0 else 0.16)
        for b in range(8):
            st2 = st + int(SR * b * beat / 2)
            sh = highpass(noise(0.04, 101 + b), 6000) * env_exp(int(SR * 0.04), 40) * 0.08
            x[st2:st2 + len(sh)] += sh
    scale = (146.83, 174.61, 185.0, 220.0, 277.18, 293.66)
    rng = np.random.default_rng(17)
    for bar in range(bars):
        for b in range(8):
            if rng.random() < 0.62:
                continue
            f = scale[int(rng.integers(0, len(scale)))]
            p = pluck(f, 0.8, 0.18)
            st = int(SR * (bar * 4 + b / 2) * beat)
            if st + len(p) < n:
                x[st:st + len(p)] += p
    wet = delay(x, 0.27, 0.22, 3)
    x = x * 0.78 + wet * 0.25
    fade = int(SR * 0.03)
    x[:fade] *= np.linspace(0, 1, fade)
    x[-fade:] *= np.linspace(1, 0, fade)
    save("music_boss", x, 0.6)


if __name__ == "__main__":
    sfx()
    music_dungeon()
    music_boss()
    print("audio:", len(list(OUT.glob("*.wav"))), "files")
    for f in sorted(OUT.glob("*.wav")):
        print(f.name, round(f.stat().st_size / 1024), "KB")
