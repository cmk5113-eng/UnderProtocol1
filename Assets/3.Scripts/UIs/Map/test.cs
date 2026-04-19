using UnityEngine;

public class TestClick : MonoBehaviour
{
    public GameObject targetUI; // ø¨∞·«“ UI

    void OnMouseDown()
    {
        Toggle();
    }

    void Toggle()
    {
        targetUI.SetActive(!targetUI.activeSelf);
    }
}