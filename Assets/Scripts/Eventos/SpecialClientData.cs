using UnityEngine;

/// <summary>Cómo pide la comida un cliente especial.</summary>
public enum OrderMode
{
    Normal,     // pedido aleatorio de la carta, como cualquier cliente
    Wildcard,   // "sorpréndeme": acepta CUALQUIER plato (Poseidón)
    FixedDish,  // pide siempre el mismo plato (Guirincianos → Paella)
}

/// <summary>Qué pasa si le sirves algo con un tag prohibido.</summary>
public enum FailConsequence
{
    LeaveAngry,     // se va enfadado sin pagar (castigo suave)
    BreakFurniture, // Poseidón: destroza su mesa y sus sillas y se va sin pagar
    SpawnMess,      // deja "regalitos" por el suelo (reutiliza las gaviotas)
}

[CreateAssetMenu(fileName = "NewSpecialClient", menuName = "Clients/Special Client")]
public class SpecialClientData : ScriptableObject
{
    [Header("Identidad")]
    public string clientName = "???";
    public GameObject visualPrefab;

    public Sprite portrait;

    public Color dialogueColor = Color.white;

    [Header("Grupo")]
    [Range(1, 4)] public int groupSize = 1;

    [Header("Pedido")]
    public OrderMode orderMode = OrderMode.Normal;

    public RecipeData fixedDish;

    public RecipeData surpriseDishPlaceholder;


    public FoodTag forbiddenTags = FoodTag.None;

    [Header("Comportamiento")]

    public float patienceMultiplier = 1f;


    public int paymentOverride = 0;

    [Header("Condición de decoración (Guirincianos)")]

    public PlaceableItemData[] requiredDecoration;


    public float decorationMaxDistance = 0f;

    public int tipIfDecorMet = 0;


    [Range(0f, 1f)]
    public float unhappyPaymentMultiplier = 0.5f;

    [Header("Resolución")]
    public FailConsequence onFail = FailConsequence.LeaveAngry;

    [Header("Diálogo y avisos")]
    public bool linesAreTranslationKeys = false;

    [Tooltip("Aviso HUD cuando aparece por la puerta. Vacío = sin aviso.")]
    public string arrivalAnnouncement;

    [Tooltip("Líneas al sentarse Vacío = sin diálogo.")]
    [TextArea] public string[] entryLines;

    [Tooltip("Líneas al irse satisfecho (tras pagar).")]
    [TextArea] public string[] successLines;

    [Tooltip("Líneas al servirle un tag prohibido, justo antes de la consecuencia.")]
    [TextArea] public string[] failLines;

    [Tooltip("Líneas al largarse porque la condición de decoración falló.")]
    [TextArea] public string[] unhappyLines;

    /// <summary>¿Este cliente exige decoración concreta para irse contento?</summary>
    public bool HasDecorCondition => requiredDecoration != null && requiredDecoration.Length > 0;

    /// <summary>¿Rechaza un plato con estos tags?</summary>
    public bool RejectsTags(FoodTag tags) => (tags & forbiddenTags) != 0;
}