using Cinemachine;
using Photon.Deterministic;
using Quantum;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public unsafe class CustomEntityView : QuantumEntityView
{
    private QuantumGame _game;
    private bool _isLocalPlayer = false;
    [SerializeField] GameObject lookObject;
    [SerializeField] GameObject turret;
    [SerializeField] GameObject gun;
    [SerializeField] UIHealth health;
    [SerializeField] TextMeshProUGUI timerGUI;
    [SerializeField] AudioSource audioSource;
    [SerializeField] Animator anim;
    PlayerVehicle* vehicle;
    int playerHealth = 100;
    CinemachineVirtualCamera virtualCamera;
    int fovAimed = 30;
    int fovDefault = 70;

    float fovOnToggle = 70;
    float camDistanceOnToggle = 2;

    bool aiming = false;
    float timer = 0;

    float timeToZoom = 0.05f;

    private void Update()
    {
        if (_isLocalPlayer)
        {
            InterpolateZoom();
            timer = Mathf.Clamp(timer + Time.deltaTime, 0, timeToZoom);
        }
        
    }

    public void OnTimerUpdated(EventTimerUpdated e)
    {
        timerGUI.text = ((int)e.timeRemaining).ToString();
    }

    public void OnInstantiated(QuantumGame game)
    {
        lookObject = GameObject.FindWithTag("Player");
        lookObject.transform.position = transform.position;
        _game = game;
        var frame = _game.Frames.Predicted;

        QuantumEvent.Subscribe<EventPlayerHealthChanged>(listener: this, OnPlayerHealthChanged);
        QuantumEvent.Subscribe<EventToggleAim>(listener: this, OnToggleAim);
        QuantumEvent.Subscribe<EventTimerUpdated>(listener: this, OnTimerUpdated);
        QuantumEvent.Subscribe<EventSoundPlayed>(listener: this, OnSoundPlayed);
        QuantumEvent.Subscribe<EventPlayerShot>(listener: this, OnPlayerShot);
        QuantumEvent.Subscribe<EventPlayerHit>(listener: this, OnPlayerHit);

        Debug.Log("Instantiating Player");

        if (frame.Unsafe.TryGetPointer<PlayerLink>(EntityRef, out var playerLink) == false)
        {
            Debug.Log("Couldn't get playerlink at " + EntityRef);
            
            return;
        }

        if (game.PlayerIsLocal(playerLink->PlayerRef) == true)
        {
            Debug.Log("Local player added");
            _isLocalPlayer = true;
            virtualCamera = FindAnyObjectByType<CinemachineVirtualCamera>();
            virtualCamera.m_Follow = lookObject.transform;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        } else
        {
            health.gameObject.SetActive(false);
        }

        if (frame.Unsafe.TryGetPointer<PlayerVehicle>(EntityRef, out var v))
        {
            vehicle = v;
        }
    }

    private void OnPlayerShot(EventPlayerShot e)
    {
        if (e.Entity != EntityRef) return;

        anim.CrossFade("fire", 0);
    }

    private void OnPlayerHit(EventPlayerHit e)
    {
        if (e.Entity != EntityRef) return;

        anim.CrossFade("hit", 0);
    }

    // Plays local (client-side) sounds only
    private void OnSoundPlayed(EventSoundPlayed e)
    {
        if (e.Local && e.Player != default && _game != null)
        {
            if (!_game.PlayerIsLocal(e.Player)) return;
            Debug.Log("Playing local sound");
        } else
        {
            return;
        }

        audioSource.Play();
    }

    private void OnPlayerHealthChanged(EventPlayerHealthChanged e)
    {
        if (!_isLocalPlayer || EntityRef != e.entityRef) return;
        
        health.UpdateHealth(e.amount);
    }

    private void OnToggleAim(EventToggleAim e)
    {
        
        if (EntityRef != e.entityRef) return;
        Debug.Log("Toggling aim");
        var frame = _game.Frames.Verified;

        if (frame.Unsafe.TryGetPointer<PlayerLink>(EntityRef, out var playerLink) == false)
        {
            return;
        }
        else if (_game.PlayerIsLocal(playerLink->PlayerRef) == false) return;

        aiming = !aiming;

        if (virtualCamera)
        {
            timer = 0;
            fovOnToggle = virtualCamera.m_Lens.FieldOfView;
        }

        if (aiming)
        {
            StartAiming();
        } 
        else
        {
            StopAiming();
        }
    }

    void StartAiming()
    {
        if (virtualCamera)
        {
            
        }
    }

    void StopAiming()
    {
        if (virtualCamera)
        {
            
        }
    }

    void InterpolateZoom()
    {
        if (aiming)
        {
            LerpField(ref virtualCamera.m_Lens.FieldOfView, fovOnToggle, fovAimed);
            
        }
        else
        {
            LerpField(ref virtualCamera.m_Lens.FieldOfView, fovOnToggle, fovDefault);
        }
    }

    void LerpField(ref float val, float from, float to)
    {
        val = Mathf.Lerp(from, to, timer / timeToZoom);
    }

    protected override void ApplyTransform(ref UpdatePositionParameter param)
    {
        base.ApplyTransform(ref param);

        if (_isLocalPlayer == false)
        {
            return;
        }

        var frame = _game.Frames.Predicted;
        var lookDirection = frame.Unsafe.GetPointer<PlayerVehicle>(EntityRef)->lookDirection;

        Debug.Log("Applying Look Direction");

        //turret.transform.rotation = Quaternion.Euler(180, lookDirection.Y.AsFloat, 0);
        //gun.transform.rotation = Quaternion.Euler(lookDirection.X.AsFloat + 90, lookDirection.Y.AsFloat, 0);
        lookObject.transform.rotation = Quaternion.Euler(lookDirection.X.AsFloat, lookDirection.Y.AsFloat, 0);
        //var angles = lookObject.transform.localEulerAngles;

        // Rotates the camera and turret based on the camera rotation
        // lookObject.transform.localEulerAngles = new Vector3(angles.x, angles.y, 0);
        // turret.transform.localEulerAngles = new Vector3(0, 180 + angles.y, 0);
        // gun.transform.localEulerAngles = new Vector3(-angles.x - 90, 0, 0);

        // Keeps the turret and camera with the tank body
        lookObject.transform.position = transform.position;
        //turret.transform.position = transform.position + new Vector3(0, 0.45f);
    }
}
