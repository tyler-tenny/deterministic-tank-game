using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using Quantum;
using UnityEngine.Scripting;
using UnityEngine.UI;
using TMPro;

namespace Quantum
{
    public class ScoreboardManager : MonoBehaviour
    {
        public GameObject scoreboardPanel;
        public Transform scoreboardContent;
        public GameObject playerEntryPrefab;
        private bool _isVisible = false;
        
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                _isVisible = !_isVisible;
                scoreboardPanel.SetActive(_isVisible);
            }

            if (_isVisible)
            {
                UpdateScoreboard();
            }
        }

        private void UpdateScoreboard()
        {
            var frame = QuantumRunner.DefaultGame?.Frames?.Predicted;

            if (frame == null)
                return;
            
            var gameplay = frame.GetSingleton<Gameplay>();
            
            foreach (Transform child in scoreboardContent)
            {
                Destroy(child.gameObject);
            }

            foreach (var entry in frame.ResolveDictionary(gameplay.PlayerData))
            {
                var playerData = entry.Value;
                GameObject newEntry = Instantiate(playerEntryPrefab, scoreboardContent);
                TextMeshProUGUI[] texts = newEntry.GetComponentsInChildren<TextMeshProUGUI>();

                var playerName = frame.GetPlayerData(playerData.PlayerRef).PlayerNickname;

                texts[0].text = playerName.ToString() != "" ? playerName : "---";
                texts[1].text = playerData.Score.ToString();
                texts[2].text = playerData.Kills.ToString();
                texts[3].text = playerData.Deaths.ToString();
            }
        }
    }
}
