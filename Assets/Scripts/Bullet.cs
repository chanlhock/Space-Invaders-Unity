using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 8f;  // Reduced from 80f to 8f (much slower)
    public int damage = 1;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        Debug.Log($"🔫 Bullet spawned at: {transform.position}");
        
        // Ensure bullet has a collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        // Set tag and layer
        gameObject.tag = "Bullet";
        
        // Try to set layer
        int bulletLayer = LayerMask.NameToLayer("Bullet");
        if (bulletLayer == -1)
        {
            Debug.LogWarning("⚠️ 'Bullet' layer not found. Please create it in Project Settings -> Tags and Layers");
        }
        else
        {
            gameObject.layer = bulletLayer;
        }

        // Make sure bullet has a visible sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // Create a simple rectangle sprite for the bullet
            CreateBulletSprite();
        }
        else if (spriteRenderer.sprite == null)
        {
            CreateBulletSprite();
        }
    }

    void CreateBulletSprite()
    {
        // Create a simple white rectangle as bullet
        Texture2D texture = new Texture2D(8, 32);
        texture.filterMode = FilterMode.Point;
        
        Color[] pixels = new Color[8 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            // Make a vertical line
            int x = i % 8;
            int y = i / 8;
            if (x > 1 && x < 6) // Center 4 pixels wide
            {
                pixels[i] = Color.white;
            }
            else
            {
                pixels[i] = Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 32), new Vector2(0.5f, 0.5f), 100);
        
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.green; // Green laser color
    }

    void Update()
    {
        // Move upward
        transform.Translate(Vector2.up * speed * Time.deltaTime);

        // Remove if off-screen (with some margin)
        if (transform.position.y > 10f || transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions with Player
        if (other.CompareTag("Player"))
        {
            return;
        }
        
        Debug.Log($"💥 Bullet hit: {other.gameObject.name} with tag: {other.tag}");
        
        if (other.CompareTag("Invader"))
        {
            // Play invader death sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayInvaderDeath();
            }

            // Tell invader to take damage
            Invader invader = other.GetComponent<Invader>();
            if (invader != null)
            {
                invader.TakeDamage();
            }

            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"⚠️ Bullet hit something else: {other.gameObject.name}");
        }
    }
}