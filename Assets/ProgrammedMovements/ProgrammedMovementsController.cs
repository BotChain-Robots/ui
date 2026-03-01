using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controller for ProgrammedMovements view: timeline banner at bottom.
/// 3 tracks, 0-10 seconds, movement blocks, Run button.
/// </summary>
public class ProgrammedMovementsController : MonoBehaviour
{
    public const int TracksCount = 3;
    public const int TimelineDurationSeconds = 10;
    public static readonly Color HighlightBlue = new Color(0.2f, 0.5f, 1f, 1f);

    [Header("References")]
    public Camera mainCamera;
    public RectTransform timelineRoot;
    public Button runButton;

    private RectTransform _bannerRoot;
    private Transform _timelineView;
    private Transform _configView;
    private readonly List<MovementBlock> _blocks = new List<MovementBlock>();
    private readonly Dictionary<int, List<MovementBlock>> _blocksBySecond = new Dictionary<int, List<MovementBlock>>();
    private bool _isRunning;
    private float _runStartTime;
    private int _lastExecutedSecond = -1;
    private float _lastRewireTime = -1f;
    private ModuleBase _highlightedModule;
    private readonly Dictionary<Renderer, Material[]> _originalMaterialsByRenderer = new Dictionary<Renderer, Material[]>();
    private MovementBlock _pickModeBlock;
    private MovementBlock _editingBlock;
    private ModuleBase _pendingModule;

