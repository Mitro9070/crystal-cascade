namespace NeonSeven.Core
{
    public sealed class BallData
    {
        public BallData(int id, int column, int row, int number, int cracks)
        {
            Id = id;
            Column = column;
            Row = row;
            Number = number;
            Cracks = cracks;
        }

        public int Id { get; }
        public int Column { get; set; }
        public int Row { get; set; }
        public int Number { get; set; }
        public int Cracks { get; set; }
        public bool IsHidden => Number == 0;

        public BallData Clone()
        {
            return new BallData(Id, Column, Row, Number, Cracks);
        }
    }
}
