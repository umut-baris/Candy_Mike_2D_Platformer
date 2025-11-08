using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 10f;
    
    // YENİ: Yerçekimi ayarları
    public float normalGravityScale = 1f;
    public float fallGravityScale = 2f; // Düşüşteki yerçekimi
    public float quickFallGravityScale = 3f; // Hızlı düşüş için
    
    public float rayLength = 0.6f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private bool facingRight = true;
    private bool isRunning = false;

    private bool IsDash = false;

    [Header("Hand System")]
    public int availableHands = 2;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    private float dashTimeLeft = 0f;

    // YENİ: Dash Efektleri
    [Header("Dash Effects")]
    public GameObject dashTrailPrefab;
    public float trailSpawnInterval = 0.05f;
    private float trailTimer;
    public ParticleSystem dashStartParticles;
    public ParticleSystem dashEndParticles;
    public AudioClip dashSound;
    public AudioClip dashEndSound;
    private AudioSource audioSource;

 

    public Camera mainCamera;

    private Vector3 originalCameraPos;

    // YENİ: Time Scale Effect
    [Header("Time Effects")]
    public float dashTimeScale = 0.8f;
    public float timeScaleTransition = 0.05f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // Eğer AudioSource yoksa ekle
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Kamera referansı yoksa main camera'yı al
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (mainCamera != null)
            originalCameraPos = mainCamera.transform.localPosition;
        
        // Başlangıç yerçekimini ayarla
        rb.gravityScale = normalGravityScale;
    }

    void Update()
    {
        // KOŞMA KONTROLÜ
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        // Yatay hareket - DASH SIRASINDA HAREKET ETME!
        if (!IsDash)
        {
            float moveX = Input.GetAxis("Horizontal");
            rb.velocity = new Vector2(moveX * currentSpeed, rb.velocity.y);
        }

        // Animasyon kontrolü
        float moveXForAnim = Input.GetAxis("Horizontal");
        animator.SetBool("IsWalking", Mathf.Abs(moveXForAnim) > 0.1f);
        animator.SetBool("IsRunning", isRunning && Mathf.Abs(moveXForAnim) > 0.1f);

        // Yön kontrolü
        if (moveXForAnim > 0.1f) facingRight = true;
        if (moveXForAnim < -0.1f) facingRight = false;

        animator.SetBool("FacingRight", facingRight);

        // DASH KONTROLÜ
        if (Input.GetKeyDown(KeyCode.C) && availableHands > 0 && !IsDash)
        {
            Dash();
        }
        
        // Dash süresi kontrolü
        if (IsDash)
        {
            HandleDash();
            HandleDashEffects();
        }

        // Zıplama
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (IsGrounded())
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }
            else if (availableHands > 0)
            {
                DoubleJump();
            }
        }

        // YENİ: Yerçekimi kontrolü
        HandleGravity();

        // Yere değince eller geri gelsin
        if (IsGrounded())
        {
            availableHands = 2;
        }
    }

    // YENİ: Dash efektlerini yönet
    void HandleDashEffects()
    {
        trailTimer += Time.deltaTime;
        if (trailTimer >= trailSpawnInterval && dashTrailPrefab != null)
        {
            SpawnDashTrail();
            trailTimer = 0f;
        }
    }

    // YENİ: Dash trail efekti
    void SpawnDashTrail()
    {
        GameObject trail = Instantiate(dashTrailPrefab, transform.position, transform.rotation);
        
        // Karakterin sprite'ını kopyala
        SpriteRenderer trailRenderer = trail.GetComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        
        if (trailRenderer != null && playerRenderer != null)
        {
            trailRenderer.sprite = playerRenderer.sprite;
            trailRenderer.flipX = playerRenderer.flipX;
            trailRenderer.color = new Color(1f, 0.5f, 0.8f, 0.7f); // Mor-pembe dash rengi
        }
        
        // 0.3 saniye sonra trail yok olsun
        Destroy(trail, 0.3f);
    }

    // YENİ: Dash başlangıç efektleri
    void OnDashStart()
    {
        // Ses efekti
        if (dashSound != null)
            audioSource.PlayOneShot(dashSound);
        
        // Particle efekti
        if (dashStartParticles != null)
        {
            dashStartParticles.transform.position = transform.position;
            dashStartParticles.Play();
        }
        
  
        // Time scale efekti
        StartCoroutine(ChangeTimeScale(1f, dashTimeScale));
        
        // Trail timer'ı sıfırla
        trailTimer = 0f;
    }

    // YENİ: Dash bitiş efektleri
    void OnDashEnd()
    {
        // Ses efekti
        if (dashEndSound != null)
            audioSource.PlayOneShot(dashEndSound);
        
        // Particle efekti
        if (dashEndParticles != null)
        {
            dashEndParticles.transform.position = transform.position;
            dashEndParticles.Play();
        }
        
        // Time scale'i normale döndür
        StartCoroutine(ChangeTimeScale(dashTimeScale, 1f));
    }


   

    // YENİ: Time scale efekti
    IEnumerator ChangeTimeScale(float from, float to)
    {
        float timer = 0f;
        
        while (timer < timeScaleTransition)
        {
            timer += Time.deltaTime;
            Time.timeScale = Mathf.Lerp(from, to, timer / timeScaleTransition);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        
        Time.timeScale = to;
    }

    // YENİ: Yerçekimini kontrol eden metod
    void HandleGravity()
    {
        // Yerdeyken normal yerçekimi
        if (IsGrounded())
        {
            rb.gravityScale = normalGravityScale;
        }
        // Havada ve yükseliyorsa normal yerçekimi
        else if (rb.velocity.y > 0)
        {
            rb.gravityScale = normalGravityScale;
        }
        // Havada ve düşüyorsa artırılmış yerçekimi
        else if (rb.velocity.y < 0)
        {
            // Aşağı ok tuşuna basılıyorsa hızlı düşüş
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                rb.gravityScale = quickFallGravityScale;
            }
            else
            {
                rb.gravityScale = fallGravityScale;
            }
        }
    }

    void DoubleJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        availableHands--;
        animator.Play("ThrowHand_Down");
        Debug.Log("Extra zıplama! Kalan el: " + availableHands);
    }

    void Dash()
    {
        availableHands--;
        IsDash = true;
        dashTimeLeft = dashDuration;
        
        float dashDirection = facingRight ? 1f : -1f;
        rb.velocity = new Vector2(dashDirection * dashForce, rb.velocity.y);

        // Animasyon
        animator.SetBool("IsDash", IsDash);
        
        // YENİ: Dash efektlerini başlat
        OnDashStart();
        
        Debug.Log("Dash! Kalan el: " + availableHands);
    }

    void HandleDash()
    {
        dashTimeLeft -= Time.deltaTime;
        
        if (dashTimeLeft <= 0f)
        {
            IsDash = false;
            animator.SetBool("IsDash", IsDash);

            // YENİ: Dash bitiş efektleri
            OnDashEnd();

            if (Mathf.Abs(rb.velocity.x) > moveSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x * 0.7f, rb.velocity.y);
            }
        }
    }

    bool IsGrounded()
    {
        float rayLength = 0.7f;
        LayerMask groundLayer = LayerMask.GetMask("Ground");
        bool isGrounded = false;
        
        for (int i = -1; i <= 1; i++)
        {
            Vector2 rayOrigin = new Vector2(
                transform.position.x + (i * 0.2f), 
                transform.position.y - 0.5f
            );
            
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, groundLayer);
            if (hit.collider != null)
            {
                Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.green);
                isGrounded = true;
            }
            else
            {
                Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.red);
            }
        }
        
        return isGrounded;
    }
}