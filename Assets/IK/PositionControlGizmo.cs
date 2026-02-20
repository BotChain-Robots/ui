using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
public class PositionControlGizmo : MonoBehaviour
{
    [Header("Gizmo Settings")]
    public float arrowLength = 1.0f;
    public float arrowHeadSize = 0.25f;
    public float lineWidth = 0.04f;
    public float clickableRadius = 0.5f;
    public float screenSpaceClickThreshold = 30f;
    
    [Header("Colors")]
    public Color xAxisColor = Color.red;
    public Color yAxisColor = Color.green;
    public Color zAxisColor = Color.blue;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = new Color(1f, 1f, 0f, 0.8f);
    
    [Header("References")]
    public Camera mainCamera;
    public ModulePositionController positionController;

    [Header("Placement")]
    [Tooltip("If enabled, the gizmo is drawn at the selected module's visual center (renderer/collider bounds center), rather than transform.position.")]
    public bool centerOnModuleBounds = true;
    [Tooltip("World-space offset added to gizmo origin.")]
    public Vector3 gizmoWorldOffset = Vector3.zero;
    
    private ModuleBase targetModule;
    private int hoveredAxis = -1;
    private int previousHoveredAxis = -1;
    private int selectedAxis = -1;
    private Color[] previousColors = new Color[3];
    private bool isDragging = false;
    private bool gizmoWasClickedThisFrame = false;
    
    private Vector3 axisDragStartGizmoOrigin;
    private Vector3 axisDragStartModulePosition;
    private Vector2 axisDragStartMousePosition;
    [Header("Axis Drag Settings")]
    public float axisDragSensitivity = 0.01f;
    
    public bool WasGizmoClicked()
    {
        return gizmoWasClickedThisFrame;
    }
    
    public void RefreshGizmoVisibility()
    {
        UpdateGizmoVisibility();
    }
    
    private LineRenderer xAxisLine;
    private LineRenderer yAxisLine;
    private LineRenderer zAxisLine;
    private GameObject gizmoContainer;

    private Transform xAxisHead;
    private Transform yAxisHead;
    private Transform zAxisHead;
    private static Mesh _coneMesh;
    
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
        
        if (positionController == null)
        {
            positionController = GetComponentInParent<ModulePositionController>();
            if (positionController == null)
            {
                positionController = FindObjectOfType<ModulePositionController>();
            }
            if (positionController != null && positionController.gizmo == null)
            {
                positionController.gizmo = this;
            }
        }
        
        previousColors[0] = xAxisColor;
        previousColors[1] = yAxisColor;
        previousColors[2] = zAxisColor;
        
