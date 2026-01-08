using UnityEngine;

public class DiscoveryButton : MonoBehaviour
{
    public TopologyBuilder topologyBuilder;

    void Start()
    {
        ControlLibrary.init(); // todo: where should this be? needs to be somewhere on startup of the game.
    }

    public void OnDiscoveryPressed()
    {
        if (topologyBuilder != null)
        {
            Debug.Log("Discovery button pressed.");
            topologyBuilder.BuildTopologyFromJson();
        }
        else
        {
            Debug.LogError("TopologyBuilder is not assigned!");
        }
    }

    void OnDestroy()
    {
        Debug.Log("Cleaning up native resources");
        ControlLibrary.cleanup();
    }
}