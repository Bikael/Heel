
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float horizontalInput;

    private bool isGrounded;
    private bool jumpPressed = false;

    public GameObject leash;
    public Transform launchPoint;
    public float launchSpeed = 15f;

    public LineRenderer lr;
    public int linePoints = 30;
    public float timeBetweenPoints = 0.05f;

    public bool isLeashOut = false;

    public RopeVerlet rope;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        PlayerMovement();
        Throw();
    }

    void FixedUpdate()
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
                rb.linearVelocityX,
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
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Destroy existing leash if one is already out
        if (isLeashOut)
        {
            GameObject currentLeash = GameObject.FindGameObjectWithTag("Leash");

            if (currentLeash != null)
                Destroy(currentLeash);

            isLeashOut = false;

            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector3 direction = (mouseWorldPos - launchPoint.position).normalized;

        // Instantiate leash
        GameObject _leash = Instantiate(
            leash,
            launchPoint.position,
            launchPoint.rotation
        );

        // Ignore collision with player
        Physics2D.IgnoreCollision(
            _leash.GetComponent<Collider2D>(),
            GetComponent<Collider2D>()
        );

        // Apply throwing velocity
        Rigidbody2D leashRb = _leash.GetComponent<Rigidbody2D>();
        leashRb.linearVelocity = launchSpeed * direction;

        // Attach rope to ball and player
        rope.AttachToBall(_leash.transform, launchPoint);

        isLeashOut = true;
    }
}
