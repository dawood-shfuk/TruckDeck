using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Funbit.Ets.Telemetry.Server.Controllers
{
    [RoutePrefix("")]
    public class Ets2AppController : StaticFileController
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public const string TelemetryAppUriPath = "/";

        [HttpGet, HttpHead]
        [Route("config.json", Name = "GetSkinConfig")]
        public HttpResponseMessage GetSkinConfig()
        {
            var skins = new JArray();
            foreach (var skinDir in EnumerateDirectories("skins"))
            {
                var folderName = Path.GetFileName(skinDir);
                if (string.Equals(folderName, "FUNBITskins", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var nested in EnumerateDirectories("skins/FUNBITskins"))
                        TryAddSkinConfig(skins, nested, "FUNBITskins", "Original Funbit skins");
                    continue;
                }
                TryAddSkinConfig(skins, skinDir, null, "TruckDeck skins");
            }
            var config = new { version = AssemblyHelper.Version, skins };
            return Request.CreateResponse(HttpStatusCode.OK, config, new JsonMediaTypeFormatter());
        }

        static void TryAddSkinConfig(JArray skins, string skinDir, string pathPrefix, string group)
        {
            var configJsonPath = Path.Combine(skinDir, "config.json");
            if (!File.Exists(configJsonPath))
                return;
            try
            {
                var skinConfigRoot = (JObject)JsonConvert.DeserializeObject(
                    File.ReadAllText(configJsonPath, Encoding.UTF8));
                var skinConfig = (JObject)skinConfigRoot["config"];
                if (skinConfig == null)
                    return;
                var skinName = Path.GetFileName(skinDir);
                if (!string.IsNullOrEmpty(pathPrefix))
                    skinConfig["name"] = pathPrefix + "/" + skinName;
                skinConfig["group"] = group;
                skins.Add(skinConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        [HttpGet, HttpHead]
        [Route("", Name = "GetRoot")]
        public HttpResponseMessage GetRoot()
        {
            return ServeStaticFile("", "index.html");
        }

        [HttpGet, HttpHead]
        [Route("{fileName:regex(^(?!.*api))}", Name = "GetRootFile")]
        public HttpResponseMessage GetRootFile(
            string fileName)
        {
            return ServeStaticFile("", fileName);
        }

        // we support up to 5 directory levels with the root directory not containing "api" prefix

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{fileName}", Name = "GetResourceFile1")]
        public HttpResponseMessage GetResourceFile1(
            string dirName1, string fileName)
        {
            return ServeStaticFile(dirName1, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{fileName}", Name = "GetResourceFile2")]
        public HttpResponseMessage GetResourceFile2(
            string dirName1, string dirName2, string fileName)
        {
            return ServeStaticFile(dirName1 + 
                "/" + dirName2, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{fileName}", Name = "GetResourceFile3")]
        public HttpResponseMessage GetResourceFile3(
            string dirName1, string dirName2, string dirName3, string fileName)
        {
            return ServeStaticFile(dirName1 + 
                "/" + dirName2 + 
                "/" + dirName3, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{dirName4}/{fileName}", Name = "GetResourceFile4")]
        public HttpResponseMessage GetResourceFile4(
            string dirName1, string dirName2, string dirName3, string dirName4, string fileName)
        {
            return ServeStaticFile(dirName1 + 
                "/" + dirName2 + 
                "/" + dirName3 + 
                "/" + dirName4, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{dirName4}/{dirName5}/{fileName}", Name = "GetResourceFile5")]
        public HttpResponseMessage GetResourceFile5(
            string dirName1, string dirName2, string dirName3, string dirName4, 
            string dirName5, string fileName)
        {
            return ServeStaticFile(dirName1 + 
                "/" + dirName2 + 
                "/" + dirName3 + 
                "/" + dirName4 + 
                "/" + dirName5, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{dirName4}/{dirName5}/{dirName6}/{fileName}", Name = "GetResourceFile6")]
        public HttpResponseMessage GetResourceFile6(
            string dirName1, string dirName2, string dirName3, string dirName4, 
            string dirName5, string dirName6, string fileName)
        {
            return ServeStaticFile(dirName1 + 
                "/" + dirName2 + 
                "/" + dirName3 + 
                "/" + dirName4 + 
                "/" + dirName5 + 
                "/" + dirName6, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{dirName4}/{dirName5}/{dirName6}/{dirName7}/{fileName}", Name = "GetResourceFile7")]
        public HttpResponseMessage GetResourceFile7(
            string dirName1, string dirName2, string dirName3, string dirName4, 
            string dirName5, string dirName6, string dirName7, string fileName)
        {
            return ServeStaticFile(dirName1 + 
                "/" + dirName2 + 
                "/" + dirName3 + 
                "/" + dirName4 + 
                "/" + dirName5 + 
                "/" + dirName6 + 
                "/" + dirName7, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{dirName4}/{dirName5}/{dirName6}/{dirName7}/{dirName8}/{fileName}", Name = "GetResourceFile8")]
        public HttpResponseMessage GetResourceFile8(
            string dirName1, string dirName2, string dirName3, string dirName4,
            string dirName5, string dirName6, string dirName7, string dirName8, string fileName)
        {
            return ServeStaticFile(dirName1 +
                "/" + dirName2 +
                "/" + dirName3 +
                "/" + dirName4 +
                "/" + dirName5 +
                "/" + dirName6 +
                "/" + dirName7 +
                "/" + dirName8, fileName);
        }

        [HttpGet, HttpHead]
        [Route("{dirName1:regex(^(?!.*api))}/{dirName2}/{dirName3}/{dirName4}/{dirName5}/{dirName6}/{dirName7}/{dirName8}/{dirName9}/{fileName}", Name = "GetResourceFile9")]
        public HttpResponseMessage GetResourceFile9(
            string dirName1, string dirName2, string dirName3, string dirName4,
            string dirName5, string dirName6, string dirName7, string dirName8, string dirName9, string fileName)
        {
            return ServeStaticFile(dirName1 +
                "/" + dirName2 +
                "/" + dirName3 +
                "/" + dirName4 +
                "/" + dirName5 +
                "/" + dirName6 +
                "/" + dirName7 +
                "/" + dirName8 +
                "/" + dirName9, fileName);
        }
    }
}