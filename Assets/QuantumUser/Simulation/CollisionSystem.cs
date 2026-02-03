using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class CollisionSystem : SystemSignalsOnly, ISignalOnCollisionEnter3D, ISignalOnTriggerEnter3D
    {
        public void OnCollisionEnter3D(Frame f, CollisionInfo3D info)
        {
            if (f.Unsafe.TryGetPointer<Projectile>(info.Entity, out var projectile))
            {
                if(info.Other == projectile->owner)
                {
                    info.IgnoreCollision = true;
                    return;
                }
                f.Signals.ExplodeProjectile(info.Entity);

            }
        }

        public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
        {
            // Explosion Collision
            if (f.Unsafe.TryGetPointer<Explosion>(info.Entity, out var tank))
            {
                // Tank hit by explosion
                if (f.Unsafe.TryGetPointer<PlayerVehicle>(info.Other, out var vehicle))
                {
                    f.Signals.ExplosionHitTank(info.Entity, info.Other);
                }

            }
        }
    }
}
