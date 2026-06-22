using UnityEngine;

public class Invader : MonoBehaviour
{
    void Start()
    {
        // Not needed for collision detection in Unity
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
            // Game Over
            GameManager.Instance?.GameOver();
        }
    }

    public void TakeDamage()
    {
        // Add score
        GameManager.Instance?.AddScore(10);

        // Play death sound
        SoundManager.Instance?.PlayInvaderDeath();

        // Destroy this invader
        Destroy(gameObject);

        // Check if all invaders are destroyed
        GameManager.Instance?.CheckAllInvadersDestroyed();
    }
}