using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ModuleSelector : MonoBehaviour
{
    public Camera mainCamera;
    public Cinemachine.CinemachineVirtualCamera virtualCamera;
    private ModuleBase prevModule; //Store previous module then when unselected call that module's unselect function to make panel disappear or something

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Don't process any clicks if the click is over UI element
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            if (prevModule != null)
            {
                prevModule.DeSelect();
                virtualCamera.LookAt = null;
                virtualCamera.Follow = null;
            }
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ModuleBase module = hit.collider.GetComponentInParent<ModuleBase>();
                if (module != null && module != prevModule)
                {
                    module.OnSelect();
                    prevModule = module;
                    virtualCamera.LookAt = module.transform;
                    virtualCamera.Follow = module.transform;
                }
                else
                {
                    prevModule = null;
                }
            }
            else
            {
                prevModule = null;
            }
        }
    }
}
