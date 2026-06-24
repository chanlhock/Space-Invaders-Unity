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
        Debug.Log($"GameInput: Starting UDP listener on port {listenPort}");
        Debug.Log($"GameInput: Listening on all interfaces (0.0.0.0)");
        Debug.Log($"GameInput: Make sure Pi Pico is sending to IP: {GetLocalIPAddress()}");
        Debug.Log($"GameInput: IMPORTANT - Windows Firewall may block UDP port {listenPort}");
        StartUDPListener();
    }

    string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "192.168.x.x (check your network)";
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
            // Bind to all network interfaces on the specified port
            udpServer = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
            
            // Set timeout to 100ms to allow thread to check if it should stop
            udpServer.Client.ReceiveTimeout = 100;
            
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            Debug.Log($"GameInput: UDP Server bound to port {listenPort}");

            while (threadRunning)
            {
                try
                {
                    // Check if data is available using Poll (non-blocking)
                    if (udpServer.Client.Poll(100, SelectMode.SelectRead))
                    {
                        byte[] data = udpServer.Receive(ref remoteEP);
                        string message = Encoding.UTF8.GetString(data);
                        
                        // Debug first few packets
                        if (packetCount < 5)
                        {
                            Debug.Log($"GameInput: Packet received from {remoteEP.Address}:{remoteEP.Port}");
                            Debug.Log($"GameInput: Data: {message}");
                        }
                        
                        lock (packetQueue)
                        {
                            packetQueue.Enqueue(message);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    // Timeout is expected, ignore
                    if (ex.SocketErrorCode != SocketError.TimedOut)
                    {
                        Debug.LogError($"GameInput: Socket error: {ex.Message}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"GameInput: Error receiving packet: {ex.Message}");
                }
                
                // Small sleep to prevent CPU spike
                Thread.Sleep(10);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameInput: Failed to start UDP listener: {ex.Message}");
        }
        finally
        {
            if (udpServer != null)
            {
                udpServer.Close();
                Debug.Log("GameInput: UDP server closed");
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

        // Timeout after 3 seconds
        if (deviceConnected && Time.time - lastPacketTime > 3f)
        {
            deviceConnected = false;
            Debug.Log("GameInput: Disconnected - timeout");
        }
    }

    void ProcessPacket(string dataString)
    {
        packetCount++;
        lastPacketTime = Time.time;

        try
        {
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
                    Debug.Log($"GameInput: ✅ Connected to Pi Pico W! (Packet {packetCount})");
                }

                // Debug every 20 packets
                if (packetCount % 20 == 0)
                {
                    Debug.Log($"GameInput: Packet {packetCount} - Raw X={rawJoystickX:F2}, Calibrated={calibratedX:F2}, Button={buttonPressed}");
                }
            }
            else
            {
                Debug.LogWarning($"GameInput: Invalid packet format: {dataString}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameInput: Error parsing packet: {ex.Message}");
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
    public void StopListening()
    {
        threadRunning = false;
        Debug.Log("GameInput: Stopping UDP listener...");
    
        if (udpThread != null && udpThread.IsAlive)
        {
            udpThread.Join(1000);
        }
}
    void OnApplicationQuit()
    {
        threadRunning = false;
        if (udpThread != null && udpThread.IsAlive)
        {
            udpThread.Join(1000);
        }
    }
}