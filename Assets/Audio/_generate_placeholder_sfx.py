"""
Generate placeholder SFX for Summit VR (ClimbAudio events).

Procedural, royalty-free, tiny WAVs so the audio chain is audible in-editor
immediately. Swap these for recorded/sourced clips later — keep the filenames
and ClimbAudio wiring stays the same.

Events: grab / slip / fall / summit  (see Assets/Scripts/Util/ClimbAudio.cs)
"""
import numpy as np
import wave
import os

SR = 44100
OUT = os.path.dirname(os.path.abspath(__file__))


def _write(name, samples):
    samples = np.clip(samples, -1.0, 1.0)
    pcm = (samples * 32767).astype(np.int16)
    path = os.path.join(OUT, name)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"wrote {name}  ({len(samples)/SR:.2f}s, {pcm.nbytes} bytes)")


def _env(n, attack=0.005, release=0.1):
    """Simple attack/decay envelope over n samples."""
    a = int(attack * SR)
    r = int(release * SR)
    e = np.ones(n)
    if a > 0:
        e[:a] = np.linspace(0, 1, a)
    if r > 0:
        e[-r:] = np.linspace(1, 0, r)
    return e


def t(dur):
    return np.linspace(0, dur, int(dur * SR), endpoint=False)


def grab():
    # Chalky, percussive grip: short filtered-noise thud + low body.
    dur = 0.14
    x = t(dur)
    noise = np.random.uniform(-1, 1, len(x))
    # one-pole low-pass to dull the noise
    lp = np.zeros_like(noise)
    a = 0.25
    for i in range(1, len(noise)):
        lp[i] = a * noise[i] + (1 - a) * lp[i - 1]
    body = 0.6 * np.sin(2 * np.pi * 90 * x)
    sig = (0.7 * lp + body) * _env(len(x), attack=0.002, release=0.10)
    _write("grab.wav", 0.9 * sig)


def slip():
    # Tense warning: wobbling, slightly dissonant rising tone.
    dur = 0.35
    x = t(dur)
    f = np.linspace(330, 470, len(x))            # rising pitch = rising tension
    vib = 1 + 0.04 * np.sin(2 * np.pi * 11 * x)  # nervous vibrato
    tone = np.sin(2 * np.pi * np.cumsum(f * vib) / SR)
    detune = 0.4 * np.sin(2 * np.pi * np.cumsum((f * 1.03) * vib) / SR)
    sig = (tone + detune) * _env(len(x), attack=0.01, release=0.12)
    _write("slip.wav", 0.5 * sig)


def fall():
    # Downward whoosh: descending pitch sweep + airy noise tail.
    dur = 0.7
    x = t(dur)
    f = np.linspace(420, 70, len(x))             # falling pitch
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR)
    noise = np.random.uniform(-1, 1, len(x))
    lp = np.zeros_like(noise)
    a = 0.05
    for i in range(1, len(noise)):
        lp[i] = a * noise[i] + (1 - a) * lp[i - 1]
    sig = (0.7 * tone + 0.8 * lp) * _env(len(x), attack=0.005, release=0.4)
    _write("fall.wav", 0.7 * sig)


def summit():
    # Reward chime: major triad bell arpeggio (C-E-G-C).
    freqs = [523.25, 659.25, 783.99, 1046.50]
    dur = 1.3
    x = t(dur)
    sig = np.zeros(len(x))
    step = 0.10
    for i, fr in enumerate(freqs):
        start = int(i * step * SR)
        seg = x[start:] - x[start]
        partial = (np.sin(2 * np.pi * fr * seg)
                   + 0.3 * np.sin(2 * np.pi * 2 * fr * seg))
        decay = np.exp(-3.0 * seg)
        chunk = partial * decay
        sig[start:start + len(chunk)] += chunk[: len(sig) - start]
    sig /= np.max(np.abs(sig))
    _write("summit.wav", 0.8 * sig)


if __name__ == "__main__":
    np.random.seed(7)  # deterministic output
    grab()
    slip()
    fall()
    summit()
    print("done")
