using System;
using System.Collections.Generic;

// Root graph container
[Serializable]
public class TopologyGraph
{
    public List<ModuleData> Modules = new List<ModuleData>();
    public List<Connection> Connections = new List<Connection>();
}

// Individual module
[Serializable]
public class ModuleData
{
    public int Id;
    public string Type;
    public float Degree;
}

// Connection between modules
[Serializable]
public class Connection
{
    public int FromModuleId;
    public string FromSocket;
    public int ToModuleId;
    public string ToSocket;
    public Orientation Orientation;
}
