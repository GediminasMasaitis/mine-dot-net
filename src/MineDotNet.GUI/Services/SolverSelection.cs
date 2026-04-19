namespace MineDotNet.GUI.Services
{
    // App-wide choice of solver backend. Set via the main window's checkbox;
    // read by everywhere that invokes the solver (MainWindow.SolveMap +
    // BenchmarkRunner). Kept static so the toggle is sticky for the whole
    // session without needing DI plumbing or event subscriptions.
    internal static class SolverSelection
    {
        public static bool UseDirect { get; set; }
    }
}
