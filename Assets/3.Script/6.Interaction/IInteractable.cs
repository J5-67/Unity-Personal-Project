using UnityEngine;

namespace Interaction
{
    public interface IInteractable
    {
        void OnInteract(GameObject instigator);
    }
}
