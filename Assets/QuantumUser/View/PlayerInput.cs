using Photon.Deterministic;
using UnityEngine;

namespace Quantum.JP
{    
    public class PlayerInput : MonoBehaviour
    {
        private void OnEnable() 
        {
            QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
        }

        public void PollInput(CallbackPollInput callback)
        {
            Quantum.Input i = new Quantum.Input();
            i.Left = UnityEngine.Input.GetKey(KeyCode.A);
            i.Right = UnityEngine.Input.GetKey(KeyCode.D);
            i.Forward = UnityEngine.Input.GetKey(KeyCode.W);
            i.Backward = UnityEngine.Input.GetKey(KeyCode.S);
            i.Fire = UnityEngine.Input.GetKey(KeyCode.Space);

            callback.SetInput(i, DeterministicInputFlags.Repeatable);
        }

        public void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Pressed respawn");
                var _playerArr = Quantum.QuantumRunner.Default.Game.GetLocalPlayers();
                var _player = _playerArr[0];
                Debug.Log(_player);
                var command = new Gameplay.CommandRespawn()
                {
                    player = _player,
                };
                Quantum.QuantumRunner.Default.Game.SendCommand(command);
            }
        }
    } 
}
