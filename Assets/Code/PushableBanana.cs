using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class PushableBanana : MonoBehaviour
{
    public LayerMask wallLayer;
    public float pushStep = 1.0f;
    private bool readyToBeEaten = false;

    public Tilemap[] tilemaps;
    public float fallSpeed = 1f;
    private bool isFalling = false;

    private Coroutine fallRoutine;

    public bool TryPush(Vector3 pushDirection, WormMovement worm)
    {
        if (readyToBeEaten) return true;

        Vector3 checkPosition = transform.position + pushDirection * pushStep;

        Collider2D wallHit = Physics2D.OverlapCircle(checkPosition, 0.5f, wallLayer);
        if (wallHit != null)
        {
            readyToBeEaten = true;

            if (worm != null)
            {
                worm.Grow(); // Rắn dài ra
            }

            Destroy(gameObject);
            return true;
        }

        Collider2D hit = Physics2D.OverlapCircle(checkPosition, 0.1f);
        if (hit == null || hit.CompareTag("Banana"))
        {
            transform.position = checkPosition;
            CheckFallCondition();
        }

        return false;
    }

    void CheckFallCondition()
    {
        if (tilemaps == null || tilemaps.Length == 0 || isFalling) return;

        bool isOnGround = false;
        foreach (var tilemap in tilemaps)
        {
            if (tilemap == null) continue; // ✅ tránh null reference

            Vector3Int cell = tilemap.WorldToCell(transform.position);
            if (tilemap.HasTile(cell))
            {
                isOnGround = true;
                break;
            }
        }

        if (!isOnGround)
        {
            fallRoutine = StartCoroutine(FallRoutine());
        }
    }


    IEnumerator FallRoutine()
    {
        isFalling = true;
        SetSpriteLayerOrder(0);

        float elapsed = 0f;
        Vector3 fallDir = Vector3.down;

        while (elapsed < 0.2f)
        {
            transform.position += fallDir * fallSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        GameManager.Instance?.Lose("Banana bị rơi khỏi nền");
        Destroy(gameObject);
    }

    void SetSpriteLayerOrder(int order)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = order;
    }
}
