# Pico_W_Joystick_UDP.py
# Direct UDP to Unity - No bridge required!

import socket
import network
import struct
import time
import json
from machine import Pin, ADC, I2C
from ssd1306 import SSD1306_I2C

# WiFi Configuration - UPDATE THESE
WIFI_SSID = "YOUR_WIFI_SSID"
WIFI_PASSWORD = "YOUR_WIFI_PASSWORD"

# Unity UDP Configuration
UNITY_IP = "192.168.0.15"  # Replace with your computer/Unity IP address
UNITY_PORT = 9000           # Port Unity is listening on

# Initialize OLED
try:
    i2c = I2C(0, scl=Pin(1), sda=Pin(0), freq=400000)
    oled = SSD1306_I2C(128, 32, i2c)
    oled_available = True
except:
    oled_available = False
    print("OLED not available - continuing without display")

# Initialize joystick pins
x_axis = ADC(Pin(27))
y_axis = ADC(Pin(26))
button = Pin(13, Pin.IN, Pin.PULL_UP)

# Connection state
wifi_connected = False
udp_socket = None

def update_display(line1, line2="", line3="", line4=""):
    """Update OLED display if available"""
    if oled_available:
        try:
            oled.fill(0)
            oled.text(line1, 0, 0)
            if line2:
                oled.text(line2, 0, 8)
            if line3:
                oled.text(line3, 0, 16)
            if line4:
                oled.text(line4, 0, 24)
            oled.show()
        except:
            pass

def connect_wifi():
    """Connect to WiFi network"""
    global wifi_connected
    
    print(f"Connecting to WiFi: {WIFI_SSID}")
    update_display("Connecting WiFi", WIFI_SSID)
    
    wlan = network.WLAN(network.STA_IF)
    wlan.active(True)
    
    if not wlan.isconnected():
        wlan.connect(WIFI_SSID, WIFI_PASSWORD)
        
        # Wait for connection with timeout
        timeout = 15
        while not wlan.isconnected() and timeout > 0:
            time.sleep(1)
            timeout -= 1
            print(f"Connecting... {timeout}s left")
            update_display("Connecting...", f"{timeout}s left")
    
    if wlan.isconnected():
        wifi_connected = True
        ip_address = wlan.ifconfig()[0]
        print(f"✓ WiFi connected! IP: {ip_address}")
        update_display("WiFi Connected!", f"IP: {ip_address}")
        return wlan, ip_address
    else:
        print("✗ WiFi connection failed!")
        update_display("WiFi Failed!", "Check credentials")
        return None, None

def init_udp_socket():
    """Create UDP socket for sending data"""
    global udp_socket
    
    try:
        udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        udp_socket.settimeout(0.1)
        print("✓ UDP socket created")
        return True
    except Exception as e:
        print(f"✗ UDP socket creation failed: {e}")
        return False

def send_to_godot(sock, target_ip, target_port, x_val, y_val, button_val):
    """Send joystick data directly to Unity"""
    try:
        # Calculate percentages for Unity
        x_percent = (x_val * 100) // 65535
        y_percent = (y_val * 100) // 65535
        
        # Normalize to -1.0 to 1.0 range for easier game controls
        x_normalized = (x_percent / 50.0) - 1.0
        y_normalized = (y_percent / 50.0) - 1.0
        
        # Button state: 1 = pressed, 0 = released (since button pulls low when pressed)
        button_pressed = 1 if button_val == 0 else 0
        
        # Option 1: Simple CSV format (easiest for Godot to parse)
        csv_data = f"{x_val},{y_val},{x_percent},{y_percent},{button_pressed}\n"
        
        # Option 2: JSON format (more structured but larger)
        json_data = {
            "x": x_val,
            "y": y_val,
            "x_percent": x_percent,
            "y_percent": y_percent,
            "x_norm": x_normalized,
            "y_norm": y_normalized,
            "button": button_pressed,
            "timestamp": time.ticks_ms()
        }
        
        # Send both formats (Godot can choose which to use)
        # Send CSV (primary)
        sock.sendto(csv_data.encode('utf-8'), (target_ip, target_port))
        
        # Optional: Send JSON to a different port
        # json_port = target_port + 1
        # sock.sendto(json.dumps(json_data).encode('utf-8'), (target_ip, json_port))
        
        return True
        
    except Exception as e:
        print(f"Send error: {e}")
        return False

# Main program
print("\n" + "="*50)
print("PICO W JOYSTICK - DIRECT TO UNITY")
print("="*50)

# Connect to WiFi
wlan, ip_address = connect_wifi()

if not wifi_connected:
    print("Cannot continue without WiFi connection")
    update_display("WiFi Failed!", "Restart device")
    while True:
        time.sleep(1)
else:
    # Initialize UDP socket
    if init_udp_socket():
        print(f"Sending directly to Unity at {UNITY_IP}:{UNITY_PORT}")
        update_display("Direct to Unity", f"→ {UNITY_IP}", f"Port: {UNITY_PORT}", "Move joystick")
        
        # Main loop
        last_values = (32768, 32768, 1)
        send_counter = 0
        last_send_time = time.ticks_ms()
        error_count = 0
        
        print("\n✓ Ready! Sending joystick data directly to Unity...")
        print("Press Ctrl+C to stop\n")
        
        while True:
            try:
                # Read current values
                x_val = x_axis.read_u16()
                y_val = y_axis.read_u16()
                button_val = button.value()
                
                current_time = time.ticks_ms()
                
                # Check if values changed significantly
                x_changed = abs(x_val - last_values[0]) > 50
                y_changed = abs(y_val - last_values[1]) > 50
                button_changed = button_val != last_values[2]
                
                # Send at least every 200ms to keep connection alive
                time_since_last = time.ticks_diff(current_time, last_send_time)
                
                # Send UDP packet when values change or periodically
                if wifi_connected and udp_socket and (x_changed or y_changed or button_changed or time_since_last > 200):
                    success = send_to_godot(udp_socket, GODOT_IP, GODOT_PORT, x_val, y_val, button_val)
                    
                    if success:
                        last_send_time = current_time
                        send_counter += 1
                        
                        # Update display periodically
                        if send_counter % 5 == 0:
                            x_percent = (x_val * 100) // 65535
                            y_percent = (y_val * 100) // 65535
                            btn_text = "Pressed" if button_val == 0 else "Released"
                            
                            update_display(
                                f"Sent: #{send_counter}",
                                f"X:{x_percent:3d}% ({x_val})",
                                f"Y:{y_percent:3d}% ({y_val})",
                                f"Btn:{btn_text}"
                            )
                            
                            # Print to console for debugging
                            print(f"[{send_counter}] X={x_percent:3d}% ({x_val:5d}), Y={y_percent:3d}% ({y_val:5d}), Button={btn_text}")
                    else:
                        error_count += 1
                        if error_count % 10 == 1:
                            print(f"⚠ UDP error count: {error_count}")
                
                # Update last values
                last_values = (x_val, y_val, button_val)
                
                # Small delay (50Hz update rate)
                time.sleep_ms(20)
                
            except KeyboardInterrupt:
                print("\n\n🛑 Stopped by user")
                break
            except Exception as e:
                print(f"❌ Main loop error: {e}")
                time.sleep_ms(500)
        
        # Cleanup
        if udp_socket:
            udp_socket.close()
        
        wlan.active(False)
        print("WiFi deactivated")
        update_display("Program", "Stopped")
    else:
        print("Failed to initialize UDP")
        update_display("UDP Failed!", "Restart device")                                                                            