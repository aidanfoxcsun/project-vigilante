using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float cameraAngle = 40f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float lookSpeed = 50f;
    [SerializeField] private Camera mainCamera;

    private float yaw;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        float horizontalInput = Input.GetAxis("Mouse X");

        yaw += horizontalInput * lookSpeed * Time.deltaTime;

        float height = cameraDistance * Mathf.Tan(cameraAngle * Mathf.Deg2Rad);

        Vector3 flatOffset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * cameraDistance;

        mainCamera.transform.position = transform.position + flatOffset + Vector3.up * height;
        mainCamera.transform.LookAt(transform.position);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
