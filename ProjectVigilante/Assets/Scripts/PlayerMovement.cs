using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera mainCamera;

    private void Update()
    {
        Vector3 lookDir = mainCamera.transform.forward;
        Vector3 moveDir = new Vector3(lookDir.x, 0, lookDir.z).normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, moveDir).normalized;

        transform.rotation = Quaternion.LookRotation(moveDir);

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= moveDir * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= rightDir * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += rightDir * moveSpeed * Time.deltaTime;
        }
    }
}
