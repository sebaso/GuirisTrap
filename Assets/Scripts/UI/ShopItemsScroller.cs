using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItemsScroller : MonoBehaviour, IScrollHandler
{
    [SerializeField] 
    private Scrollbar _scrollbar;
    [SerializeField] 
    private Transform _itemsContainer;
    [SerializeField] 
    private int _visibleCount = 3;
    [SerializeField]
    private float _scrollSensitivity = 0.15f;

    void OnEnable()
    {
        SetupScrollbar();
        _scrollbar.onValueChanged.AddListener(RefreshVisibility);
        RefreshVisibility(_scrollbar.value);
    }

    void OnDisable()
    {
        _scrollbar.onValueChanged.RemoveListener(RefreshVisibility);
    }

    public void OnScroll(PointerEventData eventData)
    {
        float delta = -eventData.scrollDelta.y * _scrollSensitivity;
        _scrollbar.value = Mathf.Clamp01(_scrollbar.value + delta);
    }

    private void SetupScrollbar()
    {
        int total = _itemsContainer.childCount;
        int maxStart = Mathf.Max(0, total - _visibleCount);

        _scrollbar.size = total > 0 ? (float)_visibleCount / total : 1f;
        _scrollbar.numberOfSteps = maxStart + 1;
    }

    private void RefreshVisibility(float scrollValue)
    {
        int total = _itemsContainer.childCount;
        int maxStart = Mathf.Max(0, total - _visibleCount);

        int startIndex = Mathf.RoundToInt(scrollValue * maxStart);

        for (int i = 0; i < total; i++)
        {
            bool visible = i >= startIndex && i < startIndex + _visibleCount;
            _itemsContainer.GetChild(i).gameObject.SetActive(visible);
        }
    }
}