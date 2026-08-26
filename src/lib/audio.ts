let ctx: AudioContext | null = null;
let muted = false;

function ac() {
  if (typeof window === "undefined") return null;
  if (!ctx) {
    const C = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!C) return null;
    ctx = new C();
  }
  if (ctx.state === "suspended") void ctx.resume();
  return ctx;
}

export function setMuted(v: boolean) {
  muted = v;
}
export function isMuted() {
  return muted;
}
export function unlockAudio() {
  ac();
}

function tone(freq: number, dur: number, type: OscillatorType, gain = 0.15, delay = 0) {
  const c = ac();
  if (!c || muted) return;
  const t0 = c.currentTime + delay;
  const osc = c.createOscillator();
  const g = c.createGain();
  const filter = c.createBiquadFilter();
  filter.type = "lowpass";
  filter.frequency.value = 6000;
  osc.type = type;
  osc.frequency.setValueAtTime(freq, t0);
  g.gain.setValueAtTime(0.0001, t0);
  g.gain.exponentialRampToValueAtTime(gain, t0 + 0.012);
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
  osc.connect(filter).connect(g).connect(c.destination);
  osc.start(t0);
  osc.stop(t0 + dur + 0.05);
}

function noise(dur: number, gain = 0.2, freq = 1200) {
  const c = ac();
  if (!c || muted) return;
  const len = Math.floor(c.sampleRate * dur);
  const buf = c.createBuffer(1, len, c.sampleRate);
  const data = buf.getChannelData(0);
  for (let i = 0; i < len; i++) data[i] = (Math.random() * 2 - 1) * (1 - i / len) ** 2;
  const src = c.createBufferSource();
  src.buffer = buf;
  const f = c.createBiquadFilter();
  f.type = "bandpass";
  f.frequency.value = freq;
  const g = c.createGain();
  g.gain.value = gain;
  src.connect(f).connect(g).connect(c.destination);
  src.start();
}

// pentatonic-ish scale rising with combo wave
const SCALE = [523.25, 587.33, 659.25, 783.99, 880, 1046.5, 1174.66, 1318.5];

export const sfx = {
  move: () => tone(320, 0.05, "triangle", 0.05),
  drop: () => {
    tone(180, 0.12, "sine", 0.18);
    noise(0.12, 0.08, 500);
  },
  pop: (wave: number, i = 0) => {
    const f = SCALE[Math.min(SCALE.length - 1, wave - 1)] * (1 + i * 0.03);
    tone(f, 0.35, "sine", 0.16, i * 0.045);
    tone(f * 2, 0.18, "triangle", 0.05, i * 0.045);
    noise(0.18, 0.05, 2400);
  },
  crack: () => {
    noise(0.2, 0.12, 900);
    tone(140, 0.2, "sawtooth", 0.06);
  },
  rise: () => {
    tone(90, 0.5, "sawtooth", 0.1);
    noise(0.4, 0.1, 300);
  },
  clear: () => {
    [0, 1, 2, 3, 4, 5].forEach((i) => tone(SCALE[i] * 2, 0.5, "sine", 0.12, i * 0.07));
  },
  over: () => {
    [440, 350, 260, 180].forEach((f, i) => tone(f, 0.5, "sawtooth", 0.12, i * 0.13));
  },
};

export function haptic(pattern: number | number[]) {
  if (typeof navigator !== "undefined" && "vibrate" in navigator) {
    try {
      navigator.vibrate(pattern);
    } catch {
      /* noop */
    }
  }
}