        FindGizmoLines();
    }
    
    void FindGizmoLines()
    {
        if (gizmoContainer != null)
        {
            return;
        }
        
        Transform existingContainer = this.transform.Find("PositionControlGizmo");
        if (existingContainer != null)
        {
            gizmoContainer = existingContainer.gameObject;
        }
        else
        {
            gizmoContainer = new GameObject("PositionControlGizmo");
            gizmoContainer.transform.SetParent(this.transform);
            gizmoContainer.transform.localPosition = Vector3.zero;
            gizmoContainer.transform.localRotation = Quaternion.identity;
            gizmoContainer.transform.localScale = Vector3.one;
        }
        
        Transform xAxisTransform = gizmoContainer.transform.Find("XAxis");
        if (xAxisTransform == null)
        {
            GameObject xAxisObj = new GameObject("XAxis");
            xAxisObj.transform.SetParent(gizmoContainer.transform);
            xAxisObj.transform.localPosition = Vector3.zero;
            xAxisObj.transform.localRotation = Quaternion.identity;
            xAxisObj.transform.localScale = Vector3.one;
            xAxisLine = xAxisObj.AddComponent<LineRenderer>();
            SetupLineRenderer(xAxisLine, xAxisColor);
        }
        else
        {
            xAxisLine = xAxisTransform.GetComponent<LineRenderer>();
            if (xAxisLine == null)
            {
                xAxisLine = xAxisTransform.gameObject.AddComponent<LineRenderer>();
                SetupLineRenderer(xAxisLine, xAxisColor);
            }
            else
            {
                SetupLineRenderer(xAxisLine, xAxisColor);
            }
        }
        
        Transform yAxisTransform = gizmoContainer.transform.Find("YAxis");
        if (yAxisTransform == null)
        {
            GameObject yAxisObj = new GameObject("YAxis");
            yAxisObj.transform.SetParent(gizmoContainer.transform);
            yAxisObj.transform.localPosition = Vector3.zero;
            yAxisObj.transform.localRotation = Quaternion.identity;
            yAxisObj.transform.localScale = Vector3.one;
            yAxisLine = yAxisObj.AddComponent<LineRenderer>();
            SetupLineRenderer(yAxisLine, yAxisColor);
        }
        else
        {
            yAxisLine = yAxisTransform.GetComponent<LineRenderer>();
            if (yAxisLine == null)
            {
                yAxisLine = yAxisTransform.gameObject.AddComponent<LineRenderer>();
                SetupLineRenderer(yAxisLine, yAxisColor);
            }
            else
            {
                SetupLineRenderer(yAxisLine, yAxisColor);
            }
        }
        
        Transform zAxisTransform = gizmoContainer.transform.Find("ZAxis");
        if (zAxisTransform == null)
        {
            GameObject zAxisObj = new GameObject("ZAxis");
            zAxisObj.transform.SetParent(gizmoContainer.transform);
            zAxisObj.transform.localPosition = Vector3.zero;
            zAxisObj.transform.localRotation = Quaternion.identity;
            zAxisObj.transform.localScale = Vector3.one;
            zAxisLine = zAxisObj.AddComponent<LineRenderer>();
            SetupLineRenderer(zAxisLine, zAxisColor);
        }
        else
        {
            zAxisLine = zAxisTransform.GetComponent<LineRenderer>();
            if (zAxisLine == null)
            {
                zAxisLine = zAxisTransform.gameObject.AddComponent<LineRenderer>();
                SetupLineRenderer(zAxisLine, zAxisColor);
            }
            else
            {
                SetupLineRenderer(zAxisLine, zAxisColor);
            }
        }

        xAxisHead = EnsureArrowHead(xAxisLine != null ? xAxisLine.transform : null, "Head");
        yAxisHead = EnsureArrowHead(yAxisLine != null ? yAxisLine.transform : null, "Head");
        zAxisHead = EnsureArrowHead(zAxisLine != null ? zAxisLine.transform : null, "Head");
        
        gizmoContainer.SetActive(false);
    }

    private Transform EnsureArrowHead(Transform axisTransform, string name)
    {
        if (axisTransform == null) return null;

        Transform head = axisTransform.Find(name);
        if (head == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(axisTransform, false);
            head = go.transform;
        }

        var mf = head.GetComponent<MeshFilter>();
        if (mf == null) mf = head.gameObject.AddComponent<MeshFilter>();

        var mr = head.GetComponent<MeshRenderer>();
        if (mr == null) mr = head.gameObject.AddComponent<MeshRenderer>();

        if (_coneMesh == null)
        {
            _coneMesh = CreateConeMesh(18);
        }
        mf.sharedMesh = _coneMesh;

        if (axisTransform.TryGetComponent<LineRenderer>(out var lr) && lr.material != null)
        {
            mr.sharedMaterial = lr.material;
        }
        else
        {
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.white;
            mr.sharedMaterial = mat;
        }

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return head;
    }

    private static Mesh CreateConeMesh(int segments)
    {
        segments = Mathf.Clamp(segments, 6, 64);

        var mesh = new Mesh();
        mesh.name = "PositionControlGizmo_Cone";

        int vertCount = segments + 2; // base ring + base center + tip
        Vector3[] v = new Vector3[vertCount];
        Vector3[] n = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];

        int baseCenter = segments;
        int tip = segments + 1;

        float radius = 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * radius;
            float y = Mathf.Sin(a) * radius;
            v[i] = new Vector3(x, y, 0f);
            n[i] = (new Vector3(x, y, radius)).normalized; // approximate
            uv[i] = new Vector2(i / (float)segments, 0f);
        }

        v[baseCenter] = new Vector3(0f, 0f, 0f);
        n[baseCenter] = Vector3.back;
        uv[baseCenter] = new Vector2(0.5f, 0f);

        v[tip] = new Vector3(0f, 0f, 1f);
        n[tip] = Vector3.forward;
        uv[tip] = new Vector2(0.5f, 1f);

        int[] tris = new int[segments * 6];
        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            tris[t++] = i;
            tris[t++] = next;
            tris[t++] = tip;

            tris[t++] = next;
            tris[t++] = i;
            tris[t++] = baseCenter;
        }

        mesh.vertices = v;
        mesh.normals = n;
        mesh.uv = uv;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }
    
    void OnEnable()
    {
        UpdateGizmoVisibility();
    }
    
    void UpdateGizmoVisibility()
    {
        if (gizmoContainer != null)
        {
            if (positionController != null)
            {
                targetModule = positionController.selectedModule;
            }
            
            bool shouldShow = targetModule != null && this.enabled;
            
            if (shouldShow)
            {
                gizmoContainer.SetActive(true);
            }
            else
            {
                gizmoContainer.SetActive(false);
            }
            
            if (!shouldShow)
            {
                hoveredAxis = -1;
                selectedAxis = -1;
                isDragging = false;
            }
        }
        else
        {
            FindGizmoLines();
        }
    }
    
    void SetupLineRenderer(LineRenderer lr, Color color)
    {
        Shader unlitShader = Shader.Find("Unlit/Color");
        Material gizmoMaterial;
        
        if (unlitShader != null)
        {
            gizmoMaterial = new Material(unlitShader);
            gizmoMaterial.color = color;
            gizmoMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            gizmoMaterial.SetInt("_ZWrite", 0);
        }
        else
        {
            gizmoMaterial = new Material(Shader.Find("Sprites/Default"));
            gizmoMaterial.color = color;
        }
        
        lr.material = gizmoMaterial;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        
        lr.sortingOrder = 1000;
    }
    
    void UpdateGizmoVisualization()
    {
        if (targetModule == null)
        {
            return;
        }
        
        if (gizmoContainer == null)
        {
            FindGizmoLines();
            if (gizmoContainer == null)
            {
                return;
            }
        }
        
        if (xAxisLine == null || yAxisLine == null || zAxisLine == null)
        {
            FindGizmoLines();
            if (xAxisLine == null || yAxisLine == null || zAxisLine == null)
            {
                return;
            }
        }
        
        Vector3 gizmoOrigin = GetGizmoOrigin();
        
        Color xColor = GetAxisColor(0);
        if (previousColors[0] != xColor)
        {
            previousColors[0] = xColor;
        }
        UpdateAxisLine(xAxisLine, gizmoOrigin, GetAxisDirection(0), arrowLength, xColor);
        UpdateAxisHead(xAxisHead, gizmoOrigin, GetAxisDirection(0), arrowLength, arrowHeadSize, xColor);
        
        Color yColor = GetAxisColor(1);
        if (previousColors[1] != yColor)
        {
            previousColors[1] = yColor;
        }
        UpdateAxisLine(yAxisLine, gizmoOrigin, GetAxisDirection(1), arrowLength, yColor);
        UpdateAxisHead(yAxisHead, gizmoOrigin, GetAxisDirection(1), arrowLength, arrowHeadSize, yColor);
        
        Color zColor = GetAxisColor(2);
        if (previousColors[2] != zColor)
        {
            previousColors[2] = zColor;
        }
        UpdateAxisLine(zAxisLine, gizmoOrigin, GetAxisDirection(2), arrowLength, zColor);
        UpdateAxisHead(zAxisHead, gizmoOrigin, GetAxisDirection(2), arrowLength, arrowHeadSize, zColor);
    }

    void UpdateAxisHead(Transform head, Vector3 start, Vector3 direction, float length, float headSize, Color color)
    {
        if (head == null) return;

        Vector3 dir = direction.normalized;
        Vector3 end = start + dir * length;

        head.position = end;
        head.rotation = Quaternion.LookRotation(dir, Vector3.up);
        head.localScale = new Vector3(headSize, headSize, headSize);

        // Update head material color (shared with line renderer in most cases)
        var mr = head.GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
        {
            if (mr.sharedMaterial.HasProperty("_Color"))
                mr.sharedMaterial.SetColor("_Color", color);
            mr.sharedMaterial.color = color;
        }
    }
    
    void UpdateAxisLine(LineRenderer lr, Vector3 start, Vector3 direction, float length, Color color)
    {
        if (lr == null)
        {
            return;
        }
        
        // Ensure LineRenderer is enabled
        if (!lr.enabled)
        {
            lr.enabled = true;
        }
        
        
        Vector3 end = start + direction.normalized * length;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startColor = color;
        lr.endColor = color;
        
        // Also update material color (important for Unlit/Color shader)
        if (lr.material != null)
        {
            lr.material.color = color;
            // Also set _Color property explicitly (some shaders use this)
            if (lr.material.HasProperty("_Color"))
            {
                lr.material.SetColor("_Color", color);
            }
        }
    }
    
    void Update()
    {
        if (positionController != null)
        {
            targetModule = positionController.selectedModule;
        }
        
        UpdateGizmoVisibility();
        
        if (targetModule != null)
        {
            HandleGizmoInteraction();
            
            UpdateGizmoVisualization();
        }
        else
        {
            hoveredAxis = -1;
            selectedAxis = -1;
            isDragging = false;
        }
    }
    
    void HandleGizmoInteraction()
    {
        if (targetModule == null || mainCamera == null)
        {
            hoveredAxis = -1;
            return;
        }
        
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            hoveredAxis = -1;
            return;
        }
        
        Vector3 gizmoOrigin = GetGizmoOrigin();
        Vector2 mouseScreenPos = Input.mousePosition;
        
        float closestDistance = float.MaxValue;
        int closestAxis = -1;
        
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 axisDirection = GetAxisDirection(axis);
            Vector3 axisStart = gizmoOrigin;
            Vector3 axisEnd = gizmoOrigin + axisDirection * arrowLength;
            
            Vector3 screenStart3D = mainCamera.WorldToScreenPoint(axisStart);
            Vector3 screenEnd3D = mainCamera.WorldToScreenPoint(axisEnd);
            
            if (screenStart3D.z < 0 || screenEnd3D.z < 0)
                continue;
            
            Vector2 screenStart = new Vector2(screenStart3D.x, screenStart3D.y);
            Vector2 screenEnd = new Vector2(screenEnd3D.x, screenEnd3D.y);
            
            Vector2 closestPointOnLine = ClosestPointOnScreenLine(screenStart, screenEnd, mouseScreenPos);
            float screenDistance = Vector2.Distance(mouseScreenPos, closestPointOnLine);
            
            float distanceToStart = Vector2.Distance(mouseScreenPos, screenStart);
            float minDistance = Mathf.Min(screenDistance, distanceToStart);
            
            if (minDistance < screenSpaceClickThreshold && minDistance < closestDistance)
            {
                closestDistance = minDistance;
                closestAxis = axis;
            }
        }
        
        int oldHoveredAxis = hoveredAxis;
        hoveredAxis = closestAxis;
        
        if (hoveredAxis != oldHoveredAxis)
        {
            // no-op (hover state tracking only)
        }
        
        gizmoWasClickedThisFrame = false;
        
        HandleAxisBasedInteraction();
    }
    
    void HandleAxisBasedInteraction()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            hoveredAxis = -1;
            return;
        }
        
        Vector3 gizmoOrigin = GetGizmoOrigin();
        Vector2 mouseScreenPos = Input.mousePosition;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (hoveredAxis >= 0)
            {
                selectedAxis = hoveredAxis;
                isDragging = true;
                axisDragStartGizmoOrigin = gizmoOrigin; // Store starting gizmo origin
                axisDragStartModulePosition = targetModule.transform.position; // Store module pivot position for IK targets
                axisDragStartMousePosition = mouseScreenPos; // Store starting mouse position
                gizmoWasClickedThisFrame = true; // Mark that gizmo was clicked
                
                if (positionController != null)
                {
                    positionController.OnGizmoClicked();
                }
            }
        }
        else if (isDragging && selectedAxis >= 0 && Input.GetMouseButton(0))
        {
            ProcessUnityStyleAxisDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                isDragging = false;
                selectedAxis = -1;
            }
        }
    }
    
    void ProcessUnityStyleAxisDrag()
    {
        if (targetModule == null || mainCamera == null || positionController == null) return;
        
        Vector2 currentMousePos = Input.mousePosition;
        Vector2 mouseDelta = currentMousePos - axisDragStartMousePosition;
        
        float minMouseMovement = 2f; // Minimum pixels of movement required
        if (mouseDelta.magnitude < minMouseMovement)
        {
            return; // User just clicked without dragging - no movement
        }
        
        Vector3 axisDirection = GetAxisDirection(selectedAxis);
        Vector3 gizmoOrigin = axisDragStartGizmoOrigin; // Use gizmo origin as reference for screen-space projection
        
        Vector2 screenAxis = (Vector2)mainCamera.WorldToScreenPoint(gizmoOrigin + axisDirection) -
                            (Vector2)mainCamera.WorldToScreenPoint(gizmoOrigin);
        screenAxis.Normalize();
        
        float movementAlongAxis = Vector2.Dot(mouseDelta, screenAxis);
        
        float worldMovement = movementAlongAxis * axisDragSensitivity;
        
        if (positionController != null && positionController.ikController != null)
        {
            worldMovement *= positionController.ikController.dragSensitivity;
        }
        
        // IMPORTANT: keep IK target based on module pivot (transform.position), not the visual gizmo origin.
        Vector3 currentPosition = targetModule.transform.position;
        Vector3 targetPosition = axisDragStartModulePosition + axisDirection * worldMovement;
        
        float minMovementThreshold = 0.03f; // Minimum 3cm movement required
        float distanceToTarget = Vector3.Distance(targetPosition, currentPosition);

        if (distanceToTarget < minMovementThreshold)
        {
            return;
        }

        Vector3 intendedMovement = targetPosition - currentPosition;
        float intendedAxisMovement = Vector3.Dot(intendedMovement, axisDirection);
        float minAxisMovement = 0.02f; // Minimum 2cm movement along the selected axis

        if (Mathf.Abs(intendedAxisMovement) < minAxisMovement)
        {
            return;
        }

        if (positionController.ikController != null)
        {
            bool success = positionController.ikController.SolveIK(targetPosition);
            
            if (success)
            {
                Vector3 actualPosition = targetModule.transform.position;
                Vector3 actualMovement = actualPosition - axisDragStartModulePosition;
                float actualAxisMovement = Vector3.Dot(actualMovement, axisDirection);
                float actualMovementDistance = actualMovement.magnitude;

                if (actualMovementDistance >= minMovementThreshold && Mathf.Abs(actualAxisMovement) >= minAxisMovement)
                {
                    if (Mathf.Sign(actualAxisMovement) == Mathf.Sign(intendedAxisMovement))
                    {
                        axisDragStartModulePosition = actualPosition;
                        axisDragStartGizmoOrigin = GetGizmoOrigin();
                        axisDragStartMousePosition = currentMousePos;
                    }
                }
            }
        }
    }

    Vector3 GetGizmoOrigin()
    {
        if (targetModule == null) return Vector3.zero;

        Vector3 origin = targetModule.transform.position;
        if (!centerOnModuleBounds)
            return origin + gizmoWorldOffset;

        bool hasBounds = false;
        Bounds b = default;

        foreach (var r in GetRenderersOwnedByModule(targetModule))
        {
            if (r == null) continue;
            if (!hasBounds)
            {
                b = r.bounds;
                hasBounds = true;
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds)
        {
            foreach (var c in GetCollidersOwnedByModule(targetModule))
            {
                if (c == null) continue;
                if (!hasBounds)
                {
                    b = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(c.bounds);
                }
            }
        }

        origin = hasBounds ? b.center : origin;
        return origin + gizmoWorldOffset;
    }

    static System.Collections.Generic.IEnumerable<Renderer> GetRenderersOwnedByModule(ModuleBase rootModule)
    {
        if (rootModule == null) yield break;
        Transform root = rootModule.transform;

        var stack = new System.Collections.Generic.Stack<Transform>();
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
                if (r != null) yield return r;

            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }

    static System.Collections.Generic.IEnumerable<Collider> GetCollidersOwnedByModule(ModuleBase rootModule)
    {
        if (rootModule == null) yield break;
        Transform root = rootModule.transform;

        var stack = new System.Collections.Generic.Stack<Transform>();
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

            foreach (var c in t.GetComponents<Collider>())
                if (c != null) yield return c;

            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }
    
    Vector2 ClosestPointOnScreenLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength < 0.001f)
            return lineStart;
        
        line.Normalize();
        Vector2 pointToStart = point - lineStart;
        float projectionLength = Vector2.Dot(pointToStart, line);
        projectionLength = Mathf.Clamp(projectionLength, 0f, lineLength);
        
        return lineStart + line * projectionLength;
    }
    
    void AdjustPosition(int axis, int sign)
    {
        if (positionController != null)
        {
            positionController.AdjustPositionAxis(axis, sign);
        }
    }
    
    Vector3 GetAxisDirection(int axis)
    {
        switch (axis)
        {
            case 0: return Vector3.right;   // X axis (world space - always points right)
            case 1: return Vector3.up;      // Y axis (world space - always points up)
            case 2: return Vector3.forward; // Z axis (world space - always points forward)
            default: return Vector3.zero;
        }
    }
    
    Color GetAxisColor(int axis)
    {
        if (axis == selectedAxis) return selectedColor;
        if (axis == hoveredAxis) return hoverColor;
        
        switch (axis)
        {
            case 0: return xAxisColor;
            case 1: return yAxisColor;
            case 2: return zAxisColor;
            default: return Color.white;
        }
    }
    
    Vector3 ClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        line.Normalize();
        
        Vector3 pointToStart = point - lineStart;
        float projectionLength = Vector3.Dot(pointToStart, line);
        projectionLength = Mathf.Clamp(projectionLength, 0f, lineLength);
        
        return lineStart + line * projectionLength;
    }
    
    Vector3 ClosestPointOnRay(Ray ray, Vector3 point)
    {
        Vector3 rayToPoint = point - ray.origin;
        float projectionLength = Vector3.Dot(rayToPoint, ray.direction);
        projectionLength = Mathf.Max(0f, projectionLength); // Don't go backwards
        
        return ray.origin + ray.direction * projectionLength;
    }
    
    void OnDestroy()
    {
        // Don't destroy gizmo container - it's part of the scene hierarchy, not created at runtime
        // Just clear the reference
        gizmoContainer = null;
        xAxisLine = null;
        yAxisLine = null;
        zAxisLine = null;
    }
}

