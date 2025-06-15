// Full WormMovement.cs with mobile UI input support
// (Xem phần giải thích chi tiết trong phần cập nhật code)

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class WormMovement : MonoBehaviour
{
    

    public float moveStep = 0.4f;
    private bool canMove = true;
    private bool isVomiting = false;
    private bool isFalling = false;

    public GameObject bodyPrefab;
    public GameObject tailPrefab;
    public int initialBodyCount = 3;

    public LayerMask obstacleLayer;
    public LayerMask appleLayer;
    public LayerMask wallLayer;

    public GameObject vomitEffectObject;
    public GameObject eyeObject;
    public GameObject mouthObject;

    public Tilemap[] tilemaps;
    public float fallSpeed = 2f;

    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Direction> directionHistory = new List<Direction>();
    private Direction currentDirection;
    private Vector3 movementDirection;

    public Sprite bodyStraight, cornerTopRight, cornerTopLeft, cornerBottomLeft, cornerBottomRight;
    public Sprite bodyHorizontal, bodyVertical;

    private enum Direction { Up, Down, Left, Right }
    private Direction? requestedDirection = null;
    void Start()
    {
       

        currentDirection = Direction.Down;
        movementDirection = Vector3.down;
        Vector3 spawnDir = Vector3.up;
        Vector3 lastPos = transform.position;

        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);

        for (int i = 0; i < initialBodyCount; i++)
        {
            lastPos += spawnDir * moveStep;
            GameObject part = Instantiate(bodyPrefab, lastPos, Quaternion.identity);
            bodyParts.Add(part.transform);
            positionHistory.Add(lastPos);
            directionHistory.Add(currentDirection);
        }

        lastPos += spawnDir * moveStep;
        GameObject tail = Instantiate(tailPrefab, lastPos, Quaternion.identity);
        bodyParts.Add(tail.transform);
        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);

        if (vomitEffectObject != null) vomitEffectObject.SetActive(false);
        if (eyeObject != null) eyeObject.SetActive(false);
        if (mouthObject != null) mouthObject.SetActive(false);
    }

    void Update()
    {
        if (!canMove || isVomiting || isFalling) return;

        Vector3 moveDir = Vector3.zero;
        float rotationZ = 0f;
        Direction newDirection = currentDirection;

        if (requestedDirection.HasValue)
        {
            Direction inputDir = requestedDirection.Value;

            if (inputDir == Direction.Up && currentDirection != Direction.Down)
            {
                moveDir = Vector3.up; rotationZ = 180f; newDirection = Direction.Up;
            }
            else if (inputDir == Direction.Down && currentDirection != Direction.Up)
            {
                moveDir = Vector3.down; rotationZ = 0f; newDirection = Direction.Down;
            }
            else if (inputDir == Direction.Left && currentDirection != Direction.Right)
            {
                moveDir = Vector3.left; rotationZ = 270f; newDirection = Direction.Left;
            }
            else if (inputDir == Direction.Right && currentDirection != Direction.Left)
            {
                moveDir = Vector3.right; rotationZ = 90f; newDirection = Direction.Right;
            }
        }
        else
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                if (currentDirection != Direction.Down)
                {
                    moveDir = Vector3.up; rotationZ = 180f; newDirection = Direction.Up;
                }
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                if (currentDirection != Direction.Up)
                {
                    moveDir = Vector3.down; rotationZ = 0f; newDirection = Direction.Down;
                }
            }
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                if (currentDirection != Direction.Right)
                {
                    moveDir = Vector3.left; rotationZ = 270f; newDirection = Direction.Left;
                }
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                if (currentDirection != Direction.Left)
                {
                    moveDir = Vector3.right; rotationZ = 90f; newDirection = Direction.Right;
                }
            }
        }

        if (moveDir != Vector3.zero)
        {
            movementDirection = moveDir;
            requestedDirection = null;

            Vector3 newPosition = transform.position + moveDir * moveStep;
            Collider2D[] hits = Physics2D.OverlapCircleAll(newPosition, 0.2f);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("LockedWin"))
                {
                    GameManager.Instance?.TryWin(hit.gameObject);
                    return;
                }
                else if (hit.CompareTag("Apple"))
                {
                    var apple = hit.GetComponent<PushableApple>();
                    if (apple != null && apple.TryPush(moveDir, this)) return;
                }
                else if (hit.CompareTag("Banana"))
                {
                    var banana = hit.GetComponent<PushableBanana>();
                    if (banana != null && banana.TryPush(moveDir, this)) return;
                }
            }

            if (IsPathBlocked()) return;

            positionHistory.Insert(0, newPosition);
            if (positionHistory.Count > bodyParts.Count + 1)
                positionHistory.RemoveAt(positionHistory.Count - 1);

            directionHistory.Insert(0, newDirection);
            if (directionHistory.Count > bodyParts.Count + 1)
                directionHistory.RemoveAt(directionHistory.Count - 1);

            transform.position = newPosition;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            currentDirection = newDirection;

            for (int i = 0; i < bodyParts.Count; i++)
            {
                int index = i + 1;
                if (index < positionHistory.Count)
                {
                    bodyParts[i].position = positionHistory[index];
                    if (i < bodyParts.Count - 1 && i + 1 < directionHistory.Count)
                    {
                        var sr = bodyParts[i].GetComponent<SpriteRenderer>();
                        if (sr) sr.sprite = GetBodySprite(directionHistory[i + 1], directionHistory[i]);
                    }
                }
            }

            UpdateTailRotation();
            StartCoroutine(MoveDelay());
            CheckFallCondition();
        }
    }

    public void SetDirectionFromUI(string direction)
    {
        switch (direction)
        {
            case "Up": requestedDirection = Direction.Up; break;
            case "Down": requestedDirection = Direction.Down; break;
            case "Left": requestedDirection = Direction.Left; break;
            case "Right": requestedDirection = Direction.Right; break;
        }
    }

    public void Grow()
    {
        if (bodyParts.Count == 0 || positionHistory.Count == 0) return;

        Vector3 newPartPos = bodyParts[bodyParts.Count - 1].position;
        GameObject newPart = Instantiate(bodyPrefab, newPartPos, Quaternion.identity);
        bodyParts.Insert(bodyParts.Count - 1, newPart.transform);

        if (positionHistory.Count < bodyParts.Count + 1)
        {
            positionHistory.Add(newPartPos);
            directionHistory.Add(currentDirection);
        }
    }

    public void TriggerVomit(Vector3 backDirection)
    {
        StartCoroutine(VomitBack(backDirection));
    }

    IEnumerator VomitBack(Vector3 backDir)
    {
        isVomiting = true;
        canMove = false;

        if (vomitEffectObject != null) vomitEffectObject.SetActive(true);
        if (mouthObject != null) mouthObject.SetActive(true);
        if (eyeObject != null) eyeObject.SetActive(false);

        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            Vector3 offset = backDir * moveStep;
            Vector3 nextHeadPos = transform.position + offset;

            bool hitWall = Physics2D.OverlapCircle(nextHeadPos, 0.05f, wallLayer);
            if (!hitWall)
            {
                foreach (Transform part in bodyParts)
                {
                    Vector3 nextPos = part.position + offset;
                    if (Physics2D.OverlapCircle(nextPos, 0.05f, wallLayer))
                    {
                        hitWall = true;
                        break;
                    }
                }
            }

            if (hitWall) break;

            transform.position += offset;
            foreach (Transform part in bodyParts)
                part.position += offset;

            yield return new WaitForSeconds(0.1f);
        }

        if (vomitEffectObject != null) vomitEffectObject.SetActive(false);
        if (mouthObject != null) mouthObject.SetActive(false);

        // Cập nhật lại positionHistory và directionHistory sau khi nôn xong
        positionHistory.Clear();
        directionHistory.Clear();

        positionHistory.Add(transform.position);
        directionHistory.Add(currentDirection);

        foreach (Transform part in bodyParts)
        {
            positionHistory.Add(part.position);
            directionHistory.Add(currentDirection);
        }

        UpdateTailRotation();

        isVomiting = false;
        canMove = true;
    }

    bool IsPathBlocked()
    {
        Vector3 checkPosition = transform.position + movementDirection * moveStep;
        Collider2D hitObstacle = Physics2D.OverlapCircle(checkPosition, 0.05f, obstacleLayer);
        Collider2D hitWall = Physics2D.OverlapCircle(checkPosition, 0.05f, wallLayer);
        return hitObstacle != null || hitWall != null;
    }

    void UpdateTailRotation()
    {
        if (bodyParts.Count < 2) return;

        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];
        Vector3 dir = beforeTail.position - tail.position;

        float angle = -90f;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            angle = dir.x > 0 ? 180f : 0f;
        else
            angle = dir.y > 0 ? -90f : 90f;

        tail.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    IEnumerator MoveDelay()
    {
        canMove = false;
        yield return new WaitForSeconds(0.1f);
        canMove = true;
    }

    Sprite GetBodySprite(Direction prevDir, Direction currDir)
    {
        if (prevDir == currDir)
            return (currDir == Direction.Left || currDir == Direction.Right) ? bodyHorizontal : bodyVertical;

        if ((prevDir == Direction.Up && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Down))
            return cornerTopRight;
        if ((prevDir == Direction.Up && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Down))
            return cornerTopLeft;
        if ((prevDir == Direction.Down && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Up))
            return cornerBottomRight;
        if ((prevDir == Direction.Down && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Up))
            return cornerBottomLeft;

        return bodyVertical;
    }

    void CheckFallCondition()
    {
        if (tilemaps == null || tilemaps.Length == 0) return;

        bool allOut = !IsInsideAnyTilemap(transform.position);
        foreach (Transform part in bodyParts)
        {
            if (IsInsideAnyTilemap(part.position))
            {
                allOut = false;
                break;
            }
        }

        if (allOut && !isFalling)
        {
            StartCoroutine(FallRoutine());
        }
    }

    bool IsInsideAnyTilemap(Vector3 worldPos)
    {
        foreach (var tilemap in tilemaps)
        {
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            if (tilemap.HasTile(cellPos)) return true;
        }
        return false;
    }

    IEnumerator FallRoutine()
    {
        isFalling = true;
        SetAllSpriteOrder(0);

        if (vomitEffectObject != null) vomitEffectObject.SetActive(false);
        if (mouthObject != null) mouthObject.SetActive(false);
        if (eyeObject != null) eyeObject.SetActive(true);

        Vector3 fallDir = Vector3.down;
        float elapsed = 0f;

        while (elapsed < 2.5f)
        {
            transform.position += fallDir * fallSpeed * Time.deltaTime;
            foreach (var part in bodyParts)
                part.position += fallDir * fallSpeed * Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }

        GameManager.Instance?.Lose("Rơi khỏi nền sau 5s");
    }

    void SetAllSpriteOrder(int order)
    {
        string defaultLayer = "Default";
        string eyeLayer = "UI";

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerName = defaultLayer;
            sr.sortingOrder = order;
        }

        foreach (var part in bodyParts)
        {
            foreach (var sr in part.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingLayerName = defaultLayer;
                sr.sortingOrder = order;
            }

            SpriteRenderer directSR = part.GetComponent<SpriteRenderer>();
            if (directSR != null)
            {
                directSR.sortingLayerName = defaultLayer;
                directSR.sortingOrder = order;
            }
        }

        if (eyeObject != null)
        {
            foreach (var sr in eyeObject.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingLayerName = eyeLayer;
                sr.sortingOrder = order + 10;
            }
        }

        if (mouthObject != null)
        {
            foreach (var sr in mouthObject.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingLayerName = defaultLayer;
                sr.sortingOrder = order;
            }
        }

        if (vomitEffectObject != null)
        {
            foreach (var sr in vomitEffectObject.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingLayerName = defaultLayer;
                sr.sortingOrder = order;
            }
        }
    }
    public void TriggerVomitWithTimeout(Vector3 backDirection, float timeout)
    {
        StartCoroutine(VomitBackWithTimeout(backDirection, timeout));
    }

    IEnumerator VomitBackWithTimeout(Vector3 backDir, float timeout)
    {
        isVomiting = true;
        canMove = false;

        if (vomitEffectObject != null) vomitEffectObject.SetActive(true);
        if (mouthObject != null) mouthObject.SetActive(true);
        if (eyeObject != null) eyeObject.SetActive(false);

        float elapsed = 0f;

        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            Vector3 offset = backDir * moveStep;
            Vector3 nextHeadPos = transform.position + offset;

            bool hitWall = Physics2D.OverlapCircle(nextHeadPos, 0.05f, wallLayer);
            if (!hitWall)
            {
                foreach (Transform part in bodyParts)
                {
                    Vector3 nextPos = part.position + offset;
                    if (Physics2D.OverlapCircle(nextPos, 0.05f, wallLayer))
                    {
                        hitWall = true;
                        break;
                    }
                }
            }

            if (hitWall) break;

            transform.position += offset;
            foreach (Transform part in bodyParts)
                part.position += offset;

            elapsed += 0.1f;
            if (elapsed >= timeout)
            {
                GameManager.Instance?.Lose("Bị đẩy lùi quá lâu mà không chạm tường");
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (vomitEffectObject != null) vomitEffectObject.SetActive(false);
        if (mouthObject != null) mouthObject.SetActive(false);

        positionHistory.Clear();
        directionHistory.Clear();

        positionHistory.Add(transform.position);
        directionHistory.Add(currentDirection);

        foreach (Transform part in bodyParts)
        {
            positionHistory.Add(part.position);
            directionHistory.Add(currentDirection);
        }

        UpdateTailRotation();

        isVomiting = false;
        canMove = true;
    }

}
