using UnityEngine;
using UnityEngine.EventSystems;
using Coffee.UIEffects; 

[RequireComponent(typeof(UIEffect))]
public class ButtonHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UIEffect _uiEffect;

    [Header("Ãè±ßÑÕÉ«")]
    public Color hoverColor = Color.white;

    [Header("Ãè±ß¾àÀë")]
    public float hoverDistance = 2f;

    private Color _originalColor;
    private Vector2 _originalDistance; 

    void Awake()
    {
        _uiEffect = GetComponent<UIEffect>();

        // ¼ÇÂ¼Ô­Ê¼×´Ì¬
        _originalColor = _uiEffect.shadowColor;
        _originalDistance = _uiEffect.shadowDistance; 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _uiEffect.shadowColor = hoverColor;
        _uiEffect.shadowDistance = new Vector2(hoverDistance, hoverDistance);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _uiEffect.shadowColor = _originalColor;
        _uiEffect.shadowDistance = _originalDistance;
    }
}