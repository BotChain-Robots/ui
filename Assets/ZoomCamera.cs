using UnityEngine;

public class ZoomCamera : MonoBehaviour
{
    public float zoomSpeed = 5f;
    public float minDistance = 2f;
    public float maxDistance = 20f;

    public Cinemachine.CinemachineVirtualCamera virtualCamera;

    private Cinemachine.CinemachineTransposer transposer;

    void Start()
    {
        if (virtualCamera != null)
        {
            transposer = virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineTransposer>();
        }
    }

    void Update()
    {
        if (transposer == null) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            Vector3 offset = transposer.m_FollowOffset;
            offset.z += scrollInput * zoomSpeed;

            // Clamp the zoom distance
            offset.z = Mathf.Clamp(offset.z, -maxDistance, -minDistance); // Negative z for zoom "in"
            transposer.m_FollowOffset = offset;
        }
    }
}
