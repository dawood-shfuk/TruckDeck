using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Funbit.Ets.Telemetry.Server.Helpers;
using Microsoft.Win32;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public class MapGenerationService
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static readonly Regex ProgressRegex = new Regex(@"(?:\[\d{2}:\d{2}:\d{2}\]\s*)?TRUCKDECK_PROGRESS:(\d+)\s+(.*)", RegexOptions.Compiled);
        static readonly Regex DoneRegex = new Regex(@"(?:\[\d{2}:\d{2}:\d{2}\]\s*)?TRUCKDECK_DONE:\s*(.*)", RegexOptions.Compiled);

        readonly object _sync = new object();
        readonly Dictionary<string, MapGenerationJob> _jobs = new Dictionary<string, MapGenerationJob>();
        readonly Dictionary<string, int> _jobLogLineCount = new Dictionary<string, int>();
        Process _runningProcess;
        string _runningLogPath;
        Thread _monitorThread;

        public static readonly MapGenerationService Instance = new MapGenerationService();

        /// <summary>
        /// On startup, copy maps/generated/*.pmtiles to Html root (and mirrors) when missing or stale.
        /// </summary>
        public void EnsureMapsActivated()
        {
            try
            {
                foreach (var game in new[] { "ets2", "ats" })
                {
                    var pmtilesName = game == "ats" ? "ats.pmtiles" : "ets2.pmtiles";
                    var generatedPmtiles = Path.Combine(HtmlRoot, "maps", "generated", pmtilesName);
                    if (!File.Exists(generatedPmtiles))
                        continue;

                    var needCopy = false;
                    foreach (var name in GetSidecarFileNames(game))
                    {
                        var generated = Path.Combine(HtmlRoot, "maps", "generated", name);
                        if (!File.Exists(generated))
                            continue;

                        var active = Path.Combine(HtmlRoot, name);
                        if (!File.Exists(active))
                        {
                            needCopy = true;
                            break;
                        }

                        var genInfo = new FileInfo(generated);
                        var activeInfo = new FileInfo(active);
                        if (genInfo.Length != activeInfo.Length || genInfo.LastWriteTimeUtc > activeInfo.LastWriteTimeUtc)
                        {
                            needCopy = true;
                            break;
                        }
                    }

                    if (needCopy)
                    {
                        Log.InfoFormat("Syncing {0} (+ NAV routing data) from maps/generated to dashboard paths", pmtilesName);
                        CopyActivatedMapFiles(game);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("EnsureMapsActivated failed", ex);
            }
        }

        /// <summary>PMTiles map plus NAV routing sidecar files that travel together per game.</summary>
        static string[] GetSidecarFileNames(string game)
        {
            var pmtilesName = game == "ats" ? "ats.pmtiles" : "ets2.pmtiles";
            return new[] { pmtilesName, game + "-graph.json", game + "-cities.json" };
        }

        static string HtmlRoot => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html");
        static string MapsDir => Path.Combine(HtmlRoot, "maps");
        static string MapToolsRoot => Path.Combine(Settings.SettingsDirectory, "map-tools", "maps");

        public string GetRecommendedBackend()
        {
            var configured = Settings.Instance.MapGenerationBackend?.Trim().ToLowerInvariant();
            if (configured == "native")
                return NativeTippecanoeAvailable() ? "native" : "none";
            if (configured == "wsl")
                return WslHelper.IsWslAvailable() && !string.IsNullOrEmpty(WslHelper.GetDefaultDistro()) ? "wsl" : "none";

            if (WslHelper.IsWslAvailable() && !string.IsNullOrEmpty(WslHelper.GetDefaultDistro()))
                return "wsl";
            if (NativeTippecanoeAvailable())
                return "native";
            return "none";
        }

        public MapGenerationJob GetJob(string id)
        {
            lock (_sync)
                return _jobs.TryGetValue(id, out var job) ? CloneJob(job) : null;
        }

        public MapGenerationJob GetActiveJob()
        {
            lock (_sync)
            {
                var active = _jobs.Values
                    .Where(j => j.Status == "running" || j.Status == "pending")
                    .OrderByDescending(j => j.StartedAt)
                    .FirstOrDefault();
                return active != null ? CloneJob(active) : null;
            }
        }

        /// <summary>Latest successful map-tools setup log (survives app restart).</summary>
        public string GetLatestCompletedSetupLog()
        {
            var dir = Path.Combine(Settings.SettingsDirectory, "map-jobs");
            if (!Directory.Exists(dir))
                return "";

            string bestPath = null;
            var bestTime = DateTime.MinValue;
            foreach (var path in Directory.GetFiles(dir, "*.log"))
            {
                var tail = ReadLogTail(path, 16000);
                if (string.IsNullOrWhiteSpace(tail))
                    continue;
                if (tail.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    tail.IndexOf("TRUCKDECK_DONE:", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (tail.IndexOf("Map tools ready", StringComparison.OrdinalIgnoreCase) < 0 &&
                    tail.IndexOf("TRUCKDECK_DONE:", StringComparison.OrdinalIgnoreCase) < 0 &&
                    tail.IndexOf("TRUCKDECK_TOOL:maptools:", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var written = File.GetLastWriteTimeUtc(path);
                if (written > bestTime)
                {
                    bestTime = written;
                    bestPath = path;
                }
            }

            return bestPath != null ? ReadLogTail(bestPath, 16000) : "";
        }

        public object GetStatus()
        {
            var runtime = GetRecommendedBackend();
            var distro = WslHelper.GetDefaultDistro();

            return new
            {
                runtime,
                wsl = new
                {
                    available = WslHelper.IsWslAvailable(),
                    distro,
                    installPath = Settings.Instance.WslInstallPath
                },
                wslTools = DescribeWslTools(distro),
                nativeTools = DescribeNativeTools(),
                outputs = new
                {
                    ets2 = DescribeOutput("ets2"),
                    ats = DescribeOutput("ats")
                },
                navHealth = DescribeNavHealth(),
                settings = DescribeSettings(),
                job = GetActiveJob(),
                htmlRoot = HtmlRoot
            };
        }

        object DescribeNavHealth()
        {
            return new
            {
                vendor = DescribeVendorScripts(),
                sprites = DescribeSprites(),
                fonts = DescribeFonts(),
                pmtiles = DescribePmtilesServing(),
                mirrors = GetMirrorHtmlRoots().Select(r => new
                {
                    path = r,
                    hasActiveEts2 = File.Exists(Path.Combine(r, "ets2.pmtiles")),
                    hasActiveAts = File.Exists(Path.Combine(r, "ats.pmtiles"))
                }).ToList()
            };
        }

        static object DescribeVendorScripts()
        {
            var vendor = Path.Combine(HtmlRoot, "scripts", "vendor");
            string Size(string name)
            {
                var p = Path.Combine(vendor, name);
                return File.Exists(p) ? new FileInfo(p).Length.ToString() : null;
            }
            return new
            {
                maplibreGl = File.Exists(Path.Combine(vendor, "maplibre-gl.js")),
                maplibreGlBytes = Size("maplibre-gl.js"),
                pmtiles = File.Exists(Path.Combine(vendor, "pmtiles.js")),
                pmtilesBytes = Size("pmtiles.js"),
                proj4 = File.Exists(Path.Combine(vendor, "proj4.js")),
                proj4Bytes = Size("proj4.js"),
                mapScript = File.Exists(Path.Combine(HtmlRoot, "scripts", "truckdeck-pmtiles-map.js"))
            };
        }

        static object DescribeSprites()
        {
            var dir = Path.Combine(HtmlRoot, "maps", "sprites");
            return new
            {
                json = File.Exists(Path.Combine(dir, "sprites.json")),
                png = File.Exists(Path.Combine(dir, "sprites.png")),
                json2x = File.Exists(Path.Combine(dir, "sprites@2x.json")),
                png2x = File.Exists(Path.Combine(dir, "sprites@2x.png"))
            };
        }

        static object DescribeFonts()
        {
            var fontDir = Path.Combine(HtmlRoot, "maps", "fonts", "Noto Sans Regular");
            var sample = Path.Combine(fontDir, "0-255.pbf");
            return new
            {
                localGlyphs = File.Exists(sample),
                samplePath = sample,
                rangeCount = Directory.Exists(fontDir)
                    ? Directory.GetFiles(fontDir, "*.pbf").Length
                    : 0
            };
        }

        static object DescribePmtilesServing()
        {
            var active = Path.Combine(HtmlRoot, "ets2.pmtiles");
            var generated = Path.Combine(HtmlRoot, "maps", "generated", "ets2.pmtiles");
            FileInfo Info(string path) => File.Exists(path) ? new FileInfo(path) : null;
            var activeInfo = Info(active);
            var genInfo = Info(generated);
            return new
            {
                middleware = true,
                activeExists = activeInfo != null,
                activeBytes = activeInfo?.Length ?? 0L,
                generatedExists = genInfo != null,
                generatedBytes = genInfo?.Length ?? 0L,
                acceptRanges = "bytes"
            };
        }

        object DescribeWslTools(string distro)
        {
            if (string.IsNullOrEmpty(distro))
            {
                return new
                {
                    node = new { available = false, version = (string)null },
                    git = new { available = false, version = (string)null },
                    tippecanoe = new { available = false, version = (string)null },
                    mapTools = new { installed = false, path = WslHelper.WslMapToolsRoot }
                };
            }

            return new
            {
                node = WslHelper.ProbeWslCommand(distro, "node"),
                git = WslHelper.ProbeWslCommand(distro, "git"),
                tippecanoe = WslHelper.ProbeWslCommand(distro, "tippecanoe"),
                mapTools = new
                {
                    installed = WslHelper.IsWslMapToolsInstalled(distro),
                    path = WslHelper.WslMapToolsRoot
                }
            };
        }

        object DescribeNativeTools()
        {
            return new
            {
                node = ProbeCommand("node", "--version"),
                git = ProbeCommand("git", "--version"),
                tippecanoe = ProbeCommand("tippecanoe", "--version"),
                mapTools = new
                {
                    installed = Directory.Exists(MapToolsRoot) &&
                                Directory.Exists(Path.Combine(MapToolsRoot, "node_modules")),
                    path = MapToolsRoot
                }
            };
        }

        static bool NativeTippecanoeAvailable()
        {
            try
            {
                string output, error;
                return ProcessHelper.RunAndWait(out output, out error, "tippecanoe", "--version", 8) == 0;
            }
            catch
            {
                return false;
            }
        }

        static object ProbeCommand(string exe, string args)
        {
            try
            {
                string output, error;
                var code = ProcessHelper.RunAndWait(out output, out error, exe, args, 8);
                var version = (output + error).Trim().Split('\n').FirstOrDefault()?.Trim();
                return new { available = code == 0, version };
            }
            catch
            {
                return new { available = false, version = (string)null };
            }
        }

        public object DescribeSettings()
        {
            var ets2 = Settings.Instance.Ets2GamePath;
            var ats = Settings.Instance.AtsGamePath;
            return new
            {
                ets2GamePath = ets2,
                atsGamePath = ats,
                ets2Valid = IsValidGamePath(ets2),
                atsValid = IsValidGamePath(ats),
                mapGenerationBackend = Settings.Instance.MapGenerationBackend ?? "wsl",
                wslInstallPath = Settings.Instance.WslInstallPath
            };
        }

        public bool UpdateSettings(string ets2Path, string atsPath, string backend, string wslInstallPath)
        {
            if (!string.IsNullOrWhiteSpace(ets2Path))
                Settings.Instance.Ets2GamePath = ets2Path.Trim().TrimEnd('\\');
            if (!string.IsNullOrWhiteSpace(atsPath))
                Settings.Instance.AtsGamePath = atsPath.Trim().TrimEnd('\\');
            if (!string.IsNullOrWhiteSpace(backend))
                Settings.Instance.MapGenerationBackend = backend.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(wslInstallPath))
                Settings.Instance.WslInstallPath = wslInstallPath.Trim().TrimEnd('\\');
            Settings.Instance.Save();
            return true;
        }

        public object ValidatePath(string game, string path)
        {
            var valid = IsValidGamePath(path);
            return new
            {
                game,
                path,
                valid,
                message = valid
                    ? "Game folder looks valid (base.scs found)."
                    : "Invalid folder — base.scs not found."
            };
        }

        public MapGenerationJob StartSetupTools()
        {
            var backend = GetRecommendedBackend();
            if (backend == "wsl")
            {
                var distro = WslHelper.GetDefaultDistro();
                if (string.IsNullOrEmpty(distro))
                    throw new InvalidOperationException("No WSL distro found. Install WSL first.");

                return StartScript("setup", null, "setup_map_tools_wsl.ps1", new Dictionary<string, string>
                {
                    ["Distro"] = distro
                });
            }
            if (backend == "native")
                return StartScript("setup", null, "setup_map_tools.ps1", new Dictionary<string, string>());
            throw new InvalidOperationException("WSL is not installed. Use Install WSL first.");
        }

        public MapGenerationJob StartGenerate(string game, bool activate)
        {
            var path = game == "ats"
                ? Settings.Instance.AtsGamePath
                : Settings.Instance.Ets2GamePath;

            if (!IsValidGamePath(path))
                throw new InvalidOperationException($"Invalid {game.ToUpper()} game path. Set it in step 2.");

            var backend = GetRecommendedBackend();
            var args = new Dictionary<string, string>
            {
                ["Game"] = game,
                ["GamePath"] = path,
                ["HtmlRoot"] = HtmlRoot
            };
            if (activate)
                args["Activate"] = "true";

            if (backend == "wsl")
            {
                var distro = WslHelper.GetDefaultDistro();
                if (string.IsNullOrEmpty(distro))
                    throw new InvalidOperationException("No WSL distro found.");

                args["Distro"] = distro;
                return StartScript("generate", game, "generate_pmtiles_wsl.ps1", args);
            }
            if (backend == "native")
                return StartScript("generate", game, "generate_pmtiles.ps1", args);
            throw new InvalidOperationException("Map tools not ready. Install WSL and map tools first.");
        }

        public MapGenerationJob StartWslInstall(string installPath)
        {
            if (string.IsNullOrWhiteSpace(installPath))
                throw new ArgumentException("installPath is required.");

            Settings.Instance.WslInstallPath = installPath.Trim().TrimEnd('\\');
            Settings.Instance.MapGenerationBackend = "wsl";
            Settings.Instance.Save();

            var args = new Dictionary<string, string>
            {
                ["InstallPath"] = Settings.Instance.WslInstallPath
            };

            if (Uac.IsProcessElevated())
                return StartScript("wsl-install", null, "install_wsl.ps1", args);

            return StartElevatedScript("wsl-install", null, "install_wsl.ps1", args);
        }

        public bool ActivateMap(string game)
        {
            var name = game == "ats" ? "ats.pmtiles" : "ets2.pmtiles";
            var source = Path.Combine(HtmlRoot, "maps", "generated", name);
            if (!File.Exists(source))
                throw new FileNotFoundException("Generated map not found. Run generation first.", source);

            CopyActivatedMapFiles(game);
            return true;
        }

        /// <summary>Copies the .pmtiles plus any generated NAV routing graph/city sidecar files (whichever exist) to Html root + mirrors.</summary>
        void CopyActivatedMapFiles(string game)
        {
            var roots = new List<string> { HtmlRoot };
            foreach (var mirror in GetMirrorHtmlRoots())
                roots.Add(mirror);

            foreach (var htmlRoot in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.Combine(htmlRoot, "maps", "generated"));
                foreach (var name in GetSidecarFileNames(game))
                {
                    var sourceFile = Path.Combine(HtmlRoot, "maps", "generated", name);
                    if (!File.Exists(sourceFile))
                        continue;

                    var destGenerated = Path.Combine(htmlRoot, "maps", "generated", name);
                    if (!string.Equals(Path.GetFullPath(sourceFile), Path.GetFullPath(destGenerated), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(sourceFile, destGenerated, true);
                    }

                    var destRoot = Path.Combine(htmlRoot, name);
                    if (!string.Equals(Path.GetFullPath(sourceFile), Path.GetFullPath(destRoot), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(sourceFile, destRoot, true);
                    }
                    
                    Log.InfoFormat("Activated {0} in {1}", name, htmlRoot);
                }
            }
        }

        /// <summary>Steam Telemetry Server Html next to the game install (common layout).</summary>
        static IEnumerable<string> GetMirrorHtmlRoots()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var own = Path.GetFullPath(HtmlRoot);

            foreach (var gamePath in new[] { Settings.Instance.Ets2GamePath, Settings.Instance.AtsGamePath })
            {
                if (string.IsNullOrWhiteSpace(gamePath))
                    continue;

                var gameDir = gamePath.Trim().TrimEnd('\\');
                if (!Directory.Exists(gameDir))
                    continue;

                var candidates = new[]
                {
                    Path.Combine(gameDir, "Telemetry Server", "Html"),
                    Path.Combine(Path.GetDirectoryName(gameDir) ?? "", "Telemetry Server", "Html")
                };

                foreach (var html in candidates)
                {
                    if (string.IsNullOrWhiteSpace(html) || !Directory.Exists(html))
                        continue;
                    var full = Path.GetFullPath(html);
                    if (string.Equals(full, own, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (seen.Add(full))
                        yield return full;
                }
            }
        }

        MapGenerationJob StartScript(string kind, string game, string scriptName, Dictionary<string, string> scriptArgs)
        {
            var scriptPath = Path.Combine(MapsDir, scriptName);
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script not found: " + scriptPath);

            var argList = new StringBuilder();
            argList.Append("-NoProfile -ExecutionPolicy Bypass -File \"")
                .Append(scriptPath)
                .Append("\"");

            return StartJob(kind, game, scriptPath, argList, scriptArgs, elevated: false);
        }

        MapGenerationJob StartElevatedScript(string kind, string game, string scriptName, Dictionary<string, string> scriptArgs)
        {
            var scriptPath = Path.Combine(MapsDir, scriptName);
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script not found: " + scriptPath);

            lock (_sync)
            {
                if (_runningProcess != null && !_runningProcess.HasExited)
                    throw new InvalidOperationException("Another map job is already running.");

                var jobId = Guid.NewGuid().ToString("N");
                var logPath = Path.Combine(Settings.SettingsDirectory, "map-jobs", jobId + ".log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? Settings.SettingsDirectory);
                File.WriteAllText(logPath, "", Encoding.UTF8);

                var argList = new StringBuilder();
                argList.Append("-NoProfile -ExecutionPolicy Bypass -File \"").Append(scriptPath).Append("\"");
                argList.Append(" -LogFile \"").Append(logPath).Append("\"");
                foreach (var kv in scriptArgs)
                {
                    if (kv.Key == "Activate") { argList.Append(" -Activate"); continue; }
                    argList.Append(" -").Append(kv.Key).Append(" \"").Append(kv.Value.Replace("\"", "`\"")).Append("\"");
                }

                var launcherPath = Path.Combine(Path.GetTempPath(), "truckdeck-elevated-" + jobId + ".ps1");
                File.WriteAllText(launcherPath,
                    "Start-Process powershell -Verb RunAs -Wait -ArgumentList '" +
                    argList.ToString().Replace("'", "''") + "'\r\n", Encoding.UTF8);

                var job = new MapGenerationJob
                {
                    Id = jobId,
                    Kind = kind,
                    Game = game,
                    Status = "running",
                    Progress = 0,
                    Message = "Waiting for Administrator approval...",
                    LogTail = "",
                    StartedAt = DateTime.UtcNow
                };
                _jobs[jobId] = job;
                _jobLogLineCount[jobId] = 0;
                _runningLogPath = logPath;

                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + launcherPath + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var process = Process.Start(psi);
                _runningProcess = process;
                process.OutputDataReceived += (_, e) => AppendProcessLine(jobId, e.Data, appendToLog: false);
                process.ErrorDataReceived += (_, e) => AppendProcessLine(jobId, e.Data, appendToLog: false);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _monitorThread = new Thread(() =>
                {
                    try
                    {
                        MonitorJob(jobId, process, logPath);
                    }
                    finally
                    {
                        try { File.Delete(launcherPath); } catch { /* ignore */ }
                    }
                })
                {
                    IsBackground = true,
                    Name = "MapGeneration-" + jobId
                };
                _monitorThread.Start();

                return CloneJob(job);
            }
        }

        MapGenerationJob StartJob(string kind, string game, string scriptPath, StringBuilder argList, Dictionary<string, string> scriptArgs, bool elevated)
        {
            if (elevated)
                throw new InvalidOperationException("Use StartElevatedScript for elevated jobs.");

            lock (_sync)
            {
                if (_runningProcess != null && !_runningProcess.HasExited)
                    throw new InvalidOperationException("Another map job is already running.");

                var jobId = Guid.NewGuid().ToString("N");
                var logPath = Path.Combine(Settings.SettingsDirectory, "map-jobs", jobId + ".log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? Settings.SettingsDirectory);
                File.WriteAllText(logPath, "", Encoding.UTF8);

                argList.Append(" -LogFile \"").Append(logPath).Append("\"");
                foreach (var kv in scriptArgs)
                {
                    if (kv.Key == "Activate")
                    {
                        argList.Append(" -Activate");
                        continue;
                    }
                    argList.Append(" -").Append(kv.Key).Append(" \"")
                        .Append(kv.Value.Replace("\"", "`\""))
                        .Append("\"");
                }

                var job = new MapGenerationJob
                {
                    Id = jobId,
                    Kind = kind,
                    Game = game,
                    Status = "running",
                    Progress = 0,
                    Message = "Starting...",
                    LogTail = "",
                    StartedAt = DateTime.UtcNow,
                    Activated = scriptArgs != null && scriptArgs.ContainsKey("Activate")
                };
                _jobs[jobId] = job;
                _jobLogLineCount[jobId] = 0;
                _runningLogPath = logPath;

                Process process;
                var psi = new ProcessStartInfo("powershell.exe", argList.ToString())
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                process = Process.Start(psi);

                _runningProcess = process;

                process.OutputDataReceived += (_, e) => AppendProcessLine(jobId, e.Data, appendToLog: false);
                process.ErrorDataReceived += (_, e) => AppendProcessLine(jobId, e.Data, appendToLog: false);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _monitorThread = new Thread(() => MonitorJob(jobId, process, logPath))
                {
                    IsBackground = true,
                    Name = "MapGeneration-" + jobId
                };
                _monitorThread.Start();

                return CloneJob(job);
            }
        }

        void MonitorJob(string jobId, Process process, string logPath)
        {
            try
            {
                while (!process.HasExited)
                {
                    PollLogFile(jobId, logPath);
                    Thread.Sleep(800);
                }

                PollLogFile(jobId, logPath);

                lock (_sync)
                {
                    if (!_jobs.TryGetValue(jobId, out var job))
                        return;

                    job.ExitCode = process.ExitCode;
                    job.CompletedAt = DateTime.UtcNow;
                    job.LogTail = ReadLogSummary(logPath);

                    if (IsJobSuccessful(job, logPath))
                    {
                        job.Status = "completed";
                        if (job.Progress < 100)
                            job.Progress = 100;
                        FinalizeSuccessfulJob(job, logPath);
                    }
                    else
                    {
                        job.Status = "failed";
                        if (string.IsNullOrWhiteSpace(job.Message) || job.Message == "Starting...")
                            job.Message = ExtractJobErrorMessage(logPath) ?? "Job failed. See log for details.";
                    }

                    _runningProcess = null;
                    _jobLogLineCount.Remove(jobId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                lock (_sync)
                {
                    if (_jobs.TryGetValue(jobId, out var job))
                    {
                        job.Status = "failed";
                        job.Message = ex.Message;
                        job.CompletedAt = DateTime.UtcNow;
                    }
                    _runningProcess = null;
                    _jobLogLineCount.Remove(jobId);
                }
            }
        }

        void PollLogFile(string jobId, string logPath)
        {
            var lines = ReadLogLinesShared(logPath);
            if (lines == null)
                return;

            int start;
            lock (_sync)
            {
                start = _jobLogLineCount.TryGetValue(jobId, out var count) ? count : 0;
            }

            for (var i = start; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    AppendProcessLine(jobId, lines[i], appendToLog: true);
            }

            lock (_sync)
                _jobLogLineCount[jobId] = lines.Length;
        }

        void AppendProcessLine(string jobId, string line, bool appendToLog)
        {
            if (string.IsNullOrEmpty(line))
                return;

            lock (_sync)
            {
                if (!_jobs.TryGetValue(jobId, out var job))
                    return;

                var progress = ProgressRegex.Match(line);
                if (progress.Success)
                {
                    job.Progress = int.Parse(progress.Groups[1].Value);
                    job.Message = progress.Groups[2].Value.Trim();
                }

                var done = DoneRegex.Match(line);
                if (done.Success)
                    job.OutputPath = done.Groups[1].Value.Trim();

                if (appendToLog)
                    job.LogTail = AppendLogLine(job.LogTail, line);
            }
        }

        static string AppendLogLine(string current, string line, int maxChars = 12000)
        {
            var next = string.IsNullOrEmpty(current) ? line : current + Environment.NewLine + line;
            if (next.Length <= maxChars)
                return next;
            return next.Substring(next.Length - maxChars);
        }

        static string[] ReadLogLinesShared(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    var text = reader.ReadToEnd();
                    if (string.IsNullOrEmpty(text))
                        return new string[0];
                    return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                }
            }
            catch (IOException ex)
            {
                Log.Warn("Could not read map job log: " + path, ex);
                return null;
            }
        }

        static bool IsJobSuccessful(MapGenerationJob job, string logPath)
        {
            if (job.ExitCode != 0)
                return false;

            var log = ReadLogTail(logPath, 20000);
            if (LogContainsErrorLine(log))
                return false;

            if (DoneRegex.IsMatch(log))
                return true;

            if (job.Kind == "generate" && !string.IsNullOrEmpty(job.Game))
            {
                var name = job.Game == "ats" ? "ats.pmtiles" : "ets2.pmtiles";
                if (File.Exists(Path.Combine(HtmlRoot, "maps", "generated", name)))
                    return true;
            }

            return false;
        }

        void FinalizeSuccessfulJob(MapGenerationJob job, string logPath)
        {
            var log = ReadLogTail(logPath, 20000);
            var done = DoneRegex.Match(log);
            if (done.Success)
                job.OutputPath = done.Groups[1].Value.Trim();

            if (job.Kind == "generate" && job.Activated && !string.IsNullOrEmpty(job.Game))
            {
                try
                {
                    ActivateMap(job.Game);
                    var name = job.Game == "ats" ? "ats.pmtiles" : "ets2.pmtiles";
                    job.OutputPath = Path.Combine(HtmlRoot, "maps", "generated", name);
                    var mirrors = GetMirrorHtmlRoots().ToList();
                    job.Message = mirrors.Count > 0
                        ? $"Map activated ({name}) — also copied to Steam Telemetry Server."
                        : $"Map activated ({name}). Hard-refresh the dashboard (Ctrl+F5).";
                }
                catch (Exception ex)
                {
                    job.Status = "failed";
                    job.Message = ex.Message;
                    Log.Error(ex);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(job.Message) || job.Message == "Starting...")
                job.Message = "Completed successfully.";
        }

        static bool LogContainsErrorLine(string log)
        {
            if (string.IsNullOrEmpty(log))
                return false;

            foreach (var line in log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        static string ExtractJobErrorMessage(string logPath)
        {
            var log = ReadLogTail(logPath, 20000);
            if (string.IsNullOrEmpty(log))
                return null;

            var lines = log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                var idx = line.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    return line.Substring(idx).Trim();
            }
            return null;
        }

        static string ReadLogSummary(string path, int maxLines = 40)
        {
            var lines = ReadLogLinesShared(path);
            if (lines == null || lines.Length == 0)
                return "";

            var important = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.IndexOf("TRUCKDECK_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Activated map:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Wrote ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Done.", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    important.Add(line);
                }
            }

            IEnumerable<string> tail = important.Count > 0
                ? important.Skip(Math.Max(0, important.Count - maxLines))
                : lines.Where(l => !string.IsNullOrWhiteSpace(l)).Skip(Math.Max(0, lines.Length - maxLines));

            return string.Join(Environment.NewLine, tail);
        }

        static string ReadLogTail(string path, int maxChars = 12000)
        {
            if (!File.Exists(path))
                return "";

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    var text = reader.ReadToEnd();
                    if (text.Length <= maxChars)
                        return text;
                    return text.Substring(text.Length - maxChars);
                }
            }
            catch (IOException ex)
            {
                Log.Warn("Could not read map job log tail: " + path, ex);
                return "";
            }
        }

        static object DescribeOutput(string game)
        {
            var name = game == "ats" ? "ats.pmtiles" : "ets2.pmtiles";
            var generated = Path.Combine(HtmlRoot, "maps", "generated", name);
            var active = Path.Combine(HtmlRoot, name);
            return new
            {
                generated = File.Exists(generated),
                active = File.Exists(active),
                generatedPath = generated,
                activePath = active,
                sizeBytes = File.Exists(generated) ? new FileInfo(generated).Length : 0L,
                activeSizeBytes = File.Exists(active) ? new FileInfo(active).Length : 0L,
                generatedAt = File.Exists(generated) ? File.GetLastWriteTimeUtc(generated) : (DateTime?)null,
                hasRouting = File.Exists(Path.Combine(HtmlRoot, "maps", "generated", game + "-graph.json")) &&
                             File.Exists(Path.Combine(HtmlRoot, "maps", "generated", game + "-cities.json"))
            };
        }

        public static bool IsValidGamePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "N/A")
                return false;
            return File.Exists(Path.Combine(path.Trim().TrimEnd('\\'), "base.scs"));
        }

        public static string DetectSteamGamePath(string game)
        {
            return game == "ats"
                ? SteamGamePathHelper.DetectAtsPath()
                : SteamGamePathHelper.DetectEts2Path();
        }

        static MapGenerationJob CloneJob(MapGenerationJob job)
        {
            return new MapGenerationJob
            {
                Id = job.Id,
                Kind = job.Kind,
                Game = job.Game,
                Status = job.Status,
                Progress = job.Progress,
                Message = job.Message,
                LogTail = job.LogTail,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                OutputPath = job.OutputPath,
                Activated = job.Activated,
                ExitCode = job.ExitCode
            };
        }
    }
}
