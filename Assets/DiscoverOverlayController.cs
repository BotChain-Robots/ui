using UnityEngine;
using UnityEngine.UI;

public class DiscoverOverlayController : MonoBehaviour
{
    public GameObject overlayPanel;
    public RectTransform overlayContent;
    public TopologyBuilder topologyBuilder;

    public void ShowOverlay()
    {
        if (overlayPanel == null) return;

        EnsureScrollRect();
        ClearContent();

        if (topologyBuilder == null)
            topologyBuilder = FindObjectOfType<TopologyBuilder>();

        bool useMock = topologyBuilder != null && topologyBuilder.mockControlLibrary;

        if (useMock)
        {
            PopulateMockLeaders();
        }
        else
        {
            int[] leaders = null;
            try
            {
                leaders = ControlLibrary.getRobotLeaders();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Discover] getRobotLeaders failed: {ex.Message}");
            }

            if (leaders != null && leaders.Length > 0)
                PopulateLeaders(leaders);
            else
                CreateLabel("No leaders found. Is the hardware connected?");
        }

        overlayPanel.SetActive(true);
    }

    public void HideOverlay()
    {
        ClearContent();
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }

    void PopulateLeaders(int[] leaderIds)
    {
        float buttonHeight = 60f;
        float spacing = 10f;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        for (int i = 0; i < leaderIds.Length; i++)
        {
            int leaderId = leaderIds[i];

            GameObject btnGo = new GameObject($"LeaderBtn_{leaderId}", typeof(RectTransform));
            btnGo.transform.SetParent(overlayContent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 1f);
            rt.anchorMax = new Vector2(0.95f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(spacing + i * (buttonHeight + spacing)));
            rt.sizeDelta = new Vector2(0f, buttonHeight);

            Image bg = btnGo.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            GameObject txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.text = $"Leader {leaderId}";
            txt.font = font;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            btn.onClick.AddListener(() => OnLeaderSelected(leaderId));
        }

        float totalHeight = leaderIds.Length * (buttonHeight + spacing) + spacing;
        overlayContent.sizeDelta = new Vector2(overlayContent.sizeDelta.x, totalHeight);
    }

    void PopulateMockLeaders()
    {
        float buttonHeight = 60f;
        float spacing = 10f;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        int i = 0;
        foreach (var kvp in TopologyBuilder.MockLeaders)
        {
            int id = kvp.Key;
            string jsonName = kvp.Value;
            int idx = i;

            GameObject btnGo = new GameObject($"MockLeaderBtn_{id}", typeof(RectTransform));
            btnGo.transform.SetParent(overlayContent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 1f);
            rt.anchorMax = new Vector2(0.95f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(spacing + idx * (buttonHeight + spacing)));
            rt.sizeDelta = new Vector2(0f, buttonHeight);

            Image bg = btnGo.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            GameObject txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Text txt = txtGo.AddComponent<Text>();
            txt.text = $"Mock Leader {id}  ({jsonName})";
            txt.font = font;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            string capturedJsonName = jsonName;
            btn.onClick.AddListener(() => OnMockLeaderSelected(capturedJsonName));
            i++;
        }

        float totalHeight = TopologyBuilder.MockLeaders.Count * (buttonHeight + spacing) + spacing;
        overlayContent.sizeDelta = new Vector2(overlayContent.sizeDelta.x, totalHeight);
    }

    void OnMockLeaderSelected(string jsonFileName)
    {
        Debug.Log($"[Discover] Mock leader selected, loading {jsonFileName}...");

        if (topologyBuilder == null)
            topologyBuilder = FindObjectOfType<TopologyBuilder>();

        if (topologyBuilder == null)
        {
            Debug.LogError("[Discover] No TopologyBuilder found in scene!");
            HideOverlay();
            return;
        }

        try
        {
            topologyBuilder.BuildTopologyFromJsonFile(jsonFileName);
            Debug.Log($"[Discover] Mock topology built from {jsonFileName}");

            var cam = FindObjectOfType<UserCameraControl>();
            if (cam != null) cam.FindModuleStructure();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Discover] Failed to build mock topology: {ex.Message}");
        }

        HideOverlay();
    }

    void OnLeaderSelected(int leaderId)
    {
        Debug.Log($"[Discover] Selected leader {leaderId}, building topology...");

        if (topologyBuilder == null)
        {
            topologyBuilder = FindObjectOfType<TopologyBuilder>();
        }

        if (topologyBuilder == null)
        {
            Debug.LogError("[Discover] No TopologyBuilder found in scene!");
            HideOverlay();
            return;
        }

        try
        {
            topologyBuilder.BuildTopologyFromLeader(leaderId);
            Debug.Log($"[Discover] Topology built successfully for leader {leaderId}");

            var cam = FindObjectOfType<UserCameraControl>();
            if (cam != null) cam.FindModuleStructure();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Discover] Failed to build topology for leader {leaderId}: {ex.Message}");
        }

        HideOverlay();
    }

    void CreateLabel(string message)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject go = new GameObject("InfoLabel", typeof(RectTransform));
        go.transform.SetParent(overlayContent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 1f);
        rt.anchorMax = new Vector2(0.95f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(0f, 50f);

        Text txt = go.AddComponent<Text>();
        txt.text = message;
        txt.font = font;
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 0.6f, 0.6f, 1f);
    }

    void EnsureScrollRect()
    {
        if (overlayContent == null) return;
        var scrollArea = overlayContent.parent;
        if (scrollArea == null) return;
        var scrollGo = scrollArea.gameObject;
        if (scrollGo.GetComponent<Mask>() == null)
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;
        var sr = scrollGo.GetComponent<ScrollRect>();
        if (sr == null)
        {
            sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.content = overlayContent;
            sr.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    void ClearContent()
    {
        if (overlayContent == null) return;
        for (int i = overlayContent.childCount - 1; i >= 0; i--)
            Destroy(overlayContent.GetChild(i).gameObject);
    }
}
