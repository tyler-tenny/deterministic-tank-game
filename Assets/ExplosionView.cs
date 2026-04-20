using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;

public unsafe class ExplosionView : QuantumEntityView
{
    [SerializeField] GameObject explosion;
    [SerializeField] AudioSource source;

    public void OnInstantiated(QuantumGame game)
    {
        var frame = game.Frames.Predicted;

        if (frame.Unsafe.TryGetPointer<Explosion>(EntityRef, out var ex) == true)
        {
            float r = (2 * (float)ex->radius);
            frame.Events.SoundPlayed(FPMathUtils.ToFPVector3(transform.position), 0, false, default);
            explosion.transform.localScale = new Vector3(r, r ,r);
        }
        
    }
}
