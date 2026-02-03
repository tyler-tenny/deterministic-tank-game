using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using Quantum;

public class DebugEntityRenderer : MonoBehaviour
{
    public QuantumEntityView View;

    void LateUpdate()
    {
        if (View != null && View.EntityRef.IsValid)
        {
            var game = QuantumRunner.Default.Game;
            var frame = game.Frames?.Predicted;
            if (frame != null && frame.TryGet<Transform3D>(View.EntityRef, out var t))
            {
                transform.position = t.Position.ToUnityVector3();
                transform.rotation = t.Rotation.ToUnityQuaternion();
            }
        }
    }
}