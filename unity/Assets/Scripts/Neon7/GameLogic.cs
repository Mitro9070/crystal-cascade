using System;
using System.Collections.Generic;
using System.Linq;

namespace Neon7
{
    /// <summary>Точный порт src/lib/game.ts.</summary>
    public class BallData
    {
        public int Id;
        public int Col;
        public int Row;      // 0 = верх, Size-1 = низ
        public int? Num;     // null = скрытый обсидиан
        public int Cracks;   // 0 целый, 1 треснутый
        public bool Dying;

        public BallData Clone() => new BallData
        { Id = Id, Col = Col, Row = Row, Num = Num, Cracks = Cracks, Dying = Dying };
    }

    public static class GameLogic
    {
        public const int Size = 7;
        public const int RiseEvery = 5;
        public const float HiddenPieceChance = 0.15f;
        public const float HiddenStartChance = 0.25f;
        public const int BoardClearBonus = 70000;

        private static int _idSeq = 1;
        private static readonly Random Rng = new Random();

        public static int NextId() => _idSeq++;
        public static void ResetIds() => _idSeq = 1;

        public static BallData MakeBall(int col, int row, int? num) =>
            new BallData { Id = NextId(), Col = col, Row = row, Num = num, Cracks = 0 };

        public static int RandNum() => 1 + Rng.Next(Size);

        /// <summary>15% — скрытый шар.</summary>
        public static int? RollPiece() => Rng.NextDouble() < HiddenPieceChance ? (int?)null : RandNum();

        /// <summary>Старт: в каждой колонке 1-2 шара снизу, 25% из них скрытые.</summary>
        public static List<BallData> StartBalls()
        {
            var outList = new List<BallData>();
            for (int c = 0; c < Size; c++)
            {
                int h = 1 + Rng.Next(2);
                for (int i = 0; i < h; i++)
                    outList.Add(MakeBall(c, Size - 1 - i,
                        Rng.NextDouble() < HiddenStartChance ? (int?)null : RandNum()));
            }
            return outList;
        }

        public static BallData[,] ToGrid(IEnumerable<BallData> balls)
        {
            var g = new BallData[Size, Size];
            foreach (var b in balls)
                if (b.Row >= 0 && b.Row < Size && b.Col >= 0 && b.Col < Size)
                    g[b.Row, b.Col] = b;
            return g;
        }

        /// <summary>Нижняя свободная строка колонки, -1 если колонка полна.</summary>
        public static int LandingRow(IEnumerable<BallData> balls, int col)
        {
            var g = ToGrid(balls);
            for (int r = Size - 1; r >= 0; r--)
                if (g[r, col] == null) return r;
            return -1;
        }

        /// <summary>Длины непрерывных линий через (col,row) включительно.</summary>
        public static (int v, int h) RunLengths(BallData[,] g, int col, int row)
        {
            int v = 1;
            for (int r = row - 1; r >= 0 && g[r, col] != null; r--) v++;
            for (int r = row + 1; r < Size && g[r, col] != null; r++) v++;
            int h = 1;
            for (int c = col - 1; c >= 0 && g[row, c] != null; c--) h++;
            for (int c = col + 1; c < Size && g[row, c] != null; c++) h++;
            return (v, h);
        }

        /// <summary>Шары, удовлетворяющие правилу детонации (линия == N).</summary>
        public static List<BallData> FindMatches(List<BallData> balls)
        {
            var g = ToGrid(balls);
            var res = new List<BallData>();
            foreach (var b in balls)
            {
                if (b.Num == null) continue;
                var (v, h) = RunLengths(g, b.Col, b.Row);
                if (v == b.Num.Value || h == b.Num.Value) res.Add(b);
            }
            return res;
        }

        private static readonly int[,] Cross = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        /// <summary>Скрытые соседи по кресту от взрывающихся шаров — получают 1 урон.</summary>
        public static List<BallData> DamagedNeighbours(List<BallData> balls, List<BallData> exploding)
        {
            var g = ToGrid(balls);
            var hit = new HashSet<int>();
            var res = new List<BallData>();
            foreach (var b in exploding)
            {
                for (int i = 0; i < 4; i++)
                {
                    int c = b.Col + Cross[i, 0];
                    int r = b.Row + Cross[i, 1];
                    if (c < 0 || c >= Size || r < 0 || r >= Size) continue;
                    var n = g[r, c];
                    if (n != null && n.Num == null && hit.Add(n.Id)) res.Add(n);
                }
            }
            return res;
        }

        /// <summary>Гравитация: шары над пустотами падают вниз.</summary>
        public static List<BallData> ApplyGravity(List<BallData> balls)
        {
            var outList = balls.Select(b => b.Clone()).ToList();
            for (int c = 0; c < Size; c++)
            {
                var col = outList.Where(b => b.Col == c).OrderByDescending(b => b.Row).ToList();
                int r = Size - 1;
                foreach (var b in col) b.Row = r--;
            }
            return outList;
        }

        /// <summary>Score = count * 100 * 2^(wave-1).</summary>
        public static int ScoreFor(int count, int wave) =>
            (int)Math.Round(count * 100.0 * Math.Pow(2, wave - 1));

        /// <summary>Подъём дна: сдвиг вверх + ряд скрытых шаров снизу. dead = шар уже в row 0.</summary>
        public static (List<BallData> arr, bool dead) Rise(List<BallData> input)
        {
            if (input.Any(b => b.Row == 0)) return (input, true);
            var arr = input.Select(b => { var n = b.Clone(); n.Row -= 1; return n; }).ToList();
            for (int c = 0; c < Size; c++) arr.Add(MakeBall(c, Size - 1, null));
            return (arr, false);
        }

        public static bool BoardFull(List<BallData> arr)
        {
            for (int c = 0; c < Size; c++) if (LandingRow(arr, c) >= 0) return false;
            return true;
        }
    }
}
