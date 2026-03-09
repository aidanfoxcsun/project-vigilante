using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float cameraAngle = 40f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float lookSpeed = 50f;
    [SerializeField] private Camera mainCamera;

    private void Start()
    {
        mainCamera.transform.position = transform.position - transform.forward * cameraDistance + Vector3.up * cameraDistance * Mathf.Tan(cameraAngle * Mathf.Deg2Rad);
        mainCamera.transform.LookAt(transform.position);

        Cursor.lockState = CursorLockMode.Locked;
        // Hide the cursor
        Cursor.visible = false;
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxis("Mouse X");
        if (Mathf.Abs(horizontalInput) > 1f)
        {
            transform.Rotate(Vector3.up, horizontalInput * Time.deltaTime * lookSpeed);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
