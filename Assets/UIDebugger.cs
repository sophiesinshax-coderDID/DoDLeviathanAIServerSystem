using UnityEngine;
using UnityEngine.EventSystems;

public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            Debug.Log("Currently Focused UI Object: " + (currentSelected != null ? currentSelected.name : "None (Clicked background or raycast failed)"));
        }
    }
}