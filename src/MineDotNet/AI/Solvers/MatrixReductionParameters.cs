namespace MineDotNet.AI.Solvers
{
    public class MatrixReductionParameters
    {
        public MatrixReductionParameters(bool skipReduction = false, bool orderColumns = true, bool reverseColumns = false, bool reverseRows = true, bool useUniqueRows = true)
        {
            SkipReduction = skipReduction;
            OrderColumns = orderColumns;
            ReverseColumns = reverseColumns;
            ReverseRows = reverseRows;
            UseUniqueRows = useUniqueRows;
        }

        public bool SkipReduction { get; set; }
        public bool OrderColumns { get; set; }
        public bool ReverseColumns { get; set; }
        public bool ReverseRows { get; set; }
        public bool UseUniqueRows { get; set; }
    }
}