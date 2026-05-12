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

    private bool canMove = true;

    private Animator animator;

    private IEnumerator FreezeMovementForDuration(float duration)
    {
        canMove = false;
        yield return new WaitForSeconds(duration);
        canMove = true;
    }

    public bool isDashing => dashing;
    
    public void FreezeMovement(float duration)
    {
        StartCoroutine(FreezeMovementForDuration(duration));
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        if (dashing || !canMove) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

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

        Vector3 moveDir = cameraForward * moveInput.y + cameraRight * moveInput.x;

        bool dashPressed = gamepad != null
            ? gamepad.buttonEast.wasPressedThisFrame
            : Input.GetKeyDown(KeyCode.LeftControl);

        if (dashPressed)
        {
            animator.SetTrigger("Dodge");
            StartCoroutine(Dash(moveDir));
            return;
        }

        float newMoveSpeed = sprinting ? moveSpeed * sprintFactor : moveSpeed;
        animator.SetFloat("Speed", (moveDir.magnitude * newMoveSpeed) / (moveSpeed * sprintFactor));

        if (moveDir.sqrMagnitude > 0.01f)
        {
            moveDir.Normalize();
            transform.position += moveDir * newMoveSpeed * Time.deltaTime;

            // Only rotate the player when moving.
            transform.rotation = Quaternion.LookRotation(moveDir);
        }
    }

    private IEnumerator Dash(Vector3 dashDir)
    {
        dashing = true;

        if (dashDir.sqrMagnitude < 0.01f)
        {
            dashDir = transform.forward;
        }
        else
        {
            dashDir.Normalize();
        }

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
