import { createFileRoute } from "@tanstack/react-router";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  SIZE,
  type Ball,
  applyGravity,
  damagedNeighbours,
  findMatches,
  landingRow,
  makeBall,
  nextId,
  randNum,
  rollPiece,
  runLengths,
  scoreFor,
  toGrid,
} from "@/lib/game";
import { haptic, isMuted, setMuted, sfx, unlockAudio } from "@/lib/audio";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "Neon Seven — головоломка 7×7 с неоновыми шарами" },
      {
        name: "description",
        content:
          "Neon Seven: гибрид тетриса и математической головоломки. Бросай светящиеся шары, взрывай линии длиной ровно N и собирай каскадные комбо.",
      },
      { property: "og:title", content: "Neon Seven — головоломка 7×7 с неоновыми шарами" },
      {
        property: "og:description",
        content: "Взрывай линии длиной ровно N, разбивай обсидиан и собирай каскадные комбо.",
      },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary_large_image" },
    ],
  }),
  component: Game,
});

const wait = (ms: number) => new Promise((r) => setTimeout(r, ms));
const COLOR = ["--n1", "--n2", "--n3", "--n4", "--n5", "--n6", "--n7"];
const RISE_EVERY = 5;

type Fx = { id: number; x: number; y: number; kind: "ring" | "spark"; color: string; dx: number; dy: number };
type Float = { id: number; x: number; y: number; text: string; color: string };
type Mode = "classic" | "zen";

function startBalls(): Ball[] {
  const out: Ball[] = [];
  for (let c = 0; c < SIZE; c++) {
    const h = 1 + Math.floor(Math.random() * 2);
    for (let i = 0; i < h; i++) out.push(makeBall(c, SIZE - 1 - i, Math.random() < 0.25 ? null : randNum()));
  }
  return out;
}

