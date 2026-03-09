using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// IK-mode side panel: shows fixed-module hint. Text is set in the scene.
/// </summary>
public class IKModulePanel : MonoBehaviour
{
    public Text moduleInfoText;

    [Header("Panel Layout")]
    public float panelWidth = 260f;
    [Range(0f, 0.5f)]
    public float cornerRadius = 0.12f;

    void Start()
    {
        ApplyRoundedMaterial();
    }

    void ApplyRoundedMaterial()
    {
        var img = GetComponent<Image>();
        if (img == null || (img.material != null && img.material.shader != null && img.material.shader.name == "UI/RoundedRect")) return;
        var shader = Shader.Find("UI/RoundedRect");
        if (shader == null) return;
        var mat = new Material(shader);
        mat.SetFloat("_Radius", cornerRadius);
        img.material = mat;
    }

}
