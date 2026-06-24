using UnityEngine;
using UnityEngine.UI;

public class LoadingCircle : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 180f;  // Degrees per second
    public bool rotateClockwise = true;
    
    [Header("Pulsing Settings")]
    public bool pulseEnabled = true;
    public float pulseSpeed = 1.5f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    
    private RectTransform rectTransform;
    private Image image;
    private float currentAngle = 0f;
    private float currentScale = 1f;
    private bool scaleIncreasing = true;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        
        // If no image, try to create one
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            // You can set a default circle sprite here if needed
        }
    }

    void Update()
    {
        // Rotate the circle
        float direction = rotateClockwise ? 1f : -1f;
        currentAngle += direction * rotationSpeed * Time.deltaTime;
        
        if (rectTransform != null)
        {
            rectTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }

        // Pulse the circle (scale up and down)
        if (pulseEnabled)
        {
            if (scaleIncreasing)
            {
                currentScale += pulseSpeed * Time.deltaTime;
                if (currentScale >= maxScale)
                {
                    scaleIncreasing = false;
                }
            }
            else
            {
                currentScale -= pulseSpeed * Time.deltaTime;
                if (currentScale <= minScale)
                {
                    scaleIncreasing = true;
                }
            }
            
            rectTransform.localScale = Vector3.one * currentScale;
        }
    }

    public void SetVisible(bool visible)
    {
        if (image != null)
        {
            image.enabled = visible;
        }
        gameObject.SetActive(visible);
    }

    public void SetColor(Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
}