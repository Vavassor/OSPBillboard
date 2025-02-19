using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace OrchidSeal.Billboard.BillboardExample
{
#if UDONSHARP
    public class PlayerController : UdonSharpBehaviour
#else
    public class PlayerController : MonoBehaviour
#endif
    {

    }
}
