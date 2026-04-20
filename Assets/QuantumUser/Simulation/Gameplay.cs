using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using Quantum;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe partial struct Gameplay
    {
        public void ConnectPlayer(Frame frame, PlayerRef playerRef)
        {
            var players = frame.ResolveDictionary(PlayerData);

            if (players.TryGetValue(playerRef, out var playerData) == false)
            {
                playerData = new PlayerData();
                playerData.PlayerRef = playerRef;
                playerData.StatisticPosition = int.MaxValue;
                playerData.IsAlive = true;
                playerData.IsConnected = false;
                playerData.Kills = 0;
                playerData.Deaths = 0;
                playerData.Score = 0;
            }

            if (playerData.IsConnected)
                return;

            Debug.Log($"{playerRef} connected.");

            playerData.IsConnected = true;
            players[playerRef] = playerData;
        }

        public void DisconnectPlayer(Frame frame, PlayerRef playerRef)
        {
            var players = frame.ResolveDictionary(PlayerData);

            if (players.TryGetValue(playerRef, out var playerData))
            {
                if (playerData.IsConnected)
                {
                    Log.Warn($"{playerRef} disconnected.");
                }

                playerData.IsConnected = false;
                playerData.IsAlive = false;
                players.Remove(playerRef);
            }

            var playerEntity = frame.GetPlayerEntity(playerRef);
            RemoveAvatar(frame, playerEntity);
        }

        public void StartGameplay(Frame frame)
        {
            SetState(frame, _GameplayState.matchRunning);
            remainingTime = gameTotalTime;
            
            var players = frame.ResolveDictionary(PlayerData);
            foreach (var playerPair in players)
            {
                var playerData = playerPair.Value;

                playerData.RespawnTimer = 0;
                playerData.Kills = 0;
                playerData.Deaths = 0;
                playerData.StatisticPosition = int.MaxValue;
                
                players[playerData.PlayerRef] = playerData;

                RespawnPlayer(frame, playerData.PlayerRef);
            }
        }

        public void StopGameplay(Frame frame)
        {
            SetState(frame, _GameplayState.matchEnd);
        }

        public void TryRespawnPlayers(Frame frame)
        {
            var players = frame.ResolveDictionary(PlayerData);
            foreach (var playerPair in players)
            {
                var playerData = playerPair.Value;

                if (playerData.RespawnTimer <= 0)
                    continue;

                playerData.RespawnTimer -= frame.DeltaTime;
                players[playerData.PlayerRef] = playerData;

                if (playerData.RespawnTimer <= 0)
                {
                    RespawnPlayer(frame, playerPair.Key);
                    continue;
                }

                
            }
        }

        private void RemoveAvatar(Frame frame, EntityRef playerEntity)
        {
            if (playerEntity.IsValid)
            {
                if (frame.Unsafe.TryGetPointer(playerEntity, out Turret* desTurret)) frame.Destroy(desTurret->barrel);
                if (frame.Unsafe.TryGetPointer(playerEntity, out TurretUpdater* desUpdater)) frame.Destroy(desUpdater->turret);

                Debug.Log("Destroying playerEntity");
                frame.Destroy(playerEntity);
            }
        }

        public void RespawnPlayer(Frame frame, PlayerRef playerRef)
        {
            Debug.Log("Respawning player");
            var players = frame.ResolveDictionary(PlayerData);

            // Despawn old player object if it exists.
            EntityRef playerEntity = frame.GetPlayerEntity(playerRef);
            if(playerEntity != null) RemoveAvatar(frame, playerEntity);

            // Don't spawn the player for disconnected clients.
            if (players.TryGetValue(playerRef, out PlayerData playerData) == false || playerData.IsConnected == false)
                return;

            // Update player data.
            playerData.IsAlive = true;
            players[playerRef] = playerData;

            //get runtimeplayer
            RuntimePlayer runtimePlayer = frame.GetPlayerData(playerRef);
            var entityPrototypeAsset = frame.FindAsset<EntityPrototype>(runtimePlayer.PlayerAvatar);

            //create the player entity
            playerEntity = frame.Create(entityPrototypeAsset);
            //grab the default updater from player and create a turret entity
            frame.Unsafe.TryGetPointer<TurretUpdater>(playerEntity, out TurretUpdater* updater);
            updater->turret = frame.Create(updater->turretProto);
            updater->body = playerEntity;

            //grab the default turret from updater and create a barrel entity
            frame.Unsafe.TryGetPointer<Turret>(updater->turret, out Turret* turret);
            turret->barrel = frame.Create(turret->barrelProto);
            turret->body = playerEntity;

            frame.Unsafe.TryGetPointer<Turret>(playerEntity, out Turret* playerTurretBackRef);
            playerTurretBackRef->barrel = turret->barrel;
            playerTurretBackRef->body  = turret->body;

            //player linking
            frame.AddOrGet<PlayerLink>(playerEntity, out var player);
            player->PlayerRef = playerRef;

            var playerTransform = frame.Unsafe.GetPointer<Transform3D>(playerEntity);

            RandomPlacement(frame, ref playerTransform);
        }

        void RandomPlacement(Frame frame, ref Transform3D* t)
        {
            t->Position = new Photon.Deterministic.FPVector3(frame.RNG->Next(-50, 51), 10, frame.RNG->Next(-50, 51));
        }

        public void KillPlayer(Frame frame, PlayerRef victim, PlayerRef perp = default)
        {
            var players = frame.ResolveDictionary(PlayerData);
            if( players.TryGetValue(victim, out PlayerData playerData) )
            {
                Debug.Log("Killing player");
                playerData.IsAlive = false;
                playerData.Deaths += 1;
                playerData.RespawnTimer = 3;
                players[victim] = playerData;
            }

            RemoveAvatar(frame, frame.GetPlayerEntity(victim));

            if (perp == default || victim == perp) return;

            if (players.TryGetValue(perp, out PlayerData perpData))
            {
                Debug.Log("Awarding kill");
                perpData.Kills += 1;
                players[perp] = perpData;
            }

            Debug.Log($"K/D/A: {playerData.Kills.ToString()}/{playerData.Deaths.ToString()}");
        }

        private void SetState(Frame frame, _GameplayState state)
        {
            State = state;
            frame.Events.GameplayStateChanged(state);
        }

        public  void SavePlayerData(string playerId, PlayerData data)
        {

        }
        public class CommandRespawn : DeterministicCommand
        {
            public Gameplay _gameplay;
            public PlayerRef player;
            public override void Serialize(Photon.Deterministic.BitStream stream)
            {
                stream.Serialize(ref player);
            }
            public void Execute(Frame f, Gameplay gameplay)
            {
                _gameplay = gameplay;
                _gameplay.KillPlayer(f, player);
                _gameplay.RespawnPlayer(f, player);
            }
        }
    }
}
