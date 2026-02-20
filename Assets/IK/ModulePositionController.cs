using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ModulePositionController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public InverseKinematicsController ikController;
    public LayerMask moduleLayer = -1; // All layers by default
    
    [Header("Position Control Settings")]
    public float positionStepSize = 0.1f; // How much to move per button click
    public float positionChangeThreshold = 0.01f; // Minimum position change to trigger IK solve
    
    [Header("Visual Feedback")]
    public Color selectedColor = new Color(0f, 0.8f, 1f, 1f); // Cyan color

    [Header("Selected Module Transparency (IK View)")]
    [Tooltip("If enabled, the currently selected module becomes transparent in IK view.")]
    public bool makeSelectedModuleTransparent = true;
    [Range(0.05f, 1f)]
    public float selectedModuleAlpha = 0.25f;
    
    [Header("UI References")]
    public Button xPlusButton;
    public Button xMinusButton;
    public Button yPlusButton;
    public Button yMinusButton;
    public Button zPlusButton;
    public Button zMinusButton;
    
    [Header("Gizmo")]
    public PositionControlGizmo gizmo;
    
    [HideInInspector] public ModuleBase selectedModule;
    private Vector3 targetPosition;
    private Vector3 lastSuccessfulPosition;
    private bool[] axisLimitReached = new bool[3];
    private int[] axisLimitDirection = new int[3];
    private bool gizmoWasClicked = false;

    // Critical: modules are parented; apply transparency only to renderers owned by the selected module.
    private readonly Dictionary<Renderer, Material[]> _originalSharedMaterialsByRenderer = new();
    private readonly List<Renderer> _transparentRenderers = new();
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (ikController == null)
        {
            ikController = GetComponentInParent<InverseKinematicsController>();
            if (ikController == null)
            {
                ikController = FindObjectOfType<InverseKinematicsController>();
            }
        }
        
        if (gizmo == null)
        {
            gizmo = GetComponentInChildren<PositionControlGizmo>();
            if (gizmo == null)
            {
                gizmo = FindObjectOfType<PositionControlGizmo>();
            }
            if (gizmo != null && gizmo.positionController == null)
            {
                gizmo.positionController = this;
            }
        }
        
        SetupUIButtons();
    }
    
    void SetupUIButtons()
    {
        if (xPlusButton != null) xPlusButton.onClick.RemoveAllListeners();
        if (xMinusButton != null) xMinusButton.onClick.RemoveAllListeners();
        if (yPlusButton != null) yPlusButton.onClick.RemoveAllListeners();
        if (yMinusButton != null) yMinusButton.onClick.RemoveAllListeners();
        if (zPlusButton != null) zPlusButton.onClick.RemoveAllListeners();
        if (zMinusButton != null) zMinusButton.onClick.RemoveAllListeners();
        
        if (xPlusButton != null)
        {
            xPlusButton.onClick.AddListener(() => AdjustPositionAxis(0, 1));
        }
        
        if (xMinusButton != null)
        {
            xMinusButton.onClick.AddListener(() => AdjustPositionAxis(0, -1));
        }
        
        if (yPlusButton != null)
        {
            yPlusButton.onClick.AddListener(() => AdjustPositionAxis(1, 1));
        }
        
        if (yMinusButton != null)
        {
            yMinusButton.onClick.AddListener(() => AdjustPositionAxis(1, -1));
        }
        
        if (zPlusButton != null)
        {
            zPlusButton.onClick.AddListener(() => AdjustPositionAxis(2, 1));
        }
        
        if (zMinusButton != null)
        {
            zMinusButton.onClick.AddListener(() => AdjustPositionAxis(2, -1));
        }
    }
    
    public void AdjustPositionAxis(int axis, int sign)
    {
        if (selectedModule == null)
        {
            return;
        }
        
        if (ikController == null)
        {
            return;
        }
        
        Vector3 currentActualPosition = selectedModule.transform.position;
        
        // Critical: preserve user intent on the other axes when adjusting only one axis.
        float targetOtherAxisX = targetPosition.x;
        float targetOtherAxisY = targetPosition.y;
        float targetOtherAxisZ = targetPosition.z;
        
        Vector3 positionBeforeIK = currentActualPosition;
        
        Vector3 newTargetPosition = targetPosition;
        
        switch (axis)
        {
            case 0: // X axis
                newTargetPosition.x += sign * positionStepSize;
                break;
            case 1: // Y axis
                newTargetPosition.y += sign * positionStepSize;
                break;
            case 2: // Z axis
                newTargetPosition.z += sign * positionStepSize;
                break;
        }
        
        float currentAxisValue = 0f;
        float newAxisValue = 0f;
        switch (axis)
        {
            case 0:
                currentAxisValue = positionBeforeIK.x;
                newAxisValue = newTargetPosition.x;
                break;
            case 1:
                currentAxisValue = positionBeforeIK.y;
                newAxisValue = newTargetPosition.y;
                break;
            case 2:
                currentAxisValue = positionBeforeIK.z;
                newAxisValue = newTargetPosition.z;
                break;
        }
        
        targetPosition = newTargetPosition;
        
        if (ikController != null)
        {
            ikController.ResetSmoothing();
        }
        
        bool success = false;
        if (ikController != null)
        {
            success = ikController.SolveIK(targetPosition);
        }
        
        Vector3 positionAfterIK = selectedModule.transform.position;
        
        float axisMovement = 0f;
        float axisValueBefore = 0f;
        float axisValueAfter = 0f;
        
        switch (axis)
        {
            case 0: // X axis
                axisValueBefore = positionBeforeIK.x;
                axisValueAfter = positionAfterIK.x;
                axisMovement = Mathf.Abs(axisValueAfter - axisValueBefore);
                break;
            case 1: // Y axis
                axisValueBefore = positionBeforeIK.y;
                axisValueAfter = positionAfterIK.y;
                axisMovement = Mathf.Abs(axisValueAfter - axisValueBefore);
                break;
            case 2: // Z axis
                axisValueBefore = positionBeforeIK.z;
                axisValueAfter = positionAfterIK.z;
                axisMovement = Mathf.Abs(axisValueAfter - axisValueBefore);
                break;
        }
        
        bool movementInCorrectDirection = false;
        switch (axis)
        {
            case 0: // X axis
                movementInCorrectDirection = (sign > 0 && axisValueAfter > axisValueBefore) || (sign < 0 && axisValueAfter < axisValueBefore);
                break;
            case 1: // Y axis
                movementInCorrectDirection = (sign > 0 && axisValueAfter > axisValueBefore) || (sign < 0 && axisValueAfter < axisValueBefore);
                break;
            case 2: // Z axis
                movementInCorrectDirection = (sign > 0 && axisValueAfter > axisValueBefore) || (sign < 0 && axisValueAfter < axisValueBefore);
                break;
        }
        
        // Critical: prevent target drift when constraints move opposite the user's intent.
        bool shouldUpdateTarget = (success || axisMovement >= positionChangeThreshold) && movementInCorrectDirection;
        Vector3 updatedTargetPosition = targetPosition;
        
        if (shouldUpdateTarget)
        {
            updatedTargetPosition = targetPosition;
            
            switch (axis)
            {
                case 0:
                    updatedTargetPosition.x = positionAfterIK.x;
                    updatedTargetPosition.y = targetOtherAxisY;
                    updatedTargetPosition.z = targetOtherAxisZ;
                    break;
                case 1:
                    updatedTargetPosition.x = targetOtherAxisX;
                    updatedTargetPosition.y = positionAfterIK.y;
                    updatedTargetPosition.z = targetOtherAxisZ;
                    break;
                case 2:
                    updatedTargetPosition.x = targetOtherAxisX;
                    updatedTargetPosition.y = targetOtherAxisY;
                    updatedTargetPosition.z = positionAfterIK.z;
                    break;
            }
            
            targetPosition = updatedTargetPosition;
        }
        else
        {
            switch (axis)
            {
                case 0:
                    targetPosition.x = targetOtherAxisX;
                    break;
                case 1:
                    targetPosition.y = targetOtherAxisY;
                    break;
                case 2:
                    targetPosition.z = targetOtherAxisZ;
                    break;
            }
            updatedTargetPosition = targetPosition;
            
            if (axisMovement >= positionChangeThreshold && !movementInCorrectDirection)
            {
                if (ikController != null)
                {
                    ikController.ResetSmoothing();
                }
            }
        }
        
        lastSuccessfulPosition = positionAfterIK;
        axisLimitReached[axis] = false;
        axisLimitDirection[axis] = 0;
        
    }
    
    void LateUpdate()
    {
        HandleSelection();
    }
    
    void HandleSelection()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            gizmoWasClicked = false;
            
            if (gizmo != null && gizmo.enabled && gizmo.WasGizmoClicked())
            {
                gizmoWasClicked = true;
                return; // Don't process selection/deselection when gizmo arrow is clicked
            }
            
            if (gizmoWasClicked)
            {
                return;
            }
            
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
                if (mainCamera == null)
                {
                    return;
                }
            }
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, moduleLayer))
            {
                ModuleBase hitModule = hit.collider.GetComponentInParent<ModuleBase>();
                
                if (hitModule != null && IsPartOfGeneratedTopology(hitModule.transform))
                {
                    SelectModule(hitModule);
                }
                else
                {
                    DeselectModule();
                }
            }
            else
            {
                if (!gizmoWasClicked)
                {
                DeselectModule();
            }
        }
        }
    }
    
    public void OnGizmoClicked()
    {
        gizmoWasClicked = true;
    }
    
    void SelectModule(ModuleBase module)
    {
        if (selectedModule != null && selectedModule != module)
        {
            DeselectModule();
        }
        
        ServoMotorModule.selectedModule = null;
        ObjectSelector.selectedObject = null;
        
        selectedModule = module;
        targetPosition = selectedModule.transform.position;
        lastSuccessfulPosition = selectedModule.transform.position;
        axisLimitReached[0] = false;
        axisLimitReached[1] = false;
        axisLimitReached[2] = false;
        axisLimitDirection[0] = 0;
        axisLimitDirection[1] = 0;
        axisLimitDirection[2] = 0;
        
        ServoMotorModule servoModule = module.GetComponent<ServoMotorModule>();
        if (servoModule != null)
        {
            ServoMotorModule.selectedModule = servoModule;
        }
        else
        {
            ObjectSelector.selectedObject = module.gameObject;
        }
        
        if (gizmo != null)
        {
            gizmo.enabled = true;
            gizmo.RefreshGizmoVisibility();
        }
        
        if (ikController != null)
        {
            bool chainBuilt = ikController.BuildKinematicChain(selectedModule.transform);
            
            if (chainBuilt)
            {
                if (ikController.IsAnchorPoint(selectedModule.transform))
                {
                    DeselectModule();
                    return;
                }
            }
            
            ikController.ResetSmoothing();
        }

        if (makeSelectedModuleTransparent)
        {
            ApplyTransparencyToSelectedModule();
        }
    }
    
    public void DeselectModule()
    {
        RestoreTransparency();

        if (selectedModule != null)
        {
            ServoMotorModule.selectedModule = null;
            ObjectSelector.selectedObject = null;
        }
        
        selectedModule = null;
        
        if (gizmo != null)
        {
            gizmo.enabled = false;
            gizmo.RefreshGizmoVisibility();
        }
    }

    private void ApplyTransparencyToSelectedModule()
    {
        RestoreTransparency();

        if (selectedModule == null) return;

        // Critical: don't descend into other ModuleBase subtrees (child modules).
        foreach (var r in GetRenderersOwnedByModule(selectedModule))
        {
            if (!_originalSharedMaterialsByRenderer.ContainsKey(r))
            {
                _originalSharedMaterialsByRenderer[r] = r.sharedMaterials;
            }

            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                ConfigureMaterialForTransparency(m, selectedModuleAlpha);
            }

            _transparentRenderers.Add(r);
        }
    }

    private static IEnumerable<Renderer> GetRenderersOwnedByModule(ModuleBase rootModule)
    {
        if (rootModule == null) yield break;

        Transform root = rootModule.transform;
        var stack = new Stack<Transform>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Transform t = stack.Pop();
            if (t == null) continue;

            if (t != root)
            {
                ModuleBase other = t.GetComponent<ModuleBase>();
                if (other != null && other != rootModule)
                    continue;
            }

            foreach (var r in t.GetComponents<Renderer>())
            {
                if (r != null)
                    yield return r;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
    }

    private static void ConfigureMaterialForTransparency(Material m, float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);

            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);

            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");

            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (m.HasProperty("_Mode"))
        {
            m.SetFloat("_Mode", 3f);
            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (m.HasProperty("_BaseColor"))
        {
            Color c = m.GetColor("_BaseColor");
            c.a = alpha;
            m.SetColor("_BaseColor", c);
        }
        if (m.HasProperty("_Color"))
        {
            Color c = m.GetColor("_Color");
            c.a = alpha;
            m.SetColor("_Color", c);
        }
    }

    private void RestoreTransparency()
    {
        if (_transparentRenderers.Count == 0 && _originalSharedMaterialsByRenderer.Count == 0) return;

        foreach (var r in _transparentRenderers)
        {
            if (r == null) continue;
            if (_originalSharedMaterialsByRenderer.TryGetValue(r, out var shared))
            {
                r.sharedMaterials = shared;
            }
        }

        _transparentRenderers.Clear();
        _originalSharedMaterialsByRenderer.Clear();
    }
    
    bool IsPartOfGeneratedTopology(Transform moduleTransform)
    {
        Transform current = moduleTransform;
        while (current != null)
        {
            if (current.name == "GeneratedTopology")
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    void OnDisable()
    {
        DeselectModule();
    }
}
