using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InverseKinematicsController : MonoBehaviour
{
    [Header("IK Settings")]
    public int maxIterations = 10;
    public float tolerance = 0.1f;
    public bool constrainAngles = true;

    [Header("Per-Joint Rotation Deadzone")]
    [Tooltip("For ServoStraightModule joints only: if CCD computes a rotation smaller than this many degrees for a joint, that joint will not be updated this step.")]
    [Min(0f)]
    public float minRotationStepDegreesForDcAndStraight = 10f;

    [Header("Drag Sensitivity")]
    [Tooltip("How much of the user's mouse drag movement is interpreted (1.0 = 100%, 0.2 = 20%)")]
    [Range(0.01f, 2.0f)]
    public float dragSensitivity = 0.7f;
    
    [Header("Movement Smoothing")]
    [Range(0.01f, 1f)]
    public float movementSpeed = 0.3f;
    public bool useSmoothing = true;
    
    [Header("Debug")]
    public bool drawDebugLines = true;
    public Color debugLineColor = Color.yellow;

    private List<IKJoint> kinematicChain = new List<IKJoint>();
    private Vector3 targetPosition;
    private Vector3 smoothedTargetPosition;
    private Transform endEffector;
    private Transform anchorPoint;
    private Vector3 anchorPosition;
    private bool isSolving = false;

    public bool BuildKinematicChain(Transform targetModule)
    {
        kinematicChain.Clear();
        endEffector = targetModule;

        Transform root = FindTopologyRoot(targetModule);
        if (root == null || root.name != "GeneratedTopology")
        {
            return false;
        }

        BuildChainFromTargetToRoot(targetModule, root);
        kinematicChain.Reverse();

        if (kinematicChain.Count > 0)
        {
            Transform firstBase = kinematicChain[0].baseTransform != null ? kinematicChain[0].baseTransform : kinematicChain[0].pivotTransform;
            Transform foundAnchor = FindAnchorPoint(firstBase);
            if (foundAnchor != null)
            {
                anchorPoint = foundAnchor;
                anchorPosition = anchorPoint.position;
            }
            else
            {
                anchorPoint = firstBase;
                anchorPosition = anchorPoint.position;
            }
        }
        else
        {
            Transform foundAnchor = FindAnchorPoint(endEffector);
            if (foundAnchor != null)
            {
                anchorPoint = foundAnchor;
                anchorPosition = anchorPoint.position;
            }
        }

        ResetSmoothing();
        return kinematicChain.Count > 0;
    }

    private void BuildChainFromTargetToRoot(Transform targetModule, Transform root)
    {
        Transform current = targetModule;
        bool skippedSelectedModuleJoint = false;
        while (current != null && current != root)
        {
            ServoMotorModule servo = current.GetComponentInParent<ServoMotorModule>();
            if (servo != null && servo.armPivot != null)
            {
                if (!kinematicChain.Any(j => j.servo == servo))
                {
                    if (!skippedSelectedModuleJoint)
                    {
                        skippedSelectedModuleJoint = true;
                        current = current.parent;
                        continue;
                    }
                    IKJoint joint = new IKJoint
                    {
                        servo = servo,
                        pivotTransform = servo.armPivot,
                        baseTransform = servo.transform,
                        minAngle = 0f,
                        maxAngle = 180f,
                        currentAngle = servo.currentAngle
                    };

                    // Critical: store axis in pivot-local space; compute world axis each iteration.
                    if (servo is ServoBendModule)
                    {
                        joint.localRotationAxis = Vector3.forward;
                    }
                    else if (servo is ServoStraightModule)
                    {
                        joint.localRotationAxis = Vector3.right;
                    }
                    else
                    {
                        joint.localRotationAxis = Vector3.up;
                    }

                    kinematicChain.Add(joint);
                }
            }

            current = current.parent;
        }
    }

    public bool SolveIK(Vector3 targetPos)
    {
        if (kinematicChain.Count == 0)
        {
            return false;
        }

        if (useSmoothing)
        {
            if (smoothedTargetPosition == Vector3.zero)
            {
                smoothedTargetPosition = endEffector != null ? endEffector.position : targetPos;
            }
            smoothedTargetPosition = Vector3.Lerp(smoothedTargetPosition, targetPos, movementSpeed);
            targetPosition = smoothedTargetPosition;
        }
        else
        {
            targetPosition = targetPos;
        }
        
        isSolving = true;

        bool success = SolveCCD();

        isSolving = false;
        return success;
    }

    private bool SolveCCD()
    {
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            for (int i = kinematicChain.Count - 1; i >= 0; i--)
            {
                IKJoint joint = kinematicChain[i];
                
                Vector3 endEffectorPos = endEffector.position;
                
                Vector3 jointPos = joint.pivotTransform.position;
                
                if (i == 0 && anchorPoint != null)
                {
                    jointPos = anchorPosition;
                }
                
                Vector3 toEndEffector = endEffectorPos - jointPos;
                Vector3 toTarget = targetPosition - jointPos;
                
                if (toEndEffector.magnitude < 0.001f || toTarget.magnitude < 0.001f)
                    continue;
                
                Vector3 worldAxis = joint.pivotTransform.TransformDirection(joint.localRotationAxis).normalized;
                float angle = Vector3.SignedAngle(toEndEffector, toTarget, worldAxis);

                float desiredAngle = joint.currentAngle + angle;
                if (constrainAngles)
                {
                    desiredAngle = Mathf.Clamp(desiredAngle, joint.minAngle, joint.maxAngle);
                }

                float appliedDelta = desiredAngle - joint.currentAngle;

                bool isStraightServo = joint.servo is ServoStraightModule;
                if (isStraightServo && Mathf.Abs(appliedDelta) < minRotationStepDegreesForDcAndStraight)
                {
                    continue;
                }

                joint.currentAngle = desiredAngle;
                joint.servo.SetAngleAndSendControlLibrary(desiredAngle, 1f);
                
                endEffectorPos = endEffector.position;
                
                if (Vector3.Distance(endEffectorPos, targetPosition) < tolerance)
                {
                    return true;
                }
            }
        }

        float finalDistance = Vector3.Distance(endEffector.position, targetPosition);
        return finalDistance < tolerance * 10f;
    }

    public void ResetSmoothing()
    {
        smoothedTargetPosition = Vector3.zero;
    }
    
    public Transform GetAnchorPoint()
    {
        return anchorPoint;
    }
    
    public bool IsAnchorPoint(Transform moduleTransform)
    {
        return anchorPoint != null && anchorPoint == moduleTransform;
    }
    
    private Transform FindTopologyRoot(Transform module)
    {
        Transform current = module;
        while (current != null)
        {
            if (current.name == "GeneratedTopology")
            {
                return current;
            }
            current = current.parent;
        }
        return null;
    }
    
    private Transform FindAnchorPoint(Transform servoTransform)
    {
        Transform topologyRoot = FindTopologyRoot(servoTransform);
        if (topologyRoot == null)
        {
            return servoTransform;
        }
        
        Transform batteryOrHub = FindBatteryOrHub(topologyRoot);
        if (batteryOrHub != null)
        {
            return batteryOrHub;
        }
        
        Transform endModule = FindEndModule(topologyRoot);
        if (endModule != null)
        {
            return endModule;
        }
        
        return servoTransform;
    }
    
    private Transform FindBatteryOrHub(Transform topologyRoot)
    {
        foreach (Transform child in topologyRoot)
        {
            ModuleBase module = child.GetComponent<ModuleBase>();
            if (module != null)
            {
                PowerModule battery = child.GetComponent<PowerModule>();
                HubModule hub = child.GetComponent<HubModule>();
                
                if (battery != null || hub != null)
                {
                    return child;
                }
            }
        }
        return null;
    }
    
    private Transform FindEndModule(Transform topologyRoot)
    {
        List<Transform> allModules = new List<Transform>();
        foreach (Transform child in topologyRoot)
        {
            ModuleBase module = child.GetComponent<ModuleBase>();
            if (module != null)
            {
                allModules.Add(child);
            }
        }
        
        foreach (Transform module in allModules)
        {
            int connectionCount = CountConnections(module, allModules);
            
            if (connectionCount == 1)
            {
                return module;
            }
        }
        
        return null;
    }
    
    private int CountConnections(Transform module, List<Transform> allModules)
    {
        int count = 0;
        
        Transform currentParent = module.parent;
        if (currentParent != null && currentParent.name != "GeneratedTopology")
        {
            ModuleBase parentModule = currentParent.GetComponent<ModuleBase>();
            if (parentModule != null)
            {
                count++;
            }
            else
            {
                Transform checkParent = currentParent.parent;
                while (checkParent != null && checkParent.name != "GeneratedTopology")
                {
                    ModuleBase checkModule = checkParent.GetComponent<ModuleBase>();
                    if (checkModule != null)
                    {
                        count++;
                        break;
                    }
                    checkParent = checkParent.parent;
                }
            }
        }
        
        foreach (Transform otherModule in allModules)
        {
            if (otherModule == module) continue;
            
            Transform checkParent = otherModule.parent;
            while (checkParent != null && checkParent.name != "GeneratedTopology")
            {
                if (checkParent == module)
                {
                    count++;
                    break;
                }
                checkParent = checkParent.parent;
            }
        }
        
        return count;
    }

    void OnDrawGizmos()
    {
        if (!drawDebugLines || kinematicChain.Count == 0)
            return;

        Gizmos.color = debugLineColor;
        
        Vector3 prevPos;
        if (anchorPoint != null)
        {
            prevPos = anchorPosition;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(prevPos, 0.15f);
            Gizmos.color = debugLineColor;
        }
        else
        {
            prevPos = kinematicChain[0].pivotTransform.position;
        }
        
        for (int i = 0; i < kinematicChain.Count; i++)
        {
            Vector3 currentPos = kinematicChain[i].pivotTransform.position;
            Gizmos.DrawLine(prevPos, currentPos);
            Gizmos.DrawSphere(currentPos, 0.1f);
            prevPos = currentPos;
        }
        
        if (endEffector != null)
        {
            Gizmos.DrawLine(prevPos, endEffector.position);
            Gizmos.DrawSphere(endEffector.position, 0.15f);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition, 0.2f);
        Gizmos.DrawLine(endEffector != null ? endEffector.position : prevPos, targetPosition);
    }

    private class IKJoint
    {
        public ServoMotorModule servo;
        public Transform pivotTransform;
        public Transform baseTransform;
        public Vector3 localRotationAxis;
        public float minAngle;
        public float maxAngle;
        public float currentAngle;
    }
}

