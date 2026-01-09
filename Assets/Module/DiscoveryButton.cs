using UnityEngine;

public class DiscoveryButton : MonoBehaviour
{
    public TopologyBuilder topologyBuilder;

    void Start()
    {
        // todo: where should this be? needs to be somewhere on startup of the game.

        // Sentry is used for crash logging
        ControlLibrary.control_sentry_init("https://945ddf43f243019f176a8b6171cf534a@o4505559031545856.ingest.us.sentry.io/4510490606567424", "env", "rel");
        ControlLibrary.control_sentry_set_app_info("unity", "1", "1");

        ControlLibrary.init();
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
        ControlLibrary.control_sentry_shutdown();
    }
}
