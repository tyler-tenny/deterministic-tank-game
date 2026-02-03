namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine.Scripting;

    [Preserve]
    public unsafe class PlayerSpawnSystem : SystemSignalsOnly, ISignalOnPlayerAdded
    {
        public void OnPlayerAdded(Frame frame, PlayerRef player, bool firstTime)
        {
            {
                var gameplay = frame.Unsafe.GetPointerSingleton<Gameplay>();

                gameplay->RespawnPlayer(frame, player);
                gameplay->ConnectPlayer(frame, player);
                
            }
        }
    }
}
