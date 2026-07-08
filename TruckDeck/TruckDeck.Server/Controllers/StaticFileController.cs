using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Controllers
{
    public class StaticFileController : ApiController
    {
        const string BaseDirectory = "Html";

        protected HttpResponseMessage ServeStaticFile(string directory, string fileName)
        {
            // basic safety check (do not serve files outside base www directory)
            var path = directory + fileName;
            if (path.Contains("..") || path.Contains(":") || path.Contains("//") || path.Contains(@"\\"))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Page not found!");

            string extension = Path.GetExtension(fileName);
            string contentType;
            switch (extension)
            {
                case ".htm":
                case ".html":
                    {
                        contentType = "text/html";
                        break;
                    }
                case ".jpg":
                    {
                        contentType = "image/jpeg";
                        break;
                    }
                case ".png":
                    {
                        contentType = "image/png";
                        break;
                    }
                case ".gif":
                    {
                        contentType = "image/gif";
                        break;
                    }
                case ".css":
                    {
                        contentType = "text/css";
                        break;
                    }
                case ".js":
                    {
                        contentType = "application/javascript";
                        break;
                    }
                case ".json":
                    {
                        contentType = "application/json";
                        break;
                    }
                case ".woff":
                    {
                        contentType = "application/font-woff";
                        break;
                    }
                case ".ttf":
                    {
                        contentType = "application/font-sfnt";
                        break;
                    }
                case ".svg":
                    {
                        contentType = "image/svg+xml";
                        break;
                    }
                case ".pmtiles":
                    {
                        contentType = "application/vnd.pmtiles";
                        break;
                    }
                default:
                {
                    contentType = "application/octet-stream";
                    break;
                }
            }

            try
            {
                // Path.Combine ignores earlier segments when a later one is rooted;
                // also avoid empty directory segments. Normalize "/" to OS separators.
                string absoluteFileName = ResolveHtmlPath(directory, fileName);
                if (absoluteFileName == null || !File.Exists(absoluteFileName))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Page not found!");

                var fileLength = new FileInfo(absoluteFileName).Length;
                var rangeHeader = ParseRangeHeader();

                if (rangeHeader != null && rangeHeader.Ranges.Any())
                {
                    var range = rangeHeader.Ranges.First();
                    long start = range.From ?? 0;
                    long end = range.To ?? fileLength - 1;

                    if (start < 0 || start >= fileLength)
                    {
                        var invalid = Request.CreateResponse(HttpStatusCode.RequestedRangeNotSatisfiable);
                        invalid.Content = new StringContent("");
                        invalid.Content.Headers.ContentRange = new ContentRangeHeaderValue(fileLength);
                        invalid.Headers.AcceptRanges.Add("bytes");
                        return invalid;
                    }

                    if (end >= fileLength)
                        end = fileLength - 1;
                    if (end < start)
                        end = start;

                    var length = end - start + 1;
                    var response = Request.CreateResponse(HttpStatusCode.PartialContent);
                    response.Content = new StreamContent(new FileRangeStream(absoluteFileName, start, length));
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    response.Content.Headers.ContentLength = length;
                    response.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, fileLength);
                    response.Headers.AcceptRanges.Add("bytes");
                    return response;
                }

                if (extension == ".html" || extension == ".htm")
                {
                    var html = File.ReadAllText(absoluteFileName, Encoding.UTF8)
                        .Replace("%TRUCKDECK_VERSION%", AssemblyHelper.Version);
                    var htmlResponse = Request.CreateResponse(HttpStatusCode.OK);
                    htmlResponse.Content = new StringContent(html, Encoding.UTF8, contentType);
                    return htmlResponse;
                }

                var full = Request.CreateResponse(HttpStatusCode.OK);
                full.Content = new StreamContent(new FileStream(absoluteFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                full.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                full.Content.Headers.ContentLength = fileLength;
                full.Headers.AcceptRanges.Add("bytes");
                return full;
            }
            catch (IOException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Page not found!");
            }
            catch
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server error.");
            }
        }


        static string ResolveHtmlPath(string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            if (fileName.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                fileName.IndexOf(':') >= 0 ||
                fileName.IndexOf('\\') >= 0)
                return null;

            var htmlRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BaseDirectory));
            var relative = string.IsNullOrWhiteSpace(directory)
                ? fileName
                : (directory.Trim().Trim('/').Replace('/', Path.DirectorySeparatorChar) +
                   Path.DirectorySeparatorChar + fileName);
            relative = relative.Replace('/', Path.DirectorySeparatorChar);
            if (relative.IndexOf("..", StringComparison.Ordinal) >= 0)
                return null;

            var candidate = Path.GetFullPath(Path.Combine(htmlRoot, relative));
            var rootPrefix = htmlRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? htmlRoot
                : htmlRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate, htmlRoot, StringComparison.OrdinalIgnoreCase))
                return null;
            return candidate;
        }
        RangeHeaderValue ParseRangeHeader()
        {
            if (Request.Headers.Range != null)
                return Request.Headers.Range;

            IEnumerable<string> values;
            if (!Request.Headers.TryGetValues("Range", out values))
                return null;

            foreach (var value in values)
            {
                RangeHeaderValue parsed;
                if (RangeHeaderValue.TryParse(value, out parsed))
                    return parsed;
            }

            return null;
        }

        protected string[] EnumerateDirectories(string path)
        {
            var directories = Directory.EnumerateDirectories(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BaseDirectory, path));
            return directories.ToArray();
        }

        sealed class FileRangeStream : Stream
        {
            readonly FileStream _file;
            long _remaining;

            public FileRangeStream(string path, long offset, long count)
            {
                _file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _file.Seek(offset, SeekOrigin.Begin);
                _remaining = count;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _remaining;
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_remaining <= 0)
                    return 0;
                if (count > _remaining)
                    count = (int)Math.Min(count, _remaining);
                var read = _file.Read(buffer, offset, count);
                _remaining -= read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _file.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
