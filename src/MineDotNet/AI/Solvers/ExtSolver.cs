using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MineDotNet.Common;
using MineDotNet.IO;

namespace MineDotNet.AI.Solvers
{
    // External solver that talks to the UMSI engine (a separate
    // minedotcpp_umsi process) over stdin/stdout. Replaces the previous
    // P/Invoke path — there is no longer a shared library loaded into
    // this process, just a child process we pipe text into.
    //
    // Usage is singleton (ExtSolver.Instance), matching the old API so
    // callers (AI.AI, MainForm.CreateSolver, TestConsole) don't change.
    public class ExtSolver : ISolver, IDisposable
    {
        public const string Alias = "C++";

        private static readonly Lazy<ExtSolver> InstanceLazy =
            new Lazy<ExtSolver>(() => new ExtSolver());
        public static ExtSolver Instance => InstanceLazy.Value;

        // Most C# setting names convert cleanly to the engine's snake_case
        // option names via ToSnakeCase below. These three historically drop
        // the `All` segment that the C++ side still has, so they need
        // explicit overrides.
        private static readonly Dictionary<string, string> NameOverrides =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { nameof(BorderSeparationSolverSettings.GaussianStopOnNoMineVerdict), "gaussian_all_stop_on_no_mine_verdict" },
                { nameof(BorderSeparationSolverSettings.GaussianStopOnAnyVerdict),    "gaussian_all_stop_on_any_verdict" },
                { nameof(BorderSeparationSolverSettings.GaussianStopAlways),          "gaussian_all_stop_always" },
            };

        private readonly Process _process;
        private readonly StreamWriter _stdin;
        private readonly StreamReader _stdout;
        private readonly TextMapVisualizer _visualizer = new TextMapVisualizer();
        private readonly HashSet<string> _knownOptions = new HashSet<string>(StringComparer.Ordinal);
        private readonly object _lock = new object();
        private bool _settingsApplied;

        private ExtSolver()
        {
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "minedotcpp_umsi.exe"
                : "minedotcpp_umsi";
            var path = Path.Combine(AppContext.BaseDirectory, exeName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"UMSI engine binary not found at '{path}'. " +
                    "Build the MineDotNet project normally — the native build step " +
                    "should produce and copy the binary alongside the managed assembly.");
            }

            var psi = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to launch UMSI engine process");

            _stdin = _process.StandardInput;
            // The engine parses lines via getline(cin, ...) on stdin and
            // trims trailing \r. StreamWriter defaults to CRLF on Windows;
            // set LF explicitly so the wire protocol is consistent across
            // platforms.
            _stdin.NewLine = "\n";
            _stdout = _process.StandardOutput;

            Handshake();

