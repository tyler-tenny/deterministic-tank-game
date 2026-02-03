using Quantum.Physics3D;
using UnityEngine;

namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class TankConfig : AssetObject
    {
        [Header("Common Settings")] [Header("Wheel")]
        public WheelConfig WheelConfig;

        [Header("Axle/Suspension")]
        public AxisConfig[] Axis;
        public bool UseGlobalUp = false;

        [Header("Physics Body")] [RangeEx(0, 1)]
        public FP GroundDrag = FP._0_50;

        [RangeEx(0, 1)] public FP AirDrag = 0;
        public FP DownGravityScale = 3;
        public FPVector3 ParametricInertia = new FPVector3(2, 5, 2);

        [Header("AeroDynamics")] [RangeEx(0, 1)]
        public FP FrontWingCoeficient = FP._0_25;

        [RangeEx(0, 1)] public FP RearWingCoeficient = FP._0_25;

        [Header("Extra Settings")]
        [Header("Steering")]
        public FPAnimationCurve SteerCurve;
        public FP TurnSpeed = 90;
        public FP HandbrakeFactor = 2;
        public bool UseTorque = true;
        public FP MaxAngularSpeed = 1;
        [RangeEx(0, 1)] public FP AirControlFactor = 0;
        [Header("Drivetrain")]
        public FP Acceleration = 10;
        public FP Brake = 10;

        [Header("Misc")] public bool Debug = true;

        public void UpdateVehicle(Frame f, ref DriveSystem.Filter filter, Input* input)
        {
            filter.Vehicle->Grounded = false;
            filter.Body->SetInertiaTensor(CalculateInertiaTensor());
            UpdateCompression(f, filter.Vehicle, filter.Transform, filter.Body);

            var modifiers = UpdateModifiers(f, ref filter);

            filter.Body->Drag = filter.Vehicle->Grounded ? GroundDrag : AirDrag;
            filter.Body->Drag *= modifiers.Item2;

            UpdateAccel(f, ref filter, input);
            UpdateSteer(f, ref filter, input, modifiers.Item1);

            UpdateGravityScale(f, ref filter);
        }

        private (FP, FP) UpdateModifiers(Frame f, ref DriveSystem.Filter filter)
        {
            bool modified = false;
            FP gripModifier = 0;
            FP dragModifier = 0;
            for (int i = 0; i < Axis.Length; i++)
            {
                UpdateModifiers(f, &filter.Vehicle->Axis.GetPointer(i)->LeftWheel, ref gripModifier, ref dragModifier,
                    ref modified);
                UpdateModifiers(f, &filter.Vehicle->Axis.GetPointer(i)->RightWheel, ref gripModifier, ref dragModifier,
                    ref modified);
            }

            
            if (modified)
            {
                var divider = 2 * Axis.Length;
                gripModifier /= divider;
                dragModifier /= divider;
                return (gripModifier, dragModifier);
            }
            return (1, 1);
        }

        private void UpdateModifiers(Frame f, WheelData* wheel, ref FP grip, ref FP drag, ref bool modified)
        {
            if (wheel->Modifier == default)
            {
                drag += 1;
                grip += 1;
                return;
            }

            var modifier = f.FindAsset(wheel->Modifier);
            grip += modifier.GripFactor;
            drag += modifier.DragFactor;
            modified = true;
        }

        private void UpdateGravityScale(Frame f, ref DriveSystem.Filter filter)
        {
            if (filter.Vehicle->Grounded || filter.Body->Velocity.Y >= 0)
            {
                filter.Body->GravityScale = 1 + (2 * FP._0_10);
            }
            else
            {
                filter.Body->GravityScale = DownGravityScale;
            }
        }

        protected void UpdateAccel(Frame f, ref DriveSystem.Filter filter, Input* input)
        {
            var forward = filter.Transform->Forward;
            var forwardSpeed = FPVector3.Dot(filter.Body->Velocity, forward);
            var wheelAngularSpeed = forwardSpeed / WheelConfig.WheelRadius;
            filter.Vehicle->DriveTrain.EngineRPM = wheelAngularSpeed * EngineSettings.RadiansToRPM;

            for (int i = 0; i < Axis.Length; i++)
            {
                var axisData = filter.Vehicle->Axis.GetPointer(i);
                if (axisData->Grounded == false) continue;

                AxisConfig axisConfig = Axis[i];

                var axisRelativeOffset = FPVector3.Forward * axisConfig.Offset + FPVector3.Down * WheelConfig.GripSettings.GripHeifghtOffset;
                var offset = filter.Transform->TransformPoint(axisRelativeOffset);

                axisData->LeftWheel.RotationSpeed = wheelAngularSpeed;
                axisData->RightWheel.RotationSpeed = wheelAngularSpeed;
                if (input->Backward)
                {
                    filter.Body->AddForceAtPosition(-forward * Brake, offset, filter.Transform);
                }

                if (axisConfig.Traction == false) continue;
                if (input->Forward)
                {
                    filter.Body->AddForceAtPosition(forward * Acceleration, offset, filter.Transform);
                    if (Debug)
                    {
                        UnityEngine.Debug.Log("Input going forward...");
                    }
                }
            }
        }

        protected void UpdateSteer(Frame f, ref DriveSystem.Filter filter, Input* input, FP gripModifier)
        {
            var forward = filter.Transform->Forward;
            var right = filter.Transform->Right;
            var up = filter.Transform->Up;
            var speedScale = WheelConfig.GripSettings.SpeedScale;

            //don't bother steering if we're flying
            if (filter.Vehicle->Grounded == false && AirControlFactor == 0) return;
            // turning 
            var forwardSpeed = FPVector3.Dot(filter.Body->Velocity, forward);
            var forwardSpeedScaled = forwardSpeed / speedScale;
            var sign = FPMath.Sign(forwardSpeed);
            var steerFactor = SteerCurve.Evaluate(FPMath.Abs(forwardSpeedScaled)) * sign;
            FP turn = 0;
            if (input->Left) turn -= 1;
            if (input->Right) turn += 1;

            if (filter.Vehicle->Grounded == false)
                turn *= AirControlFactor;
            
            else if (filter.Vehicle->Grounded && input->Jump)
            {
                filter.Body->AddForce(new FPVector3(0,50,0));
            }

            filter.Transform->Rotate(FPVector3.Up, turn * steerFactor * TurnSpeed * f.DeltaTime);

            if (Debug)
            {
                Draw.Ray(filter.Transform->Position, up * turn * steerFactor * TurnSpeed, ColorRGBA.Magenta);
            }

             if (filter.Vehicle->Grounded == false) return;
            var rightSpeed = -FPVector3.Dot(filter.Body->Velocity, right);
            var rightSpeedScaled = rightSpeed / speedScale;
            var side = FPMath.Sign(rightSpeedScaled);
            var grip =  WheelConfig.GripSettings.Grip.Evaluate(FPMath.Abs(rightSpeedScaled)) * gripModifier;
            var gripPosition = filter.Transform->Position + up *  WheelConfig.GripSettings.GripHeifghtOffset;
            filter.Body->AddForceAtPosition(grip * right *  WheelConfig.GripSettings.GripIntensity * side, gripPosition, filter.Transform);
            if (Debug)
            {
                Draw.Ray(gripPosition, grip * right * side, ColorRGBA.Green);
                Draw.Sphere(gripPosition, FP._0_10, ColorRGBA.Magenta);
            }

            if (MaxAngularSpeed != 0 && filter.Body->AngularVelocity.Magnitude > MaxAngularSpeed)
            {
                filter.Body->AngularVelocity = filter.Body->AngularVelocity.Normalized * MaxAngularSpeed;
            }


            for (int i = 0; i < Axis.Length; i++)
            {
                var axisData = filter.Vehicle->Axis.GetPointer(i);
                axisData->SteerAngle = 0;
                AxisConfig axisConfig = Axis[i];
                if (axisConfig.Turn)
                {
                    axisData->SteerAngle = turn * axisConfig.MaxSteerAngle;
                    axisData->LeftWheel.SteerAngle = axisData->SteerAngle;
                    axisData->RightWheel.SteerAngle = axisData->SteerAngle;
                }
            }
        }
        
        private FPVector3 CalculateInertiaTensor()
        {
            var areaX = (ParametricInertia.Z * ParametricInertia.Z + ParametricInertia.Y * ParametricInertia.Y) *
                        FP._0_33;
            var areaY = (ParametricInertia.Z * ParametricInertia.Z + ParametricInertia.X * ParametricInertia.X) *
                        FP._0_33;
            var areaZ = (ParametricInertia.X * ParametricInertia.X + ParametricInertia.Y * ParametricInertia.Y) *
                        FP._0_33;
            var tensor = new FPVector3(areaX, areaY, areaZ);

            return tensor;
        }

        //first called compression method
        private void UpdateCompression(Frame f, VehicleData* vehicle, Transform3D* transform, PhysicsBody3D* body)
        {
            vehicle->TotalCompression = 0;

            for (int i = 0; i < Axis.Length; i++)
            {
                var axis = vehicle->Axis.GetPointer(i);
                axis->Grounded = false;
                UpdateCompression(f, Axis[i], axis, transform, body);
                if (axis->Grounded) vehicle->Grounded = true;
                vehicle->TotalCompression += axis->LeftWheel.Compression + axis->RightWheel.Compression;
            }
            
            var forwardVelocity = FPMath.Clamp(FPVector3.Dot(body->Velocity, transform->Forward), 0,
                FP.UseableMax);
            for (int i = 0; i < Axis.Length; i++)
            {
                FP downForce = i == 0 ? forwardVelocity * FrontWingCoeficient :
                    i == Axis.Length - 1 ? forwardVelocity * RearWingCoeficient : default;
                UpdateDownforce(Axis[i], vehicle->Axis.GetPointer(i), transform, body, downForce );
            }
        }

        private void UpdateDownforce(AxisConfig axisConfig, AxisData* axisData, Transform3D* transform,
            PhysicsBody3D* body, FP downforce)
        {
            axisData->Downforce = downforce;
            var downForcePosition = transform->TransformPoint(new FPVector3(0, 0, axisConfig.Offset));
            body->AddForceAtPosition(axisData->Downforce * transform->Down, downForcePosition,
                transform);
        }

        //second called compression method, per axis
        private void UpdateCompression(Frame f, AxisConfig axisConfig, AxisData* axisData, Transform3D* transform,
            PhysicsBody3D* body)
        {
            var down = transform->Down;
            var axisRelativeOffset = FPVector3.Forward * axisConfig.Offset + FPVector3.Down * axisConfig.Height;
            var halfWidth = axisConfig.Width / 2;
            var sideRelativeOffset = FPVector3.Right * halfWidth;

            var wheelOffsetLeft = transform->TransformPoint(axisRelativeOffset - sideRelativeOffset);
            var wheelOffsetRight = transform->TransformPoint(axisRelativeOffset + sideRelativeOffset);

            var groundedRight = UpdateCompression(f, axisConfig, &axisData->RightWheel, wheelOffsetRight, down);
            var groundedLeft = UpdateCompression(f, axisConfig, &axisData->LeftWheel, wheelOffsetLeft, down);

            // apply anti-roll
            var antiRollLeft = FP._1;
            var antiRollRight = FP._1;
            if (axisConfig.TorsionBarFactor > FP._0)
            {
                var antiRollFactor = axisConfig.TorsionBarFactor *
                                     (axisData->RightWheel.Compression - axisData->LeftWheel.Compression);

                antiRollRight += antiRollFactor;
                antiRollLeft -= antiRollFactor;
            }

            var forceRight = UpdateSuspensionForce(f, axisConfig, &axisData->RightWheel, antiRollRight);
            var forceLeft = UpdateSuspensionForce(f, axisConfig, &axisData->LeftWheel, antiRollLeft);
            var up = -down;
            if (UseGlobalUp) up = FPVector3.Project(up, FPVector3.Up);

            axisRelativeOffset = FPVector3.Forward * axisConfig.Offset;
            wheelOffsetLeft = transform->TransformPoint(axisRelativeOffset - sideRelativeOffset);
            wheelOffsetRight = transform->TransformPoint(axisRelativeOffset + sideRelativeOffset);

            body->AddForceAtPosition(forceLeft * up, wheelOffsetLeft, transform);
            body->AddForceAtPosition(forceRight * up, wheelOffsetRight, transform);
            axisData->Grounded = groundedRight || groundedLeft;
            if (Debug)
            {
                Draw.Ray(wheelOffsetLeft, forceLeft * up, ColorRGBA.Cyan);
                Draw.Ray(wheelOffsetRight, forceRight * up, ColorRGBA.Cyan);
            }
        }

         private FP UpdateSuspensionForce(Frame f, AxisConfig axisConfig, WheelData* wheel, FP antiRollFactor)
        {
            if (wheel->CompressionPrevious == 0 || wheel->Compression == 0)
            {
                wheel->CompressionPrevious = wheel->Compression;
                wheel->SuspensionForce = 0;
                return 0;
            }

            var force = wheel->Compression * axisConfig.Spring * antiRollFactor;
            var updateRate = f.SessionConfig.UpdateFPS;
            var contactSpeed = (wheel->Compression - wheel->CompressionPrevious) * updateRate;
            var dampingForce = contactSpeed * axisConfig.Damper;
            force += dampingForce;
            force = FPMath.Max(force, 0);
            wheel->CompressionPrevious = wheel->Compression;
            wheel->SuspensionForce = force;
            return force;
        }

        //third called compression method, per wheel
        private bool UpdateCompression(Frame f, AxisConfig axisConfig, WheelData* wheel,
            FPVector3 castPosition,
            FPVector3 down)
        {
            wheel->Modifier = default;
            wheel->AnchorPosition = castPosition;
            wheel->HitPointPrevious = wheel->HitPoint;
            wheel->GroundedPrevious = wheel->Grounded;

            // hit everything, except triggers... compute data (normal and point)
            var options = (QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo) & ~QueryOptions.HitTriggers;
            var rayLength = axisConfig.SuspensionTravel + WheelConfig.WheelRadius;
            var hit = f.Physics3D.Raycast(castPosition, down, rayLength, WheelConfig.WheelCollisionMask,
                options);
            if (Debug)
            {
                Draw.Ray(castPosition, down * rayLength, ColorRGBA.Red);
            }


            if (hit.HasValue)
            {
                var hitData = hit.Value;
                if (hitData.IsStatic)
                {
                    var staticData = hitData.GetStaticData(f);
                    if (staticData.Asset != default)
                    {
                        wheel->Modifier.Id = staticData.Asset.Id;
                    }
                }

                if (Debug) Draw.Sphere(hitData.Point, FP._0_10, ColorRGBA.Red);
                var distance = hitData.CastDistanceNormalized * rayLength - WheelConfig.WheelRadius;
                var normalizedDistance = FPMath.Clamp01(distance / axisConfig.SuspensionTravel);
                wheel->Compression = 1 - normalizedDistance;
                wheel->HitPoint = hitData.Point;
                wheel->HitNormal = hitData.Normal;
                wheel->Grounded = true;
            }
            else
            {
                wheel->HitPoint = default;
                wheel->HitNormal = default;
                wheel->Compression = 0;
                wheel->Grounded = false;
            }

            return hit.HasValue;
        }

    }
}
