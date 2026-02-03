using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;


public class UiSignalHandler : MonoBehaviour
{
    [SerializeField] private PlayerRef _player;
    public void OnRespawnButtonClicked()
    {
        Debug.Log("Clicked respawn");
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

