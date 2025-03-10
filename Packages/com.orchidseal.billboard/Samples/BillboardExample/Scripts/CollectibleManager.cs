#if UDONSHARP
using UdonSharp;
using UnityEngine;

namespace OrchidSeal.Billboard.BillboardExample
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CollectibleManager : UdonSharpBehaviour
    {
        public void Collect(GameObject collectible)
        {
            Destroy(collectible);
        }
    }
}
#endif
