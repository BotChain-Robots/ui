using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Frontend;



public class TopologyBuilder : MonoBehaviour
{
    public ModuleSpawner spawner;

    public static Dictionary<string, GameObject> idToInstance = new();

    public void BuildTopologyFromJson()
    {

        // todo: this is some really bad temporary code
        TopologyGraph graph = new TopologyGraph();
        RobotConfiguration config = ControlLibrary.getRobotConfiguration();

        Dictionary<int, ModuleType> idToType = new();
        int moduleCount = config.ModulesLength;
        for (int i = 0; i < moduleCount; i++)
        {
            var n_module = config.Modules(i);
            if (n_module != null)
            {
                var module = n_module.Value;
                Debug.Log("Adding module " + module.Id);
                graph.Modules.Add(new ModuleData
                {
                    Id = module.Id,
                    Type = module.ModuleType.ToString(),
                    Degree = module.ConfigurationAsMotorState().Angle
                });
                idToType.Add(module.Id, module.ModuleType);
            }
        }

        int connectionCount = config.ConnectionsLength;
        for (int i = 0; i < connectionCount; i++)
        {
            var n_connection = config.Connections(i);
            if (n_connection != null)
            {
                var connection = n_connection.Value;
                Debug.Log("orientation: " + connection.Orientation);
                graph.Connections.Add(new Connection
                {
                    FromModuleId = connection.FromModuleId,
                    FromSocket = idToType[connection.FromModuleId] == ModuleType.SPLITTER ? "MaleSocket" + (connection.FromSocket == 0 ? "" : connection.FromSocket) : "MaleSocket",
                    ToModuleId = connection.ToModuleId,
                    ToSocket = "FemaleSocket",
                    Orientation = connection.Orientation
                });
            }
        }

        // Destroy previous topology root if it exists
        GameObject oldRoot = GameObject.Find("GeneratedTopology");
        if (oldRoot != null)
        {
            DestroyImmediate(oldRoot);
        }

        // Create a new root GameObject to hold all modules
        GameObject topologyRoot = new GameObject("GeneratedTopology");
        topologyRoot.transform.localScale = new Vector3(20f, 20f, 20f);


        // Clean up old instances
        foreach (var oldModule in idToInstance.Values)
        {
            Destroy(oldModule);
        }
        idToInstance.Clear();

        // First spawn all modules
        foreach (var module in graph.Modules)
        {
            // Debug.Log($"Before error: {module.Type}.");
            ModuleType type = Enum.Parse<ModuleType>(module.Type);
            GameObject prefab = spawner.GetPrefabForType(type);
            // GameObject prefab = spawner.GetPrefabForType(module.Type);
            if (prefab == null)
            {
                Debug.LogError($"No prefab found for type {module.Type}");
                continue;
            }

            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, topologyRoot.transform);
            instance.name = module.Id.ToString();

            // instance.transform.localScale = new Vector3(20f, 20f, 20f);
            instance.transform.SetParent(topologyRoot.transform, false); // attach to root
            instance.transform.localScale = Vector3.one; 
            idToInstance[module.Id.ToString()] = instance;

            ModuleBase baseScript = instance.GetComponent<ModuleBase>();
            if (baseScript != null)
            {
                baseScript.moduleID = module.Id.ToString();
                if (module.Type.Contains("Servo") && module.Degree != null)
                {
                    ServoMotorModule servo;
                    if (module.Type == "Servo1")
                    {
                        servo = instance.GetComponent<ServoBendModule>();
                    }
                    else
                    {
                        servo = instance.GetComponent<ServoStraightModule>();
                    }
                    //ServoBendModule servo = instance.GetComponent<ServoBendModule>();
                    // servo.SetInitialAngle(module.Degree);
                    servo.InitialSetAngle(module.Degree);
                }
            }
        }

        // Then connect them
        foreach (var connection in graph.Connections)
        {
            if (!idToInstance.ContainsKey(connection.FromModuleId.ToString()) || !idToInstance.ContainsKey(connection.ToModuleId.ToString()))
            {
                Debug.LogWarning($"Missing instance for {connection.FromModuleId} or {connection.ToModuleId}");
                continue;
            }

            GameObject objFrom = idToInstance[connection.FromModuleId.ToString()];
            GameObject objTo   = idToInstance[connection.ToModuleId.ToString()];

            string socketA = connection.FromSocket;
            string socketB = connection.ToSocket;
            // Debug.LogError("FromModuleId: " + connection.FromModuleId + " ToModuleId: " + connection.ToModuleId + " FromSocket: " + socketA + " ToSocket: " + socketB);

            ModuleData fromModule = graph.Modules.Find(m => m.Id == connection.FromModuleId);
            ModuleData toModule   = graph.Modules.Find(m => m.Id == connection.ToModuleId);
            ModuleType fromType = Enum.Parse<ModuleType>(fromModule.Type);
            ModuleType toType = Enum.Parse<ModuleType>(toModule.Type);
            if ((fromType == ModuleType.SERVO_1 || fromType == ModuleType.SERVO_2) && socketA == "MaleSocket") {
                socketA = "ArmSocket";
                // Debug.LogError("here");
            }
            else if ((fromType == ModuleType.SERVO_1 || fromType == ModuleType.SERVO_2) && socketA == "FemaleSocket") {
                socketA = "BodySocket";
                // Debug.LogError("here1");
            }

            if ((toType == ModuleType.SERVO_1 || toType == ModuleType.SERVO_2) && socketB == "MaleSocket") {
                socketB = "ArmSocket";
                // Debug.LogError("here2");
            }
            else if ((toType == ModuleType.SERVO_1 || toType == ModuleType.SERVO_2) && socketB == "FemaleSocket") {
                socketB = "BodySocket";
                // Debug.LogError("here3");
            }

            float twistDegrees;
            switch (connection.Orientation)
            {
                case Orientation.Deg90:
                    twistDegrees = 90f;
                    break;
                case Orientation.Deg180:
                    twistDegrees = 180f;
                    break;
                case Orientation.Deg270:
                    twistDegrees = 270f;
                    break;
                default:
                    twistDegrees = 0f;
                    break;
            }

            Debug.Log("twist deg " + twistDegrees);
            twistDegrees -= 90; // todo: we should not need to do this, there is a bug further down where the rotation is not correct

            spawner.ConnectModules(objFrom, socketA, objTo, socketB, objFrom.transform.position, twistDegrees);
        }
    }
}