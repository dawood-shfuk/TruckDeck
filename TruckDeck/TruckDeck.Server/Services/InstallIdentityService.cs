using System;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public static class InstallIdentityService
    {
        public static void EnsureIdentity()
        {
            var state = ClientState.Instance;
            if (!string.IsNullOrWhiteSpace(state.InstallId) && !string.IsNullOrWhiteSpace(state.InstallKey))
                return;

            state.InstallId = Guid.NewGuid().ToString("N");
            state.InstallKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            state.Save();
        }

        public static string InstallId
        {
            get
            {
                EnsureIdentity();
                return ClientState.Instance.InstallId;
            }
        }

        public static string InstallKey
        {
            get
            {
                EnsureIdentity();
                return ClientState.Instance.InstallKey;
            }
        }
    }
}
