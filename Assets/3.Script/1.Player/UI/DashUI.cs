using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [Header("🔗 References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private List<Image> dashIcons;

    [Header("🎨 Visuals")]
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private void Start()
    {
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

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }

    private void UpdateDashIcons()
    {
        int currentCharges = playerMovement.CurrentDashCharges;

        for (int i = 0; i < dashIcons.Count; i++)
        {
            if (i < currentCharges)
            {
                dashIcons[i].color = activeColor;
            }
            else
            {
                dashIcons[i].color = inactiveColor;
            }
        }
    }
}
