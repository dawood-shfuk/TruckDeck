using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Threading.Tasks;
using Funbit.Ets.Telemetry.Server.Helpers;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;

namespace Funbit.Ets.Telemetry.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();
            
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters
                  .JsonFormatter
                  .SerializerSettings = JsonHelper.RestSettings;
            
            config.MapHttpAttributeRoutes();
            config.EnsureInitialized();

            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(12);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(9);
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(3);
            appBuilder.MapSignalR();

            appBuilder.UseCors(CorsOptions.AllowAll);

            appBuilder.Use((context, next) =>
            {
                return PmtilesMiddleware.TryServeAsync(context).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        throw t.Exception?.InnerException ?? t.Exception;
                    if (t.Result)
                        return Task.CompletedTask;
                    return next();
                }).Unwrap();
            });

            appBuilder.UseWebApi(config);
        }
    }
}
