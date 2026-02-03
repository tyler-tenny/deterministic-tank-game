namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine;
    using UnityEngine.Scripting;

    [Preserve]
    public unsafe class DriveSystem : SystemMainThreadFilter<DriveSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* Body;
            public VehicleData* Vehicle;
        }
        public override void Update(Frame f, ref Filter filter)
        {
            var config = f.FindAsset(filter.Vehicle->Config);

            Input* input = default;
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                input = f.GetPlayerInput(playerLink->PlayerRef);
            }

            if (input == null) return;

            config.UpdateVehicle(f, ref filter, input);
        }
    }
}
