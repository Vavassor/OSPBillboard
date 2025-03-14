using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace OrchidSeal.Billboard.BillboardExample
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Billboard : UdonSharpBehaviour
    {
        [SerializeField] private float smoothTime = 0.08f;
        private VRCPlayerApi localPlayer;
        private Vector3 smoothPosition;
        private Vector3 smoothVelocity;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            smoothPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        }

        private void Update()
        {
            var position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            smoothPosition = Vector3.SmoothDamp(smoothPosition, position, ref smoothVelocity, smoothTime);
            transform.LookAt(smoothPosition);
        }
    }
}
