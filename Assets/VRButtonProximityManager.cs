using System.Collections.Generic;
using UnityEngine;

public class VRButtonProximityManager : MonoBehaviour
{
    // Singleton
    public static VRButtonProximityManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 記錄每個玩家最近的按鈕
    private Dictionary<Transform, VRGUI_Button> closestButtonMap = new();

    public void ReportProximity(Transform player, VRGUI_Button button, float distance)
    {
        if (!closestButtonMap.ContainsKey(player))
        {
            closestButtonMap[player] = button;
        }
        else
        {
            float currentDistance = Vector3.Distance(player.position, closestButtonMap[player].transform.position);
            if (distance < currentDistance)
            {
                closestButtonMap[player] = button;
            }
        }
    }

    public bool IsClosest(Transform player, VRGUI_Button button)
    {
        return closestButtonMap.TryGetValue(player, out var assignedButton) && assignedButton == button;
    }

    public void Unregister(Transform player, VRGUI_Button button)
    {
        if (closestButtonMap.TryGetValue(player, out var current) && current == button)
        {
            closestButtonMap.Remove(player);
        }
    }
}