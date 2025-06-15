using UnityEngine;
using DG.Tweening;

public class ItemEffect : MonoBehaviour
{
    public Transform shadowTransform;       // GameObject bóng (UI hoặc Sprite)
    public float floatDistance = 10f;       // Chiều cao lơ lửng
    public float floatDuration = 1.5f;      // Thời gian lên/xuống
    public float shadowMinScale = 0.7f;     // Bóng nhỏ nhất khi lên cao
    public float shadowMaxScale = 1.0f;     // Bóng to khi xuống thấp
    public Ease floatEase = Ease.InOutSine;

    private Vector3 originalItemPos;
    private Vector3 originalShadowScale;

    void Start()
    {
        originalItemPos = transform.localPosition;
        originalShadowScale = shadowTransform.localScale;

        // Tween lơ lửng item lên xuống
        transform.DOLocalMoveY(originalItemPos.y + floatDistance, floatDuration)
            .SetEase(floatEase)
            .SetLoops(-1, LoopType.Yoyo);

        // Tween scale bóng co giãn ngược lại
        shadowTransform.DOScale(originalShadowScale * shadowMinScale, floatDuration)
            .SetEase(floatEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnDisable()
    {
        transform.DOKill();
        shadowTransform.DOKill();
    }
}
