using UnityEngine;



[CreateAssetMenu(fileName = "PropEffectCatalogue", menuName = "Props/Prop Effect Catalogue")]
public class PropEffectCatalogue : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public PlaceableItemData item;

        [Range(0f, 0.5f)] public float patiencePerUnit;

        [Range(0f, 0.5f)] public float tipPerUnit;

        [Tooltip("Cuántas unidades cuentan como máximo. ")]
        public int maxUnitsCounted = 3;
    }

    public Entry[] entries;

    [Header("Topes globales")]
    [Range(0f, 2f)] public float maxPatienceBonus = 0.5f;

    [Range(0f, 2f)] public float maxTipBonus = 0.5f;

    public Entry Find(PlaceableItemData item)
    {
        if (item == null || entries == null) return null;

        foreach (Entry e in entries)
            if (e != null && e.item == item) return e;
        return null;
    }
}
