using UnityEngine;
using UnityEngine.UI;

public class ControlPanel : MonoBehaviour
{
    [Header("Core UI (assign in Inspector)")]
    public InputField inputField;
    public Dropdown directionDropdown;
    public Button button; // existing Send/Rotate button in hierarchy

    private enum Mode { None, DC, Display, Speaker }
    private Mode _mode = Mode.None;

    private DCMotorModule _dc;
    private DisplayModule _display;
    private SpeakerModule _speaker;

    private Text _caption;     // child "DegreesCaption"
    private Text _buttonText;  // child "RotateButton/Text (TMP)"
    private GameObject _directionCaptionGO;
    private GameObject _directionDropdownGO;

    private Button _uploadButton;
    private Text _uploadButtonText;
    private Button _playButton;
    private Text _playButtonText;

    private void OnEnable()
    {
        if (_mode == Mode.Speaker)
        {
            SetUploadVisible(true);
            SetPlayVisible(true);
        }
        else
        {
            SetUploadVisible(false);
            SetPlayVisible(false);
        }
    }

    private void Awake()
    {
        CacheUIFromHierarchy();

        // Create these once; hide unless Speaker mode
        CreateUploadButtonIfNeeded();
        CreatePlayButtonIfNeeded();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnMainButtonClicked);
        }
    }

    public void Initialize(ModuleBase module)
    {
        _dc = module as DCMotorModule;
        _display = module as DisplayModule;
        _speaker = module as SpeakerModule;

        if (_dc != null) { SwitchMode(Mode.DC); gameObject.SetActive(true); return; }
        if (_display != null) { SwitchMode(Mode.Display); gameObject.SetActive(true); return; }
        if (_speaker != null) { SwitchMode(Mode.Speaker); gameObject.SetActive(true); return; }

        HidePanel();
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
        _mode = Mode.None;
        _dc = null;
        _display = null;
        _speaker = null;
        SetUploadVisible(false);
        SetPlayVisible(false);
    }

    private void CacheUIFromHierarchy()
    {
        var captionT = transform.Find("DegreesCaption");
        if (captionT != null) _caption = captionT.GetComponent<Text>();

        var buttonTextT = transform.Find("RotateButton/Text (TMP)");
        if (buttonTextT != null) _buttonText = buttonTextT.GetComponent<Text>();

        var dirCaptionT = transform.Find("DirectionCaption");
        if (dirCaptionT != null) _directionCaptionGO = dirCaptionT.gameObject;

        var dirDropdownT = transform.Find("DirectionDropdown");
        if (dirDropdownT != null) _directionDropdownGO = dirDropdownT.gameObject;
    }

    private void SwitchMode(Mode newMode)
    {
        _mode = newMode;

        if (_mode == Mode.DC)
        {
            SetCaption("Degrees");
            SetCaptionVisible(true);
            SetMainButtonText("Rotate");
            SetDirectionVisible(true);
            SetInputVisible(true);
            SetUploadVisible(false);
            SetPlayVisible(false);

            if (inputField != null)
            {
                inputField.readOnly = false;
                inputField.SetTextWithoutNotify("");
            }

            if (directionDropdown != null)
                directionDropdown.SetValueWithoutNotify(0);
        }
        else if (_mode == Mode.Display)
        {
            SetCaption("Text");
            SetCaptionVisible(true);
            SetMainButtonText("Send");
            SetDirectionVisible(false);
            SetInputVisible(true);
            SetUploadVisible(false);
            SetPlayVisible(false);

            if (inputField != null)
            {
                inputField.readOnly = false;
                inputField.SetTextWithoutNotify("");
            }
        }
        else if (_mode == Mode.Speaker)
        {
            // Speaker UI: no input field (removes the white box behind buttons)
            SetMainButtonText("Send Audio");
            SetCaptionVisible(false);
            SetDirectionVisible(false);
            SetInputVisible(false);

            // Show speaker buttons
            SetUploadVisible(true);
            SetPlayVisible(true);
        }
    }

    private void SetCaption(string text)
    {
        if (_caption != null) _caption.text = text;
    }

    private void SetMainButtonText(string text)
    {
        if (_buttonText != null) _buttonText.text = text;
    }

    private void SetDirectionVisible(bool visible)
    {
        if (_directionCaptionGO != null) _directionCaptionGO.SetActive(visible);
        if (_directionDropdownGO != null) _directionDropdownGO.SetActive(visible);
    }

    private void SetInputVisible(bool visible)
    {
        if (inputField != null && inputField.gameObject.activeSelf != visible)
            inputField.gameObject.SetActive(visible);
    }

    private void OnMainButtonClicked()
    {
        if (_mode == Mode.DC)
        {
            if (_dc == null) return;

            if (!float.TryParse(inputField.text, out float degrees))
            {
                Debug.LogWarning("ControlPanel: Invalid degree input.");
                return;
            }

            int direction = (directionDropdown != null && directionDropdown.value == 0) ? 1 : -1;
            _dc.Rotate(degrees, direction);
        }
        else if (_mode == Mode.Display)
        {
            if (_display == null) return;
            _display.SetDisplayText(inputField != null ? inputField.text : "");
        }
        else if (_mode == Mode.Speaker)
        {
            if (_speaker == null) return;
            _speaker.SendAudioToHardware(); // Send Audio = upload to hardware (your implementation)
        }
    }

    // -------------------------
    // Styling helpers (copy Send button look)
    // -------------------------

    private void CopyButtonStyle(Button source, Button target)
    {
        if (source == null || target == null) return;

        // Button settings
        target.transition = source.transition;
        target.colors = source.colors;
        target.spriteState = source.spriteState;
        target.navigation = source.navigation;
        target.interactable = source.interactable;

        // Image settings
        var srcImg = source.GetComponent<Image>();
        var dstImg = target.GetComponent<Image>();
        if (srcImg != null && dstImg != null)
        {
            dstImg.sprite = srcImg.sprite;
            dstImg.type = srcImg.type;
            dstImg.pixelsPerUnitMultiplier = srcImg.pixelsPerUnitMultiplier;
            dstImg.material = srcImg.material;
            dstImg.color = srcImg.color;
            dstImg.raycastTarget = srcImg.raycastTarget;
        }
    }

    private void CopyTextStyle(Text source, Text target)
    {
        if (source == null || target == null) return;

        target.font = source.font;
        target.fontStyle = source.fontStyle;
        target.fontSize = source.fontSize;
        target.color = source.color;
        target.alignment = source.alignment;
        target.horizontalOverflow = source.horizontalOverflow;
        target.verticalOverflow = source.verticalOverflow;
    }

    // -------------------------
    // Upload button (code-created)
    // -------------------------

    private void CreateUploadButtonIfNeeded()
    {
        if (_uploadButton != null) return;

        var go = new GameObject("UploadButton");
        go.transform.SetParent(transform, false);

        var img = go.AddComponent<Image>();
        img.raycastTarget = true;

        _uploadButton = go.AddComponent<Button>();
        _uploadButton.onClick.AddListener(OnUploadClicked);

        // Copy Send button look
        CopyButtonStyle(button, _uploadButton);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        _uploadButtonText = textGo.AddComponent<Text>();
        _uploadButtonText.text = "Upload Audio";

        CopyTextStyle(_buttonText, _uploadButtonText);
        _uploadButtonText.alignment = TextAnchor.MiddleCenter;

        // Layout: above Play
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = button != null ? button.GetComponent<RectTransform>().sizeDelta : new Vector2(180f, 40f);
        rt.anchoredPosition = new Vector2(0f, 110f);

        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        go.SetActive(false);
    }

    private void SetUploadVisible(bool visible)
    {
        if (_uploadButton != null && _uploadButton.gameObject.activeSelf != visible)
            _uploadButton.gameObject.SetActive(visible);
    }

    private void OnUploadClicked()
    {
        if (_speaker == null) return;

        string path = OpenAudioFilePicker();
        if (string.IsNullOrEmpty(path)) return;

        _speaker.SetAudioFile(path);
        // If you want to display filename somewhere without the input field,
        // we can put it in the caption temporarily or add a TMP label in code.
    }

    // -------------------------
    // Play button (code-created)
    // -------------------------

    private void CreatePlayButtonIfNeeded()
    {
        if (_playButton != null) return;

        var go = new GameObject("PlayButton");
        go.transform.SetParent(transform, false);

        var img = go.AddComponent<Image>();
        img.raycastTarget = true;

        _playButton = go.AddComponent<Button>();
        _playButton.onClick.AddListener(OnPlayClicked);

        // Copy Send button look
        CopyButtonStyle(button, _playButton);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        _playButtonText = textGo.AddComponent<Text>();
        _playButtonText.text = "Play Audio";

        CopyTextStyle(_buttonText, _playButtonText);
        _playButtonText.alignment = TextAnchor.MiddleCenter;

        // Layout: between Upload and Send
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = button != null ? button.GetComponent<RectTransform>().sizeDelta : new Vector2(180f, 40f);
        rt.anchoredPosition = new Vector2(0f, 60f);

        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        go.SetActive(false);
    }

    private void SetPlayVisible(bool visible)
    {
        if (_playButton != null && _playButton.gameObject.activeSelf != visible)
            _playButton.gameObject.SetActive(visible);
    }

    private void OnPlayClicked()
    {
        if (_speaker == null) return;
        _speaker.PlayAudioOnHardware();
    }

    // -------------------------
    // File Picker (Editor + builds via SFB)
    // -------------------------

    private string OpenAudioFilePicker()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel("Select audio file", "", "wav,mp3,ogg");
#else
        var extensions = new[]
        {
            new SFB.ExtensionFilter("Audio Files", "wav", "mp3", "ogg"),
            new SFB.ExtensionFilter("All Files", "*")
        };

        string[] paths = SFB.StandaloneFileBrowser.OpenFilePanel("Select audio file", "", extensions, false);
        return (paths != null && paths.Length > 0) ? paths[0] : "";
#endif
    }

    private void SetCaptionVisible(bool visible)
    {
        if (_caption != null && _caption.gameObject.activeSelf != visible)
            _caption.gameObject.SetActive(visible);
    }
}