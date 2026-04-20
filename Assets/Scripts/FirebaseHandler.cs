using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Analytics;
using Firebase.Extensions;
using Quantum;

public class FirebaseHandler : MonoBehaviour
{
    private DatabaseReference dbRef;
    private Gameplay gameplay;
    // Start is called before the first frame update
    void Start()
    {
            var frame = QuantumRunner.DefaultGame?.Frames?.Predicted;

            if (frame == null)
                return;
            
            gameplay = frame.GetSingleton<Gameplay>();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase ready in Gameplay singleton.");
            }
            else
            {
                Debug.LogError("Firebase dependency error: " + task.Result);
            }
        });
        
    }
    // Update is called once per frame
    void Update()
    {
        var frame = QuantumRunner.DefaultGame?.Frames?.Predicted;

        if (frame == null) return;

        foreach (var entry in frame.ResolveDictionary(gameplay.PlayerData))
        {
            PlayerData playerData = entry.Value;
            
            var playerNickname = frame.GetPlayerData(playerData.PlayerRef).PlayerNickname;

            SavePlayerData(playerNickname, playerData);
        }
    }

    public void SavePlayerData(string playerNickname, PlayerData data)
    {
        string json = JsonUtility.ToJson(data);
        dbRef.Child("players").Child(playerNickname).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task => {
                if (task.IsCompleted) Debug.Log("Saved data for " + playerNickname);
                else Debug.LogError("Save failed: " + task.Exception);
            });
    }
}
