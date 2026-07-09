using System;
using System.Text;
using System.Threading.Tasks;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Services
{
    /// <summary>
    /// Feedback/review flow — deliberately thin. There is no in-app eligibility check,
    /// countdown, or dialog: the app just opens a signed link to truckdeck.site, and the
    /// website (which owns the review form, one-review-per-install enforcement, and
    /// moderation) handles everything else. See build/AGENT_HANDOFF_REVIEW_LINK.md.
    /// </summary>
    public static class ReviewService
    {
        /// <summary>
        /// Registers the install (if not already) and opens the signed feedback link in the
        /// default browser. Safe to call at any time — no usage threshold, no round trip
        /// required before opening the browser.
        /// </summary>
        public static async Task OpenFeedbackPageAsync()
        {
            try { await TruckDeckApiClient.RegisterInstallAsync().ConfigureAwait(false); }
            catch { /* best effort — the site can still show a friendly error if unregistered */ }

            ProcessHelper.OpenUrl(BuildReviewUrl());
        }

        /// <summary>
        /// Builds https://truckdeck.site/review?install_id=..&amp;ts=..&amp;sig=..&amp;key=.. — the
        /// site verifies the signature against the install's stored key hash (from /register)
        /// and shows the review form, or a "you've already reviewed" page if one exists for
        /// this install_id (enforced server-side, one row per install_id). No usage-threshold
        /// gate and no prior round trip from the app — see build/AGENT_HANDOFF_REVIEW_LINK.md.
        /// </summary>
        public static string BuildReviewUrl()
        {
            var installId = InstallIdentityService.InstallId;
            var installKey = InstallIdentityService.InstallKey;
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var sig = TruckDeckApiClient.Sign(installKey, ts, installId);
            var keyB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(installKey));
            var baseUrl = TruckDeckApiClient.ApiBase.TrimEnd('/');
            return $"{baseUrl}/review?install_id={Uri.EscapeDataString(installId)}&ts={ts}&sig={sig}&key={Uri.EscapeDataString(keyB64)}";
        }
    }
}
