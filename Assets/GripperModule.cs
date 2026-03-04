using UnityEngine;

public class GripperModule : ServoMotorModule
{
    [Header("Pincer Pivots")]
    public Transform leftPincerPivot;
    public Transform rightPincerPivot;

    [Header("Gripper Settings")]
    [Tooltip("Max separation between pincers (meters). 0.04 = 4 cm")]
    public float maxPincerDistance = 0.02f;

    public override string servoType => "Gripper";
    public override string moduleType => "Gripper";
    public override string moduleName => "Gripper Module";

    // Cache the original local positions so we don't stomp Y/Z (or any authored offsets).
    private Vector3 _leftStartLocalPos;
    private Vector3 _rightStartLocalPos;
    private Transform _cachedLeft;
    private Transform _cachedRight;
    private bool _hasCached;

    private void CacheStartPositionsIfNeeded()
    {
        // Re-cache if pivots changed, or we haven't cached yet.
        if (!_hasCached || _cachedLeft != leftPincerPivot || _cachedRight != rightPincerPivot)
        {
            if (leftPincerPivot != null)
                _leftStartLocalPos = leftPincerPivot.localPosition;

            if (rightPincerPivot != null)
                _rightStartLocalPos = rightPincerPivot.localPosition;

            _cachedLeft = leftPincerPivot;
            _cachedRight = rightPincerPivot;
            _hasCached = true;
        }
    }

    public override void MoveArmPivot(float currentAngle)
    {
        CacheStartPositionsIfNeeded();

        // Map angle 0..180 to 0..1
        float normalized = Mathf.InverseLerp(0f, 180f, currentAngle);

        // We move each pincer half the total distance, in opposite directions.
        float halfDistance = normalized * (maxPincerDistance * 0.5f);

        if (leftPincerPivot != null)
        {
            Vector3 target = _leftStartLocalPos;
            target.x += -halfDistance;   // only change local X
            leftPincerPivot.localPosition = target;
        }

        if (rightPincerPivot != null)
        {
            Vector3 target = _rightStartLocalPos;
            target.x += halfDistance;    // only change local X
            rightPincerPivot.localPosition = target;
        }
    }

    public override void SetAngle(float angle)
    {
        currentAngle = Mathf.Clamp(angle, 0f, 180f);
        MoveArmPivot(currentAngle);
    }

    public override void InitialSetAngle(float angle)
    {
        currentAngle = Mathf.Clamp(angle, 0f, 180f);
        lastSentAngle = currentAngle;

        // Ensure we cache before first move so we preserve the authored offsets.
        CacheStartPositionsIfNeeded();
        MoveArmPivot(currentAngle);
    }

#if UNITY_EDITOR
    // Optional but handy: if you tweak pivots/positions in the editor, this helps keep cache accurate.
    private void OnValidate()
    {
        _hasCached = false;
    }
#endif
}