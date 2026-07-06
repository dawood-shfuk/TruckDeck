using System;
using System.Linq;
using System.Reflection;
using Funbit.Ets.Telemetry.Server.Helpers;
using SCSSdkClient;
using SCSSdkClient.Object;

namespace Funbit.Ets.Telemetry.Server.Data
{
    class ScsTelemetryData : IEts2TelemetryData
    {
        Box<SCSTelemetry> _rawData = new Box<SCSTelemetry>(new SCSTelemetry());

        public void Update(SCSTelemetry raw)
        {
            _rawData = new Box<SCSTelemetry>(raw ?? new SCSTelemetry());
        }

        public IEts2Game Game => new ScsGame(_rawData);
        public IEts2Truck Truck => new ScsTruck(_rawData);
        public IEts2Shifter Shifter => new ScsShifter(_rawData);
        public int TrailerCount
        {
            get
            {
                var trailers = _rawData.Struct.TrailerValues;
                if (trailers == null)
                    return 0;
                for (int i = trailers.Length - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrEmpty(trailers[i]?.Id))
                        return i + 1;
                }
                return 0;
            }
        }
        public IEts2Trailer[] Trailers
        {
            get
            {
                IEts2Trailer[] array;
                if (TrailerCount == 0)
                {
                    array = new IEts2Trailer[1];
                    array[0] = new ScsTrailer(_rawData, 0);
                    return array;
                }

                array = new IEts2Trailer[TrailerCount];
                for (int i = 0; i < array.Length; i++)
                    array[i] = new ScsTrailer(_rawData, i);
                return array;
            }
        }
        public IEts2Trailer Trailer
        {
            get
            {
                IEts2Trailer t = new ScsTrailer(_rawData, 0);
                if (TrailerCount != 0)
                {
                    float m = 0f;
                    for (int i = 0; i < TrailerCount; i++)
                        m += Trailers[i].Wear;
                    t.Wear = m / TrailerCount;
                }
                return t;
            }
        }
        public IEts2Job Job => new ScsJob(_rawData);
        public IEts2Cargo Cargo => new ScsCargo(_rawData);
        public IEts2Navigation Navigation => new ScsNavigation(_rawData);
        public IEts2FinedGameplayEvent FinedEvent => new ScsFinedGameplayEvent(_rawData);
        public IEts2JobGameplayEvent JobEvent => new ScsJobGameplayEvent(_rawData);
        public IEts2TollgateGameplayEvent TollgateEvent => new ScsTollgateGameplayEvent(_rawData);
        public IEts2FerryGameplayEvent FerryEvent => new ScsFerryGameplayEvent(_rawData);
        public IEts2TrainGameplayEvent TrainEvent => new ScsTrainGameplayEvent(_rawData);

        internal static string ShifterTypeToString(ShifterType type)
        {
            switch (type)
            {
                case ShifterType.HShifter: return "hshifter";
                case ShifterType.Automatic: return "automatic";
                case ShifterType.Manual: return "manual";
                case ShifterType.Arcade: return "arcade";
                default: return "unknown";
            }
        }

        internal static string JobMarketToString(JobMarket market)
        {
            if (market == JobMarket.NoValue)
                return string.Empty;
            return market.ToString();
        }

        internal static string OffenceToString(Offence offence)
        {
            if (offence == Offence.NoValue)
                return string.Empty;
            return offence.ToString().ToLowerInvariant();
        }

        internal static string GameNameFromScs(SCSGame game)
        {
            switch (game)
            {
                case SCSGame.Ets2: return "ETS2";
                case SCSGame.Ats: return "ATS";
                default: return null;
            }
        }

        internal static Ets2Vector ToVector(SCSTelemetry.FVector v)
        {
            if (v == null)
                return new Ets2Vector(0, 0, 0);
            return new Ets2Vector(v.X, v.Y, v.Z);
        }

