using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a track's Blocks rect. On click, invokes callback with the clicked time (0-10).
/// </summary>
public class TrackClickHandler : MonoBehaviour, IPointerClickHandler
{
    public System.Action<int, float> OnTrackClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnTrackClicked == null) return;
        var rect = GetComponent<RectTransform>();
        if (rect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var localPoint);
        float normalizedX = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, localPoint.x);
        float second = Mathf.Clamp(normalizedX * ProgrammedMovementsController.TimelineDurationSeconds, 0f, ProgrammedMovementsController.TimelineDurationSeconds - 0.1f);
        int trackIndex = transform.parent.GetSiblingIndex();
        OnTrackClicked.Invoke(trackIndex, second);
    }
}
