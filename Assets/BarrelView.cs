using Quantum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class BarrelView : QuantumEntityView
{
    [SerializeField] Animator anim;
    QuantumGame _game;

    public void OnInstantiated(QuantumGame game)
    {
        _game = game;
        QuantumEvent.Subscribe<EventPlayerShot>(listener: this, OnPlayerShot);
        QuantumEvent.Subscribe<EventPlayerHit>(listener: this, OnPlayerHit);
    }

    private void OnPlayerShot(EventPlayerShot e)
    {
        if (_game.Frames.Predicted.Unsafe.TryGetPointer(EntityRef, out TurretUpdater* tu))
        {
            if (e.Entity != tu->body) return;
            anim.Play("fire");
        }
    }

    private void OnPlayerHit(EventPlayerHit e)
    {
        if (_game.Frames.Predicted.Unsafe.TryGetPointer(EntityRef, out TurretUpdater* tu))
        {
            if (e.Entity != tu->body) return;
            anim.Play("hit");
        }
    }
}
