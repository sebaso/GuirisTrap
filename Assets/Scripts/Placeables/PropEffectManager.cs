using System.Collections.Generic;
using UnityEngine;


public class PropEffectManager : MonoBehaviour
{
    public static PropEffectManager Instance { get; private set; }

    [SerializeField] private PropEffectCatalogue _catalogue;

    [Header("Ritmo")]
    [SerializeField] private float _recountInterval = 2f;

    [SerializeField] private float _clientScanInterval = 0.25f;

    /// <summary>Bonus actual de paciencia. 0.2 = +20%.</summary>
    public float PatienceBonus { get; private set; }

    /// <summary>Bonus actual de propina. 0.15 = +15%.</summary>
    public float TipBonus { get; private set; }

    [Header("Aviso al jugador")]
    [SerializeField] private bool _announceChanges = true;

    private float _recountTimer;
    private float _scanTimer;
    private bool _firstCountDone;
    private float _lastAnnouncedPatience = -1f;
    private float _lastAnnouncedTip = -1f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start() => Recount();

    void Update()
    {
        _recountTimer += Time.deltaTime;
        if (_recountTimer >= _recountInterval)
        {
            _recountTimer = 0f;
            Recount();
        }

        _scanTimer += Time.deltaTime;
        if (_scanTimer >= _clientScanInterval)
        {
            _scanTimer = 0f;
            ApplyToNewClients();
        }
    }


    public void Recount()
    {
        PatienceBonus = 0f;
        TipBonus = 0f;

        if (_catalogue == null) return;

        // Cuántas unidades hay colocadas de cada item.
        var counts = new Dictionary<PlaceableItemData, int>();

        foreach (PlaceableObject p in FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None))
        {
            if (p == null) continue;

            PlaceableItemData data = p.GetItemData();
            if (data == null) continue; 

            counts.TryGetValue(data, out int n);
            counts[data] = n + 1;
        }

        foreach (var kv in counts)
        {
            PropEffectCatalogue.Entry e = _catalogue.Find(kv.Key);
            if (e == null) continue;

            int units = kv.Value;
            if (e.maxUnitsCounted > 0) units = Mathf.Min(units, e.maxUnitsCounted);

            PatienceBonus += e.patiencePerUnit * units;
            TipBonus      += e.tipPerUnit * units;
        }

        PatienceBonus = Mathf.Min(PatienceBonus, _catalogue.maxPatienceBonus);
        TipBonus      = Mathf.Min(TipBonus, _catalogue.maxTipBonus);

        AnnounceIfChanged();
    }

    private void AnnounceIfChanged()
    {
        if (!_announceChanges) return;

        if (!_firstCountDone)
        {
            _firstCountDone = true;
            _lastAnnouncedPatience = PatienceBonus;
            _lastAnnouncedTip = TipBonus;
            return;
        }

        bool changed = !Mathf.Approximately(PatienceBonus, _lastAnnouncedPatience)
                    || !Mathf.Approximately(TipBonus, _lastAnnouncedTip);
        if (!changed) return;

        bool better = PatienceBonus > _lastAnnouncedPatience || TipBonus > _lastAnnouncedTip;

        _lastAnnouncedPatience = PatienceBonus;
        _lastAnnouncedTip = TipBonus;

        string msg = BonusSummary();

        if (better) HUDMessage.Instance?.ShowGood($"Tu decoración: {msg}");
        else        HUDMessage.Instance?.ShowWarning($"Has perdido decoración: {msg}");
    }

    /// <summary>Los bonus actuales en texto, para el HUD o para el informe.</summary>
    public string BonusSummary()
    {
        if (PatienceBonus <= 0f && TipBonus <= 0f) return "sin bonus";

        var parts = new List<string>();
        if (TipBonus > 0f)      parts.Add($"+{TipBonus:P0} propina");
        if (PatienceBonus > 0f) parts.Add($"+{PatienceBonus:P0} paciencia");
        return string.Join(" · ", parts);
    }


    private void ApplyToNewClients()
    {
        if (PatienceBonus <= 0f && TipBonus <= 0f) return;

        foreach (Client c in FindObjectsByType<Client>(FindObjectsSortMode.None))
        {
            if (c == null || c.GetComponent<PropBonusTag>() != null) continue;

            // Solo a los que aún no se han sentado: una vez sentados su
            // paciencia ya está corriendo y tocar maxPatience no haría nada.
            if (c.CurrentState != Client.State.WalkingToEntrance &&
                c.CurrentState != Client.State.Waiting &&
                c.CurrentState != Client.State.WalkingToTable)
                continue;

            c.gameObject.AddComponent<PropBonusTag>();

            if (PatienceBonus > 0f)
            {
                c.maxPatience      *= 1f + PatienceBonus;
                c.maxQueuePatience *= 1f + PatienceBonus;
            }

            if (TipBonus > 0f)
                c.money = Mathf.RoundToInt(c.money * (1f + TipBonus));
        }
    }

    [ContextMenu("DEBUG: recontar y mostrar bonus")]
    private void DebugRecount()
    {
        Recount();
        Debug.Log($"[PropEffectManager] Paciencia +{PatienceBonus:P0} · Propina +{TipBonus:P0}");
    }
}
public class PropBonusTag : MonoBehaviour { }
