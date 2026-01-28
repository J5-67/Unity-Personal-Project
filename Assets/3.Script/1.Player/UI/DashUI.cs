using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [Header("🔗 References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private List<Image> dashIcons; // 대시 아이콘들 (이미지)

    [Header("🎨 Visuals")]
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 어두운 회색 (반투명)

    private void Start()
    {
        // PlayerMovement 자동 찾기 시도
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (playerMovement == null) return;

        UpdateDashIcons();
    }

    // [유니] 플레이어 몸통이 회전해도 UI는 돌아가지 않게 고정! 📌
    private void LateUpdate()
    {
        // 부모(플레이어)가 회전하더라도 나는 항상 정면(Quaternion.identity)을 유지!
        transform.rotation = Quaternion.identity;
        
        // 만약 3D라서 카메라를 봐야 한다면 아래 코드 사용:
        // transform.rotation = Camera.main.transform.rotation;
    }

    private void UpdateDashIcons()
    {
        int currentCharges = playerMovement.CurrentDashCharges;

        // 아이콘 리스트를 순회하며 상태에 따라 색상 변경
        for (int i = 0; i < dashIcons.Count; i++)
        {
            if (i < currentCharges)
            {
                // 충전됨 (활성)
                dashIcons[i].color = activeColor;
            }
            else
            {
                // 사용함 (쿨타임/비활성)
                dashIcons[i].color = inactiveColor;
            }
        }
    }
}
