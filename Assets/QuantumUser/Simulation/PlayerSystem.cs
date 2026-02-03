namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine;
    using UnityEngine.Scripting;
    using UnityEngine.Windows;


    [Preserve]
    public unsafe class PlayerSystem : SystemMainThreadFilter<PlayerSystem.Filter>, ISignalExplosionHitTank
    {

        public struct Filter 
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* Body;
            public PlayerVehicle* PlayerVehicle;
            public FireCoolDown* FireCoolDown;
            public TurretUpdater* TurretUpdater;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            var gameplay = f.Unsafe.GetPointerSingleton<Gameplay>();

            filter.FireCoolDown->fireCoolDown -= f.DeltaTime;

            HandleInputs(f, ref filter);

            if (filter.Transform->Position.Y <= -(FP._10 * FP._5))
            {
                Debug.Log("Killing out of bounds player");
                KillTank(f, filter.Entity);
            }
        }

        private void HandleInputs(Frame f, ref Filter filter)
        {
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* playerLink))
            {
                if (!f.GetPlayerEntity(playerLink->PlayerRef).IsValid) return;
            }
            else return;

            Input* input = default;

            input = f.GetPlayerInput(playerLink->PlayerRef);
            filter.PlayerVehicle->lookDirection.Y += input->LookRotationDelta.Y;
            filter.PlayerVehicle->lookDirection.X = FPMath.Clamp(filter.PlayerVehicle->lookDirection.X + input->LookRotationDelta.X, -90, 90);

            var lookDirection = filter.PlayerVehicle->lookDirection;
            
            //get updater, grab entityref to turret
            f.Unsafe.TryGetPointer(filter.Entity, out TurretUpdater* updater);
            EntityRef turret = updater->turret;

            //get turret component, grab entityref to barrel
            f.Unsafe.TryGetPointer(turret, out Turret* turretComponent);
            EntityRef barrel = turretComponent->barrel;
            //end up with a barrel and a turret as entity refs
            f.Unsafe.TryGetPointer(turret, out Transform3D* turretTransform);
            f.Unsafe.TryGetPointer(barrel, out Transform3D* barrelTransform);

            FP rotationSmoothing = FP._0_10;

            FPVector3 rootRotation = filter.Transform->Rotation.AsEuler;
            

            FPQuaternion desiredTurretRotation = FPQuaternion.Euler(rootRotation.X, lookDirection.Y, rootRotation.Z);
            FPQuaternion desiredBarrelRotation = FPQuaternion.Euler(lookDirection.X + 180, lookDirection.Y, 180);
            
            turretTransform->Rotation = FPQuaternion.Slerp(turretTransform->Rotation, desiredTurretRotation, rotationSmoothing);
            barrelTransform->Rotation = FPQuaternion.Slerp(barrelTransform->Rotation, desiredBarrelRotation, rotationSmoothing);

            turretTransform->Position = (filter.Transform->Position + (filter.Transform->Up * (FP._0_50 - FP._0_05)));
            barrelTransform->Position = (filter.Transform->Position + (filter.Transform->Up * FP._0_50));
            


            if (input->Escape)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (input->Fire)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                TryFire(f, ref filter);
            }

            if (input->Aim.WasPressed)
            {
                f.Events.ToggleAim(filter.Entity);
            }
        }

        private void TryFire(Frame f, ref Filter filter)
        {
            if (filter.FireCoolDown->fireCoolDown <= 0)
            {
                Debug.Log("Player fired");
                // To-Do: Move the TankShellPrototype to a config file
                f.Signals.TankShoot(filter.Entity, filter.Transform->TransformPoint(FPVector3.Up * FP._0_50), filter.FireCoolDown->projectilePrototype);
                // To-Do: Move this to a config as well
                filter.FireCoolDown->fireCoolDown = 1;
                // Move both to a TankConfig file?
                // https://doc.photonengine.com/quantum/current/tutorials/asteroids/9-shooting
            }
            else
            {
                Debug.Log("Player tried to fire on cooldown");
            }
        }

        public void ExplosionHitTank(Frame f, EntityRef explosion, EntityRef victim)
        {
            Explosion* e = f.Unsafe.GetPointer<Explosion>(explosion);
            DamageTank(f, victim, e->damage, e->owner);
        }

        private void DamageTank(Frame f, EntityRef victim, int damage, EntityRef owner = default)
        {
            PlayerVehicle* target = f.Unsafe.GetPointer<PlayerVehicle>(victim);
            target->currentHealth -= (int)(damage * (owner == victim ? 0.1f : 1));
            f.Events.PlayerHealthChanged(victim, target->currentHealth);
            // Debug.Log("Tank health: " +  target->currentHealth);
            if (victim != owner)
            {
                f.Events.SoundPlayed(f.Unsafe.GetPointer<Transform3D>(owner)->Position, 0, true, f.Unsafe.GetPointer<PlayerLink>(owner)->PlayerRef);
            }

            f.Events.PlayerHit(victim);

            if (target->currentHealth <= 0)
            {
                KillTank(f, victim, owner);
            }
        }

        private void KillTank(Frame f, EntityRef victim, EntityRef perpetrator = default) 
        {
            PlayerRef p = default;

            if (f.Unsafe.TryGetPointer(perpetrator, out PlayerLink* perpLink))
            {
                p = perpLink->PlayerRef;
            }

            if (f.Unsafe.TryGetPointer(victim, out PlayerLink* playerLink))
            {
                var gameplay = f.Unsafe.GetPointerSingleton<Gameplay>();

                PlayerRef player = playerLink->PlayerRef;

                gameplay->KillPlayer(f, player, p);
            }
        }
    }
}
