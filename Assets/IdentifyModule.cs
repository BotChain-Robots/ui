using UnityEngine;
using UnityEngine.EventSystems;

public class IdentifyModule : MonoBehaviour
{

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            if (IsPointerOverUI()) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                //ServoBendModule hitModule = hit.transform.GetComponent<ServoBendModule>();
                ServoMotorModule hitModule = hit.collider.GetComponent<ServoMotorModule>();

                if (hitModule != null)
                {
                    // if (ServoBendModule.selectedModule != null && ServoBendModule.selectedModule != hitModule)
                    // {
                    //     ServoBendModule.selectedModule.SetHighlight(false);
                    // }

                    ServoMotorModule.selectedModule = hitModule;
                    // hitModule.SetHighlight(true);
                    Debug.Log($"Selected: {hitModule.name}");
                }
                else
                {
                    // Clicked something else
                    if (ServoMotorModule.selectedModule != null)
                    {
                        // ServoBendModule.selectedModule.SetHighlight(false);
                        ServoMotorModule.selectedModule = null;
                        // Debug.Log("Deselected module (clicked non-module object).");
                    }
                }
            }
            else
            {
                // Clicked empty space
                if (ServoMotorModule.selectedModule != null)
                {
                    // ServoBendModule.selectedModule.SetHighlight(false);
                    ServoMotorModule.selectedModule = null;
                    Debug.Log("Deselected module (clicked empty space).");
                }
            }
        }
    }
}
