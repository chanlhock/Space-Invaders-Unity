using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 300f;
    public float smoothTime = 0.1f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.5f;

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
            }
        }

        // Calculate bounds
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            spriteHalfWidth = sr.sprite.bounds.extents.x;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            float screenWidth = cam.orthographicSize * cam.aspect;
            leftBound = -screenWidth + spriteHalfWidth;
            rightBound = screenWidth - spriteHalfWidth;
        }

        // Start at center bottom
        targetX = 0f;
        transform.position = new Vector3(0f, -4f, 0f);
        Debug.Log("Player initialized at center: " + transform.position);
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

            if (keyboardFire)
                Debug.Log("🔫 Keyboard fire!");
            else if (joystickFire)
                Debug.Log("🔫 Joystick fire!");
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
                          transform.position + new Vector3(0, 0.5f, 0);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // Play shoot sound
        SoundManager.Instance?.PlayShoot();
    }

    // Debug function - press D key
    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.D)
        {
            Debug.Log("=== Player Debug ===");
            Debug.Log("Position: " + transform.position);
            Debug.Log("Target X: " + targetX);
            Debug.Log("Moving Left: " + movingLeft);
            Debug.Log("Moving Right: " + movingRight);
            if (GameInput.Instance != null)
            {
                Debug.Log("GameInput Connected: " + GameInput.Instance.IsDeviceConnected());
                if (GameInput.Instance.IsDeviceConnected())
                {
                    Debug.Log("Calibrated X: " + GameInput.Instance.GetCalibratedX());
                }
            }
        }
    }
}