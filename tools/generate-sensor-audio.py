"""Bake the existing sensor tones for browser sample playback; no new audio dependencies."""
import math
from pathlib import Path
import struct
import wave

destination = Path(__file__).resolve().parents[1] / 'assets' / 'audio'
destination.mkdir(parents=True, exist_ok=True)
rate = 22050
for side, frequency in [('front', 650), ('rear', 500)]:
    for kind, duration in [('pulse', .075), ('loop', .20)]:
        count = int(rate * duration)
        samples = []
        for i in range(count):
            envelope = 1 if kind == 'loop' else min(1, i / (rate * .012), (count - 1 - i) / (rate * .018))
            samples.append(int(math.sin(i * 2 * math.pi * frequency / rate) * 4200 * envelope))
        with wave.open(str(destination / f'sensor-{side}-{kind}.wav'), 'wb') as output:
            output.setnchannels(1)
            output.setsampwidth(2)
            output.setframerate(rate)
            output.writeframes(struct.pack(f'<{count}h', *samples))
