using System;

namespace Quantum
{
    using Photon.Deterministic;

    [Serializable]
    public class AxisConfig
    {
        public bool Turn;
        public bool Traction;
        public FP MaxSteerAngle = 30;
        public FP Offset;
        public FP Height = FP._0_75;
        public FP Width;

        public FP Spring = 10;
        public FP Damper = 1;
        [RangeEx(0, 1)] public FP TorsionBarFactor = FP._0_75;
        public FP SuspensionTravel = FP._0_25;
    }

    [Serializable]
    public class WheelConfig
    {
        public LayerMask WheelCollisionMask;
        public FP WheelRadius = FP._0_25;
        public FP WheelMass = FP._0_25;
        public GripSettings GripSettings = new GripSettings();
    }

    [Serializable]
    public class GripSettings
    {
        public FPAnimationCurve Grip;
        public FP GripIntensity = 10;
        public FP SpeedScale = 10;
        public FP GripHeifghtOffset = -FP._0_10;
    }

    [Serializable]
    public class EngineSettings
    {
        public FPAnimationCurve TorqueCurve;
        public int MaxTorque = 20;
        public int MaxRPM = 6000;
        public int MinRPM = 800;
        public FP FlywheelMass = 1;
        public FP FlywheelRadius = 1;

        public static FP RPMtoRadians = FP.PiTimes2 / 60;
        public static FP RadiansToRPM = ((FP)60) / FP.PiTimes2;

        public FP[] GearRatios = new FP[] { 5, 4, 3, 2 };
        public FP DifferentialRatio = 2;
        public FP GearUpRPM = 4000;
        public FP GearDownRPM = 1500;
        public FP ShiftCooldown = FP._0_25;

        public FP Accelerate(Frame f, ref DrivetrainData drivetrain)
        {
            if (drivetrain.ShiftTimer.IsExpiredOrNotValid(f) == false) return 0;
            // RPM is increased (because of input) based on current RPM torque
            var inertia = (FlywheelMass * FlywheelRadius * FlywheelRadius) / 2;
            var rotationSpeedRadians = drivetrain.EngineRPM * RPMtoRadians;
            var currentTorque = GetTorque(ref drivetrain);
            rotationSpeedRadians += (currentTorque * f.DeltaTime) / inertia;
            drivetrain.EngineRPM = FPMath.Clamp(rotationSpeedRadians * RadiansToRPM, MinRPM, MaxRPM);
            if (drivetrain.EngineRPM > GearUpRPM)
            {
                var oldGear = drivetrain.Gear;
                drivetrain.Gear = Math.Clamp(drivetrain.Gear + 1, 0, GearRatios.Length - 1);
                if (oldGear != drivetrain.Gear)
                {
                    drivetrain.ShiftTimer = FrameTimer.FromSeconds(f, ShiftCooldown);
                }
            }

            // new torque (from RPM) is passed returned (if input)
            return GetTorque(ref drivetrain, true);
        }

        private FP GetTorque(ref DrivetrainData drivetrain, bool useGear = false)
        {
            // to account for numerical error
            if (drivetrain.EngineRPM + 1 >= MaxRPM)
            {
                return 0;
            }
            var key = drivetrain.EngineRPM / MaxRPM;
            FP ratio = useGear ? GearRatios[drivetrain.Gear] : 1;
            return TorqueCurve.Evaluate(key) * MaxTorque * ratio * DifferentialRatio;
        }

        public void UpdateRPM(Frame f, ref DrivetrainData drivetrain, FP wheelRPM)
        {
            // counter-torque (accumulated) is re-introduced
            //var inertia = (FlywheelMass * FlywheelRadius * FlywheelRadius) / 2;
            //var rotationSpeedRadians = drivetrain.EngineRPM * RPMtoRadians;
            // RPM is reduced back
            //rotationSpeedRadians += (counterTorque * f.DeltaTime) / inertia;
            //drivetrain.EngineRPM = FPMath.Clamp(rotationSpeedRadians * RadiansToRPM, 1000, MaxRPM);
            FP ratio = GearRatios[drivetrain.Gear] * DifferentialRatio;
            drivetrain.EngineRPM = FPMath.Clamp(wheelRPM * RadiansToRPM * ratio, MinRPM, MaxRPM);
            if (drivetrain.EngineRPM < GearDownRPM)
            {
                var oldGear = drivetrain.Gear;
                drivetrain.Gear = Math.Clamp(drivetrain.Gear - 1, 0, GearRatios.Length - 1);
                if (oldGear != drivetrain.Gear)
                {
                    drivetrain.ShiftTimer = FrameTimer.FromSeconds(f, ShiftCooldown);
                }
            }
        }
    }
}