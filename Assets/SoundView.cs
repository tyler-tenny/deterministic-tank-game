using Photon.Deterministic;
using Quantum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundView : QuantumEntityView
{
    [SerializeField] AudioSource source;
    [SerializeField] List<AudioClip> clips;
    private void Awake()
    {
        QuantumEvent.Subscribe<EventSoundPlayed>(listener: this, OnSoundPlayed);
    }

    public void OnSoundPlayed(EventSoundPlayed e)
    {
        if (e.Local) return;

        AudioSource.PlayClipAtPoint(clips[e.SoundID], e.Position.ToUnityVector3());
    }
}
