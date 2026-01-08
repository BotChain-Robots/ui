using UnityEngine;

public class ModuleSelectionManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Try get ServoBendModule directly on hit object
                ServoMotorModule module = hit.transform.GetComponent<ServoMotorModule>();

                if (module != null)
                {
                    // Deselect previous
                    // if (ServoBendModule.selectedModule != null && ServoBendModule.selectedModule != module)
                    // {
                    //     ServoBendModule.selectedModule.SetHighlight(false);
                    // }

                    // Select this
                    ServoMotorModule.selectedModule = module;
                    // module.SetHighlight(true);

                    // Debug.Log($"[Selection] Selected: {module.name}");
                }
                else
                {
                    // Debug.Log($"[Selection] Hit {hit.transform.name} — no ServoBendModule attached");
                }
            }
        }
    }
}