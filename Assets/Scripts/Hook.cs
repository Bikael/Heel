using UnityEngine;

public class Hook : MonoBehaviour
{
    public RopeVerlet rope;

    public bool touchedGround = false;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rope == null)
        {
            rope = FindAnyObjectByType<RopeVerlet>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (touchedGround)
            return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Hook hit the ground!");

            touchedGround = true;
        }
    }
}
