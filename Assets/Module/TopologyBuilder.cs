using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Frontend;

[System.Serializable]
public class JsonTopologyGraph { public List<JsonModuleData> Modules; public List<JsonConnection> Connections; }
[System.Serializable]
public class JsonModuleData { public string Id; public string Type; public float Degree; }
[System.Serializable]
public class JsonConnection { public string FromModuleId; public string ToModuleId; public string FromSocket; public string ToSocket; public int Orientation; }

public class TopologyBuilder : MonoBehaviour
{
    public ModuleSpawner spawner;

    [Header("Topology Selection")]
    [Tooltip("Set to true to load from JSON file (for local testing). Set to false to use ControlLibrary.")]
    public bool useJsonFile = true;
    [Tooltip("When true, angle commands are not sent to native ControlLibrary - use only for local testing without hardware.")]
    public bool skipControlLibraryCalls = false;
    [Tooltip("JSON file name (without .json) in Resources folder. e.g. 'mockData' or 'mockDataSimple'")]
    public string jsonFileName = "mockDataNewConfig";

    public static Dictionary<string, GameObject> idToInstance = new();
    public static bool _skipControlLibraryCalls = false;

    public static bool SkipControlLibraryCalls => _skipControlLibraryCalls;

    public void BuildTopologyFromJson()
    {
        TopologyGraph graph = new TopologyGraph();
        Dictionary<int, ModuleType> idToType = new();

        if (useJsonFile)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
            if (jsonAsset == null) { Debug.LogError($"JSON not found: Resources/{jsonFileName}.json"); return; }
            JsonTopologyGraph jsonGraph = JsonUtility.FromJson<JsonTopologyGraph>(jsonAsset.text);
            if (jsonGraph?.Modules == null || jsonGraph.Modules.Count == 0) { Debug.LogError("JSON has no modules"); return; }

            Dictionary<string, int> stringToIntId = new Dictionary<string, int>();
            int nextIntId = 1;
            foreach (var m in jsonGraph.Modules)
            {
                if (!stringToIntId.ContainsKey(m.Id)) stringToIntId[m.Id] = nextIntId++;
                int intId = stringToIntId[m.Id];
                ModuleType mt = ParseModuleType(m.Type);
                idToType[intId] = mt;
                graph.Modules.Add(new ModuleData { Id = intId, Type = mt.ToString(), Degree = m.Degree });
            }
            if (jsonGraph.Connections != null)
                foreach (var c in jsonGraph.Connections)
                {
                    if (!stringToIntId.TryGetValue(c.FromModuleId, out int fromId) || !stringToIntId.TryGetValue(c.ToModuleId, out int toId)) continue;
                    Orientation o = c.Orientation == 90 ? Orientation.Deg90 : c.Orientation == 180 ? Orientation.Deg180 : c.Orientation == 270 ? Orientation.Deg270 : Orientation.Deg0;
                    graph.Connections.Add(new Connection { FromModuleId = fromId, ToModuleId = toId, FromSocket = c.FromSocket ?? "MaleSocket", ToSocket = c.ToSocket ?? "FemaleSocket", Orientation = o });
                }
            Debug.Log($"[TopologyBuilder] Built from JSON: {jsonFileName}");
            _skipControlLibraryCalls = skipControlLibraryCalls;
        }
        else
        {
            RobotConfiguration config = ControlLibrary.getRobotConfiguration();
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
            _skipControlLibraryCalls = false;
        }

        BuildTopologyFromGraph(graph);
    }

    private static ModuleType ParseModuleType(string typeString)
    {
        if (string.IsNullOrEmpty(typeString)) return ModuleType.SPLITTER;
        switch (typeString)
        {
            case "Servo1": return ModuleType.SERVO_1;
            case "Servo2": return ModuleType.SERVO_2;
            case "DC": return ModuleType.DC_MOTOR;
            case "Hub": return ModuleType.SPLITTER;
            case "Battery": return ModuleType.BATTERY;
            case "Gripper": return ModuleType.GRIPPER;
            case "Display": return ModuleType.DISPLAY;
            case "Distance": return ModuleType.DISTANCE_SENSOR;
            case "IMU": return ModuleType.IMU;
            default: return ModuleType.SPLITTER;
        }
    }

    private void BuildTopologyFromGraph(TopologyGraph graph)
    {
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
                ModuleType parsedType = Enum.Parse<ModuleType>(module.Type);
                if ((parsedType == ModuleType.SERVO_1 || parsedType == ModuleType.SERVO_2))
                {
                    ServoMotorModule servo;
                    if (parsedType == ModuleType.SERVO_1)
                    {
                        servo = instance.GetComponent<ServoBendModule>();
                    }
                    else
                    {
                        servo = instance.GetComponent<ServoStraightModule>();
                    }
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