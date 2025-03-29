using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace OrchidSeal.Billboard.BillboardExample
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InteractionManager : UdonSharpBehaviour
    {
        [SerializeField] private TextAsset interactableData;
        [SerializeField] private GameObject[] menus = new GameObject[2];
        [SerializeField] private TMP_Text[] menuContents = new TMP_Text[2];
        [SerializeField] private TMP_Text[] menuSubtitles = new TMP_Text[2];
        [SerializeField] private TMP_Text[] menuTitles = new TMP_Text[2];
        private GameObject[] touchedObjects = new GameObject[2];
        
        public void _TouchInteractable(GameObject gameObj, VRCPlayerApi.TrackingDataType trackingType)
        {
            int windowIndex;
            switch (trackingType)
            {
                default:
                case VRCPlayerApi.TrackingDataType.Head:
                case VRCPlayerApi.TrackingDataType.LeftHand:
                    windowIndex = 0;
                    break;
                case VRCPlayerApi.TrackingDataType.RightHand:
                    windowIndex = 1;
                    break;
            }

            if (gameObj && touchedObjects[windowIndex] != gameObj)
            {
                // Place the menu to one side of the interactable.
                var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                var viewDirection = head.position - gameObj.transform.position;
                var offset = 0.35f * Vector3.Cross(viewDirection, Vector3.up).normalized;
                // Use the side closer to the middle of the screen.
                if (Vector3.Dot(head.rotation * Vector3.right, viewDirection) < 0)
                {
                    offset *= -1.0f;
                }
                menus[windowIndex].transform.position = gameObj.transform.position + offset;

                // Get the interactable info from a JSON file to show in the window.
                var idIndex = gameObj.name.IndexOf("OID", System.StringComparison.Ordinal);
                var hasId = int.TryParse(gameObj.name.Substring(idIndex + 3), out var interactableId);
                var hasData = VRCJson.TryDeserializeFromJson(interactableData.text, out var json);
                if (hasId && hasData)
                {
                    var interactable = json.DataDictionary[interactableId.ToString()].DataDictionary;
                    menuTitles[windowIndex].text = interactable["name"].String;
                    menuSubtitles[windowIndex].text = interactable["type"].String;
                    menuContents[windowIndex].text = interactable["description"].String;
                }
                else
                {
                    menuTitles[windowIndex].text = "Unknown";
                    menuSubtitles[windowIndex].text = "No type";
                    menuContents[windowIndex].text = "This object is a mystery.";
                }
            }
            
            menus[windowIndex].SetActive(gameObj);
            touchedObjects[windowIndex] = gameObj;
        }
    }
}
