using System;
using System.Collections.Generic;

namespace NeonSeven.Core
{
    public sealed class BoardModel
    {
        private readonly BallData[,] _grid;
        private readonly Random _random;
        private int _nextId = 1;

        public BoardModel(int size, int seed)
        {
            Size = size;
            _grid = new BallData[Size, Size];
            _random = new Random(seed);
        }

        public int Size { get; }

        public IReadOnlyList<BallData> Balls
        {
            get
            {
                var balls = new List<BallData>();
                for (int row = 0; row < Size; row++)
                {
                    for (int column = 0; column < Size; column++)
                    {
                        var ball = _grid[column, row];
                        if (ball != null)
                            balls.Add(ball.Clone());
                    }
                }

                return balls;
            }
        }

        public void FillStartRows(int rows, float hiddenChance)
        {
            for (int row = Size - rows; row < Size; row++)
            {
                if (row < 0)
                    continue;

                for (int column = 0; column < Size; column++)
                {
                    if (_random.NextDouble() < 0.70d)
                        AddBall(column, row, RollNumber(hiddenChance), 0);
                }
            }

            ApplyGravity();
        }

        public void FillPrototypeStartBalls(float hiddenChance = 0.25f)
        {
            for (int column = 0; column < Size; column++)
            {
                int height = _random.Next(1, 3);
                for (int i = 0; i < height; i++)
                {
                    int row = Size - 1 - i;
                    AddBall(column, row, RollNumber(hiddenChance), 0);
                }
            }
        }

        public void FillFromMatrix(IReadOnlyList<int> matrix)
        {
            if (matrix == null)
                return;

            int limit = Math.Min(Size * Size, matrix.Count);
            for (int index = 0; index < limit; index++)
            {
                int value = matrix[index];
                if (value < 0)
                    continue;

                int row = index / Size;
                int column = index % Size;
                int number = value > Size ? ((value - 1) % Size) + 1 : value;
                AddBall(column, row, number, 0);
            }
        }

        public int RollNumber(float hiddenChance)
        {
            return _random.NextDouble() < hiddenChance ? 0 : _random.Next(1, Size + 1);
        }

        public bool TryDrop(int column, int number, out BallData dropped)
        {
            dropped = null;
            int row = LandingRow(column);
            if (row < 0)
                return false;

            dropped = AddBall(column, row, number, 0);
            return true;
        }

        public int LandingRow(int column)
        {
            if (column < 0 || column >= Size)
                return -1;

            for (int row = Size - 1; row >= 0; row--)
            {
                if (_grid[column, row] == null)
                    return row;
            }

            return -1;
        }

        public PreviewInfo PreviewDrop(int column, int number)
        {
            int row = LandingRow(column);
            if (row < 0)
                return new PreviewInfo(-1, 0, 0, false);

            int vertical = CountLine(column, row, 0, -1, true) + CountLine(column, row, 0, 1, true) + 1;
            int horizontal = CountLine(column, row, -1, 0, true) + CountLine(column, row, 1, 0, true) + 1;
            bool willMatch = number > 0 && (vertical == number || horizontal == number);
            return new PreviewInfo(row, vertical, horizontal, willMatch);
        }

        public IReadOnlyList<BallData> PeekMatches()
        {
            return CloneList(FindMatches());
        }

        public ResolutionStep CommitDestroy(IReadOnlyList<BallData> matches, int wave)
        {
            var live = ResolveLiveMatches(matches);
            if (live.Count == 0)
                return new ResolutionStep(wave, live, 0, 0);

            int hiddenHits = DamageHiddenNeighbours(live);
            int score = (int)Math.Round(live.Count * 100.0d * Math.Pow(2.0d, wave - 1));
            var step = new ResolutionStep(wave, CloneList(live), hiddenHits, score);
            Remove(live);
            return step;
        }

        public void ApplyGravity()
        {
            for (int column = 0; column < Size; column++)
            {
                int targetRow = Size - 1;
                for (int row = Size - 1; row >= 0; row--)
                {
                    var ball = _grid[column, row];
                    if (ball == null)
                        continue;

                    _grid[column, row] = null;
                    _grid[column, targetRow] = ball;
                    ball.Column = column;
                    ball.Row = targetRow;
                    targetRow--;
                }
            }
        }

