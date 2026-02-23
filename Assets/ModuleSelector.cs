using UnityEngine;
using UnityEngine.EventSystems;

public class ModuleSelector : MonoBehaviour
{
    public Camera mainCamera;
    public UserCameraControl cameraController; 
    
    [Header("Selection Camera Focus")]
    [Tooltip("If enabled, selecting a module will rotate the camera to look at it.")]
    public bool focusCameraOnSelection = true;

    [Tooltip("When this view is active, camera focus-on-selection is suppressed (used for IK view). If not set, will auto-resolve Views/InverseKinematicsView.")]
    public GameObject inverseKinematicsViewRoot;

    [System.NonSerialized]
    public ModuleBase prevModule;

    private static ModuleSelector _instance;
    public static ModuleBase SelectedModule => _instance != null ? _instance.prevModule : null;

    void Awake()
    {
        _instance = this;
        // Auto-resolve the IK view root if not wired in the inspector.
        if (inverseKinematicsViewRoot == null)
        {
            Transform views = GameObject.Find("Views")?.transform;
            inverseKinematicsViewRoot =
                views != null ? views.Find("InverseKinematicsView")?.gameObject : null;

            if (inverseKinematicsViewRoot == null)
                inverseKinematicsViewRoot = GameObject.Find("InverseKinematicsView");
        }
    }

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
                    bool ikViewActive = inverseKinematicsViewRoot != null && inverseKinematicsViewRoot.activeInHierarchy;
                    if (focusCameraOnSelection && !ikViewActive && cameraController != null)
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

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}