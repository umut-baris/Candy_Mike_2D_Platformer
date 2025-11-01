using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 8f; // YENİ: Koşma hızı
    public float jumpForce = 10f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private bool facingRight = true;
    private bool isRunning = false; // YENİ: Koşuyor mu?

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // KOŞMA KONTROLÜ - YENİ
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        // Hızı belirle
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        // Yatay hareket
        float moveX = Input.GetAxis("Horizontal");
        
        // Animasyon kontrolü - YENİ PARAMETER
        animator.SetBool("IsWalking", Mathf.Abs(moveX) > 0.1f);
        animator.SetBool("IsRunning", isRunning && Mathf.Abs(moveX) > 0.1f); // YENİ
        
        // Yön kontrolü
        if (moveX > 0.1f) facingRight = true;
        if (moveX < -0.1f) facingRight = false;

        // ANIMATOR PARAMETER'larını güncelle
        animator.SetBool("FacingRight", facingRight);
        
        // Hareket uygulama
        rb.velocity = new Vector2(moveX * currentSpeed, rb.velocity.y);

        // Zıplama
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    // Yer kontrolü metodu (aynı)
    bool IsGrounded()
    {
        float rayLength = 0.6f;
        Vector2 rayOrigin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength);
        return hit.collider != null;
    }
}