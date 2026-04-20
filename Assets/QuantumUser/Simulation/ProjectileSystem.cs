using Photon.Deterministic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class ProjectileSystem : SystemMainThreadFilter<ProjectileSystem.Filter>, ISignalTankShoot, ISignalExplodeProjectile, ISignalExplosionHitTank {

        public struct Filter
        {
            public EntityRef Entity;
            public Projectile* Projectile;
            public PhysicsBody3D* PhysicsBody;
            public Transform3D* Transform;
        }

        public void TankShoot(Frame f, EntityRef owner, FPVector3 position, AssetRef<EntityPrototype> projectilePrototype)
        {
            EntityRef projectileEntity = f.Create(projectilePrototype);
            Transform3D* projectileTransform = f.Unsafe.GetPointer<Transform3D>(projectileEntity);
            FPVector2 ownerLookDirection = f.Unsafe.GetPointer<PlayerVehicle>(owner)->lookDirection;
            f.Unsafe.GetPointer<Projectile>(projectileEntity)->owner = owner;

            // Eventually, this should just be spawned at the end of the barrel
            projectileTransform->Rotate(ownerLookDirection.X, ownerLookDirection.Y, 0);
            
            projectileTransform->Position = position;

            PhysicsBody3D* body = f.Unsafe.GetPointer<PhysicsBody3D>(projectileEntity);
            body->Velocity = 30 * projectileTransform->Forward;

            f.Events.SoundPlayed(position, 1, false, default);
            f.Events.PlayerShot(owner);
        }

        public void ExplodeProjectile(Frame f, EntityRef projectile)
        {
            EntityRef explosionEntity = f.Create(f.Unsafe.GetPointer<Projectile>(projectile)->explosion);

            Transform3D* explosionTransform = f.Unsafe.GetPointer<Transform3D>(explosionEntity);
            explosionTransform->Position = f.Unsafe.GetPointer<Transform3D>(projectile)->Position - new FPVector3(0, FP._0_20);

            PhysicsCollider3D* explosionCollider = f.Unsafe.GetPointer<PhysicsCollider3D>(explosionEntity);
            Explosion* explosionData = f.Unsafe.GetPointer<Explosion>(explosionEntity);
            explosionCollider->Shape.Sphere.Radius = explosionData->radius;
            explosionData->owner = f.Unsafe.GetPointer<Projectile>(projectile)->owner;

            f.Destroy(projectile);
        }

        public override void Update(Frame f, ref Filter filter)
        {
            filter.Projectile->MaxLifetime -= f.DeltaTime;

            if(filter.Projectile->MaxLifetime <= 0 )
            {
                ExplodeProjectile(f, filter.Entity);
            }
        }

        public void ExplosionHitTank(Frame f, EntityRef explosion, EntityRef victim)
        {
            Debug.Log("Explosion hit tank");
            Transform3D* t1 = f.Unsafe.GetPointer<Transform3D>(explosion);
            Transform3D* t2 = f.Unsafe.GetPointer<Transform3D>(victim);
            PhysicsBody3D* body2 = f.Unsafe.GetPointer<PhysicsBody3D>(victim);

            Explosion* ex = f.Unsafe.GetPointer<Explosion>(explosion);
 
            var pushDistanceModifier = FPMath.Clamp(1 - FPMath.Abs((t2->Position - t1->Position).Magnitude / (2 * ex->radius)), 0, 1);
            var pushDirection = (t2->Position + new FPVector3(0, FP._0_50) - t1->Position).Normalized;
            body2->AddLinearImpulse(pushDirection * 14 * pushDistanceModifier);

            f.Events.SoundPlayed(t2->Position, 2, false, default);
        }
    }
}
