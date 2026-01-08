using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragMove : MonoBehaviour
{
    public float dragSpeed = 10f;
    public float scrollSensitivity = 5f;
    private bool isDragging = false;
    private float dragDepth;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); //Creates ray object where a ray is sent from the camera to your mouse position
            if(Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform) //Checks if ray hit this exact object and not some other object
            {
                // dragPlane = new Plane(Camera.main.transform.forward * -1, transform.position); //Creates invisible horizontal plane that passes through the object's current position, used to calculate where in 3D space the mouse is pointing as you drag
                // if(dragPlane.Raycast(ray, out float distance)) //Calculates where the mouse ray intersects that invisible plane
                // {
                //     offset = transform.position - ray.GetPoint(distance); //Gets the actual 3D position on the plane where the ray hit and clculates how far the object’s current position is from the mouse point
                //     isDragging = true;
                // }
                isDragging = true;
                dragDepth = Vector3.Distance(Camera.main.transform.position, transform.position);
            }
        }

        if(Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        if(isDragging)
        {
            // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // // Intersect with an invisible plane that is defined by the object's current position and normal
            // Plane dragPlane = new Plane(-Camera.main.transform.forward, transform.position);
            
            // if (dragPlane.Raycast(ray, out float distance))
            // {
            //     Vector3 hitPoint = ray.GetPoint(distance);
            //     Vector3 targetPosition = hitPoint + offset;

            //     transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);
            // }

            if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                dragDepth -= scrollSensitivity * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                dragDepth += scrollSensitivity * Time.deltaTime;
            }
            dragDepth = Mathf.Clamp(dragDepth, 1f, 20f);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 targetPos = ray.origin + ray.direction * dragDepth;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * dragSpeed);
        }
    }
}
