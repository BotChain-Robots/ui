using UnityEngine;
using UnityEngine.EventSystems;

public class ModuleSelector : MonoBehaviour
{
    public Camera mainCamera;
    public UserCameraControl cameraController; 

    [System.NonSerialized]
    public ModuleBase prevModule;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Ignore clicks over UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            ClearSelection();

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ModuleBase module = hit.collider.GetComponentInParent<ModuleBase>();
                if (module != null)
                {
                    module.OnSelect();
                    prevModule = module;

                    // Servo highlighting support
                    ServoMotorModule servo = module.GetComponent<ServoMotorModule>();
                    ServoMotorModule.selectedModule = servo;

                    // tell camera to look at module
                    if (cameraController != null)
                        cameraController.LookAtTarget(module.transform);
                }
            }
            else
            {
                ClearSelection();
            }
        }
    }

    // Deselect current module and clear selection state
    void ClearSelection()
    {
        if (prevModule != null)
        {
            prevModule.DeSelect();
            prevModule = null;
        }

        ServoMotorModule.selectedModule = null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}