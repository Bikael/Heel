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

    // Vector2 


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

    private void PlayerMovement()
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

    private void Throw()
    {   
        
        if (Mouse.current.leftButton.wasPressedThisFrame && !isLeashOut)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            mouseWorldPos.z = 0f;

            Vector3 direction = (mouseWorldPos - transform.position).normalized;
            
            GameObject _leash = Instantiate(leash, launchPoint.position, launchPoint.rotation);

            Physics2D.IgnoreCollision( _leash.GetComponent<Collider2D>(), GetComponent<Collider2D>());

            Vector2 leashVel = launchSpeed * direction;
 
            _leash.GetComponent<Rigidbody2D>().linearVelocity = leashVel;

            isLeashOut = true;
            
        } else if (Mouse.current.leftButton.wasPressedThisFrame && isLeashOut){
            GameObject currentLeash = GameObject.FindGameObjectWithTag("Leash");
            Destroy(currentLeash);

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            mouseWorldPos.z = 0f;

            Vector3 direction = (mouseWorldPos - transform.position).normalized;
            
            GameObject _leash = Instantiate(leash, launchPoint.position, launchPoint.rotation);

            Physics2D.IgnoreCollision( _leash.GetComponent<Collider2D>(), GetComponent<Collider2D>());

            Vector2 leashVel = launchSpeed * direction;
 
            _leash.GetComponent<Rigidbody2D>().linearVelocity = leashVel;

            isLeashOut = true;
        }
    }

}