    void OnEnable()
    {
        ResolveReferences();
        if (_bannerRoot == null)
        {
            Debug.LogError("[ProgrammedMovements] timelineRoot not set. Run Tools > ProgrammedMovements > Setup UI Hierarchy in the Editor.");
            return;
        }
        EnsureTimelineViewShown();
        WireUpButtons();
        RefreshRunButton();
        // Force layout rebuild so block positions align with timeline after view switch
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bannerRoot);
    }

    void EnsureTimelineViewShown()
    {
        if (_timelineView != null) _timelineView.gameObject.SetActive(true);
        if (_configView != null) _configView.gameObject.SetActive(false);
    }

    void ResolveReferences()
    {
        if (timelineRoot != null)
        {
            _bannerRoot = timelineRoot;
        }
        else
        {
            var pmView = transform.parent;
            if (pmView == null) return;
            var canvas = pmView.Find("ProgrammedMovementsCanvas");
            if (canvas == null) return;
            var banner = canvas.Find("TimelineBanner");
            if (banner != null)
            {
                _bannerRoot = banner.GetComponent<RectTransform>();
                if (timelineRoot == null) timelineRoot = _bannerRoot;
            }
        }
        if (_bannerRoot != null)
        {
            _timelineView = _bannerRoot.Find("TimelineView");
            _configView = _bannerRoot.Find("ConfigView");
            if (_timelineView == null)
            {
                _timelineView = _bannerRoot;
            }
        }
        if (runButton == null && _bannerRoot != null)
        {
            var canvas = _bannerRoot.parent;
            if (canvas != null)
            {
                var runGO = canvas.Find("RunButton");
                if (runGO != null) runButton = runGO.GetComponent<Button>();
            }
        }
    }

    void WireUpButtons()
    {
        WireUpRunButton();
        var tracks = _timelineView != null ? _timelineView.Find("TracksContainer") : null;
        if (tracks != null)
        {
            for (int t = 0; t < TracksCount; t++)
            {
                var track = tracks.GetChild(t);
                var addBtn = track.Find("AddButton")?.GetComponent<Button>();
                if (addBtn != null)
                {
                    int trackIndex = t;
                    addBtn.onClick.RemoveAllListeners();
                    addBtn.onClick.AddListener(() => AddBlock(trackIndex, 0));
                }
                var blocks = track.Find("Blocks");
                var clickHandler = blocks?.GetComponent<TrackClickHandler>();
                if (clickHandler != null)
                {
                    int trackIndex = t;
                    clickHandler.OnTrackClicked = (tr, sec) => AddBlock(tr, sec);
                }
            }
        }
        var config = _configView?.Find("ConfigContent");
        if (config != null)
        {
            var row1 = config.Find("ConfigRow1");
            var row2 = config.Find("ConfigRow2");
            var selectBtn = (row1 != null ? row1.Find("SelectButton") : config.Find("SelectButton"))?.GetComponent<Button>();
            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.AddListener(OnConfigSelectClicked);
            }
            var setBtn = (row1 != null ? row1.Find("SetButton") : null)?.GetComponent<Button>();
            if (setBtn != null)
            {
                setBtn.onClick.RemoveAllListeners();
                setBtn.onClick.AddListener(OnConfigSetClicked);
            }
            var saveBtn = (row2 != null ? row2.Find("SaveButton") : config.Find("SaveButton"))?.GetComponent<Button>();
            if (saveBtn != null)
            {
                saveBtn.onClick.RemoveAllListeners();
                saveBtn.onClick.AddListener(OnConfigSaveClicked);
            }
            var backBtn = (row2 != null ? row2.Find("BackButton") : null)?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(OnConfigBackClicked);
            }
            var deleteBtn = (row2 != null ? row2.Find("DeleteButton") : config.Find("DeleteButton"))?.GetComponent<Button>();
            if (deleteBtn != null)
            {
                deleteBtn.onClick.RemoveAllListeners();
                deleteBtn.onClick.AddListener(OnConfigDeleteClicked);
            }
            var dirButtons = (row2 != null ? row2.Find("DirectionSection/DirectionButtons") : config.Find("DirectionSection/DirectionButtons"));
            var fwdBtn = dirButtons?.Find("ForwardButton")?.GetComponent<Button>();
            var bwdBtn = dirButtons?.Find("BackwardButton")?.GetComponent<Button>();
            if (fwdBtn != null)
            {
                fwdBtn.onClick.RemoveAllListeners();
                fwdBtn.onClick.AddListener(OnDirectionForwardClicked);
            }
            if (bwdBtn != null)
            {
                bwdBtn.onClick.RemoveAllListeners();
                bwdBtn.onClick.AddListener(OnDirectionBackwardClicked);
            }
        }
    }

    void OnDirectionForwardClicked()
    {
        if (_editingBlock == null) return;
        _editingBlock.dcDirection = 1;
        var config = _configView?.Find("ConfigContent");
        var dirButtons = config?.Find("ConfigRow2/DirectionSection/DirectionButtons") ?? config?.Find("DirectionSection/DirectionButtons");
        if (dirButtons != null) UpdateDirectionButtonsVisual(dirButtons, 1);
    }

    void OnDirectionBackwardClicked()
    {
        if (_editingBlock == null) return;
        _editingBlock.dcDirection = -1;
        var config = _configView?.Find("ConfigContent");
        var dirButtons = config?.Find("ConfigRow2/DirectionSection/DirectionButtons") ?? config?.Find("DirectionSection/DirectionButtons");
        if (dirButtons != null) UpdateDirectionButtonsVisual(dirButtons, -1);
    }

    void UpdateDirectionButtonsVisual(Transform dirButtons, int dcDirection)
    {
        var fwdImg = dirButtons.Find("ForwardButton")?.GetComponent<Image>();
        var bwdImg = dirButtons.Find("BackwardButton")?.GetComponent<Image>();
        var selected = new Color(0.2f, 0.6f, 0.4f, 1f);
        var normal = new Color(0.3f, 0.5f, 0.8f, 1f);
        if (fwdImg != null) fwdImg.color = dcDirection >= 0 ? selected : normal;
        if (bwdImg != null) bwdImg.color = dcDirection < 0 ? selected : normal;
    }

    void OnDisable()
    {
        _isRunning = false;
        ClearModuleHighlight();
    }

    void Update()
    {
        if (_isRunning)
        {
            float elapsed = Time.time - _runStartTime;
            if (elapsed >= TimelineDurationSeconds)
            {
                _isRunning = false;
                _lastExecutedSecond = -1;
                RefreshRunButton();
                Invoke(nameof(WireUpRunButton), 0.05f); // Delayed re-wire so Run works again
                return;
            }
            int currentSecond = Mathf.FloorToInt(elapsed);
            if (currentSecond != _lastExecutedSecond && _blocksBySecond.TryGetValue(currentSecond, out var list))
            {
                _lastExecutedSecond = currentSecond;
                foreach (var b in list)
                {
                    if (b.moduleServo != null)
                        b.moduleServo.SetAngleAndSendControlLibrary(b.targetAngle, 0f);
                    else if (b.moduleDC != null)
                        b.moduleDC.Rotate(b.targetAngle, b.dcDirection);
                }
            }
        }
        else
        {
            // Periodically re-wire Run button when idle (every 2s) so it stays clickable
            if (Time.time - _lastRewireTime > 2f)
            {
                _lastRewireTime = Time.time;
                WireUpRunButton();
            }
        }

        if (_pickModeBlock != null && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam != null && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit))
            {
                var servo = hit.collider.GetComponentInParent<ServoMotorModule>();
                var dc = hit.collider.GetComponentInParent<DCMotorModule>();
                // Prefer DC when both exist (e.g. DC attached to servo arm) - user clicked the DC
                var module = (ModuleBase)dc ?? servo;
                if (module != null && IsInGeneratedTopology(module.transform))
                {
                    _pendingModule = module;
                    SetModuleHighlight(module);
                    UpdateConfigModuleLabel(GetModuleTypeName(module));
                }
                else
                {
                    _pendingModule = null;
                    ClearModuleHighlight();
                    UpdateConfigModuleLabel("None");
                }
            }
        }
    }

    GameObject CreateText(Transform parent, string text, int fontSize)
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

    GameObject CreateButton(Transform parent, string label, Action onClick)
    {
        var go = new GameObject();
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.8f, 1f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
        var textGO = CreateText(go.transform, label, 14);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return go;
    }

    void AddBlock(int trackIndex, float second)
    {
        if (_bannerRoot == null) return;

        int sec = Mathf.Clamp(Mathf.FloorToInt(second), 0, TimelineDurationSeconds - 1);
        var block = new MovementBlock { trackIndex = trackIndex, second = sec, targetAngle = 90f };
        _blocks.Add(block);
        IndexBlock(block);

        var tracksRect = _timelineView?.Find("TracksContainer");
        if (tracksRect == null) return;
        var track = tracksRect.GetChild(trackIndex);
        var blocksHolder = track.Find("Blocks");
        if (blocksHolder == null) return;

        CreateMovementBlockUI(blocksHolder, block);
        OpenConfigPanel(block);
    }

    GameObject CreateMovementBlockUI(Transform parent, MovementBlock block)
    {
        var go = new GameObject($"Block_{block.second}s_T{block.trackIndex}");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        float x = (float)block.second / TimelineDurationSeconds;
        float w = 1f / TimelineDurationSeconds; // 1 second width to align with timeline
        rect.anchorMin = new Vector2(x, 0);
        rect.anchorMax = new Vector2(x + w, 1);
        rect.offsetMin = new Vector2(2, 2);
        rect.offsetMax = new Vector2(-2, -2);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.35f, 0.5f, 0.7f, 0.95f);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => OpenConfigPanel(block));

        block.uiRoot = go;

        return go;
    }

    void OpenConfigPanel(MovementBlock block)
    {
        _editingBlock = block;
        _pendingModule = (ModuleBase)block.moduleServo ?? block.moduleDC;
        _pickModeBlock = null;
        if (_configView != null)
        {
            if (_timelineView != null) _timelineView.gameObject.SetActive(false);
            _configView.gameObject.SetActive(true);
            PopulateConfigPanel(block);
        }
        var mod = (ModuleBase)block.moduleServo ?? block.moduleDC;
        if (mod != null) SetModuleHighlight(mod);
        else ClearModuleHighlight();
    }

    void CloseConfigPanel()
    {
        _editingBlock = null;
        _pickModeBlock = null;
        _pendingModule = null;
        ClearModuleHighlight();
        if (_timelineView != null) _timelineView.gameObject.SetActive(true);
        if (_configView != null) _configView.gameObject.SetActive(false);
    }

    void PopulateConfigPanel(MovementBlock block)
    {
        var config = _configView?.Find("ConfigContent");
        if (config == null) return;

        var mod = (ModuleBase)block.moduleServo ?? block.moduleDC;
        var moduleLabel = config.Find("ConfigRow1/ModuleLabel") ?? config.Find("ModuleLabel");
        var ml = moduleLabel?.GetComponent<TextMeshProUGUI>();
        if (ml != null) ml.text = mod != null ? GetModuleTypeName(mod) : "None";

        var row2 = config.Find("ConfigRow2");
        var angleSection = row2?.Find("AngleSection") ?? config.Find("AngleSection");
        var angleInput = angleSection?.Find("AngleInput") ?? row2?.Find("AngleInput") ?? config.Find("AngleInput");
        var directionSection = row2?.Find("DirectionSection") ?? config.Find("DirectionSection");
        var dirButtons = directionSection?.Find("DirectionButtons");
        var dcDegreesInput = directionSection?.Find("DCDegreesInput");

        bool isDC = block.moduleDC != null;
        if (angleSection != null) angleSection.gameObject.SetActive(!isDC);
        if (directionSection != null) directionSection.gameObject.SetActive(isDC);

        if (!isDC)
        {
            var ai = angleInput?.GetComponent<TMP_InputField>();
            if (ai != null) ai.text = block.targetAngle.ToString("F0");
        }
        else
        {
            if (dirButtons != null) UpdateDirectionButtonsVisual(dirButtons, block.dcDirection);
            var dcd = dcDegreesInput?.GetComponent<TMP_InputField>();
            if (dcd != null) dcd.text = block.targetAngle.ToString("F0");
        }

        var secondInput = config.Find("ConfigRow2/SecondInput") ?? config.Find("SecondInput");
        var si = secondInput?.GetComponent<TMP_InputField>();
        if (si != null) si.text = block.second.ToString();
    }

    void UpdateConfigModuleLabel(string text)
    {
        var config = _configView?.Find("ConfigContent");
        var moduleLabel = config?.Find("ConfigRow1/ModuleLabel") ?? config?.Find("ModuleLabel");
        var ml = moduleLabel?.GetComponent<TextMeshProUGUI>();
        if (ml != null) ml.text = text;
    }

    static string GetModuleTypeName(ModuleBase m)
    {
        if (m == null) return "None";
        var t = m.GetType();
        if (t.Name == "ServoBendModule") return "Servo Bend";
        if (t.Name == "ServoStraightModule") return "Servo Straight";
        if (t.Name == "DCMotorModule") return "DC Motor";
        return t.Name.Replace("Module", "");
    }

    void OnConfigSelectClicked()
    {
        if (_editingBlock == null) return;
        _pickModeBlock = _editingBlock;
        _pendingModule = (ModuleBase)_editingBlock.moduleServo ?? _editingBlock.moduleDC;
        if (_pendingModule != null)
        {
            SetModuleHighlight(_pendingModule);
            UpdateConfigModuleLabel(GetModuleTypeName(_pendingModule));
        }
        else
        {
            ClearModuleHighlight();
            UpdateConfigModuleLabel("Click a module, then Set");
        }
    }

    void OnConfigSetClicked()
    {
        if (_editingBlock == null) return;
        var block = _editingBlock;
        block.moduleServo = null;
        block.moduleDC = null;
        if (_pendingModule is ServoMotorModule sm)
            block.moduleServo = sm;
        else if (_pendingModule is DCMotorModule dm)
            block.moduleDC = dm;
        _pickModeBlock = null;
        if (_pendingModule != null)
        {
            SetModuleHighlight(_pendingModule);
            UpdateConfigModuleLabel(GetModuleTypeName(_pendingModule));
        }
        else
        {
            ClearModuleHighlight();
            UpdateConfigModuleLabel("None");
        }
        PopulateConfigPanel(block);
    }

    void OnConfigBackClicked()
    {
        CloseConfigPanel();
    }

    void OnConfigSaveClicked()
    {
        if (_editingBlock == null) return;
        ApplyConfigToBlock();
        CloseConfigPanel();
    }

    void OnConfigDeleteClicked()
    {
        if (_editingBlock == null) return;
        var block = _editingBlock;
        UnindexBlock(block);
        _blocks.Remove(block);
        if (block.uiRoot != null) Destroy(block.uiRoot);
        CloseConfigPanel();
    }

    void ApplyConfigToBlock()
    {
        if (_editingBlock == null) return;
        var config = _configView?.Find("ConfigContent");
        if (config == null) return;

        var block = _editingBlock;
        var directionSection = config.Find("ConfigRow2/DirectionSection") ?? config.Find("DirectionSection");
        if (block.moduleDC == null)
        {
            var angleInput = config.Find("ConfigRow2/AngleSection/AngleInput") ?? config.Find("ConfigRow2/AngleInput") ?? config.Find("AngleInput");
            if (angleInput != null && float.TryParse(angleInput.GetComponent<TMP_InputField>()?.text, out float angle))
                block.targetAngle = Mathf.Clamp(angle, 0f, 180f);
        }
        else
        {
            var dcDegreesInput = directionSection?.Find("DCDegreesInput");
            if (dcDegreesInput != null && float.TryParse(dcDegreesInput.GetComponent<TMP_InputField>()?.text, out float degrees))
                block.targetAngle = Mathf.Clamp(degrees, 1f, 3600f);
        }

        var secondInput = config.Find("ConfigRow2/SecondInput") ?? config.Find("SecondInput");
        if (secondInput != null && int.TryParse(secondInput.GetComponent<TMP_InputField>()?.text, out int sec))
        {
            UnindexBlock(_editingBlock);
            _editingBlock.second = Mathf.Clamp(sec, 0, TimelineDurationSeconds - 1);
            IndexBlock(_editingBlock);
            UpdateBlockPosition(_editingBlock);
        }
    }

    GameObject CreateSimpleInput(Transform parent, string initial, Action<string> onEndEdit)
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
        input.onEndEdit.AddListener((s) => onEndEdit?.Invoke(s));
        return go;
    }

    bool IsInGeneratedTopology(Transform t)
    {
        while (t != null)
        {
            if (t.name == "GeneratedTopology") return true;
            t = t.parent;
        }
        return false;
    }

    List<ModuleBase> GetModulesInTopology()
    {
        var list = new List<ModuleBase>();
        var root = GameObject.Find("GeneratedTopology")?.transform;
        if (root == null) return list;

        foreach (var s in root.GetComponentsInChildren<ServoMotorModule>(true))
            list.Add(s);
        foreach (var d in root.GetComponentsInChildren<DCMotorModule>(true))
            list.Add(d);
        return list;
    }

    void IndexBlock(MovementBlock b)
    {
        if (!_blocksBySecond.ContainsKey(b.second))
            _blocksBySecond[b.second] = new List<MovementBlock>();
        _blocksBySecond[b.second].Add(b);
    }

    void UnindexBlock(MovementBlock b)
    {
        if (_blocksBySecond.TryGetValue(b.second, out var list))
        {
            list.Remove(b);
            if (list.Count == 0) _blocksBySecond.Remove(b.second);
        }
    }

    void UpdateBlockPosition(MovementBlock b)
    {
        if (b.uiRoot == null) return;
        var rect = b.uiRoot.GetComponent<RectTransform>();
        if (rect == null) return;
        float x = (float)b.second / TimelineDurationSeconds;
        float w = 1f / TimelineDurationSeconds;
        rect.anchorMin = new Vector2(x, 0);
        rect.anchorMax = new Vector2(x + w, 1);
        b.uiRoot.name = $"Block_{b.second}s_T{b.trackIndex}";
    }

    void WireUpRunButton()
    {
        if (runButton != null)
        {
            runButton.onClick.RemoveAllListeners();
            runButton.onClick.AddListener(OnRunClicked);
        }
    }

    void OnRunClicked()
    {
        CancelInvoke(nameof(WireUpRunButton)); // Cancel any pending delayed re-wire
        _isRunning = true;
        _runStartTime = Time.time;
        _lastExecutedSecond = -1;
        RefreshRunButton();
    }

    void RefreshRunButton()
    {
        if (runButton != null)
        {
            runButton.interactable = true;
            var tmp = runButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = _isRunning ? "Running" : "Run";
        }
    }

    void SetModuleHighlight(ModuleBase module)
    {
        ClearModuleHighlight();
        _highlightedModule = module;
        if (module == null) return;

        foreach (var r in GetRenderersOwnedByModule(module))
        {
            if (!_originalMaterialsByRenderer.ContainsKey(r))
                _originalMaterialsByRenderer[r] = r.sharedMaterials;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", HighlightBlue);
                if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", HighlightBlue);
            }
        }
    }

    void ClearModuleHighlight()
    {
        if (_highlightedModule == null) return;
        foreach (var kv in _originalMaterialsByRenderer)
        {
            if (kv.Key != null && kv.Value != null)
                kv.Key.sharedMaterials = kv.Value;
        }
        _originalMaterialsByRenderer.Clear();
        _highlightedModule = null;
    }

    static IEnumerable<Renderer> GetRenderersOwnedByModule(ModuleBase rootModule)
    {
        if (rootModule == null) yield break;
        var root = rootModule.transform;
        var stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t != root)
            {
                var other = t.GetComponent<ModuleBase>();
                if (other != null && other != rootModule) continue;
            }
            foreach (var r in t.GetComponents<Renderer>())
                if (r != null) yield return r;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }

    [Serializable]
    public class MovementBlock
    {
        public int trackIndex;
        public int second;
        public float targetAngle;
        public ServoMotorModule moduleServo;
        public DCMotorModule moduleDC;
        public int dcDirection = 1; // 1 = forward, -1 = backward
        public GameObject uiRoot;
    }
}
