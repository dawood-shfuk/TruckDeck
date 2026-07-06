using System.Linq;
using System.Reflection;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class AssemblyHelper
    {
        public static string Version
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly();
                var info = asm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .OfType<AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault();
                if (info != null && !string.IsNullOrWhiteSpace(info.InformationalVersion))
                    return info.InformationalVersion;

                var v = asm.GetName().Version;
                if (v == null)
                    return "unknown";

                if (v.Build < 0)
                    return $"{v.Major}.{v.Minor}";
                if (v.Revision < 0)
                    return $"{v.Major}.{v.Minor}.{v.Build}";
                if (v.Revision == 0)
                    return $"{v.Major}.{v.Minor}.{v.Build}";
                return v.ToString();
            }
        }
    }
}