        public IReadOnlyList<ResolutionStep> ResolveAllWaves()
        {
            var steps = new List<ResolutionStep>();
            int wave = 1;

            while (true)
            {
                var matches = PeekMatches();
                if (matches.Count == 0)
                    break;

                steps.Add(CommitDestroy(matches, wave));
                ApplyGravity();
                wave++;
            }

            return steps;
        }

        public bool TryRiseHiddenRow()
        {
            for (int column = 0; column < Size; column++)
            {
                if (_grid[column, 0] != null)
                    return false;
            }

            for (int row = 1; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    var ball = _grid[column, row];
                    _grid[column, row - 1] = ball;
                    if (ball != null)
                        ball.Row = row - 1;
                }
            }

            for (int column = 0; column < Size; column++)
                AddBall(column, Size - 1, 0, 0);

            return true;
        }

        public bool IsFull()
        {
            for (int column = 0; column < Size; column++)
            {
                if (LandingRow(column) >= 0)
                    return false;
            }

            return true;
        }

        public int CountHiddenDamagedOrRevealed()
        {
            int count = 0;
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    var ball = _grid[column, row];
                    if (ball != null && ball.Cracks > 0)
                        count++;
                }
            }

            return count;
        }

        public int CountBalls()
        {
            int count = 0;
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    if (_grid[column, row] != null)
                        count++;
                }
            }

            return count;
        }

        private BallData AddBall(int column, int row, int number, int cracks)
        {
            var ball = new BallData(_nextId++, column, row, number, cracks);
            _grid[column, row] = ball;
            return ball;
        }

        private List<BallData> FindMatches()
        {
            var matches = new List<BallData>();
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    var ball = _grid[column, row];
                    if (ball == null || ball.IsHidden)
                        continue;

                    int vertical = CountLine(column, row, 0, -1, false) + CountLine(column, row, 0, 1, false) + 1;
                    int horizontal = CountLine(column, row, -1, 0, false) + CountLine(column, row, 1, 0, false) + 1;
                    if (vertical == ball.Number || horizontal == ball.Number)
                        matches.Add(ball);
                }
            }

            return matches;
        }

        private int CountLine(int column, int row, int deltaColumn, int deltaRow, bool includeGhost)
        {
            int count = 0;
            int c = column + deltaColumn;
            int r = row + deltaRow;

            while (c >= 0 && c < Size && r >= 0 && r < Size)
            {
                if (_grid[c, r] == null)
                    break;

                count++;
                c += deltaColumn;
                r += deltaRow;
            }

            return count;
        }

        private int DamageHiddenNeighbours(IReadOnlyList<BallData> matches)
        {
            var hit = new HashSet<int>();
            int damage = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                DamageAt(matches[i].Column + 1, matches[i].Row, hit, ref damage);
                DamageAt(matches[i].Column - 1, matches[i].Row, hit, ref damage);
                DamageAt(matches[i].Column, matches[i].Row + 1, hit, ref damage);
                DamageAt(matches[i].Column, matches[i].Row - 1, hit, ref damage);
            }

            return damage;
        }

        private void DamageAt(int column, int row, HashSet<int> hit, ref int damage)
        {
            if (column < 0 || column >= Size || row < 0 || row >= Size)
                return;

            var ball = _grid[column, row];
            if (ball == null || !ball.IsHidden || hit.Contains(ball.Id))
                return;

            hit.Add(ball.Id);
            damage++;
            if (ball.Cracks == 0)
                ball.Cracks = 1;
            else
            {
                ball.Cracks = 0;
                ball.Number = _random.Next(1, Size + 1);
            }
        }

        private void Remove(IReadOnlyList<BallData> matches)
        {
            for (int i = 0; i < matches.Count; i++)
                _grid[matches[i].Column, matches[i].Row] = null;
        }

        private List<BallData> ResolveLiveMatches(IReadOnlyList<BallData> matches)
        {
            var live = new List<BallData>();
            if (matches == null)
                return live;

            var seen = new HashSet<int>();
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (match == null || seen.Contains(match.Id))
                    continue;

                if (match.Column < 0 || match.Column >= Size || match.Row < 0 || match.Row >= Size)
                    continue;

                var ball = _grid[match.Column, match.Row];
                if (ball == null || ball.Id != match.Id)
                    continue;

                seen.Add(ball.Id);
                live.Add(ball);
            }

            return live;
        }

        private static IReadOnlyList<BallData> CloneList(IReadOnlyList<BallData> source)
        {
            var result = new List<BallData>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(source[i].Clone());

            return result;
        }
    }
}
