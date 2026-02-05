using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top banner with three buttons (1=Live View, 2=Inverse Kinematics, 3=Programmed Movements).
/// Each button enables the corresponding view GameObject and disables the other two.
/// View-specific logic should be attached under those GameObjects; when disabled, it is suspended.
/// </summary>
public class ViewBannerUI : MonoBehaviour
{
    private const string ViewsParentName = "Views";
    private const string LiveViewName = "LiveView";
    private const string InverseKinematicsViewName = "InverseKinematicsView";
    private const string ProgrammedMovementsViewName = "ProgrammedMovementsView";

    [Header("Layout")]
    [SerializeField] private float bannerHeight = 48f;
    [SerializeField] private float buttonPadding = 6f;

    [Header("Colors")]
    [SerializeField] private Color bannerBackground = Color.black;
    [SerializeField] private Color buttonDefault = Color.white;
    [SerializeField] private Color buttonSelected = Color.white;
    [SerializeField] private Color buttonTextColor = Color.black;

    private GameObject _liveViewRoot;
    private GameObject _inverseKinematicsViewRoot;
    private GameObject _programmedMovementsViewRoot;
    private RectTransform _bannerRoot;
    private Image[] _buttonImages = new Image[3];
    // Labels without "View": Live, Inverse Kinematics, Programmed Movements
    private static readonly string[] Labels = { "Live", "Inverse Kinematics", "Programmed Movements" };

    private void Awake()
    {
        if (!ResolveViewRoots())
            return;

        // Use this GameObject as the banner (must be under Canvas with RectTransform + Image).
        _bannerRoot = GetComponent<RectTransform>();
        if (_bannerRoot == null)
        {
            Debug.LogError("[ViewBannerUI] ViewBanner must have a RectTransform (e.g. under Canvas).");
            return;
        }

        var bannerImage = GetComponent<Image>();
        if (bannerImage != null)
            bannerImage.color = new Color(0f, 0f, 0f, 1f); // solid black, full opacity

        // Default to Live View on startup.
        SetActiveView(ViewMode.LiveView);

        for (int i = 0; i < 3; i++)
        {
            ViewMode mode = (ViewMode)i;
            _buttonImages[i] = CreateViewButton(_bannerRoot, i, Labels[i], () => OnViewButtonClicked(mode));
        }
        RefreshButtonVisuals();
    }

    /// <summary>
    /// Resolves the three view root GameObjects that must already exist in the scene hierarchy:
    /// Views/LiveView, Views/InverseKinematicsView, Views/ProgrammedMovementsView.
    /// Returns false and logs a clear error if anything is missing.
    /// </summary>
    private bool ResolveViewRoots()
    {
        Transform viewsParent = GameObject.Find(ViewsParentName)?.transform;
        if (viewsParent == null)
        {
            Debug.LogError(
                $"[ViewBannerUI] Missing '{ViewsParentName}' GameObject. " +
                $"Please create it in the scene with children '{LiveViewName}', '{InverseKinematicsViewName}', '{ProgrammedMovementsViewName}'."
            );
            return false;
        }

        _liveViewRoot = viewsParent.Find(LiveViewName)?.gameObject;
        _inverseKinematicsViewRoot = viewsParent.Find(InverseKinematicsViewName)?.gameObject;
        _programmedMovementsViewRoot = viewsParent.Find(ProgrammedMovementsViewName)?.gameObject;

        if (_liveViewRoot == null || _inverseKinematicsViewRoot == null || _programmedMovementsViewRoot == null)
        {
            Debug.LogError(
                $"[ViewBannerUI] Missing one or more view roots under '{ViewsParentName}'. " +
                $"Expected children named '{LiveViewName}', '{InverseKinematicsViewName}', '{ProgrammedMovementsViewName}'."
            );
            return false;
        }

        return true;
    }

    private void OnViewButtonClicked(ViewMode mode)
    {
        SetActiveView(mode);
        RefreshButtonVisuals();
    }

    /// <summary>
    /// Enables the view GameObject for the given mode and disables the other two.
    /// </summary>
    public void SetActiveView(ViewMode mode)
    {
        _liveViewRoot.SetActive(mode == ViewMode.LiveView);
        _inverseKinematicsViewRoot.SetActive(mode == ViewMode.InverseKinematics);
        _programmedMovementsViewRoot.SetActive(mode == ViewMode.ProgrammedMovements);
    }

    private void RefreshButtonVisuals()
    {
        bool liveActive = _liveViewRoot != null && _liveViewRoot.activeSelf;
        bool ikActive = _inverseKinematicsViewRoot != null && _inverseKinematicsViewRoot.activeSelf;
        bool pmActive = _programmedMovementsViewRoot != null && _programmedMovementsViewRoot.activeSelf;

        if (_buttonImages[0] != null) _buttonImages[0].color = liveActive ? buttonSelected : buttonDefault;
        if (_buttonImages[1] != null) _buttonImages[1].color = ikActive ? buttonSelected : buttonDefault;
        if (_buttonImages[2] != null) _buttonImages[2].color = pmActive ? buttonSelected : buttonDefault;
    }

    private Image CreateViewButton(RectTransform parent, int index, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"ViewButton_{label}");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        // Buttons at 1/6 width each, centered as a group (1/4 margin each side)
        float sixth = 1f / 6f;
        float startX = 0.25f; // left margin so block is centered
        rect.anchorMin = new Vector2(startX + index * sixth, 0f);
        rect.anchorMax = new Vector2(startX + (index + 1) * sixth, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = new Vector2(buttonPadding, buttonPadding);
        rect.offsetMax = new Vector2(-buttonPadding, -buttonPadding);

        var image = go.AddComponent<Image>();
        image.color = buttonDefault;
        image.raycastTarget = true;
        image.sprite = CreateRoundedRectSprite();
        image.type = Image.Type.Simple;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        // Use white for all states so Unity doesn't tint the button blue when selected/highlighted
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6, 4);
        textRect.offsetMax = new Vector2(-6, -4);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = buttonTextColor;
        tmp.enableWordWrapping = true;

        return image;
    }

    private static Sprite _roundedRectSprite;

    /// <summary>
    /// Creates or returns a cached white rounded-rectangle sprite for button backgrounds.
    /// </summary>
    private static Sprite CreateRoundedRectSprite()
    {
        if (_roundedRectSprite != null)
            return _roundedRectSprite;

        const int size = 64;
        const int radius = 12;
        var tex = new Texture2D(size, size);
        var pixels = new Color32[size * size];
        float r = radius - 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x < r ? r - x : (x >= size - r ? x - (size - 1 - r) : 0);
            float dy = y < r ? r - y : (y >= size - r ? y - (size - 1 - r) : 0);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(255 * (1f - Mathf.Clamp01((d - r) / 1.5f))), 0, 255);
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply(true, true);
        tex.filterMode = FilterMode.Bilinear;
        _roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _roundedRectSprite;
    }
}
