export const SIZE = 7;

export type Ball = {
  id: number;
  col: number;
  row: number; // 0 = top, SIZE-1 = bottom
  num: number | null; // null = hidden obsidian
  cracks: number; // 0 whole, 1 cracked
  dying?: boolean;
  landed?: boolean;
};

let idSeq = 1;
export const nextId = () => idSeq++;

export const makeBall = (col: number, row: number, num: number | null): Ball => ({
  id: nextId(),
  col,
  row,
  num,
  cracks: 0,
});

export const randNum = () => 1 + Math.floor(Math.random() * SIZE);

/** 15% chance of a hidden obsidian ball */
export const rollPiece = (): number | null => (Math.random() < 0.15 ? null : randNum());

export function toGrid(balls: Ball[]): (Ball | null)[][] {
  const g: (Ball | null)[][] = Array.from({ length: SIZE }, () => Array(SIZE).fill(null));
  for (const b of balls) if (b.row >= 0 && b.row < SIZE) g[b.row][b.col] = b;
  return g;
}

export function landingRow(balls: Ball[], col: number): number {
  const g = toGrid(balls);
  for (let r = SIZE - 1; r >= 0; r--) if (!g[r][col]) return r;
  return -1;
}

export function runLengths(g: (Ball | null)[][], col: number, row: number) {
  let v = 1;
  for (let r = row - 1; r >= 0 && g[r][col]; r--) v++;
  for (let r = row + 1; r < SIZE && g[r][col]; r++) v++;
  let h = 1;
  for (let c = col - 1; c >= 0 && g[row][c]; c--) h++;
  for (let c = col + 1; c < SIZE && g[row][c]; c++) h++;
  return { v, h };
}

/** balls that satisfy the detonation rule */
export function findMatches(balls: Ball[]): Ball[] {
  const g = toGrid(balls);
  const out: Ball[] = [];
  for (const b of balls) {
    if (b.num == null) continue;
    const { v, h } = runLengths(g, b.col, b.row);
    if (v === b.num || h === b.num) out.push(b);
  }
  return out;
}

/** neighbours (cross) of exploding balls that are hidden -> take 1 damage */
export function damagedNeighbours(balls: Ball[], exploding: Ball[]): Ball[] {
  const g = toGrid(balls);
  const hit = new Set<number>();
  const res: Ball[] = [];
  for (const b of exploding) {
    for (const [dc, dr] of [
      [1, 0],
      [-1, 0],
      [0, 1],
      [0, -1],
    ]) {
      const c = b.col + dc;
      const r = b.row + dr;
      if (c < 0 || c >= SIZE || r < 0 || r >= SIZE) continue;
      const n = g[r][c];
      if (n && n.num == null && !hit.has(n.id)) {
        hit.add(n.id);
        res.push(n);
      }
    }
  }
  return res;
}

/** apply gravity, returns new positions (mutates copies) */
export function applyGravity(balls: Ball[]): Ball[] {
  const out = balls.map((b) => ({ ...b }));
  for (let c = 0; c < SIZE; c++) {
    const col = out.filter((b) => b.col === c).sort((a, b) => b.row - a.row);
    let r = SIZE - 1;
    for (const b of col) {
      b.row = r;
      r--;
    }
  }
  return out;
}

export function scoreFor(count: number, wave: number) {
  return Math.round(count * 100 * 2 ** (wave - 1));
}
