using UnityEngine;

public class RotateBillboardUI : MonoBehaviour
{
    [SerializeField]
    private Vector2 _screenOffset = new Vector2(0f, 80f);
    [SerializeField]
    private Camera _mainCamera;

    private RectTransform _rectTransform;
    private Transform _target;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Hide();
    }

    void LateUpdate()
    {
        if (!gameObject.activeSelf || _target == null || _mainCamera == null) return;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(_target.position);

        if (screenPos.z < 0f)
        {
            _rectTransform.position = new Vector3(-9999f, -9999f, 0f);
            return;
        }

        screenPos.x += _screenOffset.x;
        screenPos.y += _screenOffset.y;

        _rectTransform.position = screenPos;
    }

    public void Show(Transform target)
    {
        _target = target;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }
}