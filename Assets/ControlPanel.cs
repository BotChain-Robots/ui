using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanel : MonoBehaviour
{
    [Header("Core UI (assign in Inspector)")]
    public TMP_InputField inputField;          // was DegreesInputField
    public TMP_Dropdown directionDropdown;     // was DirectionDropdown
    public Button button;                      // was RotateButton

    private enum Mode { None, DC, Display, Gripper }
    private Mode _mode = Mode.None;

    private DCMotorModule _dc;
    private DisplayModule _display;
    private GripperModule _gripper;

    // Keep separate inputs so they don't interfere
    private string _savedDCInput = "";
    private int _savedDirectionIndex = 0;
    private string _savedDisplayInput = "";
    private string _savedGripperInput = "";

    // Found automatically (no new UI objects)
    private TextMeshProUGUI _caption;          // DegreesCaption text
    private TextMeshProUGUI _buttonText;       // RotateButton/Text (TMP)
    private GameObject _directionCaptionGO;    // DirectionCaption
    private GameObject _directionDropdownGO;   // DirectionDropdown GameObject

    private void Awake()
    {
        CacheUIFromHierarchy();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        // Cache typed values so switching modules restores what you typed
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(_ => CacheCurrentInput());
            inputField.onEndEdit.AddListener(_ => CacheCurrentInput());
        }

        if (directionDropdown != null)
        {
            directionDropdown.onValueChanged.AddListener(v =>
            {
                if (_mode == Mode.DC) _savedDirectionIndex = v;
            });
        }
    }

    /// <summary>
    /// Call this whenever a DC or Display module is selected.
    /// </summary>
    public void Initialize(ModuleBase module)
    {
        CacheCurrentInput();

        _dc = module as DCMotorModule;
        _display = module as DisplayModule;
        _gripper = module as GripperModule;

        if (_dc != null)
        {
            SwitchMode(Mode.DC);
            gameObject.SetActive(true);
            return;
        }

        if (_display != null)
        {
            SwitchMode(Mode.Display);
            gameObject.SetActive(true);
            return;
        }

        if (_gripper != null)
        {
            SwitchMode(Mode.Gripper);
            gameObject.SetActive(true);
            return;
        }

        HidePanel();
    }

    public void HidePanel()
    {
        CacheCurrentInput();
        gameObject.SetActive(false);
        _mode = Mode.None;
        _dc = null;
        _display = null;
        _gripper = null;
    }

    private void CacheUIFromHierarchy()
    {
        // Assumes this script is on the parent that contains these children
        var captionT = transform.Find("DegreesCaption");
        if (captionT != null) _caption = captionT.GetComponent<TextMeshProUGUI>();

        var buttonTextT = transform.Find("RotateButton/Text (TMP)");
        if (buttonTextT != null) _buttonText = buttonTextT.GetComponent<TextMeshProUGUI>();

        var dirCaptionT = transform.Find("DirectionCaption");
        if (dirCaptionT != null) _directionCaptionGO = dirCaptionT.gameObject;

        var dirDropdownT = transform.Find("DirectionDropdown");
        if (dirDropdownT != null) _directionDropdownGO = dirDropdownT.gameObject;
    }

    private void SwitchMode(Mode newMode)
    {
        CacheCurrentInput();
        _mode = newMode;

        if (_mode == Mode.DC)
        {
            if (_caption != null) _caption.text = "Degrees";
            if (_buttonText != null) _buttonText.text = "Rotate";
            SetDirectionVisible(true);

            if (inputField != null)
                inputField.SetTextWithoutNotify(_savedDCInput);

            if (directionDropdown != null)
                directionDropdown.SetValueWithoutNotify(_savedDirectionIndex);
        }
        else if (_mode == Mode.Display)
        {
            if (_caption != null) _caption.text = "Text";
            if (_buttonText != null) _buttonText.text = "Send";
            SetDirectionVisible(false);

            // Prefer module's current displayText; fallback to last typed text
            string toShow = _savedDisplayInput;
            if (_display != null && !string.IsNullOrEmpty(_display.displayText))
                toShow = _display.displayText;

            if (inputField != null)
                inputField.SetTextWithoutNotify(toShow);
        }
        else if (_mode == Mode.Gripper)
        {
            if (_caption != null) _caption.text = "Degrees";
            if (_buttonText != null) _buttonText.text = "Apply";
            SetDirectionVisible(false);

            if (inputField != null)
                inputField.SetTextWithoutNotify(_gripper != null ? _gripper.currentAngle.ToString("F0") : _savedGripperInput);
        }
    }

    private void SetDirectionVisible(bool visible)
    {
        if (_directionCaptionGO != null) _directionCaptionGO.SetActive(visible);
        if (_directionDropdownGO != null) _directionDropdownGO.SetActive(visible);
    }

    private void CacheCurrentInput()
    {
        if (inputField == null) return;

        if (_mode == Mode.DC)
        {
            _savedDCInput = inputField.text;
            if (directionDropdown != null)
                _savedDirectionIndex = directionDropdown.value;
        }
        else if (_mode == Mode.Display)
        {
            _savedDisplayInput = inputField.text;
        }
        else if (_mode == Mode.Gripper)
        {
            _savedGripperInput = inputField.text;
        }
    }

    private void OnButtonClicked()
    {
        if (_mode == Mode.DC)
        {
            if (_dc == null)
            {
                Debug.LogWarning("ControlPanel: No DC module selected.");
                return;
            }

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
            if (_display == null)
            {
                Debug.LogWarning("ControlPanel: No Display module selected.");
                return;
            }

            string text = inputField != null ? inputField.text : "";
            _display.SetDisplayText(text); // <- THIS is where Display module gets the text
        }
        else if (_mode == Mode.Gripper)
        {
            if (_gripper == null)
            {
                Debug.LogWarning("ControlPanel: No Gripper module selected.");
                return;
            }

            if (!float.TryParse(inputField.text, out float degrees))
            {
                Debug.LogWarning("ControlPanel: Invalid degree input for Gripper.");
                return;
            }

            _gripper.SetAngleAndSendControlLibrary(Mathf.Clamp(degrees, 0f, 180f));
        }
    }
}