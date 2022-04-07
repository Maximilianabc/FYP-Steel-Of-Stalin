using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;
    private Transform followTransform;
    public Transform cameraTransform;

    public float normalSpeed;
    public float fastSpeed;
    private float movementSpeed;
    public float movementTime;
    public float rotationAmount;
    public float zoomAmount;
    public float cameraAngleAmount;
    public float smallTransformThreshold;
    public float smallRotationThreshold;
    public bool mouseControlEnabled;
    public bool keyboardControlEnabled;

    public float minCameraAngle;
    public float maxCameraAngle;
    public float minX, minZ, maxX, maxZ;
    public float minZoom, maxZoom;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public float newZoom;
    public float newCameraAngle;

    public Vector3 dragStartPosition;
    public Vector3 dragCurrentPosition;
    public Vector3 rotateStartPosition;
    public Vector3 rotateCurrentPosition;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition.magnitude;
        newCameraAngle = cameraTransform.localRotation.eulerAngles.x;

    }

    void Update()
    {
        if (mouseControlEnabled) HandleMouseInput();
        if (keyboardControlEnabled) HandleKeyboardInput();
        if (followTransform != null)
        {
            newPosition = followTransform.position;
        }
    }

    void LateUpdate()
    {

        HandleMovement();
    }

    void HandleMouseInput() {
        if (Input.mouseScrollDelta.y != 0) {
            newZoom -= Input.mouseScrollDelta.y * zoomAmount*5f;
        }
        if (Input.GetMouseButtonDown(0)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if (plane.Raycast(ray, out entry)) {
                dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (Input.GetMouseButton(0)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }

        if (Input.GetMouseButtonDown(1)) {
            rotateStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(1)) {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateCurrentPosition - rotateStartPosition;
            rotateStartPosition = rotateCurrentPosition;
            newRotation *= Quaternion.Euler(Vector3.up * (difference.x/5f) * rotationAmount);
            newCameraAngle += (difference.y/5f)*(-cameraAngleAmount);
        }
    }

    void HandleKeyboardInput() {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            movementSpeed = fastSpeed;
        }
        else
        {
            movementSpeed = normalSpeed;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition += (transform.forward * movementSpeed);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition += (transform.forward * -movementSpeed);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition += (transform.right * movementSpeed);
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition += (transform.right * -movementSpeed);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        }
        if (Input.GetKey(KeyCode.Home))
        {
            newZoom -= zoomAmount;
        }
        if (Input.GetKey(KeyCode.End))
        {
            newZoom += zoomAmount;
        }
        if (Input.GetKey(KeyCode.PageUp))
        {
            newCameraAngle += cameraAngleAmount;
        }
        if (Input.GetKey(KeyCode.PageDown))
        {
            newCameraAngle -= cameraAngleAmount;
        }
    }

    void HandleMovement() {
        newCameraAngle = Mathf.Clamp(newCameraAngle, minCameraAngle, maxCameraAngle);
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
        newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

        if ((transform.position - newPosition).magnitude < smallTransformThreshold)
        {
            transform.position = newPosition;
        }
        else {
            transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        }

        if ((transform.rotation.eulerAngles - newRotation.eulerAngles).magnitude < smallRotationThreshold)
        {
            transform.rotation = newRotation;
        }
        else {
            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        }

        if (cameraTransform.localRotation.eulerAngles.x - newCameraAngle < smallRotationThreshold)
        {
            cameraTransform.localRotation = Quaternion.AngleAxis(newCameraAngle, Vector3.right);
        }
        else {
            cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation, Quaternion.AngleAxis(newCameraAngle, Vector3.right), Time.deltaTime * movementTime);
        }

        //calculate camera position by localrotation and distance of camera
        cameraTransform.localPosition = Mathf.Lerp(cameraTransform.localPosition.magnitude, newZoom, Time.deltaTime * movementTime)*(cameraTransform.localRotation * -Vector3.forward);



    }

    public void FocusOn(Transform transform) {
        newPosition = transform.position;
    }

    public void FollowTransform(Transform transform) {
        followTransform = transform;
    }

    public void UnfollowTransform() {
        followTransform = null;
    }
}
