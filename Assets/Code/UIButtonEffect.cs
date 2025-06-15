using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.1f;           // Tỷ lệ phóng to khi hover
    public float scaleDuration = 0.2f;        // Thời gian tween

    private Vector3 originalScale;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOScale(originalScale * hoverScale, scaleDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOScale(originalScale, scaleDuration).SetEase(Ease.InBack);
    }
}
