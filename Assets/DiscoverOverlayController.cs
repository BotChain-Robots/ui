using UnityEngine;
using UnityEngine.UI;

public class DiscoverOverlayController : MonoBehaviour
{
    public GameObject overlayPanel;
    public RectTransform overlayContent;

    public void ShowOverlay()
    {
        if (overlayPanel != null)
        {
            EnsureScrollRect();
            overlayPanel.SetActive(true);
        }
    }

    public void HideOverlay()
    {
        ClearContent();
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
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
