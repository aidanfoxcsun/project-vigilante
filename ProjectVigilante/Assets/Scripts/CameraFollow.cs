using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float cameraAngle = 40f;
    [SerializeField] private float cameraDistance = 10f;
    //[SerializeField] private float lookSpeed = 50f;
    [SerializeField] private float mouseLookSpeed = 0.1f;
    [SerializeField] private float controllerLookSpeed = 120f;
    [SerializeField] private float minCameraAngle = 10f;
    [SerializeField] private float maxCameraAngle = 75f;
    [SerializeField] private float lookHeight = 1.2f;
    [SerializeField] private Camera mainCamera;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = cameraAngle;

        UpdateCameraPosition();

        Cursor.lockState = CursorLockMode.Locked;
        // Hide the cursor
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        Gamepad gamepad = Gamepad.current;

        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }

        yaw += mouseDelta.x * mouseLookSpeed;
        pitch -= mouseDelta.y * mouseLookSpeed;

        if (gamepad != null)
        {
            Vector2 stickInput = gamepad.rightStick.ReadValue();

            if (stickInput.sqrMagnitude > 0.01f)
            {
                yaw += stickInput.x * controllerLookSpeed * Time.deltaTime;
                pitch -= stickInput.y * controllerLookSpeed * Time.deltaTime;
            }
        }

        pitch = Mathf.Clamp(pitch, minCameraAngle, maxCameraAngle);

        UpdateCameraPosition();

        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (escapePressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 lookTarget = transform.position + Vector3.up * lookHeight;
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraOffset = cameraRotation * Vector3.back * cameraDistance;

        mainCamera.transform.position = lookTarget + cameraOffset;
        mainCamera.transform.LookAt(lookTarget);
    }
}