function Game() {
  const [balls, setBalls] = useState<Ball[]>([]);
  const [current, setCurrent] = useState<number | null>(null);
  const [next, setNext] = useState<number | null>(null);

  const [aim, setAim] = useState(3);
  const [score, setScore] = useState(0);
  const [best, setBest] = useState(0);
  const [movesLeft, setMovesLeft] = useState(RISE_EVERY);
  const [busy, setBusy] = useState(false);
  const [over, setOver] = useState(false);
  const [mode, setMode] = useState<Mode>("classic");
  const [muted, setMutedState] = useState(false);
  const [fx, setFx] = useState<Fx[]>([]);
  const [floats, setFloats] = useState<Float[]>([]);
  const [shake, setShake] = useState(0);
  const [banner, setBanner] = useState<string | null>(null);
  const [squash, setSquash] = useState<number | null>(null);
  const boardRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const b = Number(localStorage.getItem("neon7-best") ?? 0);
    if (b) setBest(b);
  }, []);
  useEffect(() => {
    if (score > best) {
      setBest(score);
      localStorage.setItem("neon7-best", String(score));
    }
  }, [score, best]);

  const cell = 100 / SIZE;
  const pos = (v: number) => `${v * cell}%`;

  const addFx = (col: number, row: number, color: string) => {
    const base = nextId() * 100;
    const items: Fx[] = [
      { id: base, x: col, y: row, kind: "ring", color, dx: 0, dy: 0 },
      ...Array.from({ length: 7 }, (_, i) => {
        const a = (i / 7) * Math.PI * 2 + Math.random();
        const d = 40 + Math.random() * 70;
        return {
          id: base + i + 1,
          x: col,
          y: row,
          kind: "spark" as const,
          color,
          dx: Math.cos(a) * d,
          dy: Math.sin(a) * d,
        };
      }),
    ];
    setFx((f) => [...f, ...items]);
    setTimeout(() => setFx((f) => f.filter((i) => i.id < base || i.id > base + 8)), 800);
  };

  const addFloat = (col: number, row: number, text: string, color: string) => {
    const id = nextId();
    setFloats((f) => [...f, { id, x: col, y: row, text, color }]);
    setTimeout(() => setFloats((f) => f.filter((i) => i.id !== id)), 950);
  };

  const kick = (level: number) => {
    setShake(level);
    setTimeout(() => setShake(0), 450);
  };

  const showBanner = (t: string) => {
    setBanner(t);
    setTimeout(() => setBanner(null), 1500);
  };

  const resolve = useCallback(async (input: Ball[]): Promise<Ball[]> => {
    let arr = input;
    let wave = 1;
    for (;;) {
      const matches = findMatches(arr);
      if (matches.length === 0) break;
      const hidden = damagedNeighbours(arr, matches);
      const gained = scoreFor(matches.length, wave);

      matches.forEach((m, i) => {
        sfx.pop(wave, i);
        addFx(m.col, m.row, `var(${COLOR[(m.num ?? 1) - 1]})`);
      });
      if (hidden.length) sfx.crack();
      const anchor = matches[0]!;
      addFloat(
        anchor.col,
        anchor.row,
        wave > 1 ? `+${gained} COMBO x${wave}!` : `+${gained}`,
        wave > 1 ? "var(--n4)" : "var(--n2)",
      );
      kick(Math.min(3, wave));
      haptic(wave > 1 ? [18, 30, 24] : 16);
      setScore((s) => s + gained);
      if (wave >= 3) showBanner(`WAVE ${wave} • x${2 ** (wave - 1)}`);

      const dead = new Set(matches.map((m) => m.id));
      const dmg = new Set(hidden.map((h) => h.id));
      arr = arr.map((b) =>
        dead.has(b.id)
          ? { ...b, dying: true }
          : dmg.has(b.id)
            ? b.cracks === 0
              ? { ...b, cracks: 1 }
              : { ...b, cracks: 0, num: randNum() }
            : b,
      );
      setBalls(arr);
      await wait(300);

      arr = applyGravity(arr.filter((b) => !b.dying));
      setBalls(arr);
      await wait(230);
      wave++;
    }
    if (arr.length === 0) {
      sfx.clear();
      haptic([30, 40, 30, 60]);
      showBanner("BOARD CLEAR! +70,000");
      setScore((s) => s + 70000);
      kick(3);
      await wait(700);
    }
    return arr;
  }, []);

  const rise = useCallback(async (input: Ball[]): Promise<{ arr: Ball[]; dead: boolean }> => {
    if (input.some((b) => b.row === 0)) return { arr: input, dead: true };
    sfx.rise();
    haptic([10, 20, 10]);
    const shifted = input.map((b) => ({ ...b, row: b.row - 1 }));
    const row = Array.from({ length: SIZE }, (_, c) => makeBall(c, SIZE - 1, null));
    const arr = [...shifted, ...row];
    setBalls(arr);
    await wait(260);
    return { arr, dead: false };
  }, []);

  const drop = useCallback(
    async (col: number) => {
      if (busy || over) return;
      unlockAudio();
      const row = landingRow(balls, col);
      if (row < 0) return;
      setBusy(true);

      const ball = makeBall(col, row, current);
      let arr: Ball[] = [...balls, { ...ball, row: -1 }];
      setBalls(arr);
      await wait(30);
      arr = arr.map((b) => (b.id === ball.id ? { ...b, row } : b));
      setBalls(arr);
      sfx.drop();
      haptic(14);
      await wait(190);
      setSquash(ball.id);
      setTimeout(() => setSquash(null), 280);

      arr = await resolve(arr);

      setCurrent(next);
      setNext(rollPiece());

      if (mode === "classic") {
        const left = movesLeft - 1;
        if (left <= 0) {
          setMovesLeft(RISE_EVERY);
          const res = await rise(arr);
          arr = res.arr;
          if (res.dead) {
            sfx.over();
            setOver(true);
            setBusy(false);
            return;
          }
          arr = await resolve(arr);
        } else {
          setMovesLeft(left);
        }
      }

      const full = Array.from({ length: SIZE }, (_, c) => landingRow(arr, c) < 0).every(Boolean);
      if (full) {
        sfx.over();
        setOver(true);
      }
      setBusy(false);
    },
    [balls, busy, current, mode, movesLeft, next, over, resolve, rise],
  );

  const colFromEvent = (clientX: number) => {
    const el = boardRef.current;
    if (!el) return aim;
    const r = el.getBoundingClientRect();
    return Math.max(0, Math.min(SIZE - 1, Math.floor(((clientX - r.left) / r.width) * SIZE)));
  };

  const restart = () => {
    setBalls(startBalls());
    setCurrent(rollPiece());
    setNext(rollPiece());
    setScore(0);
    setMovesLeft(RISE_EVERY);
    setOver(false);
    setBusy(false);
  };

  // aiming preview info
  const landing = landingRow(balls, aim);
  let preview = { v: 0, h: 0, match: false };
  if (landing >= 0) {
    const g = toGrid([...balls, makeBall(aim, landing, current)]);
    const { v, h } = runLengths(g, aim, landing);
    preview = { v, h, match: current != null && (v === current || h === current) };
  }

  return (
    <main className="game-root flex min-h-screen w-full flex-col items-center overflow-hidden px-3 pb-5 pt-4">
      <div className="w-full max-w-md">
        <header className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3">
          <div className="min-w-0">
            <h1 className="truncate text-xl font-black tracking-tight">
              NEON <span style={{ color: "var(--n2)" }}>SEVEN</span>
            </h1>
            <p className="truncate text-[11px] text-ink-dim">линия ровно N — взрыв</p>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <button
              aria-label={muted ? "Включить звук" : "Выключить звук"}
              onClick={() => {
                const m = !isMuted();
                setMuted(m);
                setMutedState(m);
                unlockAudio();
              }}
              className="glass grid size-11 place-items-center rounded-2xl text-base"
            >
              {muted ? "🔇" : "🔊"}
            </button>
            <button
              onClick={restart}
              className="glass grid size-11 place-items-center rounded-2xl text-base"
              aria-label="Начать заново"
            >
              ↻
            </button>
          </div>
        </header>

        <section className="glass mt-3 grid grid-cols-3 items-center gap-2 rounded-3xl px-4 py-3">
          <div className="min-w-0">
            <div className="text-[10px] uppercase tracking-widest text-ink-dim">Счёт</div>
            <div className="truncate text-lg font-black" style={{ color: "var(--n2)" }}>
              {score.toLocaleString("ru-RU")}
            </div>
          </div>
          <div className="min-w-0 text-center">
            <div className="text-[10px] uppercase tracking-widest text-ink-dim">Рекорд</div>
            <div className="truncate text-lg font-black">{best.toLocaleString("ru-RU")}</div>
          </div>
          <div className="min-w-0 text-right">
            <div className="text-[10px] uppercase tracking-widest text-ink-dim">
              {mode === "classic" ? "До подъёма" : "Дзен"}
            </div>
            <div className="truncate text-lg font-black" style={{ color: "var(--n5)" }}>
              {mode === "classic" ? movesLeft : "∞"}
            </div>
          </div>
        </section>

        <div className="mt-3 flex items-center justify-between gap-3">
          <div className="glass flex items-center gap-3 rounded-3xl px-3 py-2">
            <div className="relative size-12">
              <MiniBall num={current} />
            </div>
            <div className="text-[10px] uppercase tracking-widest text-ink-dim">next</div>
            <div className="relative size-8 opacity-80">
              <MiniBall num={next} />
            </div>
          </div>
          <div className="glass flex items-center gap-1 rounded-3xl p-1">
            {(["classic", "zen"] as Mode[]).map((m) => (
              <button
                key={m}
                onClick={() => {
                  setMode(m);
                  setMovesLeft(RISE_EVERY);
                }}
                className={`rounded-2xl px-3 py-2 text-xs font-bold transition ${
                  mode === m ? "bg-white/15 text-ink" : "text-ink-dim"
                }`}
              >
                {m === "classic" ? "Классика" : "Дзен"}
              </button>
            ))}
          </div>
        </div>

        {/* aim strip */}
        <div className="mt-3 flex items-center justify-center gap-2 text-[11px] font-bold">
          <span
            className="rounded-full px-2 py-1"
            style={{
              color: preview.match ? "var(--n3)" : "var(--ink-dim)",
              background: preview.match ? "oklch(0.79 0.17 155 / 18%)" : "transparent",
            }}
          >
            ↕ {preview.v} · ↔ {preview.h} {preview.match ? "· ВЗРЫВ!" : ""}
          </span>
        </div>

        <div
          ref={boardRef}
          className={`glass relative mt-2 aspect-square w-full touch-none overflow-hidden rounded-[2rem] p-0 ${
            shake ? `shake-${shake}` : ""
          }`}
          onPointerDown={(e) => {
            const c = colFromEvent(e.clientX);
            if (c !== aim) sfx.move();
            setAim(c);
          }}
          onPointerMove={(e) => {
            if (e.buttons === 0) return;
            const c = colFromEvent(e.clientX);
            if (c !== aim) {
              sfx.move();
              haptic(6);
              setAim(c);
            }
          }}
          onPointerUp={() => void drop(aim)}
        >
          {/* grid lines */}
          <div className="absolute inset-0 grid grid-cols-7 grid-rows-7">
            {Array.from({ length: SIZE * SIZE }, (_, i) => (
              <div key={i} className="border border-white/5" />
            ))}
          </div>

          {/* aim column glow */}
          <div
            className="pointer-events-none absolute top-0 h-full transition-all duration-150"
            style={{
              left: pos(aim),
              width: `${cell}%`,
              background: preview.match
                ? "linear-gradient(180deg, oklch(0.79 0.17 155 / 26%), transparent 85%)"
                : "linear-gradient(180deg, oklch(0.82 0.15 205 / 18%), transparent 85%)",
              boxShadow: "inset 0 0 24px oklch(1 0 0 / 8%)",
            }}
          />
          {/* landing ghost */}
          {landing >= 0 && (
            <div
              className="pointer-events-none absolute rounded-full border-2 border-dashed border-white/30 transition-all duration-150"
              style={{ left: pos(aim), top: pos(landing), width: `${cell}%`, height: `${cell}%` }}
            />
          )}

          {balls.map((b) => (
            <div
              key={b.id}
              className={`ball ${b.num == null ? "ball-obsidian" : ""} ${
                b.cracks ? "ball-cracked" : ""
              } ${b.dying ? "pop" : ""} ${squash === b.id ? "squash" : ""}`}
              style={
                {
                  left: pos(b.col),
                  top: pos(b.row),
                  width: `${cell}%`,
                  height: `${cell}%`,
                  "--c": b.num ? `var(${COLOR[b.num - 1]})` : "var(--obsidian)",
                  "--c-dark": "oklch(0.2 0.04 280 / 55%)",
                } as React.CSSProperties
              }
            >
              <span className="ball-face" />
              {b.num != null && <span className="ball-num">{b.num}</span>}
            </div>
          ))}

          {fx.map((f) =>
            f.kind === "ring" ? (
              <span
                key={f.id}
                className="ring"
                style={{
                  left: pos(f.x),
                  top: pos(f.y),
                  width: `${cell}%`,
                  height: `${cell}%`,
                  borderColor: f.color,
                  boxShadow: `0 0 24px ${f.color}`,
                }}
              />
            ) : (
              <span
                key={f.id}
                className="spark"
                style={
                  {
                    left: `calc(${pos(f.x)} + ${cell / 2}%)`,
                    top: `calc(${pos(f.y)} + ${cell / 2}%)`,
                    background: f.color,
                    boxShadow: `0 0 12px ${f.color}`,
                    "--dx": `${f.dx}px`,
                    "--dy": `${f.dy}px`,
                  } as React.CSSProperties
                }
              />
            ),
          )}

          {floats.map((f) => (
            <span
              key={f.id}
              className="float-score text-sm"
              style={{
                left: `calc(${pos(f.x)} + ${cell / 2}%)`,
                top: pos(f.y),
                color: f.color,
              }}
            >
              {f.text}
            </span>
          ))}

          {banner && (
            <div className="pointer-events-none absolute inset-0 grid place-items-center">
              <div className="banner glass rounded-3xl px-5 py-3 text-center text-lg font-black" style={{ color: "var(--n4)" }}>
                {banner}
              </div>
            </div>
          )}

          {over && (
            <div className="absolute inset-0 grid place-items-center bg-black/55 backdrop-blur-md">
              <div className="glass w-64 rounded-3xl p-5 text-center">
                <div className="text-sm uppercase tracking-widest text-ink-dim">Game Over</div>
                <div className="mt-1 text-3xl font-black" style={{ color: "var(--n6)" }}>
                  {score.toLocaleString("ru-RU")}
                </div>
                <button
                  onClick={restart}
                  className="mt-4 w-full rounded-2xl px-4 py-3 text-sm font-black text-black"
                  style={{ background: "linear-gradient(120deg, var(--n2), var(--n3))" }}
                >
                  Играть снова
                </button>
              </div>
            </div>
          )}
        </div>

        <p className="pulse-slow mt-3 text-center text-[11px] text-ink-dim">
          Проведи пальцем по колонке и отпусти, чтобы бросить шар
        </p>
      </div>
    </main>
  );
}

function MiniBall({ num }: { num: number | null }) {
  return (
    <div
      className={`ball ${num == null ? "ball-obsidian" : ""}`}
      style={
        {
          position: "relative",
          width: "100%",
          height: "100%",
          "--c": num ? `var(${COLOR[num - 1]})` : "var(--obsidian)",
          "--c-dark": "oklch(0.2 0.04 280 / 55%)",
        } as React.CSSProperties
      }
    >
      <span className="ball-face" />
      {num != null && <span className="ball-num">{num}</span>}
    </div>
  );
}
