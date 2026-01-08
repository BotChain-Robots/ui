using UnityEngine;

public class ObjectSelector : MonoBehaviour
{
    public static GameObject selectedObject;

    void Update()
{
    Debug.Log("Selector is running");

    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name);
            if (hit.collider.CompareTag("Selectable"))
            {
                ObjectSelector.selectedObject = hit.collider.transform.root.gameObject;
                Debug.Log("Selected Root Object: " + ObjectSelector.selectedObject.name);
            }
        }
    }
}
}