            // Best-effort clean shutdown when the host app exits: closing
            // stdin signals EOF, which ends the engine's read loop. If the
            // host is killed hard, the OS closes the pipes for us, which
            // also ends the engine.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
        }

        private void Handshake()
        {
            Send("umsi");
            string line;
            while ((line = _stdout.ReadLine()) != null)
            {
                if (line == "umsiok") return;
                if (line.StartsWith("option name ", StringComparison.Ordinal))
                {
                    // "option name <name> type <type> ..."
                    var parts = line.Split(' ');
                    if (parts.Length >= 3) _knownOptions.Add(parts[2]);
                }
                // id / error / info lines don't matter here — we only need
                // to collect option names and wait for umsiok.
            }
            throw new InvalidOperationException(
                "UMSI engine exited before completing handshake");
        }

        public void InitSolver(BorderSeparationSolverSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            lock (_lock)
            {
                foreach (var prop in typeof(BorderSeparationSolverSettings).GetProperties())
                {
                    var umsiName = GetUmsiName(prop.Name);
                    // Skip settings the engine doesn't recognise (e.g.
                    // PrintTrace* — intentionally not exposed by the engine
                    // because their output would corrupt the stdout
                    // protocol channel).
                    if (!_knownOptions.Contains(umsiName)) continue;

                    var value = prop.GetValue(settings);
                    var valueStr = value switch
                    {
                        bool b      => b ? "true" : "false",
                        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                        null        => "",
                        _           => value.ToString(),
                    };
                    Send($"setoption name {umsiName} value {valueStr}");
                }

                // Sync point. Any `error` lines the engine emitted in
                // response to unrecognised options (shouldn't happen after
                // the _knownOptions filter, but be defensive) sit in the
                // pipe ahead of the readyok we're waiting for; skip them.
                Send("isready");
                string line;
                while ((line = _stdout.ReadLine()) != null)
                {
                    if (line == "readyok") break;
                }

                _settingsApplied = true;
            }
        }

        public IDictionary<Coordinate, SolverResult> Solve(IMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            lock (_lock)
            {
                if (!_settingsApplied)
                {
                    // Match the old ExtSolver's behaviour: lazy-init with
                    // defaults if the caller never called InitSolver.
                    InitSolver(new BorderSeparationSolverSettings());
                }

                // VisualizeToString(map, multiline: false) emits rows joined
                // by `;` — exactly the single-line form the engine accepts
                // after `position`. One write, one flush, no EOF sentinel.
                var mapStr = _visualizer.VisualizeToString(map, multiline: false);
                Send("position " + mapStr);
                Send("go");

                var results = new Dictionary<Coordinate, SolverResult>();
                string line;
                while ((line = _stdout.ReadLine()) != null)
                {
                    if (line == "done") break;
                    if (!line.StartsWith("result ", StringComparison.Ordinal)) continue;

                    // Format: "result <x> <y> <probability> <mine|safe|unknown>"
                    var parts = line.Split(' ');
                    if (parts.Length < 5) continue;
                    if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) continue;
                    if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) continue;
                    if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var prob)) continue;

                    bool? verdict = parts[4] switch
                    {
                        "mine"    => true,
                        "safe"    => false,
                        _         => null,   // "unknown" or anything else
                    };
                    var coord = new Coordinate(x, y);
                    results[coord] = new SolverResult(coord, prob, verdict);
                }
                return results;
            }
        }

        private void Send(string line)
        {
            _stdin.WriteLine(line);
            _stdin.Flush();
        }

        private static string GetUmsiName(string pascal)
            => NameOverrides.TryGetValue(pascal, out var over) ? over : ToSnakeCase(pascal);

        // PascalCase → snake_case, with the usual rules plus a tweak for
        // digits:
        //   * Underscore before any uppercase letter that follows a lower
        //     (start of a new word).
        //   * Underscore before the last uppercase in a run immediately
        //     followed by a lowercase — so "PlatformID" → "platform_id"
        //     (not "platform_i_d"), "OpenCl" → "open_cl".
        //   * Underscore before a digit that follows a letter — so
        //     "DebugSetting1" → "debug_setting_1" (the C++ struct field
        //     has the underscore, and without this rule we'd silently
        //     fail to map the debug settings).
        private static string ToSnakeCase(string pascal)
        {
            if (string.IsNullOrEmpty(pascal)) return pascal;
            var sb = new StringBuilder(pascal.Length + 8);
            for (int i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        var prev = pascal[i - 1];
                        var nextLower = i + 1 < pascal.Length && char.IsLower(pascal[i + 1]);
                        if (char.IsLower(prev) || (char.IsUpper(prev) && nextLower))
                        {
                            sb.Append('_');
                        }
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (char.IsDigit(c))
                {
                    if (i > 0 && !char.IsDigit(pascal[i - 1]))
                    {
                        sb.Append('_');
                    }
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void Shutdown()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    try { _stdin?.WriteLine("quit"); _stdin?.Flush(); } catch { }
                    try { _stdin?.Close(); } catch { }
                    _process.WaitForExit(1000);
                    if (!_process.HasExited)
                    {
                        try { _process.Kill(); } catch { }
                    }
                }
            }
            catch { /* best effort — we're shutting down */ }
        }

        public void Dispose()
        {
            Shutdown();
            _stdin?.Dispose();
            _stdout?.Dispose();
            _process?.Dispose();
        }
    }
}
