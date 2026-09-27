using UnityEngine;

/// <summary>
/// Yellow pickup cube — destroys itself when the player walks into it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    bool picked;

    void OnTriggerEnter(Collider other)
    {
        if (picked || !other.CompareTag("Player"))
            return;

        picked = true;
        GameManager.Instance?.OnCollectiblePicked();
        Destroy(gameObject);
    }
}
