using System;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public class MapGenerationJob
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Game { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string Message { get; set; }
        public string LogTail { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string OutputPath { get; set; }
        public bool Activated { get; set; }
        public int ExitCode { get; set; }
    }
}
