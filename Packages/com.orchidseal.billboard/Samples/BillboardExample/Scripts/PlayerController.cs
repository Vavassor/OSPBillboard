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
        [SerializeField] private string collectibleTag = "Collectible";
        [SerializeField] private LayerMask colliderLayerMask;
        [SerializeField] private string interactableTag = "Interactable";
        [SerializeField] private float interactionDistanceNonVr = 1.5f;
        [SerializeField] private float interactionDistanceVr = 0.05f;
        [SerializeField] private InteractionManager interactionManager;
        private VRCPlayerApi localPlayer;
        private Collider[] overlapColliders = new Collider[16];
        private RaycastHit[] raycastHits = new RaycastHit[16];
        [SerializeField] private float touchDetectionSeconds = 0.25f;
        
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            
            if (localPlayer.IsUserInVR())
            {
                SendCustomEventDelayedSeconds(nameof(_UpdateTouchDetectionVr), touchDetectionSeconds);
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(_UpdateTouchDetectionNonVr), touchDetectionSeconds);
            }
        }

        public void _UpdateTouchDetectionNonVr()
        {
            var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var ray = new Ray(head.position, head.rotation * Vector3.forward);
            var hitCount = Physics.RaycastNonAlloc(ray, raycastHits, interactionDistanceNonVr, colliderLayerMask, QueryTriggerInteraction.Ignore);
            
            var closestDistance = float.MaxValue;
            GameObject closestObject = null;
            for (var i = 0; i < hitCount; i++)
            {
                var hitTransform = raycastHits[i].transform;
                if (!hitTransform) continue;
                var gameObj = hitTransform.gameObject;
                if (!gameObj || !gameObj.name.Contains(interactableTag) || raycastHits[i].distance >= closestDistance) continue;
                closestDistance = raycastHits[i].distance;
                closestObject = gameObj;
            }

            interactionManager._TouchInteractable(closestObject, VRCPlayerApi.TrackingDataType.Head);
            SendCustomEventDelayedSeconds(nameof(_UpdateTouchDetectionNonVr), touchDetectionSeconds);
        }

        public void _UpdateTouchDetectionVr()
        {
            UpdateTouch(VRCPlayerApi.TrackingDataType.LeftHand);
            UpdateTouch(VRCPlayerApi.TrackingDataType.RightHand);
            SendCustomEventDelayedSeconds(nameof(_UpdateTouchDetectionVr), touchDetectionSeconds);
        }

        private void UpdateTouch(VRCPlayerApi.TrackingDataType trackingType)
        {
            var trackingData = localPlayer.GetTrackingData(trackingType);
            var position = trackingData.position;
            var collidersCount = Physics.OverlapSphereNonAlloc(position, interactionDistanceVr, overlapColliders, colliderLayerMask);
            
            var closestDistance = float.MaxValue;
            GameObject closestObject = null;
            for (var i = 0; i < collidersCount; i++)
            {
                var overlappedObject = overlapColliders[i].gameObject;
                if (!overlappedObject || !overlappedObject.name.Contains(interactableTag)) continue;
                var distance = Vector3.SqrMagnitude(position - overlappedObject.transform.position);
                if (!(distance < closestDistance)) continue;
                closestDistance = distance;
                closestObject = overlappedObject;
            }
            
            interactionManager._TouchInteractable(closestObject, trackingType);
        }
        
        private void Update()
        {
            // We may be moving fast relative to collectibles, so cast instead of overlap.
            var playerPosition = localPlayer.GetPosition();
            var playerVelocity = localPlayer.GetVelocity();
            var direction = playerVelocity.normalized;
            var distance = playerVelocity.magnitude * Time.deltaTime;
            var hitCount = Physics.CapsuleCastNonAlloc(playerPosition, playerPosition + 1.25f * Vector3.up, 0.2f, direction,  raycastHits, distance,colliderLayerMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hitCount; i++)
            {
                var overlappedTransform = raycastHits[i].transform;
                if (overlappedTransform && overlappedTransform.name.Contains(collectibleTag))
                {
                    collectibleManager._Collect(overlappedTransform.gameObject);
                }
            }
        }
    }
}

#endif // UDONSHARP
