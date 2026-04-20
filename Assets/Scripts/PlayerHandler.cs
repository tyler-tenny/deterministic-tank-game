using UnityEngine;
using UnityEngine.InputSystem;
using Quantum;
using Photon.Deterministic;
using Cinemachine;

public class PlayerHandler : MonoBehaviour
{
    [SerializeField] QuantumEntityView entityView;
    [SerializeField] GameObject firePoint;
    [Header("Camera Stuff")]
    [SerializeField] GameObject turret;
    [SerializeField] GameObject gun;
    [SerializeField] GameObject lookObject;
    [SerializeField] int lookSensitivity = 15;

    private Quantum.Input _accumulatedInput;
    private bool _resetAccumulatedInput;
    private int _lastAccumulateFrame;

    private void OnEnable()
    {
        QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    public void PollInput(CallbackPollInput callback)
    {
        AccumulateInput();

        callback.SetInput(_accumulatedInput, DeterministicInputFlags.Repeatable);

        _resetAccumulatedInput = true;
        _accumulatedInput.LookRotationDelta = default;
    }

    private void ProcessStandaloneInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();

            Vector2 lookRotationDelta = new Vector2(-mouseDelta.y, mouseDelta.x);
            
            lookRotationDelta *= lookSensitivity / 60f;
            _accumulatedInput.LookRotationDelta += lookRotationDelta.ToFPVector2();

            // lookObject.transform.rotation *= Quaternion.AngleAxis(lookRotationDelta.y, Vector3.up);
        }
        else
        {
            Debug.Log("No mouse");
        }

        _accumulatedInput.Left = UnityEngine.Input.GetKey(KeyCode.A);
        _accumulatedInput.Right = UnityEngine.Input.GetKey(KeyCode.D);
        _accumulatedInput.Forward = UnityEngine.Input.GetKey(KeyCode.W);
        _accumulatedInput.Backward = UnityEngine.Input.GetKey(KeyCode.S);
        _accumulatedInput.Jump = UnityEngine.Input.GetKey(KeyCode.Space);
        _accumulatedInput.Escape = UnityEngine.Input.GetKey(KeyCode.Escape);
        _accumulatedInput.Fire = UnityEngine.Input.GetKey(KeyCode.Mouse0);
        _accumulatedInput.Aim = UnityEngine.Input.GetKey(KeyCode.Mouse1);
    }

    private void AccumulateInput()
    {
        if (_lastAccumulateFrame == Time.frameCount)
            return;

        _lastAccumulateFrame = Time.frameCount;

        if (_resetAccumulatedInput)
        {
            _resetAccumulatedInput = false;
            _accumulatedInput = default;
        }

        ProcessStandaloneInput();
    }

    private void Update()
    {
        AccumulateInput();
    }


}
