using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 500f;
    public int damage = 1;

    void Start()
    {
        Debug.Log("🔫 Bullet spawned at position: " + transform.position);
    }

    void Update()
    {
        // Move upward
        transform.Translate(Vector2.up * speed * Time.deltaTime);

        // Remove if off-screen
        if (transform.position.y > 10f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Invader"))
        {
            Debug.Log("✓ Hit an invader!");

            // Play invader death sound
            SoundManager.Instance?.PlayInvaderDeath();

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
            Debug.Log("⚠️ Hit something else: " + other.gameObject.name);
        }
    }
}