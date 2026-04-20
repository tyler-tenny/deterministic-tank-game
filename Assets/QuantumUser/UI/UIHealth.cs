using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHealth : MonoBehaviour
{
    int _health;
    int rightMax = 355;
    [SerializeField] RectTransform healthBar;

    public void UpdateHealth(int health)
    {
        _health = health;
        healthBar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 152, rightMax * (_health / 100f));
        // Debug.Log("Health updated to " + _health);
    }
}
