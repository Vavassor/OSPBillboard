#if UDONSHARP
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace OrchidSeal.Billboard.BillboardExample
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerController : UdonSharpBehaviour
    {
        [SerializeField] private CollectibleManager collectibleManager;
        [SerializeField] private LayerMask colliderLayerMask;
        private VRCPlayerApi localPlayer;
        private Collider[] overlapCapsuleColliders = new Collider[16];
        
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }
        
        private void Update()
        {
            var playerPosition = localPlayer.GetPosition();
            var collidersCount = Physics.OverlapCapsuleNonAlloc(playerPosition, playerPosition + 1.25f * Vector3.up, 0.2f, overlapCapsuleColliders, colliderLayerMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < collidersCount; i++)
            {
                var overlappedObject = overlapCapsuleColliders[i].gameObject;
                if (overlappedObject && overlappedObject.name.Contains("Collectible"))
                {
                    collectibleManager.Collect(overlappedObject);
                }
            }
        }
    }
}

#endif // UDONSHARP
