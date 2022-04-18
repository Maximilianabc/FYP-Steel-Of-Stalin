using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

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

    private bool dragStart;
    private bool rotateStart;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition.magnitude;
        newCameraAngle = cameraTransform.localRotation.eulerAngles.x;
        dragStart = false;
        rotateStart = false;

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
        bool mouseOnUI=UIUtil.instance.isBlockedByUI();

        if (!mouseOnUI&&Input.mouseScrollDelta.y != 0) {
            newZoom -= Input.mouseScrollDelta.y * zoomAmount*5f;
        }
        if (!mouseOnUI && Input.GetMouseButtonDown(0)) {
            dragStart = true;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (dragStart&&Input.GetMouseButton(0)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }
        if (Input.GetMouseButtonUp(0)) {
            dragStart = false;
        }

        if (!mouseOnUI && Input.GetMouseButtonDown(1)) {
            rotateStart = true;
            rotateStartPosition = Input.mousePosition;
        }
        if (rotateStart&&Input.GetMouseButton(1)) {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateCurrentPosition - rotateStartPosition;
            rotateStartPosition = rotateCurrentPosition;
            newRotation *= Quaternion.Euler(Vector3.up * (difference.x/5f) * rotationAmount);
            newCameraAngle += (difference.y/5f)*(-cameraAngleAmount);
        }
        if (Input.GetMouseButtonUp(1)) {
            rotateStart = false;
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
