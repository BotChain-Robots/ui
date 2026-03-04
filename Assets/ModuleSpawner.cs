using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ModuleSpawner : MonoBehaviour
{
    public GameObject batteryModulePrefab;
    public GameObject hubModuleMMMFPrefab;
    public GameObject hubModuleMMMMPrefab;
    public GameObject triangleHubMMFPrefab;
    public GameObject triangleHubMMMPrefab;
    public GameObject dcMotorModulePrefab;
    public GameObject servoBendModulePrefab;
    public GameObject servoStraightModulePrefab;
    public GameObject gripperModulePrefab;
    public GameObject displayModulePrefab;
    public GameObject distanceSensorModulePrefab;
    public GameObject imuSensorModulePrefab;
    public GameObject speakerModulePrefab;

    public GameObject GetPrefabForType(ModuleType type)
    {
        // Debug.Log($"In spawner with type: {type}.");
        switch (type)
        {
            case ModuleType.BATTERY:
                return batteryModulePrefab;
            case ModuleType.SPLITTER:
                return hubModuleMMMFPrefab;
            case ModuleType.DC_MOTOR:
                return dcMotorModulePrefab; // DC module deactivated
            case ModuleType.SERVO_1:
                return servoBendModulePrefab;
            case ModuleType.SERVO_2:
                return servoStraightModulePrefab;
            case ModuleType.GRIPPER:
                return gripperModulePrefab;
            case ModuleType.DISPLAY:
                return displayModulePrefab;
            case ModuleType.DISTANCE_SENSOR:
                return distanceSensorModulePrefab;
            case ModuleType.IMU:
                return imuSensorModulePrefab;
            case ModuleType.SPEAKER:
                return speakerModulePrefab;
            case ModuleType.SPLITTER_2:
                return hubModuleMMMMPrefab;
            case ModuleType.SPLITTER_3:
                return triangleHubMMFPrefab;
            case ModuleType.SPLITTER_4:
                return triangleHubMMMPrefab;
            default:
                Debug.LogError("Unknown module type: " + type);
                return null;
        }
    }


    private void Start()
    {
        // SpawnDCUnderServo(0);     // Default orientation
        // SpawnDCUnderServo(90);    // Rotate DC 90° clockwise around the socket
        // SpawnDCUnderServo(180);   // Flip DC around the socket
        // SpawnDCUnderServo(270);   // Rotate DC 270 counter-clockwise around the socket
        // SpawnServoServoDCChain();

        // GameObject hubInstance = Instantiate(hubModulePrefab, Vector3.zero, Quaternion.identity);
        //         GameObject hubInstance1 = Instantiate(hubModulePrefab, Vector3.zero, Quaternion.identity);

        // GameObject hubInstance2 = Instantiate(hubModulePrefab, Vector3.zero, Quaternion.identity);

        // GameObject hubInstance3 = Instantiate(hubModulePrefab, Vector3.zero, Quaternion.identity);

        // GameObject servo1 = Instantiate(servoBendModulePrefab, Vector3.right * 2, Quaternion.identity);
        // GameObject servo2 = Instantiate(servoBendModulePrefab, Vector3.right * 4, Quaternion.identity);
        // GameObject servo3 = Instantiate(servoBendModulePrefab, Vector3.right * 2, Quaternion.identity);
        // GameObject servo4 = Instantiate(servoBendModulePrefab, Vector3.right * 4, Quaternion.identity);
        // GameObject battery1 = Instantiate(batteryModulePrefab, Vector3.right * 6, Quaternion.identity);

        // ConnectModules(
        //     hubInstance,
        //     "FemaleSocket",
        //     servo1,
        //     "ArmSocket",
        //     new Vector3(2, 0, 0),
        //     0f
        // );
        // ConnectModules(
        //     hubInstance1,
        //     "FemaleSocket",
        //     servo2,
        //     "ArmSocket",
        //     new Vector3(2, 0, 0),
        //     90f
        // );
        // ConnectModules(
        //     hubInstance2,
        //     "FemaleSocket",
        //     servo3,
        //     "ArmSocket",
        //     new Vector3(2, 0, 0),
        //     180f
        // );
        // ConnectModules(
        //     hubInstance3,
        //     "FemaleSocket",
        //     servo4,
        //     "ArmSocket",
        //     new Vector3(2, 0, 0),
        //     270f
        // );
    }

    // private void SpawnBatteryModule()
    // {
    //     Instantiate(batteryModulePrefab, new Vector3(0, 0, 0), Quaternion.identity);
    // }

    // private void SpawnHubModule()
    // {
    //     Instantiate(hubModulePrefab, new Vector3(1.5f, 0, 0), Quaternion.identity);
    // }

    // private void SpawnDCMotorModule()
    // {
    //     Instantiate(dcMotorModulePrefab, new Vector3(3.0f, 0, 0), Quaternion.identity);
    // }

    // private void SpawnServoBendModule()
    // {
    //     Instantiate(servoBendModulePrefab, new Vector3(4.5f, 0, 0), Quaternion.identity);
    // }

    public void ConnectModules(
        GameObject parentPrefab,
        string parentSocketPath,
        GameObject childPrefab,
        string childSocketPath,
        Vector3 spawnPosition,
        float twistDegrees = 0f
    )
    {
        // Spawn and scale both
        // GameObject objA = Instantiate(parentPrefab, spawnPosition, Quaternion.identity);
        // GameObject objB = Instantiate(childPrefab);
        // parentPrefab.transform.localScale = new Vector3(20f, 20f, 20f);
        // childPrefab.transform.localScale = new Vector3(20f, 20f, 20f);

        // Locate sockets
        Transform socketA = FindSocketSmartPath(parentPrefab, parentSocketPath);
        Transform socketB = FindSocketSmartPath(childPrefab, childSocketPath);

        if (socketA == null || socketB == null)
        {
            Debug.LogError("One or both sockets not found.");
            Destroy(parentPrefab);
            Destroy(childPrefab);
            return;
        }

        // Check if either socket is a Servo ArmSocket
        if (IsArmSocket(socketA.name) ^ IsArmSocket(socketB.name))
        {
            ConnectWithServoArm(parentPrefab, socketA, childPrefab, socketB, twistDegrees);
            return;
        }

        // Normal case (no servo arm involved)
        // bool aIsFemale = IsFemaleSocket(socketA.name);
        // Transform parentSocket = aIsFemale ? socketA : socketB;
        // Transform childSocket  = aIsFemale ? socketB : socketA;
        // GameObject parentObj   = aIsFemale ? parentPrefab : childPrefab;
        // GameObject childObj    = aIsFemale ? childPrefab : parentPrefab;

        bool aIsMale = IsMaleSocket(socketA.name);
        Transform parentSocket = aIsMale ? socketA : socketB;
        Transform childSocket  = aIsMale ? socketB : socketA;
        GameObject parentObj   = aIsMale ? parentPrefab : childPrefab;
        GameObject childObj    = aIsMale ? childPrefab : parentPrefab;

        Quaternion alignRotation = parentSocket.rotation * Quaternion.Inverse(childSocket.rotation);
        childObj.transform.rotation = alignRotation * childObj.transform.rotation;

        Vector3 posOffset = parentSocket.position - childSocket.position;
        childObj.transform.position += posOffset;

        if (twistDegrees != 0f)
        {
            childObj.transform.RotateAround(parentSocket.position, parentSocket.right, twistDegrees);
        }

        childObj.transform.SetParent(parentSocket.parent, worldPositionStays: true);
        childObj.transform.localScale = Vector3.one;
    }

    private bool IsMaleSocket(string socketName)
    {
        return socketName.Contains("ArmSocket") || socketName.Contains("MaleSocket");
    }

    private void ConnectWithServoArm(
        GameObject objA, Transform socketA,
        GameObject objB, Transform socketB,
        float twistDegrees
    )
    {
        bool aIsArm = IsArmSocket(socketA.name);

        GameObject servoObj   = aIsArm ? objA : objB;
        GameObject childObj   = aIsArm ? objB : objA;
        Transform armSocket   = aIsArm ? socketA : socketB;
        Transform childSocket = aIsArm ? socketB : socketA;

        Quaternion alignRotation = armSocket.rotation * Quaternion.Inverse(childSocket.rotation);
        childObj.transform.rotation = alignRotation * childObj.transform.rotation;

        Vector3 posOffset = armSocket.position - childSocket.position;
        childObj.transform.position += posOffset;

        if (twistDegrees != 0f)
        {
            childObj.transform.RotateAround(armSocket.position, armSocket.right, twistDegrees);
        }

        Transform armPivot = servoObj.transform.Find("ArmPivot");
        if (armPivot == null)
        {
            Debug.LogWarning("ArmPivot not found on servo. Falling back to default parent.");
            armPivot = armSocket.parent;
        }

        childObj.transform.SetParent(armPivot, worldPositionStays: true);
        childObj.transform.localScale = Vector3.one;
    }

    // private bool IsFemaleSocket(string socketName)
    // {
    //     return socketName.Contains("BodySocket") || socketName.Contains("FemaleSocket");
    // }

    private Transform FindSocketSmartPath(GameObject module, string socketName)
    {
        // Try direct child first (for hub, battery, etc.)
        // Debug.LogError("inside findSocketSmartPath for" + socketName + " " + module.name);
        Transform direct = module.transform.Find(socketName);
        if (direct != null) return direct;
        // Debug.LogError("huh?");
        // Lookup known paths based on socket name
        switch (socketName)
        {
            case "BodySocket":
                return module.transform.Find("servo_module_body_unity/BodySocket");

            case "ArmSocket":
                // Only Servo uses this
                return module.transform.Find("ArmPivot/ArmSocket");

            case "MaleSocket":
            case "FemaleSocket":
                return module.transform.Find(socketName);

            case "MaleSocket1":
            case "MaleSocket2":
            case "MaleSocket3":
            case "MaleSocket4":
                // Hub
                return module.transform.Find(socketName);

            default:
                Debug.LogError($"Unknown socket name '{socketName}'");
                return null;
        }
    }

    private bool IsArmSocket(string socketName)
    {
        return socketName == "ArmSocket";
    }
}