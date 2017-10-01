using MineDotNet.AI.Solvers;

namespace MineDotNet.GUI.Models
{
    class SolverListEntry
    {
        public SolverListEntry(string solverName, string solverImplementation, BorderSeparationSolverSettings settings)
        {
            SolverName = solverName;
            SolverImplementation = solverImplementation;
            Settings = settings;
        }

        public string SolverName { get; set; }
        public string SolverImplementation { get; set; }
        public BorderSeparationSolverSettings Settings { get; set; }

        public override string ToString()
        {
            var tag = $"[{SolverImplementation}]".PadRight(5);
            return $"{tag} {SolverName}";
        }
    }
}