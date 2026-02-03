using Photon.Deterministic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class GameplayControl : SystemMainThread, ISignalOnPlayerAdded, ISignalOnPlayerRemoved, ISignalPlayerKilled
    {
        public override void Update(Frame frame)
        {
            var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
            frame.Events.TimerUpdated(gameplay->remainingTime);

            if (gameplay->State != _GameplayState.matchLimbo)
            {
                gameplay->remainingTime -= frame.DeltaTime;
            }

            if (gameplay->State == _GameplayState.matchLimbo && frame.ComponentCount<PlayerLink>() > 1)
            {
                gameplay->StartGameplay(frame);
            }

            if (gameplay->State == _GameplayState.matchRunning)
            {
                if (gameplay->remainingTime <= 0)
                {
                    gameplay->StopGameplay(frame);
                }
            }

            if (gameplay->remainingTime <= -3)
            {
                gameplay->StartGameplay(frame);
            }

            frame.Events.TimerUpdated(gameplay->remainingTime);
            gameplay->TryRespawnPlayers(frame);

            for (int i = 0; i < frame.PlayerCount; i++)
            {
                var command = frame.GetPlayerCommand(i) as Gameplay.CommandRespawn;
                command?.Execute(frame, *gameplay);
            }
        }

        void ISignalOnPlayerAdded.OnPlayerAdded(Frame frame, PlayerRef playerRef, bool firstTime)
        {
            var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
            gameplay->ConnectPlayer(frame, playerRef);
        }

        void ISignalOnPlayerRemoved.OnPlayerRemoved(Frame frame, PlayerRef playerRef)
        {
            var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
            gameplay->DisconnectPlayer(frame, playerRef);
        }

        void ISignalPlayerKilled.PlayerKilled(Frame frame, PlayerRef perpKillerRef, PlayerRef victimPlayerRef)
        {
            var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();
            var players = frame.ResolveDictionary(gameplay->PlayerData);

            if (players.TryGetValue(perpKillerRef, out PlayerData killerPlayerData))
            {
                killerPlayerData.Kills++;
                players[perpKillerRef] = killerPlayerData;
            }

            if (players.TryGetValue(victimPlayerRef, out PlayerData victimPlayerData))
            {
                victimPlayerData.Deaths++;
                victimPlayerData.IsAlive = false;
                victimPlayerData.RespawnTimer = gameplay->respawnTime;
                players[victimPlayerRef] = victimPlayerData;
            }

            frame.Events.PlayerKilled(perpKillerRef, victimPlayerRef);
        }
    }
}
