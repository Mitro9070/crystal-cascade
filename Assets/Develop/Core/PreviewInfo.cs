namespace NeonSeven.Core
{
    public readonly struct PreviewInfo
    {
        public PreviewInfo(int row, int verticalLength, int horizontalLength, bool willMatch)
        {
            Row = row;
            VerticalLength = verticalLength;
            HorizontalLength = horizontalLength;
            WillMatch = willMatch;
        }

        public int Row { get; }
        public int VerticalLength { get; }
        public int HorizontalLength { get; }
        public bool WillMatch { get; }
    }
}