        internal static Ets2Placement ToPlacement(SCSTelemetry.DPlacement p)
        {
            if (p?.Position == null || p.Orientation == null)
                return new Ets2Placement(0, 0, 0, 0, 0, 0);
            return new Ets2Placement(
                p.Position.X,
                p.Position.Y,
                p.Position.Z,
                p.Orientation.Heading,
                p.Orientation.Pitch,
                p.Orientation.Roll);
        }
    }

    class ScsGame : IEts2Game
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsGame(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        public bool Connected =>
            _rawData.Struct.SdkActive &&
            Ets2ProcessHelper.IsEts2Running &&
            _rawData.Struct.CommonValues?.GameTime?.Value > 0;

        public string GameName => ScsTelemetryData.GameNameFromScs(_rawData.Struct.Game);
        public bool Paused => _rawData.Struct.Paused;
        public DateTime Time => Ets2TelemetryData.MinutesToDate((int)(_rawData.Struct.CommonValues?.GameTime?.Value ?? 0));
        public float TimeScale => _rawData.Struct.CommonValues?.Scale ?? 0;
        public DateTime NextRestStopTime => Ets2TelemetryData.MinutesToDate((int)(_rawData.Struct.CommonValues?.NextRestStopTime?.Value ?? 0));
        public string Version => $"{_rawData.Struct.GameVersion?.Major ?? 0}.{_rawData.Struct.GameVersion?.Minor ?? 0}";
        public string TelemetryPluginVersion => _rawData.Struct.DllVersion.ToString();
        public string TelemetryServerVersion => Assembly.GetEntryAssembly().GetName().Version.ToString();
        public int MaxTrailerCount => (int)_rawData.Struct.MaxTrailerCount;
        public long MultiplayerTimeOffset => _rawData.Struct.MultiplayerTimeOffset;
    }

    class ScsTruck : IEts2Truck
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsTruck(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.Truck Truck => _rawData.Struct.TruckValues;
        SCSTelemetry.Truck.Constants Constants => Truck?.ConstantsValues;
        SCSTelemetry.Truck.Current Current => Truck?.CurrentValues;
        SCSTelemetry.Truck.Constants.Motor MotorConstants => Constants?.MotorValues;
        SCSTelemetry.Truck.Current.Motor MotorCurrent => Current?.MotorValues;
        SCSTelemetry.Truck.Current.Dashboard Dashboard => Current?.DashboardValues;
        SCSTelemetry.Truck.Current.Lights Lights => Current?.LightsValues;

        public string Id => Constants?.Id ?? string.Empty;
        public string Make => Constants?.Brand ?? string.Empty;
        public string Model => Constants?.Name ?? string.Empty;
        public float Speed => Dashboard?.Speed?.Kph ?? 0;
        public float CruiseControlSpeed => Dashboard?.CruiseControlSpeed?.Kph ?? 0;
        public bool CruiseControlOn => Dashboard?.CruiseControl ?? false;
        public float Odometer => Dashboard?.Odometer ?? 0;
        public string ShifterType => ScsTelemetryData.ShifterTypeToString(MotorConstants?.ShifterTypeValue ?? SCSSdkClient.ShifterType.Unknown);
        public int ForwardGears => (int)(MotorConstants?.ForwardGearCount ?? 0);
        public int ReverseGears => (int)(MotorConstants?.ReverseGearCount ?? 0);
        public int Gear => MotorCurrent?.GearValues?.Selected ?? 0;
        public int DisplayedGear => Dashboard?.GearDashboards ?? 0;
        public float EngineRpm => Dashboard?.RPM ?? 0;
        public float EngineRpmMax => MotorConstants?.EngineRpmMax ?? 0;
        public float Fuel => Dashboard?.FuelValue?.Amount ?? 0;
        public float FuelCapacity => Constants?.CapacityValues?.Fuel ?? 0;
        public float FuelAverageConsumption => Dashboard?.FuelValue?.AverageConsumption ?? 0;
        public float FuelWarningFactor => Constants?.WarningFactorValues?.Fuel ?? 0;
        public bool FuelWarningOn => Dashboard?.WarningValues?.FuelW ?? false;
        public float WearEngine => Current?.DamageValues?.Engine ?? 0;
        public float WearTransmission => Current?.DamageValues?.Transmission ?? 0;
        public float WearCabin => Current?.DamageValues?.Cabin ?? 0;
        public float WearChassis => Current?.DamageValues?.Chassis ?? 0;
        public float WearWheels => Current?.DamageValues?.WheelsAvg ?? 0;
        public float UserSteer => _rawData.Struct.ControlValues?.InputValues?.Steering ?? 0;
        public float UserThrottle => _rawData.Struct.ControlValues?.InputValues?.Throttle ?? 0;
        public float UserBrake => _rawData.Struct.ControlValues?.InputValues?.Brake ?? 0;
        public float UserClutch => _rawData.Struct.ControlValues?.InputValues?.Clutch ?? 0;
        public float GameSteer => _rawData.Struct.ControlValues?.GameValues?.Steering ?? 0;
        public float GameThrottle => _rawData.Struct.ControlValues?.GameValues?.Throttle ?? 0;
        public float GameBrake => _rawData.Struct.ControlValues?.GameValues?.Brake ?? 0;
        public float GameClutch => _rawData.Struct.ControlValues?.GameValues?.Clutch ?? 0;
        public bool EngineOn => Current?.EngineEnabled ?? false;
        public bool ElectricOn => Current?.ElectricEnabled ?? false;
        public bool WipersOn => Dashboard?.Wipers ?? false;
        public int RetarderBrake => (int)(MotorCurrent?.BrakeValues?.RetarderLevel ?? 0);
        public int RetarderStepCount => (int)(MotorConstants?.RetarderStepCount ?? 0);
        public bool ParkBrakeOn => MotorCurrent?.BrakeValues?.ParkingBrake ?? false;
        public bool MotorBrakeOn => MotorCurrent?.BrakeValues?.MotorBrake ?? false;
        public float BrakeTemperature => MotorCurrent?.BrakeValues?.Temperature ?? 0;
        public float Adblue => Dashboard?.AdBlue ?? 0;
        public float AdblueCapacity => Constants?.CapacityValues?.AdBlue ?? 0;
        public float AdblueAverageConsumption => 0.0F;
        public bool AdblueWarningOn => Dashboard?.WarningValues?.AdBlue ?? false;
        public float AirPressure => MotorCurrent?.BrakeValues?.AirPressure ?? 0;
        public bool AirPressureWarningOn => Dashboard?.WarningValues?.AirPressure ?? false;
        public float AirPressureWarningValue => Constants?.WarningFactorValues?.AirPressure ?? 0;
        public bool AirPressureEmergencyOn => Dashboard?.WarningValues?.AirPressureEmergency ?? false;
        public float AirPressureEmergencyValue => Constants?.WarningFactorValues?.AirPressureEmergency ?? 0;
        public float OilTemperature => Dashboard?.OilTemperature ?? 0;
        public float OilPressure => Dashboard?.OilPressure ?? 0;
        public bool OilPressureWarningOn => Dashboard?.WarningValues?.OilPressure ?? false;
        public float OilPressureWarningValue => Constants?.WarningFactorValues?.OilPressure ?? 0;
        public float WaterTemperature => Dashboard?.WaterTemperature ?? 0;
        public bool WaterTemperatureWarningOn => Dashboard?.WarningValues?.WaterTemperature ?? false;
        public float WaterTemperatureWarningValue => Constants?.WarningFactorValues?.WaterTemperature ?? 0;
        public float BatteryVoltage => Dashboard?.BatteryVoltage ?? 0;
        public bool BatteryVoltageWarningOn => Dashboard?.WarningValues?.BatteryVoltage ?? false;
        public float BatteryVoltageWarningValue => Constants?.WarningFactorValues?.BatteryVoltage ?? 0;
        public float LightsDashboardValue => Lights?.DashboardBacklight ?? 0;
        public bool LightsDashboardOn => (Lights?.DashboardBacklight ?? 0) > 0;
        public bool BlinkerLeftActive => Lights?.BlinkerLeftActive ?? false;
        public bool BlinkerRightActive => Lights?.BlinkerRightActive ?? false;
        public bool BlinkerLeftOn => Lights?.BlinkerLeftOn ?? false;
        public bool BlinkerRightOn => Lights?.BlinkerRightOn ?? false;
        public bool LightsParkingOn => Lights?.Parking ?? false;
        public bool LightsBeamLowOn => Lights?.BeamLow ?? false;
        public bool LightsBeamHighOn => Lights?.BeamHigh ?? false;
        public bool LightsAuxFrontOn => (Lights?.AuxFront ?? AuxLevel.Off) != AuxLevel.Off;
        public bool LightsAuxRoofOn => (Lights?.AuxRoof ?? AuxLevel.Off) != AuxLevel.Off;
        public bool LightsBeaconOn => Lights?.Beacon ?? false;
        public bool LightsBrakeOn => Lights?.Brake ?? false;
        public bool LightsReverseOn => Lights?.Reverse ?? false;
        public bool LightsHazardOn => Lights?.HazardWarningLights ?? false;
        public bool DifferentialLockOn => Current?.DifferentialLock ?? false;
        public bool LiftAxleOn => Current?.LiftAxle ?? false;
        public bool LiftAxleIndicatorOn => Current?.LiftAxleIndicator ?? false;
        public bool TrailerLiftAxleOn => Current?.TrailerLiftAxle ?? false;
        public bool TrailerLiftAxleIndicatorOn => Current?.TrailerLiftAxleIndicator ?? false;
        public float FuelRange => Dashboard?.FuelValue?.Range ?? 0;
        public int RetarderLevel => RetarderBrake;
        public int LightsAuxFrontValue => (int)(Lights?.AuxFront ?? AuxLevel.Off);
        public int LightsAuxRoofValue => (int)(Lights?.AuxRoof ?? AuxLevel.Off);
        public IEts2Placement Placement => ScsTelemetryData.ToPlacement(Current?.PositionValue);
        public IEts2Vector Acceleration => ScsTelemetryData.ToVector(Current?.AccelerationValues?.LinearAcceleration);
        public IEts2Vector Head => ScsTelemetryData.ToVector(Truck?.Positioning?.Head);
        public IEts2Vector Cabin => ScsTelemetryData.ToVector(Truck?.Positioning?.Cabin);
        public IEts2Vector Hook => ScsTelemetryData.ToVector(Truck?.Positioning?.Hook);
        public string LicensePlate => Constants?.LicensePlate ?? string.Empty;
        public string LicensePlateCountryId => Constants?.LicensePlateCountryId ?? string.Empty;
        public string LicensePlateCountry => Constants?.LicensePlateCountry ?? string.Empty;
        public int WheelCount => (int)(Constants?.WheelsValues?.Count ?? 0);
        public IEts2Wheel[] Wheels
        {
            get
            {
                var count = WheelCount;
                var array = new IEts2Wheel[count];
                for (int i = 0; i < array.Length; i++)
                    array[i] = new ScsTruckWheel(_rawData, i);
                Array.Sort(array, new Ets2WheelSorter());
                return array;
            }
        }
    }

    class ScsShifter : IEts2Shifter
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsShifter(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.Truck.Constants.Motor Motor =>
            _rawData.Struct.TruckValues?.ConstantsValues?.MotorValues;

        SCSTelemetry.Truck.Current.Motor.Gear CurrentGear =>
            _rawData.Struct.TruckValues?.CurrentValues?.MotorValues?.GearValues;

        SCSTelemetry.Truck.Current.Dashboard Dashboard =>
            _rawData.Struct.TruckValues?.CurrentValues?.DashboardValues;

        SCSTelemetry.WheelsConstants Wheels =>
            _rawData.Struct.TruckValues?.ConstantsValues?.WheelsValues;

        public string Type => ScsTelemetryData.ShifterTypeToString(Motor?.ShifterTypeValue ?? ShifterType.Unknown);
        public int ForwardGears => (int)(Motor?.ForwardGearCount ?? 0);
        public string[] ForwardGearNames
        {
            get
            {
                if (ForwardGears == 0) return null;
                string[] fwGears = new string[ForwardGears + 1];
                switch (ForwardGears)
                {
                    case 18:
                        fwGears = (Type == "hshifter")
                            ? new string[] { "N", "CL", "CH", "1L", "1H", "2L", "2H", "3L", "3H", "4L", "4H", "5L", "5H", "6L", "6H", "7L", "7H", "8L", "8H" }
                            : new string[] { "N", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18" };
                        break;
                    case 16:
                        fwGears = (Type == "hshifter")
                            ? new string[] { "N", "1L", "1H", "2L", "2H", "3L", "3H", "4L", "4H", "5L", "5H", "6L", "6H", "7L", "7H", "8L", "8H" }
                            : new string[] { "N", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
                        break;
                    case 14:
                        fwGears = (Type == "hshifter")
                            ? new string[] { "N", "CL", "CH", "1", "2", "3", "4", "5L", "5H", "6L", "6H", "7L", "7H", "8L", "8H" }
                            : new string[] { "N", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14" };
                        break;
                    case 13:
                        fwGears = (Type == "hshifter")
                            ? new string[] { "N", "L", "1", "2", "3", "4", "5L", "5H", "6L", "6H", "7L", "7H", "8L", "8H" }
                            : new string[] { "N", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
                        break;
                    case 12:
                        fwGears = (Type == "hshifter")
                            ? new string[] { "N", "1", "2", "3", "4", "5L", "5H", "6L", "6H", "7L", "7H", "8L", "8H" }
                            : new string[] { "N", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
                        break;
                    case < 11:
                        fwGears[0] = "N";
                        for (int i = 1; i < fwGears.Length; i++)
                            fwGears[i] = i.ToString();
                        break;
                }
                return fwGears;
            }
        }
        public int ReverseGears => (int)(Motor?.ReverseGearCount ?? 0);
        public string[] ReverseGearNames
        {
            get
            {
                if (ReverseGears == 0) return null;
                string[] rvGears = new string[ReverseGears + 1];
                switch (ReverseGears)
                {
                    case 4:
                        rvGears = (Type == "hshifter")
                            ? new string[] { "N", "R1L", "R1H", "R2L", "R2H" }
                            : new string[] { "N", "R1", "R2", "R3", "R4" };
                        break;
                    case 3:
                        rvGears = (Type == "hshifter")
                            ? new string[] { "N", "RL", "RH", "RO" }
                            : new string[] { "N", "R1", "R2", "R3" };
                        break;
                    case 2:
                        rvGears = (Type == "hshifter")
                            ? new string[] { "N", "RL", "RH" }
                            : new string[] { "N", "R1", "R2" };
                        break;
                    case 1:
                        rvGears = new string[] { "N", "R" };
                        break;
                }
                return rvGears;
            }
        }
        public float DifferentialRatio => Motor?.DifferentialRation ?? 0;
        public float[] ForwardGearRatios
        {
            get
            {
                if (ForwardGears == 0 || Motor?.GearRatiosForward == null) return null;
                float[] array = (float[])Motor.GearRatiosForward.Clone();
                Array.Resize(ref array, ForwardGears);
                return array;
            }
        }
        public float[] ReverseGearRatios
        {
            get
            {
                if (ReverseGears == 0 || Motor?.GearRatiosReverse == null) return null;
                float[] array = (float[])Motor.GearRatiosReverse.Clone();
                Array.Resize(ref array, ReverseGears);
                return array;
            }
        }
        public double TyreCircumference
        {
            get
            {
                if (Wheels?.Powered == null || Wheels.Radius == null || Wheels.Count == 0)
                    return 0;
                int index = Wheels.Count > 4 && Wheels.Powered.Length > 4 && Wheels.Powered[4]
                    ? 4
                    : Wheels.Powered.Length > 2 && Wheels.Powered[2] ? 2 : 0;
                if (index >= Wheels.Radius.Length)
                    index = 0;
                return Wheels.Radius[index] * 2 * Math.PI;
            }
        }
        float SpeedMs => Dashboard?.Speed?.Value ?? 0;
        public int[] ForwardSpeedAt1500Rpm
        {
            get
            {
                if (ForwardGears == 0 || ForwardGearRatios == null) return null;
                int[] array = new int[ForwardGears + 1];
                for (int i = 1; i < array.Length; i++)
                    array[i] = (int)Math.Round(90 * TyreCircumference / (DifferentialRatio * ForwardGearRatios[i - 1]));
                return array;
            }
        }
        public int[] ReverseSpeedAt1500Rpm
        {
            get
            {
                if (ReverseGears == 0 || ReverseGearRatios == null) return null;
                int[] array = new int[ReverseGears + 1];
                for (int i = 1; i < array.Length; i++)
                    array[i] = (int)Math.Round(90 * TyreCircumference / (DifferentialRatio * ReverseGearRatios[i - 1]));
                return array;
            }
        }
        public int[] ForwardRpmAtCurrentSpeed
        {
            get
            {
                if (ForwardGears == 0 || ForwardGearRatios == null || TyreCircumference == 0) return null;
                int[] array = new int[ForwardGears + 1];
                for (int i = 1; i < array.Length; i++)
                    array[i] = (int)Math.Round(60 * Math.Abs(SpeedMs) * DifferentialRatio * ForwardGearRatios[i - 1] / TyreCircumference);
                return array;
            }
        }
        public int[] ReverseRpmAtCurrentSpeed
        {
            get
            {
                if (ReverseGears == 0 || ReverseGearRatios == null || TyreCircumference == 0) return null;
                int[] array = new int[ReverseGears + 1];
                for (int i = 1; i < array.Length; i++)
                    array[i] = (int)Math.Round(60 * Math.Abs(SpeedMs) * DifferentialRatio * ReverseGearRatios[i - 1] / TyreCircumference);
                return array;
            }
        }
        public int SelectorCount => Type != "hshifter" ? 1 : (int)Math.Pow(2, Motor?.SelectorCount ?? 0);
        public int SlotCount
        {
            get
            {
                if (Motor?.SlotHandlePosition == null || Type != "hshifter")
                    return Type != "hshifter" ? 1 : 0;
                return (int)Motor.SlotHandlePosition.Max() + 1;
            }
        }
        public IEts2ShifterSlot[] Slots
        {
            get
            {
                if (SlotCount == 0 || ReverseGearNames == null) return null;
                IEts2ShifterSlot[] slots;
                if (Type == "hshifter")
                {
                    slots = new IEts2ShifterSlot[SlotCount];
                    for (int slot = 0; slot < slots.Length; slot++)
                        slots[slot] = new ScsShifterSlot(_rawData, SelectorCount, slot, ForwardGearNames, ReverseGearNames);
                }
                else
                {
                    slots = new IEts2ShifterSlot[1];
                    slots[0] = new ScsShifterSlot(_rawData, SelectorCount, 0, ForwardGearNames, ReverseGearNames);
                    slots[0].Seletors[0].Gear = Gear;
                    slots[0].Seletors[0].GearName = "Unknow";
                    if (Gear < 0)
                    {
                        if (Math.Abs(Gear) < ReverseGearNames.Length)
                            slots[0].Seletors[0].GearName = ReverseGearNames[Math.Abs(Gear)];
                    }
                    else if (Gear < ForwardGearNames.Length)
                    {
                        slots[0].Seletors[0].GearName = ForwardGearNames[Gear];
                    }
                }
                return slots;
            }
        }
        public int Gear => CurrentGear?.Selected ?? 0;
        public int DisplayedGear => Dashboard?.GearDashboards ?? 0;
        public string DisplayedGearName => (DisplayedGear < 0)
            ? ReverseGearNames == null ? "" : ReverseGearNames[Math.Abs(DisplayedGear)]
            : ForwardGearNames == null ? "" : ForwardGearNames[DisplayedGear];
        public float GearRatio
        {
            get
            {
                if (Gear == 0) return 0;
                if (Gear < 0)
                {
                    var ratios = ReverseGearRatios;
                    if (ratios == null || Math.Abs(Gear) > ratios.Length)
                        return 0;
                    return ratios[Math.Abs(Gear) - 1];
                }
                var forward = ForwardGearRatios;
                if (forward == null || Gear > forward.Length)
                    return 0;
                return forward[Gear - 1];
            }
        }
        public int Slot => Type != "hshifter" ? 0 : (int)(CurrentGear?.HShifterSlot ?? 0);
        public int Selector
        {
            get
            {
                int selectors = 0;
                if (Type == "hshifter")
                {
                    var toggles = CurrentGear?.HShifterSelector;
                    if (toggles != null)
                    {
                        for (int i = 0; i < toggles.Length; i++)
                            selectors += (int)Math.Pow(2, i) * (toggles[i] ? 1 : 0);
                    }
                    if (SelectorCount > 0 && selectors >= SelectorCount)
                        selectors = SelectorCount - 1;
                }
                return selectors;
            }
        }
        public int BestGear
        {
            get
            {
                if (SpeedMs == 0) return 0;
                int r = 0;
                int gap = 1500;
                int[] array = SpeedMs > 0 ? ForwardRpmAtCurrentSpeed : ReverseRpmAtCurrentSpeed;
                if (array == null) return 0;
                for (int i = 1; i < array.Length; i++)
                {
                    if (array[i] < 0)
                    {
                        if (gap > Math.Abs(array[i] + 1300))
                        {
                            r = -i;
                            gap = Math.Abs(array[i] + 1300);
                        }
                    }
                    else if (gap > Math.Abs(array[i] - 1300))
                    {
                        r = i;
                        gap = Math.Abs(array[i] - 1300);
                    }
                }
                return r;
            }
        }
        public string BestGearName => ReverseGearNames == null ? "" : BestGear < 0 ? ReverseGearNames[Math.Abs(BestGear)] : ForwardGearNames[BestGear];
    }

    class ScsShifterSlot : IEts2ShifterSlot
    {
        public ScsShifterSlot(Box<SCSTelemetry> rawData, int selectorCount, int slot, string[] fwGearNames, string[] rvGearNames)
        {
            var motor = rawData.Struct.TruckValues?.ConstantsValues?.MotorValues;
            int i = selectorCount * slot;
            Slot = motor?.SlotHandlePosition != null && i < motor.SlotHandlePosition.Length
                ? (int)motor.SlotHandlePosition[i]
                : slot;
            Seletors = new IEts2ShifterSelector[selectorCount];
            for (int selector = 0; selector < Seletors.Length; selector++)
                Seletors[selector] = new ScsShifterSelector(rawData, selectorCount, slot, selector, fwGearNames, rvGearNames);
        }
        public int Slot { get; private set; }
        public IEts2ShifterSelector[] Seletors { get; private set; }
    }

    class ScsShifterSelector : IEts2ShifterSelector
    {
        public ScsShifterSelector(Box<SCSTelemetry> rawData, int selectorCount, int slot, int selector, string[] fwGearNames, string[] rvGearNames)
        {
            var motor = rawData.Struct.TruckValues?.ConstantsValues?.MotorValues;
            int i = selectorCount * slot + selector;
            Selector = motor?.SlotSelectors != null && i < motor.SlotSelectors.Length
                ? (int)motor.SlotSelectors[i]
                : selector;
            Gear = motor?.SlotGear != null && i < motor.SlotGear.Length
                ? motor.SlotGear[i]
                : 0;
            GearName = "Unknow";
            if (Gear < 0)
            {
                if (Math.Abs(Gear) < rvGearNames.Length)
                    GearName = rvGearNames[Math.Abs(Gear)];
            }
            else if (Gear < fwGearNames.Length)
            {
                GearName = fwGearNames[Gear];
            }
        }
        public int Selector { get; private set; }
        public int Gear { get; set; }
        public string GearName { get; set; }
    }

    class ScsTrailer : IEts2Trailer
    {
        readonly Box<SCSTelemetry> _rawData;
        readonly int _trailerNumber;

        public ScsTrailer(Box<SCSTelemetry> rawData, int trailerNumber)
        {
            if (trailerNumber < 0 || trailerNumber > 9)
                throw new ArgumentException($"trailerNumber must be between 0-9. Found: {trailerNumber}");
            _rawData = rawData;
            _trailerNumber = trailerNumber;
        }

        SCSTelemetry.Trailer Trailer
        {
            get
            {
                var trailers = _rawData.Struct.TrailerValues;
                if (trailers == null || _trailerNumber >= trailers.Length)
                    return null;
                return trailers[_trailerNumber];
            }
        }

        public int Number => _trailerNumber;
        public bool Attached => Trailer?.Attached ?? false;
        public bool Present => !string.IsNullOrEmpty(Trailer?.Id);
        public string Id => Trailer?.Id ?? string.Empty;
        public string Name => Trailer?.Name ?? string.Empty;
        public float WearWheels => Trailer?.DamageValues?.Wheels ?? 0;
        public float WearChassis => Trailer?.DamageValues?.Chassis ?? 0;
        public float WearBody => Trailer?.DamageValues?.Body ?? 0;
        private float _wear;
        public float Wear
        {
            get => (_wear == 0) ? Math.Max(Math.Max(WearWheels, WearChassis), WearBody) : _wear;
            set => _wear = value;
        }
        public float CargoDamage => Trailer?.DamageValues?.Cargo ?? 0;
        public string CargoAccessoryId => Trailer?.CargoAccessoryId ?? string.Empty;
        public string BrandId => Trailer?.BrandId ?? string.Empty;
        public string Brand => Trailer?.Brand ?? string.Empty;
        public string BodyType => Trailer?.BodyType ?? string.Empty;
        public string LicensePlate => Trailer?.LicensePlate ?? string.Empty;
        public string LicensePlateCountry => Trailer?.LicensePlateCountry ?? string.Empty;
        public string LicensePlateCountryId => Trailer?.LicensePlateCountryId ?? string.Empty;
        public string ChainType => Trailer?.ChainType ?? string.Empty;
        public IEts2Placement Placement => ScsTelemetryData.ToPlacement(Trailer?.Position);
        public float Distance
        {
            get
            {
                if (!Present || Trailer?.Position?.Position == null)
                    return 0;
                var truckPos = _rawData.Struct.TruckValues?.CurrentValues?.PositionValue?.Position;
                if (truckPos == null)
                    return 0;
                var trailerPos = Trailer.Position.Position;
                return (float)Math.Sqrt(
                    Math.Pow(truckPos.X - trailerPos.X, 2) +
                    Math.Pow(truckPos.Y - trailerPos.Y, 2) +
                    Math.Pow(truckPos.Z - trailerPos.Z, 2));
            }
        }
        public IEts2Vector Hook => ScsTelemetryData.ToVector(Trailer?.Hook);
        public uint WheelCount => Trailer?.WheelsConstant?.Count ?? 0;
        public IEts2Wheel[] Wheels
        {
            get
            {
                uint wheelCount = WheelCount;
                var array = new IEts2Wheel[wheelCount];
                if (wheelCount > 0)
                {
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new ScsTrailerWheel(_rawData, _trailerNumber, i);
                    Array.Sort(array, new Ets2WheelSorter());
                }
                return array;
            }
        }
    }

    class ScsNavigation : IEts2Navigation
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsNavigation(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.Navigation Navigation => _rawData.Struct.NavigationValues;

        public DateTime EstimatedTime => Ets2TelemetryData.SecondsToDate((int)(Navigation?.NavigationTime ?? 0));
        public int EstimatedDistance => (int)(Navigation?.NavigationDistance ?? 0);
        public int SpeedLimit => Navigation?.SpeedLimit != null ? (int)Math.Round(Navigation.SpeedLimit.Kph) : 0;
        public float RouteDistance => Navigation?.NavigationDistance ?? 0;
        public float RouteTimeSeconds => Navigation?.NavigationTime ?? 0;
    }

    class ScsJob : IEts2Job
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsJob(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.Job Job => _rawData.Struct.JobValues;

        public int Income => (int)(Job?.Income ?? 0);
        public DateTime DeadlineTime => Ets2TelemetryData.MinutesToDate((int)(Job?.DeliveryTime?.Value ?? 0));
        public DateTime RemainingTime
        {
            get
            {
                if (Job?.DeliveryTime?.Value > 0)
                    return Ets2TelemetryData.MinutesToDate(Job.RemainingDeliveryTime?.Value ?? 0);
                return Ets2TelemetryData.MinutesToDate(0);
            }
        }
        public string SourceCity => Job?.CitySource ?? string.Empty;
        public string SourceCompany => Job?.CompanySource ?? string.Empty;
        public string DestinationCity => Job?.CityDestination ?? string.Empty;
        public string DestinationCompany => Job?.CompanyDestination ?? string.Empty;
        public bool SpecialTransport => Job?.SpecialJob ?? false;
        public string JobMarket => ScsTelemetryData.JobMarketToString(Job?.Market ?? SCSSdkClient.JobMarket.NoValue);
        public int PlannedDistanceKm => (int)(Job?.PlannedDistanceKm ?? 0);
    }

    class ScsCargo : IEts2Cargo
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsCargo(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.Job.Cargo CargoValues => _rawData.Struct.JobValues?.CargoValues;

        public bool CargoLoaded => _rawData.Struct.JobValues?.CargoLoaded ?? false;
        public string CargoId => CargoValues?.Id ?? string.Empty;
        public string Cargo => CargoValues?.Name ?? string.Empty;
        public float Mass => CargoValues?.Mass ?? 0;
        public float UnitMass => CargoValues?.UnitMass ?? 0;
        public int UnitCount => (int)(CargoValues?.UnitCount ?? 0);
        public float Damage => CargoValues?.CargoDamage ?? 0;
    }

    class ScsFinedGameplayEvent : IEts2FinedGameplayEvent
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsFinedGameplayEvent(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        public string FineOffense => ScsTelemetryData.OffenceToString(_rawData.Struct.GamePlay?.FinedEvent?.Offence ?? Offence.NoValue);
        public int FineAmount => (int)(_rawData.Struct.GamePlay?.FinedEvent?.Amount ?? 0);
        public bool Fined => _rawData.Struct.SpecialEventsValues?.Fined ?? false;
    }

    class ScsJobGameplayEvent : IEts2JobGameplayEvent
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsJobGameplayEvent(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.SpecialEvents Events => _rawData.Struct.SpecialEventsValues;
        SCSTelemetry.GamePlayEvents GamePlay => _rawData.Struct.GamePlay;

        public bool JobFinished => Events?.JobFinished ?? false;
        public bool JobCancelled => Events?.JobCancelled ?? false;
        public bool JobDelivered => Events?.JobDelivered ?? false;
        public int CancelPenalty => (int)(GamePlay?.JobCancelled?.Penalty ?? 0);
        public int Revenue => (int)(GamePlay?.JobDelivered?.Revenue ?? 0);
        public int EarnedXp => GamePlay?.JobDelivered?.EarnedXp ?? 0;
        public float CargoDamage => GamePlay?.JobDelivered?.CargoDamage ?? 0;
        public int Distance => (int)(GamePlay?.JobDelivered?.DistanceKm ?? 0);
        public DateTime DeliveryTime => Ets2TelemetryData.MinutesToDate((int)(GamePlay?.JobDelivered?.DeliveryTime?.Value ?? 0));
        public bool AutoparkUsed => GamePlay?.JobDelivered?.AutoParked ?? false;
        public bool AutoloadUsed => GamePlay?.JobDelivered?.AutoLoaded ?? false;
    }

    class ScsTollgateGameplayEvent : IEts2TollgateGameplayEvent
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsTollgateGameplayEvent(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        public bool TollgateUsed => _rawData.Struct.SpecialEventsValues?.Tollgate ?? false;
        public int PayAmount => (int)(_rawData.Struct.GamePlay?.TollgateEvent?.PayAmount ?? 0);
    }

    class ScsFerryGameplayEvent : IEts2FerryGameplayEvent
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsFerryGameplayEvent(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.GamePlayEvents.Transport Ferry => _rawData.Struct.GamePlay?.FerryEvent;

        public bool FerryUsed => _rawData.Struct.SpecialEventsValues?.Ferry ?? false;
        public string SourceName => Ferry?.SourceName ?? string.Empty;
        public string TargetName => Ferry?.TargetName ?? string.Empty;
        public string SourceId => Ferry?.SourceId ?? string.Empty;
        public string TargetId => Ferry?.TargetId ?? string.Empty;
        public int PayAmount => (int)(Ferry?.PayAmount ?? 0);
    }

    class ScsTrainGameplayEvent : IEts2TrainGameplayEvent
    {
        readonly Box<SCSTelemetry> _rawData;

        public ScsTrainGameplayEvent(Box<SCSTelemetry> rawData)
        {
            _rawData = rawData;
        }

        SCSTelemetry.GamePlayEvents.Transport Train => _rawData.Struct.GamePlay?.TrainEvent;

        public bool TrainUsed => _rawData.Struct.SpecialEventsValues?.Train ?? false;
        public string SourceName => Train?.SourceName ?? string.Empty;
        public string TargetName => Train?.TargetName ?? string.Empty;
        public string SourceId => Train?.SourceId ?? string.Empty;
        public string TargetId => Train?.TargetId ?? string.Empty;
        public int PayAmount => (int)(Train?.PayAmount ?? 0);
    }

    class ScsTruckWheel : IEts2Wheel
    {
        public ScsTruckWheel(Box<SCSTelemetry> rawData, int wheelIndex)
        {
            var constants = rawData.Struct.TruckValues?.ConstantsValues?.WheelsValues;
            var current = rawData.Struct.TruckValues?.CurrentValues?.WheelsValues;
            Simulated = constants?.Simulated != null && wheelIndex < constants.Simulated.Length && constants.Simulated[wheelIndex];
            Steerable = constants?.Steerable != null && wheelIndex < constants.Steerable.Length && constants.Steerable[wheelIndex];
            Radius = constants?.Radius != null && wheelIndex < constants.Radius.Length ? constants.Radius[wheelIndex] : 0;
            Position = constants?.PositionValues != null && wheelIndex < constants.PositionValues.Length
                ? ScsTelemetryData.ToVector(constants.PositionValues[wheelIndex])
                : new Ets2Vector(0, 0, 0);
            Powered = constants?.Powered != null && wheelIndex < constants.Powered.Length && constants.Powered[wheelIndex];
            Liftable = constants?.Liftable != null && wheelIndex < constants.Liftable.Length && constants.Liftable[wheelIndex];
            Lifted = current?.Lift != null && wheelIndex < current.Lift.Length && current.Lift[wheelIndex] > 0;
        }

        public bool Simulated { get; private set; }
        public bool Steerable { get; private set; }
        public bool Powered { get; private set; }
        public bool Liftable { get; private set; }
        public bool Lifted { get; private set; }
        public float Radius { get; private set; }
        public IEts2Vector Position { get; private set; }
    }

    class ScsTrailerWheel : IEts2Wheel
    {
        public ScsTrailerWheel(Box<SCSTelemetry> rawData, int trailerNumber, int wheelIndex)
        {
            SCSTelemetry.Trailer trailer = null;
            var trailers = rawData.Struct.TrailerValues;
            if (trailers != null && trailerNumber < trailers.Length)
                trailer = trailers[trailerNumber];

            var constants = trailer?.WheelsConstant;
            var current = trailer?.Wheelvalues;
            Simulated = constants?.Simulated != null && wheelIndex < constants.Simulated.Length && constants.Simulated[wheelIndex];
            Steerable = constants?.Steerable != null && wheelIndex < constants.Steerable.Length && constants.Steerable[wheelIndex];
            Radius = constants?.Radius != null && wheelIndex < constants.Radius.Length ? constants.Radius[wheelIndex] : 0;
            Position = constants?.PositionValues != null && wheelIndex < constants.PositionValues.Length
                ? ScsTelemetryData.ToVector(constants.PositionValues[wheelIndex])
                : new Ets2Vector(0, 0, 0);
            Powered = constants?.Powered != null && wheelIndex < constants.Powered.Length && constants.Powered[wheelIndex];
            Liftable = constants?.Liftable != null && wheelIndex < constants.Liftable.Length && constants.Liftable[wheelIndex];
            Lifted = current?.Lift != null && wheelIndex < current.Lift.Length && current.Lift[wheelIndex] > 0;
        }

        public bool Simulated { get; private set; }
        public bool Steerable { get; private set; }
        public bool Powered { get; private set; }
        public bool Liftable { get; private set; }
        public bool Lifted { get; private set; }
        public float Radius { get; private set; }
        public IEts2Vector Position { get; private set; }
    }
}
