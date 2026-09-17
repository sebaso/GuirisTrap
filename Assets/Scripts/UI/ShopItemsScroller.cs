using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
        int total = 0;
        
        for (int i = 0; i < _itemsContainer.childCount; i++)
        {
            Transform child = _itemsContainer.GetChild(i);

            if ((child.TryGetComponent<ShopItemUI>(out var shopItem) && shopItem.GetEnable()) ||
                (child.TryGetComponent<IngredientShopItemUI>(out var ingredientItem) && ingredientItem.GetEnable()))
                total += 1;
        }


        int maxStart = Mathf.Max(0, total - _visibleCount);

        _scrollbar.size = total > 0 ? (float)_visibleCount / total : 1f;
        _scrollbar.numberOfSteps = maxStart + 1;
    }

    private void RefreshVisibility(float scrollValue)
    {
        List<Transform> enabledItems = new List<Transform>();

        for (int i = 0; i < _itemsContainer.childCount; i++)
        {
            Transform child = _itemsContainer.GetChild(i);

            if ((child.TryGetComponent<ShopItemUI>(out var shopItem) && shopItem.GetEnable()) ||
                (child.TryGetComponent<IngredientShopItemUI>(out var ingredientItem) && ingredientItem.GetEnable()))
                enabledItems.Add(child);
        }

        int total = enabledItems.Count;
        int maxStart = Mathf.Max(0, total - _visibleCount);
        int startIndex = Mathf.RoundToInt(scrollValue * maxStart);

        for (int i = 0; i < total; i++)
        {
            bool visible = i >= startIndex && i < startIndex + _visibleCount;
            enabledItems[i].gameObject.SetActive(visible);
        }
    }
}