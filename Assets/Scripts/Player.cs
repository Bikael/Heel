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
    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        horizontalInput = 0f;

        if (Keyboard.current.dKey.isPressed)
        {
            horizontalInput = 1f;
        }else if (Keyboard.current.aKey.isPressed)
        {
            horizontalInput = -1f;
        }

        
        if(Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            jumpPressed = true;
        }
    }
    void FixedUpdate()
    {
        

        // horizontal movement
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed, 
            rb.linearVelocity.y
        );

        // jump logic
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

    
}
