using System;
using System.Configuration;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public static class TruckDeckApiClient
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        public static string ApiBase =>
            ConfigurationManager.AppSettings["TruckDeckApiBase"] ?? "https://truckdeck.site";

        public static async Task<bool> RegisterInstallAsync()
        {
            InstallIdentityService.EnsureIdentity();
            var body = new
            {
                install_id = InstallIdentityService.InstallId,
                install_key = InstallIdentityService.InstallKey,
                platform = "windows",
                app_version = AssemblyHelper.Version,
            };
            var json = JsonConvert.SerializeObject(body);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                try
                {
                    var res = await Http.PostAsync(ApiBase.TrimEnd('/') + "/api/v1/reviews/register", content)
                        .ConfigureAwait(false);
                    if (!res.IsSuccessStatusCode)
                    {
                        var respBody = "";
                        try { respBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { /* ignore */ }
                        Log.Warn($"Install registration rejected: {(int)res.StatusCode} {respBody}");
                    }
                    return res.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Log.Warn("Install registration failed: " + ex.Message);
                    return false;
                }
            }
        }

        public static async Task<HttpResponseMessage> PostSignedAsync(string path, object payload)
        {
            var registered = await RegisterInstallAsync().ConfigureAwait(false);
            if (!registered)
                Log.Warn($"Proceeding to {path} despite failed (re)registration — server may reject with 403");
            var json = JsonConvert.SerializeObject(payload);
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var sig = Sign(InstallIdentityService.InstallKey, ts, json);
            var keyB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(InstallIdentityService.InstallKey));

            using (var req = new HttpRequestMessage(HttpMethod.Post, ApiBase.TrimEnd('/') + path))
            {
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                req.Headers.Add("X-Install-Id", InstallIdentityService.InstallId);
                req.Headers.Add("X-Timestamp", ts);
                req.Headers.Add("X-Signature", sig);
                req.Headers.Add("X-Install-Key", keyB64);
                return await Http.SendAsync(req);
            }
        }

        public static string Sign(string installKey, string timestamp, string json)
        {
            var msg = Encoding.UTF8.GetBytes(timestamp + "\n" + json);
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(installKey)))
                return BitConverter.ToString(hmac.ComputeHash(msg)).Replace("-", "").ToLowerInvariant();
        }
    }
}
