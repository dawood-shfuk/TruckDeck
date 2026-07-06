using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Services;

namespace Funbit.Ets.Telemetry.Server.Controllers
{
    [RoutePrefix("api/maps")]
    public class MapGenerationController : ApiController
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly MapGenerationService _maps = MapGenerationService.Instance;

        [HttpGet]
        [Route("status")]
        public HttpResponseMessage GetStatus() => OkCors(_maps.GetStatus());

        [HttpGet]
        [Route("settings")]
        public HttpResponseMessage GetSettings() => OkCors(_maps.DescribeSettings());

        [HttpPost]
        [Route("settings")]
        public HttpResponseMessage SaveSettings([FromBody] GamePathsRequest body)
        {
            if (body == null)
                return ErrorCors("Missing body.");
            _maps.UpdateSettings(body.ets2GamePath, body.atsGamePath, body.mapGenerationBackend, body.wslInstallPath);
            return OkCors(_maps.DescribeSettings());
        }

        [HttpPost]
        [Route("validate-path")]
        public HttpResponseMessage ValidatePath([FromBody] ValidatePathRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.path))
                return ErrorCors("path is required.");
            return OkCors(_maps.ValidatePath(body.game ?? "ets2", body.path));
        }

        [HttpPost]
        [Route("detect-path")]
        public HttpResponseMessage DetectPath([FromBody] GameOnlyRequest body)
        {
            var game = body?.game ?? "ets2";
            var path = MapGenerationService.DetectSteamGamePath(game);
            return OkCors(new
            {
                game,
                path,
                valid = MapGenerationService.IsValidGamePath(path)
            });
        }

        [HttpPost]
        [Route("browse-path")]
        public HttpResponseMessage BrowsePath([FromBody] BrowsePathRequest body)
        {
            string title;
            if (body?.purpose == "wsl")
            {
                title = "Select folder for WSL install (e.g. D:\\WSL)";
            }
            else
            {
                var game = body?.game ?? "ets2";
                title = game == "ats"
                    ? "Select American Truck Simulator folder"
                    : "Select Euro Truck Simulator 2 folder";
            }

            var selected = UiBridge.PickFolder(title);
            if (string.IsNullOrWhiteSpace(selected))
                return OkCors(new { cancelled = true });

            if (body?.purpose == "wsl")
            {
                return OkCors(new { cancelled = false, path = selected, purpose = "wsl" });
            }

            var gameName = body?.game ?? "ets2";
            return OkCors(new
            {
                cancelled = false,
                game = gameName,
                path = selected,
                valid = MapGenerationService.IsValidGamePath(selected)
            });
        }

        [HttpPost]
        [Route("install-wsl")]
        public HttpResponseMessage InstallWsl([FromBody] WslInstallRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.installPath))
                return ErrorCors("installPath is required (e.g. D:\\WSL).");

            try
            {
                if (_maps.GetActiveJob() != null)
                    return ErrorCors("A job is already running.");
                return OkCors(_maps.StartWslInstall(body.installPath));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ErrorCors(ex.Message);
            }
        }

        [HttpPost]
        [Route("setup-tools")]
        public HttpResponseMessage SetupTools()
        {
            try
            {
                if (_maps.GetActiveJob() != null)
                    return ErrorCors("A job is already running.");
                return OkCors(_maps.StartSetupTools());
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ErrorCors(ex.Message);
            }
        }

        [HttpPost]
        [Route("generate")]
        public HttpResponseMessage Generate([FromBody] GenerateRequest body)
        {
            if (body?.games == null || body.games.Length == 0)
                return ErrorCors("Select at least one game.");

            try
            {
                if (_maps.GetActiveJob() != null)
                    return ErrorCors("A job is already running.");

                var game = body.games.First().ToLowerInvariant();
                if (game != "ets2" && game != "ats")
                    return ErrorCors("games must contain 'ets2' and/or 'ats'.");

                return OkCors(_maps.StartGenerate(game, body.activate));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ErrorCors(ex.Message);
            }
        }

        [HttpGet]
        [Route("jobs/{id}")]
        public HttpResponseMessage GetJob(string id)
        {
            var job = _maps.GetJob(id);
            if (job == null)
                return ErrorCors("Job not found.", HttpStatusCode.NotFound);
            return OkCors(job);
        }

        [HttpGet]
        [Route("jobs")]
        public HttpResponseMessage GetJobs() => OkCors(_maps.GetActiveJob());

        [HttpPost]
        [Route("activate")]
        public HttpResponseMessage Activate([FromBody] GameOnlyRequest body)
        {
            var game = body?.game ?? "ets2";
            try
            {
                _maps.ActivateMap(game);
                return OkCors(new { ok = true, game });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ErrorCors(ex.Message);
            }
        }

        [HttpOptions]
        [Route("{*catchAll}")]
        public HttpResponseMessage Options() => CorsPreflight();

        HttpResponseMessage OkCors(object payload)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK, payload);
            AddCors(response);
            return response;
        }

        HttpResponseMessage ErrorCors(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        {
            var response = Request.CreateResponse(code, new { error = message });
            AddCors(response);
            return response;
        }

        static void AddCors(HttpResponseMessage response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        }

        HttpResponseMessage CorsPreflight()
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);
            AddCors(response);
            return response;
        }

        public class GamePathsRequest
        {
            public string ets2GamePath { get; set; }
            public string atsGamePath { get; set; }
            public string mapGenerationBackend { get; set; }
            public string wslInstallPath { get; set; }
        }

        public class ValidatePathRequest
        {
            public string game { get; set; }
            public string path { get; set; }
        }

        public class GameOnlyRequest
        {
            public string game { get; set; }
        }

        public class BrowsePathRequest
        {
            public string game { get; set; }
            public string purpose { get; set; }
        }

        public class WslInstallRequest
        {
            public string installPath { get; set; }
        }

        public class GenerateRequest
        {
            public string[] games { get; set; }
            public bool activate { get; set; }
        }
    }
}
