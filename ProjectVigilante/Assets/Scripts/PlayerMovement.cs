using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintFactor = 1.2f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private Camera mainCamera;

    private Vector2 moveInput;
    private bool sprinting = false;
    private bool dashing = false;

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        if (dashing) return;

        Vector3 lookDir = mainCamera.transform.forward;
        lookDir.y = 0;
        Vector3 moveDir = lookDir.normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, moveDir).normalized;

        if (gamepad != null)
        {
            moveInput = gamepad.leftStick.ReadValue();
            sprinting = gamepad.buttonSouth.IsPressed();
        }
        else
        {
            moveInput.x = Input.GetAxis("Horizontal");
            moveInput.y = Input.GetAxis("Vertical");
            sprinting = Input.GetKey(KeyCode.LeftShift);
        }

        if (moveDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(moveDir);

        if(gamepad.buttonEast.wasPressedThisFrame)
        {
            StartCoroutine(Dash(moveDir, rightDir));
            return;
        }

        float newMoveSpeed = sprinting ? moveSpeed * sprintFactor : moveSpeed;

        if (moveInput.magnitude > 0.1f)
        {
            transform.position += moveDir * moveInput.y * newMoveSpeed * Time.deltaTime;
            transform.position += rightDir * moveInput.x * newMoveSpeed * Time.deltaTime;
            return;
        }
    }

    private IEnumerator Dash(Vector3 moveDir, Vector3 rightDir)
    {
        dashing = true;

        Vector3 dashDir = moveDir * moveInput.y + rightDir * moveInput.x;
        if (dashDir.magnitude < 0.1f) dashDir = transform.forward;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            transform.position += dashDir * dashSpeed * Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(dashCooldown);
        dashing = false;
    }
}
