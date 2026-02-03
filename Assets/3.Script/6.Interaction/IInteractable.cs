using UnityEngine;

namespace Interaction
{
    // [유니] 상호작용 가능한 모든 오브젝트의 조상님! 
    // 훅, 총알, 플레이어 등 누구든 이 인터페이스가 달린 놈을 건드리면 반응할 수 있어!
    public interface IInteractable
    {
        // instigator: 상호작용을 시도한 주체 (예: 플레이어, 투사체 등)
        void OnInteract(GameObject instigator);
    }
}
