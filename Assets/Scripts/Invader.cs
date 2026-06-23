using UnityEngine;

public class Invader : MonoBehaviour
{
    private bool isDestroyed = false;  // Prevent double destruction

    void Start()
    {
        // Ensure the invader has a collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Make it a trigger
        col.isTrigger = true;
        
        // Set tag to Invader
        gameObject.tag = "Invader";
        
        // Log spawn position
        Debug.Log($"👾 Invader spawned at: {transform.position}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet"))
        {
            TakeDamage();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }

    public void TakeDamage()
    {
        // Prevent double destruction
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"🎯 Invader taking damage! Remaining: {GameManager.Instance?.GetInvaderCount()}");
        
        // Add score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(10);
        }

        // Play death sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayInvaderDeath();
        }

        // Destroy this invader FIRST
        Destroy(gameObject);

        // THEN check if all invaders are destroyed
        if (GameManager.Instance != null)
        {
            // Use Invoke to check after destruction
            GameManager.Instance.CheckAllInvadersDestroyed();
        }
    }
}