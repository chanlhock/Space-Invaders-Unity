using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GameObject playerPrefab;
    public GameObject invaderPrefab;
    public Transform invaderContainer;
    public Transform bulletContainer;
    public GameObject splashScreen;
    public GameObject gameOverScreen;
    public GameObject hud;

    [Header("HUD References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI soundText;
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI wifiStatusText;
    public TextMeshProUGUI rawDataText;
    public TextMeshProUGUI buttonStatusText;
    public TextMeshProUGUI calibrationText;
    public TextMeshProUGUI packetText;

    [Header("Game Settings")]
    public int rows = 3;
    public int cols = 12;
    public float invaderSpeed = 0.002f;
    public float invaderDropAmount = 0.002f;
    public float invaderSpeedIncrease = 0.0005f;
    public float spacingX = 0.8f;  // Small spacing for invaders
    public float spacingY = 0.6f;
    public float formationWidth = 660f;
    private float screenHalfWidth = 8f;

    // Game state
    public int Score { get; private set; } = 0;
    public bool GameActive { get; private set; } = false;

    // Invader movement
    private int invaderDirection = 1;
    private float currentInvaderSpeed;
    private bool isAtEdge = false;
    private int invaderCount = 0;

    // Screen bounds - will be calculated from camera
    private float screenLeft = -8f;
    private float screenRight = 8f;
    private float screenTop = 4.5f;
    private float screenBottom = -4.5f;

    // Player
    private GameObject player;

    // FPS tracking
    private float fpsUpdateTimer = 0f;
    private float fpsUpdateInterval = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Calculate screen bounds from camera
        CalculateScreenBounds();

        // Show splash screen
        if (splashScreen != null)
            splashScreen.SetActive(true);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (hud != null)
            hud.SetActive(false);

        // Start the connection search
        StartCoroutine(SearchForJoystick());
        
    }

void CalculateScreenBounds()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            
            screenLeft = -width;
            screenRight = width;
            screenTop = height;
            screenBottom = -height;
            
            Debug.Log($"📐 Screen bounds: Left={screenLeft}, Right={screenRight}, Top={screenTop}, Bottom={screenBottom}");
        }
        else
        {
            Debug.LogWarning("⚠️ Main Camera not found! Using default bounds.");
        }
    }

    IEnumerator SearchForJoystick()
    {
        // Show loading message
        TextMeshProUGUI connectionLabel = splashScreen?.GetComponentInChildren<TextMeshProUGUI>();
        if (connectionLabel != null)
        {
            connectionLabel.text = "Searching for WiFi Joystick...\n(Please turn on your Pi Pico W)";
        }

        // Start background music on splash screen
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic();
            Debug.Log("🎵 Background music started on splash screen");
        }

        // Wait 10 seconds for connection
        float waitTime = 10f;
        float elapsed = 0f;

        while (elapsed < waitTime)
        {
            elapsed += Time.deltaTime;

            // Check if connected
            if (GameInput.Instance != null && GameInput.Instance.IsDeviceConnected())
            {
                if (connectionLabel != null)
                {
                    connectionLabel.text = "Pi Pico W WiFi Joystick Connected!\nPress SPACE to Start Game...";
                }
                Debug.Log("Pi Pico W WiFi Joystick Connected Successfully!");
                break;
            }

            yield return null;
        }

        // If not connected after timeout
        if (GameInput.Instance == null || !GameInput.Instance.IsDeviceConnected())
        {
            if (connectionLabel != null)
            {
                connectionLabel.text = "Joystick Not Found.\nPress SPACE for Keyboard mode...";
            }
            Debug.Log("Connection Timeout! Falling back to Keyboard controls.");
        }

        // Wait for SPACE key
        while (!Input.GetKeyDown(KeyCode.Space))
        {
            // Allow ESC to quit
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
            yield return null;
        }

        StartGame();
    }

    void StartGame()
    {
        if (splashScreen != null)
            splashScreen.SetActive(false);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (hud != null)
            hud.SetActive(true);

        // Music continues playing during game
        // (SoundManager music is already playing from splash screen)
        // If it stopped for any reason, resume it:
        if (SoundManager.Instance != null && !SoundManager.Instance.IsMusicPlaying())
        {
            SoundManager.Instance.ResumeMusic();
            Debug.Log("🎵 Music resumed for gameplay");
        }

        // Reset invader container position
        if (invaderContainer != null)
        {
            invaderContainer.position = Vector3.zero;
        }

        // Spawn player
        if (playerPrefab != null)
        {
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            player.name = "Player";
            player.SetActive(true);
        }

        // Reset game state
        GameActive = true;
        Score = 0;
        currentInvaderSpeed = invaderSpeed;
        invaderDirection = 1;
        isAtEdge = false;
        invaderCount = 0;

        // Update HUD
        UpdateScore();
        UpdateMode();
        UpdateSoundStatus();

        // Spawn invaders
        SpawnInvaders();
    }

    void SpawnInvaders()
    {
        if (invaderPrefab == null || invaderContainer == null)
        {
            Debug.LogError("Invader prefab or container not assigned!");
            return;
        }

        // Clear existing invaders
        foreach (Transform child in invaderContainer)
        {
            Destroy(child.gameObject);
        }

        // Calculate start position - centered
        float totalWidth = (cols - 1) * spacingX;
        float startX = -totalWidth / 2f;
        float startY = screenTop - 0.8f;  // Start near top of screen


        // Reset container position
        invaderContainer.position = Vector3.zero;
        invaderCount = 0;

        // Spawn invaders in grid
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject invader = Instantiate(invaderPrefab, invaderContainer);
                // Set local position relative to container
                invader.transform.localPosition = new Vector3(
                    startX + c * spacingX,
                    startY - r * spacingY,
                    0
                );
                invaderCount++;
            }
        }

        Debug.Log($"✅ Spawned {invaderCount} invaders in {rows}x{cols} grid");
        Debug.Log($"📐 Container position: {invaderContainer.position}");
        //invaderContainer.position = Vector3.zero;
    }

    void Update()
    {
        // Handle restart and quit globally
        if (Input.GetKeyDown(KeyCode.R) && !GameActive && gameOverScreen != null && gameOverScreen.activeSelf)
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (!GameActive || invaderContainer == null)
            return;

        // Check if there are any invaders left
        if (invaderContainer.childCount == 0)
        {
            // All invaders destroyed - don't try to move them
            return;
        }

        // Get the leftmost and rightmost invader positions
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;

        foreach (Transform child in invaderContainer)
        {
            // Use world position (not local)
            Vector3 worldPos = child.position;
            if (worldPos.x < minX) minX = worldPos.x;
            if (worldPos.x > maxX) maxX = worldPos.x;
            if (worldPos.y < minY) minY = worldPos.y;
        }

        float formationWidth = maxX - minX;
        float centerX = (minX + maxX) / 2f;

        // Screen bounds (with padding)
        float padding = 0.5f;
        float leftBound = screenLeft + padding;
        float rightBound = screenRight - padding;
        //float halfScreen = screenHalfWidth - padding;

        // Debug the positions
        if (Time.frameCount % 120 == 0)  // Log every 2 seconds
        {
            Debug.Log($"📍 CenterX: {centerX:F2}, MinX: {minX:F2}, MaxX: {maxX:F2}, LeftBound: {leftBound:F2}, RightBound: {rightBound:F2}, Direction: {invaderDirection}");
        }

        // Check if hitting right edge
        if (centerX + formationWidth / 2f > rightBound)
        {
            if (!isAtEdge)
            {
                invaderDirection = -1;
                invaderContainer.position += Vector3.down * invaderDropAmount;
                currentInvaderSpeed += invaderSpeedIncrease;
                isAtEdge = true;
                Debug.Log($"↔️ Invaders hit RIGHT edge at {centerX + formationWidth/2f:F2}. Direction: LEFT, Speed: {currentInvaderSpeed:F2}");
            }
        }
        // Check if hitting left edge
        else if (centerX - formationWidth / 2f < leftBound)
        {
            if (!isAtEdge)
            {
                invaderDirection = 1;
                invaderContainer.position += Vector3.down * invaderDropAmount;
                currentInvaderSpeed += invaderSpeedIncrease;
                isAtEdge = true;
                Debug.Log($"↔️ Invaders hit LEFT edge at {centerX - formationWidth/2f:F2}. Direction: RIGHT, Speed: {currentInvaderSpeed:F2}");
            }
        }
        else
        {
            isAtEdge = false;
        }

        // Move invaders
        invaderContainer.position += Vector3.right * invaderDirection * currentInvaderSpeed * Time.deltaTime;

        // Check if invaders reached bottom (near player)
        if (minY < screenBottom + 0.5f)
        {
            Debug.Log($"💀 Invaders reached bottom! Y: {minY:F2}");
            GameOver();
        }

        // Update HUD
        UpdateFPS();
        UpdateJoystickDisplay();
    }

    public void AddScore(int points)
    {
        Score += points;
        UpdateScore();
    }

    // Add this method to GameManager.cs
    public int GetInvaderCount()
    {
        if (invaderContainer == null) return 0;
        return invaderContainer.childCount;
    }

    public void CheckAllInvadersDestroyed()
    {
        if (invaderContainer == null) return;
    
        // Count remaining invaders
        invaderCount = invaderContainer.childCount;
        Debug.Log($"👾 Invader Count: {invaderCount}");

        if (invaderCount <= 1)
        {
            // Use Invoke to ensure it runs after all destruction is complete
            Invoke(nameof(GameWon), 0.1f);
        }
    }

    // Make sure GameWon is called only once
    private bool gameWonCalled = false;
    void GameWon()
    {
        if (gameWonCalled) return;
        gameWonCalled = true;
    
        GameActive = false;
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
            TextMeshProUGUI label = gameOverScreen.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "YOU WIN!\nFinal Score: " + Score + "\nPress R to Restart or ESC to Quit";
            }
        }
        Debug.Log("🏆 Player won! All invaders destroyed!");
    }

    public void GameOver()
    {
        GameActive = false;

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
            TextMeshProUGUI label = gameOverScreen.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "GAME OVER!\nAlas, You only scored: " + Score + "\nPress R to Restart or ESC to Quit";
            }
        }

        if (hud != null)
            hud.SetActive(false);

        SoundManager.Instance?.PlayExplosion();
        Debug.Log("🔊 Playing explosion sound");
    }

    public void RestartGame()
    {
        gameWonCalled = false;  // Reset win flag

        // Clear invaders
        if (invaderContainer != null)
        {
            foreach (Transform child in invaderContainer)
            {
                Destroy(child.gameObject);
            }
            invaderContainer.position = Vector3.zero;
        }

        // Reset player
        if (player != null)
        {
            Destroy(player);
        }

        // Music continues playing during restart
        // Ensure it's playing if it stopped
        if (SoundManager.Instance != null && !SoundManager.Instance.IsMusicPlaying())
        {
            SoundManager.Instance.ResumeMusic();
        }
        // Reset speed and direction
        currentInvaderSpeed = invaderSpeed;
        invaderDirection = 1;
        isAtEdge = false;

        GameActive = false;
        StartGame();
    }

    void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Score;
    }

    void UpdateMode()
    {
        if (modeText != null)
        {
            bool isConnected = GameInput.Instance != null && GameInput.Instance.IsDeviceConnected();
            modeText.text = isConnected ? "JOYSTICK MODE" : "KEYBOARD MODE";
        }
    }

    void UpdateSoundStatus()
    {
        if (soundText != null)
        {
            bool soundOn = SoundManager.Instance != null && SoundManager.Instance.IsSoundOn();
            soundText.text = soundOn ? "SOUND ON" : "SOUND OFF";
        }
    }

    void UpdateFPS()
    {
        fpsUpdateTimer += Time.deltaTime;
        if (fpsUpdateTimer >= fpsUpdateInterval)
        {
            fpsUpdateTimer = 0f;
            if (fpsText != null)
            {
                float fps = 1f / Time.deltaTime;
                fpsText.text = "FPS: " + Mathf.RoundToInt(fps);
            }
        }
    }

    void UpdateJoystickDisplay()
    {
        if (GameInput.Instance == null)
            return;

        bool isConnected = GameInput.Instance.IsDeviceConnected();

        // WiFi status
        if (wifiStatusText != null)
        {
            wifiStatusText.text = isConnected ? "WiFi: RECEIVING DATA" : "WiFi: NO DATA";
            wifiStatusText.color = isConnected ? Color.green : Color.red;
        }

        // Raw data
        if (rawDataText != null && isConnected)
        {
            float rawX = GameInput.Instance.GetRawX();
            float rawY = GameInput.Instance.GetRawY();
            int xPercent = Mathf.RoundToInt(rawX * 100);
            int yPercent = Mathf.RoundToInt(rawY * 100);
            rawDataText.text = $"Raw X: {Mathf.RoundToInt(rawX * 65535):D5} ({xPercent:D3}%)  Y: {Mathf.RoundToInt(rawY * 65535):D5} ({yPercent:D3}%)";
        }

        // Button status
        if (buttonStatusText != null && isConnected)
        {
            bool button = GameInput.Instance.IsButtonPressed();
            buttonStatusText.text = button ? "Button: PRESSED" : "Button: RELEASED";
            buttonStatusText.color = button ? Color.yellow : Color.white;
        }

        // Calibration
        if (calibrationText != null && isConnected)
        {
            float calX = GameInput.Instance.GetCalibratedX();
            string direction = "CENTER";
            if (calX < -10) direction = "LEFT";
            else if (calX > 10) direction = "RIGHT";
            calibrationText.text = $"Calib: {calX:+F1}%  [{direction}]";
            calibrationText.color = Mathf.Abs(calX) > 50 ? Color.red : Mathf.Abs(calX) > 20 ? Color.yellow : Color.white;
        }

        // Packet count
        if (packetText != null && isConnected)
        {
            packetText.text = "Packets: " + GameInput.Instance.GetPacketCount();
            packetText.color = Color.cyan;
        }
    }
}