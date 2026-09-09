using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class ChiringuitoUpgradeManager : MonoBehaviour
{
    public static event System.Action<int> OnVenueUpgraded;

    [SerializeField] private List<ChiringuitoTierData> _tiers = new();
    private int _currentTier = 0;

    void Start()
    {
        _currentTier = SaveManager.Instance != null ? SaveManager.Instance.ChiringuitoTier : 0;
        ApplyTier(_currentTier, notify: false);
    }

    public bool TryUpgrade()
    {
        int next = _currentTier + 1;
        if (next >= _tiers.Count)
        {
            Debug.Log("[VenueUpgradeManager] El chiringuito ya está en el tier máximo.");
            return false;
        }

        _currentTier = next;
        ApplyTier(_currentTier, notify: true);

        if (SaveManager.Instance != null) SaveManager.Instance.ChiringuitoTier = _currentTier;
        return true;
    }

    private void ApplyTier(int tierIndex, bool notify)
    {
        for (int i = 0; i < _tiers.Count; i++)
            if (_tiers[i].tierRoot != null) _tiers[i].tierRoot.SetActive(i == tierIndex);

        var allButtons = new HashSet<GameObject>();
        foreach (var t in _tiers) foreach (var b in t.cameraViewButtons) if (b != null) allButtons.Add(b);
        foreach (var b in allButtons)
            b.SetActive(_tiers[tierIndex].cameraViewButtons.Contains(b));

        if (notify) OnVenueUpgraded?.Invoke(tierIndex);
    }

    [ContextMenu("Debug: Mejorar chiringuito")]
    private void DebugUpgrade() => TryUpgrade();
}