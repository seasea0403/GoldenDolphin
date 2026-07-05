using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingButtonDOTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image normalImage;      
    public Image hoverImage;       
    public float heightAdd = 15f; 
    public float dur = 0.2f;      

    private RectTransform rt;
    private float origH;           
    private Vector2 origSize;     

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        origH = rt.sizeDelta.y;
        origSize = rt.sizeDelta;

        if (normalImage != null) normalImage.gameObject.SetActive(true);
        if (hoverImage != null) hoverImage.gameObject.SetActive(false);

        if (normalImage != null) normalImage.color = Color.white;
        if (hoverImage != null) hoverImage.color = Color.white;
    }

    public void OnPointerEnter(PointerEventData data)
    {
        ActivateHover(true);
    }

    public void OnPointerExit(PointerEventData data)
    {
        ActivateHover(false);
    }

    public void OnPointerDown(PointerEventData data) { ActivateHover(true); }
    public void OnPointerUp(PointerEventData data) { ActivateHover(false); }

    void ActivateHover(bool isOn)
    {
        if (hoverImage != null) hoverImage.gameObject.SetActive(isOn);
        if (normalImage != null) normalImage.gameObject.SetActive(!isOn);

        float targetH = isOn ? origH + heightAdd : origH;

        if (Mathf.Abs(rt.sizeDelta.y - targetH) > 0.1f)
        {
            rt.DOSizeDelta(new Vector2(rt.sizeDelta.x, targetH), dur).SetEase(Ease.OutCubic);
        }
    }
}