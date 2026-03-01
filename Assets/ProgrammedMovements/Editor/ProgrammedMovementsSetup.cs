using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor menu to build ProgrammedMovements UI hierarchy in the scene.
/// Run once via Tools > ProgrammedMovements > Setup UI Hierarchy.
/// The hierarchy is created in the scene (not at runtime) so you can modify it.
/// </summary>
public static class ProgrammedMovementsSetup
{
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        EditorSceneManager.sceneOpened += (scene, mode) =>
        {
            if (Application.isPlaying) return;
            var pmView = GameObject.Find("Views")?.transform?.Find("ProgrammedMovementsView");
            if (pmView == null) return;
            var canvas = pmView.Find("ProgrammedMovementsCanvas");
            if (canvas == null)
            {
                SetupHierarchy();
                EditorSceneManager.MarkSceneDirty(scene);
                return;
            }
            var banner = canvas.Find("TimelineBanner");
            if (banner != null && banner.Find("ConfigView") == null && banner.Find("TracksContainer") != null)
            {
                MigrateToNewLayout();
                EditorSceneManager.MarkSceneDirty(scene);
            }
        };
    }
    const int TracksCount = 3;
    const int TimelineDurationSeconds = 10;

    [MenuItem("Tools/ProgrammedMovements/Setup UI Hierarchy")]
    public static void SetupHierarchy()
    {
        var pmView = GameObject.Find("Views")?.transform?.Find("ProgrammedMovementsView");
        if (pmView == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] Views/ProgrammedMovementsView not found.");
            return;
        }

        var existing = pmView.Find("ProgrammedMovementsCanvas");
        if (existing != null)
        {
            Debug.Log("[ProgrammedMovementsSetup] Hierarchy already exists. Use Tools > ProgrammedMovements > Force Rebuild UI to replace.");
            return;
        }
        DoSetup(pmView);
    }

    [MenuItem("Tools/ProgrammedMovements/Force Rebuild UI")]
    public static void ForceRebuildHierarchy()
    {
        var pmView = GameObject.Find("Views")?.transform?.Find("ProgrammedMovementsView");
        if (pmView == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] Views/ProgrammedMovementsView not found.");
            return;
        }
        var existing = pmView.Find("ProgrammedMovementsCanvas");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
        DoSetup(pmView);
    }

    [MenuItem("Tools/ProgrammedMovements/Migrate to New Layout (add Config panel)")]
    public static void MigrateToNewLayout()
    {
        var pmView = GameObject.Find("Views")?.transform?.Find("ProgrammedMovementsView");
        if (pmView == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] Views/ProgrammedMovementsView not found.");
            return;
        }
        var canvas = pmView.Find("ProgrammedMovementsCanvas");
        if (canvas == null)
        {
            Debug.Log("[ProgrammedMovementsSetup] No canvas found. Run Setup UI Hierarchy first.");
            return;
        }
        var banner = canvas.Find("TimelineBanner");
        if (banner == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] TimelineBanner not found.");
            return;
        }
        if (banner.Find("ConfigView") != null)
        {
            Debug.Log("[ProgrammedMovementsSetup] Already has ConfigView. No migration needed.");
            return;
        }
        var bannerRect = banner.GetComponent<RectTransform>();
        if (bannerRect == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] TimelineBanner has no RectTransform.");
            return;
        }

        Transform timeLabels = banner.Find("TimeLabels");
        Transform verticalLines = banner.Find("VerticalLines");
        Transform tracksContainer = banner.Find("TracksContainer");
        var timelineViewExisting = banner.Find("TimelineView");
        if (timeLabels == null && timelineViewExisting != null)
        {
            timeLabels = timelineViewExisting.Find("TimeLabels");
            verticalLines = timelineViewExisting.Find("VerticalLines");
            tracksContainer = timelineViewExisting.Find("TracksContainer");
        }
        if (timeLabels == null || verticalLines == null || tracksContainer == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] Old structure not found. Use Force Rebuild UI.");
            return;
        }

        GameObject timelineViewGO;
        if (timelineViewExisting != null)
        {
            timelineViewGO = timelineViewExisting.gameObject;
        }
        else
        {
            timelineViewGO = new GameObject("TimelineView");
            Undo.RegisterCreatedObjectUndo(timelineViewGO, "Migrate PM");
            timelineViewGO.transform.SetParent(banner, false);
            var tr = timelineViewGO.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            Undo.SetTransformParent(timeLabels, timelineViewGO.transform, "Migrate PM");
            Undo.SetTransformParent(verticalLines, timelineViewGO.transform, "Migrate PM");
            Undo.SetTransformParent(tracksContainer, timelineViewGO.transform, "Migrate PM");
        }

        var configViewGO = new GameObject("ConfigView");
        Undo.RegisterCreatedObjectUndo(configViewGO, "Migrate PM");
        configViewGO.transform.SetParent(banner, false);
        configViewGO.SetActive(false);
        var configViewRect = configViewGO.AddComponent<RectTransform>();
        configViewRect.anchorMin = Vector2.zero;
        configViewRect.anchorMax = Vector2.one;
        configViewRect.offsetMin = Vector2.zero;
        configViewRect.offsetMax = Vector2.zero;

        var configContent = new GameObject("ConfigContent");
        configContent.transform.SetParent(configViewRect, false);
        var configContentRect = configContent.AddComponent<RectTransform>();
        configContentRect.anchorMin = Vector2.zero;
        configContentRect.anchorMax = Vector2.one;
        configContentRect.offsetMin = new Vector2(12, 8);
        configContentRect.offsetMax = new Vector2(-12, -8);

        var configVertical = configContent.AddComponent<VerticalLayoutGroup>();
        configVertical.spacing = 8;
        configVertical.padding = new RectOffset(8, 8, 8, 8);
        configVertical.childAlignment = TextAnchor.MiddleLeft;
        configVertical.childControlWidth = true;
        configVertical.childControlHeight = true;
        configVertical.childForceExpandWidth = true;
        configVertical.childForceExpandHeight = false;

        var row1 = CreateConfigRow1(configContent.transform);
        var row2 = CreateConfigRow2(configContent.transform);

        var tracksRect = tracksContainer.GetComponent<RectTransform>();
        if (tracksRect == null)
        {
            Debug.LogError("[ProgrammedMovementsSetup] TracksContainer has no RectTransform.");
            return;
        }
        for (int t = 0; t < TracksCount; t++)
        {
            if (t >= tracksRect.childCount) break;
            var track = tracksRect.GetChild(t);
            if (track == null) continue;
            var blocks = track.Find("Blocks");
            if (blocks != null)
            {
                if (blocks.GetComponent<Image>() == null)
                {
                    var img = blocks.gameObject.AddComponent<Image>();
                    img.color = new Color(0, 0, 0, 0);
                }
                if (blocks.GetComponent<TrackClickHandler>() == null)
                    blocks.gameObject.AddComponent<TrackClickHandler>();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ProgrammedMovementsSetup] Migrated to new layout. Save the scene. Enter Play mode to see Config panel when adding/editing blocks.");
    }

    static void DoSetup(Transform pmView)
    {
        var canvasGO = new GameObject("ProgrammedMovementsCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create PM Canvas");
        canvasGO.transform.SetParent(pmView, false);

        var canvasRect = canvasGO.AddComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.GetComponent<Canvas>().sortingOrder = 2;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        canvasGO.AddComponent<GraphicRaycaster>();

        float bannerHeight = 100f;
        float runButtonHeight = 36f;

        var runGO = CreateButton(canvasRect, "Run", 16);
        runGO.name = "RunButton";
        var runRect = runGO.GetComponent<RectTransform>();
        runRect.anchorMin = new Vector2(0, 0);
        runRect.anchorMax = new Vector2(0, 0);
        runRect.pivot = new Vector2(0, 0);
        runRect.anchoredPosition = new Vector2(12, bannerHeight + 8);
        runRect.sizeDelta = new Vector2(80, runButtonHeight);

        var bannerGO = new GameObject("TimelineBanner");
        Undo.RegisterCreatedObjectUndo(bannerGO, "Create PM Banner");
        bannerGO.transform.SetParent(canvasRect, false);
        var bannerRect = bannerGO.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0, 0);
        bannerRect.anchorMax = new Vector2(1, 0);
        bannerRect.pivot = new Vector2(0.5f, 0);
        bannerRect.anchoredPosition = Vector2.zero;
        bannerRect.sizeDelta = new Vector2(0, bannerHeight);
        bannerGO.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        var timelineViewGO = new GameObject("TimelineView");
        timelineViewGO.transform.SetParent(bannerRect, false);
        var timelineViewRect = timelineViewGO.AddComponent<RectTransform>();
        timelineViewRect.anchorMin = Vector2.zero;
        timelineViewRect.anchorMax = Vector2.one;
        timelineViewRect.offsetMin = Vector2.zero;
        timelineViewRect.offsetMax = Vector2.zero;

        var configViewGO = new GameObject("ConfigView");
        configViewGO.transform.SetParent(bannerRect, false);
        configViewGO.SetActive(false);
        var configViewRect = configViewGO.AddComponent<RectTransform>();
        configViewRect.anchorMin = Vector2.zero;
        configViewRect.anchorMax = Vector2.one;
        configViewRect.offsetMin = Vector2.zero;
        configViewRect.offsetMax = Vector2.zero;

        var configContent = new GameObject("ConfigContent");
        configContent.transform.SetParent(configViewRect, false);
        var configContentRect = configContent.AddComponent<RectTransform>();
        configContentRect.anchorMin = Vector2.zero;
        configContentRect.anchorMax = Vector2.one;
        configContentRect.offsetMin = new Vector2(12, 8);
        configContentRect.offsetMax = new Vector2(-12, -8);

        var configVertical = configContent.AddComponent<VerticalLayoutGroup>();
        configVertical.spacing = 8;
        configVertical.padding = new RectOffset(8, 8, 8, 8);
        configVertical.childAlignment = TextAnchor.MiddleLeft;
        configVertical.childControlWidth = true;
        configVertical.childControlHeight = true;
        configVertical.childForceExpandWidth = true;
        configVertical.childForceExpandHeight = false;

        CreateConfigRow1(configContent.transform);
        CreateConfigRow2(configContent.transform);

        var timeLabelsGO = new GameObject("TimeLabels");
        timeLabelsGO.transform.SetParent(timelineViewRect, false);
        var timeLabelsRect = timeLabelsGO.AddComponent<RectTransform>();
        timeLabelsRect.anchorMin = new Vector2(0, 1);
        timeLabelsRect.anchorMax = new Vector2(1, 1);
        timeLabelsRect.pivot = new Vector2(0.5f, 1);
        timeLabelsRect.anchoredPosition = new Vector2(0, -8);
        timeLabelsRect.sizeDelta = new Vector2(0, 20);

        for (int s = 0; s <= TimelineDurationSeconds; s++)
        {
            float x = (float)s / TimelineDurationSeconds;
            var label = CreateText(timeLabelsRect.transform, $"{s}s", 12);
            label.name = $"Label{s}s";
            var lr = label.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(x, 0);
            lr.anchorMax = new Vector2(x, 1);
            lr.pivot = new Vector2(0.5f, 0.5f);
            lr.anchoredPosition = Vector2.zero;
            lr.sizeDelta = new Vector2(28, 0);
        }
        var linesGO = new GameObject("VerticalLines");
        linesGO.transform.SetParent(timelineViewRect, false);
        var linesRect = linesGO.AddComponent<RectTransform>();
        linesRect.anchorMin = new Vector2(0, 0);
        linesRect.anchorMax = new Vector2(1, 1);
        linesRect.offsetMin = new Vector2(4, 4);
        linesRect.offsetMax = new Vector2(-4, -32);
        for (int s = 0; s <= TimelineDurationSeconds; s++)
        {
            float x = (float)s / TimelineDurationSeconds;
            var line = new GameObject($"Line{s}");
            line.transform.SetParent(linesRect, false);
            var lineImg = line.AddComponent<Image>();
            lineImg.color = new Color(0.35f, 0.35f, 0.4f, 0.7f);
            var lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(x, 0);
            lineRect.anchorMax = new Vector2(x, 1);
            lineRect.pivot = new Vector2(0.5f, 0);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(2, 0);
        }

        var tracksGO = new GameObject("TracksContainer");
        tracksGO.transform.SetParent(timelineViewRect, false);
        var tracksRect = tracksGO.AddComponent<RectTransform>();
        tracksRect.anchorMin = new Vector2(0, 0);
        tracksRect.anchorMax = new Vector2(1, 1);
        tracksRect.offsetMin = new Vector2(4, 4);
        tracksRect.offsetMax = new Vector2(-4, -32);

        for (int t = 0; t < TracksCount; t++)
        {
            var trackGO = new GameObject($"Track{t}");
            trackGO.transform.SetParent(tracksRect, false);
            var tr = trackGO.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, (float)(TracksCount - 1 - t) / TracksCount);
            tr.anchorMax = new Vector2(1, (float)(TracksCount - t) / TracksCount);
            tr.offsetMin = new Vector2(2, 2);
            tr.offsetMax = new Vector2(-2, -2);

            var trackBg = trackGO.AddComponent<Image>();
            trackBg.color = new Color(0.18f, 0.18f, 0.2f, 0.9f);

            var blocksGO = new GameObject("Blocks");
            blocksGO.transform.SetParent(tr, false);
            var blocksRect = blocksGO.AddComponent<RectTransform>();
            blocksRect.anchorMin = Vector2.zero;
            blocksRect.anchorMax = Vector2.one;
            blocksRect.offsetMin = Vector2.zero;
            blocksRect.offsetMax = Vector2.zero; // Full width so block coords (0-1) align with timeline seconds
            blocksGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            blocksGO.AddComponent<TrackClickHandler>();

            var addBtn = CreateButton(tr, "+");
            addBtn.name = "AddButton";
            var addRect = addBtn.GetComponent<RectTransform>();
            addRect.anchorMin = new Vector2(1, 0);
            addRect.anchorMax = new Vector2(1, 1);
            addRect.pivot = new Vector2(1, 0.5f);
            addRect.anchoredPosition = new Vector2(-4, 0);
            addRect.sizeDelta = new Vector2(28, 0);
        }

        var controller = pmView.GetComponentInChildren<ProgrammedMovementsController>(true);
        if (controller != null)
        {
            controller.timelineRoot = bannerRect;
            controller.runButton = runGO.GetComponent<Button>();
            EditorUtility.SetDirty(controller);
        }

        Debug.Log("[ProgrammedMovementsSetup] ProgrammedMovements UI hierarchy created. Save the scene to persist.");
    }

    static GameObject CreateText(Transform parent, string text, int fontSize)
    {
        var go = new GameObject();
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject CreateButton(Transform parent, string label, int fontSize = 14)
    {
        var go = new GameObject();
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.8f, 1f);
        go.AddComponent<Button>();
        var textGO = CreateText(go.transform, label, fontSize);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return go;
    }

    static void CreateConfigLabel(Transform parent, string text)
    {
        var go = CreateText(parent, text, 12);
        if (go == null) return;
        var rect = go.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector2(60, 24);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 60;
        le.preferredHeight = 24;
    }

    static GameObject CreateConfigRow1(Transform parent)
    {
        var row = new GameObject("ConfigRow1");
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10;
        h.padding = new RectOffset(0, 0, 0, 0);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 32;

        CreateConfigLabel(row.transform, "Module:");
        var moduleLabelGO = new GameObject("ModuleLabel");
        moduleLabelGO.transform.SetParent(row.transform, false);
        var moduleLabelTmp = moduleLabelGO.AddComponent<TextMeshProUGUI>();
        moduleLabelTmp.text = "None";
        moduleLabelTmp.fontSize = 12;
        moduleLabelTmp.color = Color.white;
        var mlRect = moduleLabelGO.GetComponent<RectTransform>() ?? moduleLabelGO.AddComponent<RectTransform>();
        mlRect.sizeDelta = new Vector2(120, 24);
        var mlLe = moduleLabelGO.AddComponent<LayoutElement>();
        mlLe.preferredWidth = 120;
        mlLe.preferredHeight = 24;

        var selectBtn = CreateButton(row.transform, "Select");
        selectBtn.name = "SelectButton";
        SetLayout(selectBtn, 70, 28);
        var setBtn = CreateButton(row.transform, "Set");
        setBtn.name = "SetButton";
        setBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 1f);
        SetLayout(setBtn, 60, 28);
        return row;
    }

    static GameObject CreateConfigRow2(Transform parent)
    {
        var row = new GameObject("ConfigRow2");
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10;
        h.padding = new RectOffset(0, 0, 0, 0);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 32;

        var angleSection = new GameObject("AngleSection");
        angleSection.transform.SetParent(row.transform, false);
        angleSection.AddComponent<RectTransform>();
        var angleSectionH = angleSection.AddComponent<HorizontalLayoutGroup>();
        angleSectionH.spacing = 6;
        angleSectionH.childForceExpandWidth = false;
        var angleSectionLe = angleSection.AddComponent<LayoutElement>();
        angleSectionLe.preferredWidth = 90;
        angleSectionLe.preferredHeight = 28;
        CreateConfigLabel(angleSection.transform, "Angle:");
        var angleInputGO = CreateSimpleInputField(angleSection.transform, "90");
        angleInputGO.name = "AngleInput";
        SetLayout(angleInputGO, 55, 26);

        var directionSection = new GameObject("DirectionSection");
        directionSection.transform.SetParent(row.transform, false);
        directionSection.SetActive(false);
        directionSection.AddComponent<RectTransform>();
        var dirSectionH = directionSection.AddComponent<HorizontalLayoutGroup>();
        dirSectionH.spacing = 6;
        dirSectionH.childForceExpandWidth = false;
        var dirSectionLe = directionSection.AddComponent<LayoutElement>();
        dirSectionLe.preferredWidth = 220;
        dirSectionLe.preferredHeight = 28;
        CreateConfigLabel(directionSection.transform, "Direction:");
        var dirButtons = new GameObject("DirectionButtons");
        dirButtons.transform.SetParent(directionSection.transform, false);
        var dirH = dirButtons.AddComponent<HorizontalLayoutGroup>();
        dirH.spacing = 4;
        dirH.childForceExpandWidth = false;
        var dirLe = dirButtons.AddComponent<LayoutElement>();
        dirLe.preferredWidth = 100;
        dirLe.preferredHeight = 26;
        var fwdBtn = CreateButton(dirButtons.transform, "Fwd", 11);
        fwdBtn.name = "ForwardButton";
        SetLayout(fwdBtn, 45, 24);
        var bwdBtn = CreateButton(dirButtons.transform, "Bwd", 11);
        bwdBtn.name = "BackwardButton";
        SetLayout(bwdBtn, 45, 24);
        CreateConfigLabel(directionSection.transform, "Degrees:");
        var dcDegreesInput = CreateSimpleInputField(directionSection.transform, "90");
        dcDegreesInput.name = "DCDegreesInput";
        SetLayout(dcDegreesInput, 45, 26);

        var movementLabel = CreateText(row.transform, "Movement set at", 12);
        movementLabel.name = "MovementSetAtLabel";
        movementLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        var mslLe = movementLabel.AddComponent<LayoutElement>();
        mslLe.preferredWidth = 95;
        mslLe.preferredHeight = 24;
        var secondInputGO = CreateSimpleInputField(row.transform, "0");
        secondInputGO.name = "SecondInput";
        SetLayout(secondInputGO, 45, 26);
        var secondSuffix = CreateText(row.transform, " second", 12);
        secondSuffix.name = "SecondSuffix";
        var ssLe = secondSuffix.AddComponent<LayoutElement>();
        ssLe.preferredWidth = 50;
        ssLe.preferredHeight = 24;

        var saveBtn = CreateButton(row.transform, "Save");
        saveBtn.name = "SaveButton";
        SetLayout(saveBtn, 65, 28);

        var backBtn = CreateButton(row.transform, "Back");
        backBtn.name = "BackButton";
        SetLayout(backBtn, 65, 28);

        var deleteBtn = CreateButton(row.transform, "Delete");
        deleteBtn.name = "DeleteButton";
        deleteBtn.GetComponent<Image>().color = new Color(0.7f, 0.3f, 0.3f, 1f);
        SetLayout(deleteBtn, 65, 28);
        return row;
    }

    static void SetLayout(GameObject go, float w, float h)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector2(w, h);
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
    }

    static GameObject CreateSimpleInputField(Transform parent, string initial)
    {
        var go = new GameObject("Input");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var input = go.AddComponent<TMP_InputField>();
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = initial;
        text.fontSize = 11;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 2);
        textRect.offsetMax = new Vector2(-4, -2);
        input.textComponent = text;
        input.text = initial;
        return go;
    }
}
