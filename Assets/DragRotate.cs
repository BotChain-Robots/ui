using UnityEngine;

public class DragRotate : MonoBehaviour
{
    public float rotationSpeed = 100f;
    private bool isSelected = false;

    void OnMouseDown()
    {
        isSelected = true;
    }

    void OnMouseUp()
    {
        isSelected = false;
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && isSelected)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Horizontal (Y axis)
            transform.Rotate(Vector3.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World);
            // Vertical (X axis)
            transform.Rotate(Vector3.right, mouseY * rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}