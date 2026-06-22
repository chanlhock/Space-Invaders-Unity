using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class GameInput : MonoBehaviour
{
    // Singleton instance
    public static GameInput Instance { get; private set; }

    // UDP Settings
    [Header("UDP Settings")]
    public int listenPort = 9000;
    public string listenIP = "0.0.0.0";

    // Joystick values (0-1 range)
    private float rawJoystickX = 0.5f;
    private float rawJoystickY = 0.5f;

    // Calibrated values (-100 to 100)
    private float calibratedX = 0f;
    private float calibratedY = 0f;

    // Button state
    private bool buttonPressed = false;
    private bool deviceConnected = false;
    private int packetCount = 0;
    private float lastPacketTime = 0f;

    // Calibration parameters
    [Header("Calibration")]
    public float xCenter = 0.77f;
    public float xMin = 0.0f;
    public float xMax = 1.0f;
    public float deadzone = 0.08f;

    // Threading
    private Thread udpThread;
    private volatile bool threadRunning = true;
    private Queue<string> packetQueue = new Queue<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("GameInput: Starting UDP listener on port " + listenPort);
        StartUDPListener();
    }

    void StartUDPListener()
    {
        udpThread = new Thread(UDPListenerThread);
        udpThread.IsBackground = true;
        udpThread.Start();
    }

    void UDPListenerThread()
    {
        UdpClient udpServer = null;
        try
        {
            udpServer = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (threadRunning)
            {
                try
                {
                    byte[] data = udpServer.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    lock (packetQueue)
                    {
                        packetQueue.Enqueue(message);
                    }
                }
                catch (SocketException ex)
                {
                    Debug.LogError("UDP error: " + ex.Message);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to start UDP listener: " + ex.Message);
        }
        finally
        {
            if (udpServer != null)
            {
                udpServer.Close();
            }
        }
    }

    void Update()
    {
        // Process queued packets
        lock (packetQueue)
        {
            while (packetQueue.Count > 0)
            {
                string packet = packetQueue.Dequeue();
                ProcessPacket(packet);
            }
        }

        // Timeout after 2 seconds
        if (deviceConnected && Time.time - lastPacketTime > 2f)
        {
            deviceConnected = false;
            Debug.Log("GameInput: Disconnected - timeout");
        }
    }

    void ProcessPacket(string dataString)
    {
        packetCount++;
        lastPacketTime = Time.time;

        // Parse CSV: x_val,y_val,x_percent,y_percent,button
        string[] parts = dataString.Split(',');
        if (parts.Length >= 5)
        {
            float xPercent = float.Parse(parts[2]) / 100f;
            float yPercent = float.Parse(parts[3]) / 100f;
            int buttonVal = int.Parse(parts[4]);

            rawJoystickX = xPercent;
            rawJoystickY = yPercent;

            calibratedX = CalibrateJoystick(xPercent);
            calibratedY = CalibrateJoystick(yPercent);

            buttonPressed = (buttonVal == 1);

            if (!deviceConnected)
            {
                deviceConnected = true;
                Debug.Log("GameInput: ✅ Connected to Pi Pico W! (Packet " + packetCount + ")");
            }

            // Debug occasionally
            if (packetCount % 10 == 0)
            {
                string direction = "CENTER";
                if (calibratedX < -10) direction = "LEFT";
                else if (calibratedX > 10) direction = "RIGHT";
                // Debug.Log("DEBUG - Raw X=" + xPercent + " | Calibrated=" + calibratedX + " | " + direction);
            }
        }
    }

    float CalibrateJoystick(float rawValue)
    {
        if (Mathf.Abs(rawValue - xCenter) < deadzone)
            return 0;

        if (rawValue < xCenter)
        {
            if (rawValue <= xMin)
                return -100;
            return -((xCenter - rawValue) / (xCenter - xMin)) * 100;
        }
        else
        {
            if (rawValue >= xMax)
                return 100;
            return ((rawValue - xCenter) / (xMax - xCenter)) * 100;
        }
    }

    // Public API
    public float GetRawX() => rawJoystickX;
    public float GetRawY() => rawJoystickY;
    public float GetCalibratedX() => calibratedX;
    public float GetCalibratedY() => calibratedY;
    public float GetJoystickX() => calibratedX / 100f;
    public float GetJoystickY() => calibratedY / 100f;
    public bool IsButtonPressed() => buttonPressed;
    public bool IsDeviceConnected() => deviceConnected;
    public int GetPacketCount() => packetCount;
    public Vector2 GetMovementVector() => new Vector2(GetJoystickX(), -GetJoystickY());

    void OnDestroy()
    {
        threadRunning = false;
        if (udpThread != null && udpThread.IsAlive)
        {
            udpThread.Join(1000);
        }
    }
}