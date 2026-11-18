
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody2D rb;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float horizontalInput;

    private bool isGrounded;
    private bool jumpPressed;

    [Header("Hook Settings")]
    public GameObject hook;
    public Transform launchPoint;
    public float launchSpeed = 15f;

    [Header("Leash Settings")]
    public GameObject leash;
    public RopeVerlet rope;

    public bool isLeashOut = false;

    private GameObject activeHook;
    private GameObject activeLeash;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        PlayerMovement();
        Throw();
    }

    private void FixedUpdate()
    {
        // Horizontal movement
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y
        );

        // Jump logic
        if (jumpPressed)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );
        }

        jumpPressed = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    private void PlayerMovement()
    {
        horizontalInput = 0f;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.dKey.isPressed)
        {
            horizontalInput = 1f;
        }
        else if (Keyboard.current.aKey.isPressed)
        {
            horizontalInput = -1f;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            jumpPressed = true;
        }
    }

    private void Throw()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Retract existing hook and leash
        if (isLeashOut)
        {
            RetractHook();
            return;
        }

        // Check references
        if (hook == null || leash == null || launchPoint == null)
        {
            Debug.LogError("Missing Hook, Leash, or Launch Point reference!");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // Get mouse world position
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
            mouseScreenPos
        );

        mouseWorldPos.z = 0f;

        // Calculate throwing direction
        Vector2 direction = (
            (Vector2)mouseWorldPos -
            (Vector2)launchPoint.position
        ).normalized;

        // Instantiate hook
        activeHook = Instantiate(
            hook,
            launchPoint.position,
            launchPoint.rotation
        );

        // Get hook components
        Rigidbody2D hookRb = activeHook.GetComponent<Rigidbody2D>();
        Collider2D hookCollider = activeHook.GetComponent<Collider2D>();

        if (hookRb == null || hookCollider == null)
        {
            Debug.LogError("Hook needs a Rigidbody2D and Collider2D!");

            Destroy(activeHook);
            activeHook = null;
            return;
        }

        // Ignore collision between player and hook
        Collider2D playerCollider = GetComponent<Collider2D>();

        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(
                hookCollider,
                playerCollider
            );
        }

        // Instantiate leash prefab
        activeLeash = Instantiate(
            leash,
            launchPoint.position,
            Quaternion.identity
        );

        // Get RopeVerlet component from leash
        rope = activeLeash.GetComponent<RopeVerlet>();

        if (rope == null)
        {
            Debug.LogError("Leash prefab is missing RopeVerlet!");

            Destroy(activeHook);
            Destroy(activeLeash);

            activeHook = null;
            activeLeash = null;

            return;
        }

        // Create rope segments
        rope.CreateLeash(launchPoint);

        // Attach rope between player and hook
        rope.AttachToHook(
            activeHook.transform,
            launchPoint
        );

        // Throw hook
        hookRb.linearVelocity = direction * launchSpeed;

        isLeashOut = true;

        Debug.Log("Hook and leash instantiated!");
    }

    private void RetractHook()
    {
        // Destroy hook
        if (activeHook != null)
        {
            Destroy(activeHook);
            activeHook = null;
        }

        // Destroy leash
        if (activeLeash != null)
        {
            Destroy(activeLeash);
            activeLeash = null;
        }

        rope = null;

        isLeashOut = false;

        Debug.Log("Hook and leash retracted!");
    }
}
