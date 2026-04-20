using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class ExplosionSystem : SystemMainThreadFilter<ExplosionSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Explosion* Explosion;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            filter.Explosion->maxLifeTime -= f.DeltaTime;
            if(filter.Explosion->maxLifeTime <= 0)
            {
                f.Destroy(filter.Entity);
            }
        }
    }
}
