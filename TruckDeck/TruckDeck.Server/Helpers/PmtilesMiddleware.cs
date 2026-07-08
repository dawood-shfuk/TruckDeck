using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    /// <summary>
    /// Serves .pmtiles with HTTP byte-range support. OWIN self-host does not always
    /// forward Range headers to Web API, which breaks the PMTiles JS client.
    /// </summary>
    public static class PmtilesMiddleware
    {
        const string HtmlRootName = "Html";

        public static async Task<bool> TryServeAsync(IOwinContext context)
        {
            var path = context.Request.Path.Value;
            if (string.IsNullOrEmpty(path) ||
                !path.EndsWith(".pmtiles", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
                return false;

            var absolutePath = MapRequestPathToFile(path);
            if (absolutePath == null || !File.Exists(absolutePath))
                return false;

            var fileLength = new FileInfo(absolutePath).Length;
            var response = context.Response;
            response.Headers.Set("Accept-Ranges", "bytes");
            response.ContentType = "application/vnd.pmtiles";

            var rangeHeader = context.Request.Headers.Get("Range");
            var isHead = string.Equals(context.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(rangeHeader))
            {
                RangeHeaderValue range;
                if (RangeHeaderValue.TryParse(rangeHeader, out range) && range.Ranges.Any())
                {
                    var item = range.Ranges.First();
                    long start = item.From ?? 0;
                    long end = item.To ?? fileLength - 1;

                    if (start < 0 || start >= fileLength)
                    {
                        response.StatusCode = 416;
                        response.Headers.Set("Content-Range", "bytes */" + fileLength);
                        return true;
                    }

                    if (end >= fileLength)
                        end = fileLength - 1;
                    if (end < start)
                        end = start;

                    var length = end - start + 1;
                    response.StatusCode = 206;
                    response.Headers.Set("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, fileLength));
                    response.Headers.Set("Content-Length", length.ToString());

                    if (!isHead)
                    {
                        using (var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Seek(start, SeekOrigin.Begin);
                            await CopyBytesAsync(fs, response.Body, length);
                        }
                    }

                    return true;
                }
            }

            response.StatusCode = 200;
            response.Headers.Set("Content-Length", fileLength.ToString());

            if (!isHead)
            {
                using (var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    await fs.CopyToAsync(response.Body);
            }

            return true;
        }

        static string MapRequestPathToFile(string requestPath)
        {
            var relative = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(relative))
                return null;
            if (relative.IndexOf("..", StringComparison.Ordinal) >= 0)
                return null;

            var htmlRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HtmlRootName));
            var candidate = Path.GetFullPath(Path.Combine(htmlRoot, relative));
            // Require trailing separator so Html\ets2.pmtiles is not rejected vs Html root.
            var rootPrefix = htmlRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? htmlRoot
                : htmlRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate, htmlRoot, StringComparison.OrdinalIgnoreCase))
                return null;
            return candidate;
        }

        static async Task CopyBytesAsync(Stream source, Stream destination, long count)
        {
            var buffer = new byte[64 * 1024];
            var remaining = count;
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = await source.ReadAsync(buffer, 0, toRead);
                if (read == 0)
                    break;
                await destination.WriteAsync(buffer, 0, read);
                remaining -= read;
            }
        }
    }
}
