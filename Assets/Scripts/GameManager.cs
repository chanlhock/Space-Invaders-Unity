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
    public float invaderSpeed = 100f;
    public float invaderDropAmount = 10f;
    public float invaderSpeedIncrease = 8f;
    public float formationWidth = 720f;

    // Game state
    public int Score { get; private set; } = 0;
    public bool GameActive { get; private set; } = false;

    // Invader movement
    private int invaderDirection = 1;
    private float currentInvaderSpeed;
    private bool isAtEdge = false;
    private int invaderCount = 0;

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

    IEnumerator SearchForJoystick()
    {
        // Show loading message
        TextMeshProUGUI connectionLabel = splashScreen?.GetComponentInChildren<TextMeshProUGUI>();
        if (connectionLabel != null)
        {
            connectionLabel.text = "Searching for WiFi Joystick...\n(Please turn on your Pi Pico W)";
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
        invaderCount = rows * cols;

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

        float startX = -((cols - 1) * 60f) / 2f;
        float startY = 3f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject invader = Instantiate(invaderPrefab, invaderContainer);
                invader.transform.localPosition = new Vector3(
                    startX + c * 60f,
                    startY - r * 50f,
                    0
                );
                invaderCount++;
            }
        }

        invaderContainer.position = Vector3.zero;
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

        if (!GameActive)
            return;

        // 1. Move invaders
        invaderContainer.position += Vector3.right * invaderDirection * currentInvaderSpeed * Time.deltaTime;

        // Check right edge
        if (invaderContainer.position.x + formationWidth / 2f > 8f)
        {
            if (!isAtEdge)
            {
                invaderDirection = -1;
                invaderContainer.position += Vector3.down * invaderDropAmount;
                currentInvaderSpeed += invaderSpeedIncrease;
                isAtEdge = true;
            }
        }
        // Check left edge
        else if (invaderContainer.position.x - formationWidth / 2f < -8f)
        {
            if (!isAtEdge)
            {
                invaderDirection = 1;
                invaderContainer.position += Vector3.down * invaderDropAmount;
                currentInvaderSpeed += invaderSpeedIncrease;
                isAtEdge = true;
            }
        }
        else
        {
            isAtEdge = false;
        }

        // Check if invaders reached bottom
        if (invaderContainer.position.y < -4f)
        {
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

    public void CheckAllInvadersDestroyed()
    {
        invaderCount = invaderContainer.childCount;
        Debug.Log("Invader Count: " + invaderCount);

        if (invaderCount == 0)
        {
            GameWon();
        }
    }

    void GameWon()
    {
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
        // Clear invaders
        foreach (Transform child in invaderContainer)
        {
            Destroy(child.gameObject);
        }

        // Reset player
        if (player != null)
        {
            Destroy(player);
        }

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