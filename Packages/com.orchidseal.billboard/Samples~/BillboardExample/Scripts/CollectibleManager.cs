#if UDONSHARP
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace OrchidSeal.Billboard.BillboardExample
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CollectibleManager : UdonSharpBehaviour
    {
        private DataList respawnCollectibles = new DataList();
        [SerializeField] private float respawnDurationSeconds = 5.0f;
        private DataList respawnStartSeconds = new DataList();
        [SerializeField] private AudioClip collectSound;

        public void _CheckRespawn()
        {
            var respawnTime = Time.time;
            
            for (var i = 0; i < respawnStartSeconds.Count;)
            {
                if (respawnStartSeconds[i].Float + respawnDurationSeconds > respawnTime)
                {
                    i++;
                    continue;
                }
                
                ((GameObject) respawnCollectibles[i].Reference).SetActive(true);
                respawnCollectibles.RemoveAt(i);
                respawnStartSeconds.RemoveAt(i);
            }
            
            SendCustomEventDelayedSeconds(nameof(_CheckRespawn), 1f);
        }
        
        public void _Collect(GameObject collectible)
        {
            collectible.SetActive(false);
            respawnCollectibles.Add(collectible);
            respawnStartSeconds.Add(Time.time);
            // TODO: Create a sound bank for different sounds per collectible.
            AudioSource.PlayClipAtPoint(collectSound, collectible.transform.position, 0.3f);
        }

        private void Start()
        {
            SendCustomEventDelayedSeconds(nameof(_CheckRespawn), 1f);
        }
    }
}
#endif
