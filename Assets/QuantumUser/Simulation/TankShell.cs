using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;

// This can probably be deleted altogether

namespace Quantum
{
    public class TankShell : MonoBehaviour
    {
        [SerializeField]
        CharacterController3D kcc;
        FP maxVelocity;

        TankShell(Transform3D t, FP maxVelocity)
        {
            var fireAngle = t.Rotation;
            kcc.Velocity = maxVelocity * fireAngle.AsEuler.Normalized;
            this.maxVelocity = maxVelocity;
        }
    }
}
