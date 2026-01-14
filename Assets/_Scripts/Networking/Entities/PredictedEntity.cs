using UnityEngine;
using ExileSurvival.Networking.Data;

namespace ExileSurvival.Networking.Entities
{
    public abstract class PredictedEntity : NetworkEntity
    {
        // State history for interpolation/reconciliation
        
        public abstract void OnServerInput(PlayerInputPacket input);
        public abstract void OnClientState(PlayerStatePacket state);
    }
}