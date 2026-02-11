using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UserCameraControl : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;

    [Header("Cursors")]
    public Texture2D handCursor;
    public Texture2D eyeCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    [Header("Move")]
    public float panSpeed = 3.5f;        // units/sec
    public float zoomSpeed = 6f;         // scroll multiplier
    public float moveSmoothTime = 0.12f; // higher = snappier, lower = floatier

    [Header("Orbit")]
    public float orbitSpeed = 20f;      // degrees/sec per pixel-ish delta
    public float minPitch = -20f;
    public float maxPitch = 80f;
    public float orbitSmoothTime = 0.08f;

    [Header("Orbit Feel")]
    public bool lockCursorWhileOrbiting = true;
    public float maxMouseDelta = 40f;          // clamps rare huge spikes
    public float orbitSmoothing = 0.08f;       // 0 = no smoothing, ~0.05-0.15 feels nice

    Vector2 orbitDeltaSmoothed;
    Vector2 orbitDeltaVel;

    [Header("Look / Home Smooth")]
    public float lookAtDuration = 0.25f;
    public float homeDuration = 0.35f;

    // internal angles
    float yaw;
    float pitch;

    // rotation smoothing
    Vector2 lastMouse;
    bool orbiting;
    float yawDeltaVel, pitchDeltaVel;
    float yawDeltaSmoothed, pitchDeltaSmoothed;

    // movement smoothing
    Vector3 targetPosition;
    Vector3 positionVel;

    Coroutine lookRoutine;
    Coroutine homeRoutine;

    void Awake()
    {
        if (!mainCamera) mainCamera = Camera.main;

        targetPosition = transform.position;
        SyncAnglesFromTransform();
    }

    void Update()
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        {
            SetCursor(null);
            return;
        }

        HandleKeyboardPanAndZoom();
        HandleOrbit();

        // smooth position every frame
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVel,
            moveSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            SetCursor(null);
    }

    void HandleOrbit()
    {
        if (Input.GetMouseButtonDown(1))
        {
            orbiting = true;
            SetCursor(eyeCursor);

            SyncAnglesFromTransform();

            if (lockCursorWhileOrbiting)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            orbitDeltaSmoothed = Vector2.zero;
            orbitDeltaVel = Vector2.zero;
        }

        if (Input.GetMouseButtonUp(1))
        {
            orbiting = false;

            if (lockCursorWhileOrbiting)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            orbitDeltaSmoothed = Vector2.zero;
            orbitDeltaVel = Vector2.zero;
            return;
        }

        if (!orbiting) return;

        // Use Input axes while locked (stable, no screen-edge issues)
        float dx = Input.GetAxisRaw("Mouse X");
        float dy = Input.GetAxisRaw("Mouse Y");

        // Clamp rare spikes (frame hitch / window focus changes)
        dx = Mathf.Clamp(dx, -maxMouseDelta, maxMouseDelta);
        dy = Mathf.Clamp(dy, -maxMouseDelta, maxMouseDelta);

        Vector2 targetDelta = new Vector2(dx, dy);

        if (orbitSmoothing > 0f)
            orbitDeltaSmoothed = Vector2.SmoothDamp(orbitDeltaSmoothed, targetDelta, ref orbitDeltaVel, orbitSmoothing);
        else
            orbitDeltaSmoothed = targetDelta;

        yaw += orbitDeltaSmoothed.x * orbitSpeed;
        pitch -= orbitDeltaSmoothed.y * orbitSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleKeyboardPanAndZoom()
    {
        float dt = Time.unscaledDeltaTime;

        Vector3 right = transform.right;
        Vector3 up = transform.up;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += up;
        if (Input.GetKey(KeyCode.S)) move -= up;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
            move += transform.forward * (scroll * zoomSpeed);

        if (move.sqrMagnitude < 0.0000001f) return;

        // write into targetPosition instead of snapping transform.position
        targetPosition += move * panSpeed * dt;
    }

    // -------- Public API used by ModuleSelector / UI --------

    public void LookAtTarget(Transform target)
    {
        if (target == null) return;

        StopLookRoutine();
        lookRoutine = StartCoroutine(SmoothLookAt(target.position, lookAtDuration));
    }

    public void FindModuleStructure()
    {
        GameObject topologyRoot = GameObject.Find("GeneratedTopology");
        if (topologyRoot == null) return;

        Vector3 focus = topologyRoot.transform.position;

        float defaultYaw = 0f;
        float defaultPitch = 20f;
        float defaultDistance = 10f;

        float clampedPitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);
        Quaternion rot = Quaternion.Euler(clampedPitch, defaultYaw, 0f);
        Vector3 homePos = focus - (rot * Vector3.forward) * defaultDistance;

        StopLookRoutine();
        if (homeRoutine != null) StopCoroutine(homeRoutine);
        homeRoutine = StartCoroutine(SmoothMoveAndRotate(homePos, rot, homeDuration));
    }

    // -------- Smooth routines --------

    IEnumerator SmoothLookAt(Vector3 targetPoint, float duration)
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.LookRotation((targetPoint - transform.position).normalized, Vector3.up);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        SyncAnglesFromTransform();
        lookRoutine = null;
    }

    IEnumerator SmoothMoveAndRotate(Vector3 endPos, Quaternion endRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);

            // position
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            transform.position = pos;

            // rotation
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        // keep move smoothing target aligned so it doesn't “fight” after home
        targetPosition = transform.position;
        positionVel = Vector3.zero;

        SyncAnglesFromTransform();
        homeRoutine = null;
    }

    void StopLookRoutine()
    {
        if (lookRoutine != null)
        {
            StopCoroutine(lookRoutine);
            lookRoutine = null;
        }
    }

    // -------- Helpers --------

    void SyncAnglesFromTransform()
    {
        Vector3 e = transform.eulerAngles;
        yaw = NormalizeAngle(e.y);
        pitch = NormalizeAngle(e.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    void SetCursor(Texture2D tex)
    {
        Cursor.SetCursor(tex, cursorHotspot, CursorMode.Auto);
    }
}