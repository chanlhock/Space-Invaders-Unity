using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 30f;
    public float smoothTime = 0.1f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.3f;

    private float lastFireTime = 0f;
    private float targetX;
    private float velocity = 0f;
    private bool buttonWasPressed = false;

    // Keyboard state
    private bool movingLeft = false;
    private bool movingRight = false;

    // Bounds
    private float leftBound;
    private float rightBound;
    private float spriteHalfWidth;

    void Start()
    {
        // Auto-load bullet prefab if not assigned
        if (bulletPrefab == null)
        {
            bulletPrefab = Resources.Load<GameObject>("Bullet");
            if (bulletPrefab == null)
            {
                Debug.LogWarning("⚠️ Bullet prefab not assigned and not found in Resources!");
                // Try to find it in the scene
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
            }
        }

        // Setup collider - ensure it's NOT a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = false;
        
        // Set tag
        gameObject.tag = "Player";

        // Calculate bounds
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            spriteHalfWidth = sr.sprite.bounds.extents.x;
        }
        else
        {
            spriteHalfWidth = 0.3f;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            float screenWidth = cam.orthographicSize * cam.aspect;
            leftBound = -screenWidth + spriteHalfWidth;
            rightBound = screenWidth - spriteHalfWidth;
        }
        else
        {
            leftBound = -7.5f;
            rightBound = 7.5f;
        }

        // Start at center bottom
        targetX = 0f;
        transform.position = new Vector3(0f, -4.5f, 0f);
        Debug.Log("Player initialized at center: " + transform.position);

        // Create fire point if it doesn't exist
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.parent = transform;
            fp.transform.localPosition = new Vector3(0, 0.5f, 0);
            firePoint = fp.transform;
        }
    }

    void Update()
    {
        // 1. Handle keyboard input
        movingLeft = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        movingRight = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);

        // 2. Handle joystick input
        float joystickMovement = 0f;
        if (GameInput.Instance != null && GameInput.Instance.IsDeviceConnected())
        {
            float calibratedX = GameInput.Instance.GetCalibratedX();

            if (calibratedX < -20)
                joystickMovement = -1f;
            else if (calibratedX > 20)
                joystickMovement = 1f;
        }

        // 3. Apply movement (keyboard overrides joystick)
        float moveInput = 0f;
        if (movingLeft)
            moveInput = -1f;
        else if (movingRight)
            moveInput = 1f;
        else if (joystickMovement != 0)
            moveInput = joystickMovement;

        // Calculate target position
        float moveAmount = moveInput * speed * Time.deltaTime;
        targetX = Mathf.Clamp(targetX + moveAmount, leftBound, rightBound);

        // Smooth movement
        transform.position = new Vector3(
            Mathf.SmoothDamp(transform.position.x, targetX, ref velocity, smoothTime),
            transform.position.y,
            transform.position.z
        );

        // 4. Handle shooting
        bool keyboardFire = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);

        bool joystickFire = false;
        if (GameInput.Instance != null && GameInput.Instance.IsDeviceConnected())
        {
            bool currentButton = GameInput.Instance.IsButtonPressed();
            if (!buttonWasPressed && currentButton)
                joystickFire = true;
            buttonWasPressed = currentButton;
        }

        if ((keyboardFire || joystickFire) && Time.time - lastFireTime > fireCooldown)
        {
            FireBullet();
            lastFireTime = Time.time;
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("❌ Bullet prefab is null!");
            return;
        }

        // Use firePoint or position + offset
        Vector3 spawnPos = firePoint != null ? firePoint.position : 
                          transform.position + new Vector3(0, 0.6f, 0);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"🔫 Bullet fired from: {spawnPos}");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShoot();
        }
    }
}