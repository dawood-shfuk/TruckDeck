using System;
using SCSSdkClient;
using SCSSdkClient.Object;

namespace Funbit.Ets.Telemetry.Server.Data.Reader
{
    public class ScsTelemetryDataReader : IDisposable
    {
        const string TruckSimGpsMap = "Local\\TSGPSTelemetry";
        const string RenCloudMap = "Local\\SCSTelemetry";

        readonly SharedMemory _sharedMemory = new SharedMemory();
        readonly ScsTelemetryData _data = new ScsTelemetryData();
        readonly object _lock = new object();

        string _activeMap;
        bool _triedRenCloud;

        static readonly Lazy<ScsTelemetryDataReader> InstanceHolder =
            new Lazy<ScsTelemetryDataReader>(() => new ScsTelemetryDataReader());

        public static ScsTelemetryDataReader Instance => InstanceHolder.Value;

        ScsTelemetryDataReader()
        {
            TryConnect(TruckSimGpsMap);
        }

        public bool IsConnected => _sharedMemory.Hooked;

        public string ActiveMapName => _activeMap;

        public IEts2TelemetryData Read()
        {
            lock (_lock)
            {
                EnsureConnected();
                if (!_sharedMemory.Hooked)
                {
                    _data.Update(null);
                    return _data;
                }

                var scs = _sharedMemory.Update<SCSTelemetry>();
                _data.Update(scs);
                return _data;
            }
        }

        void EnsureConnected()
        {
            if (_sharedMemory.Hooked)
                return;

            if (!_triedRenCloud)
            {
                TryConnect(TruckSimGpsMap);
                if (!_sharedMemory.Hooked)
                {
                    _triedRenCloud = true;
                    TryConnect(RenCloudMap);
                }
            }
            else
            {
                TryConnect(_activeMap ?? TruckSimGpsMap);
                if (!_sharedMemory.Hooked)
                    TryConnect(_activeMap == TruckSimGpsMap ? RenCloudMap : TruckSimGpsMap);
            }
        }

        void TryConnect(string mapName)
        {
            _sharedMemory.Connect(mapName);
            if (_sharedMemory.Hooked)
                _activeMap = mapName;
        }

        public void Dispose()
        {
            if (_sharedMemory.Hooked)
                _sharedMemory.Disconnect();
        }
    }
}
