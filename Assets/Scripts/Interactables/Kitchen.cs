using UnityEngine;
using UnityEngine.SceneManagement;

public class Kitchen : MonoBehaviour
{
    [Header("Kitchen Settings")]
    public GameObject foodPrefab;
    public Transform spawnPoint;
    public float cookTime = 3f;
    public string[] availableFoods = { "Burger", "Pizza", "Salad", "Pasta" }; // o lo que fuera

    private bool isCooking = false;
    private float cookTimer = 0f;
    private Food readyFood;

    void Start()
    {
        if (spawnPoint == null)
        {
            GameObject spawnObj = new GameObject("FoodSpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = new Vector3(0, 1f, 0);
            spawnPoint = spawnObj.transform;
        }

        SpawnFood();
    }

    void Update()
    {
        if (isCooking)
        {
            cookTimer += Time.deltaTime;
            if (cookTimer >= cookTime)
            {
                FinishCooking();
            }
        }
    }
//TODO: Hacer que el jugador pueda hacer la comida
    public Food GetFood()
    {
        if (readyFood != null)
        {
            Food foodToGive = readyFood;
            readyFood = null;
            
            StartCooking();
            
            return foodToGive;
        }
        
        Debug.Log("No food ready! Still cooking.");
        return null;
    }

    private void StartCooking()
    {
        isCooking = true;
        cookTimer = 0f;
        Debug.Log("Started cooking new food.");
    }

    private void FinishCooking()
    {
        isCooking = false;
        SpawnFood();
        Debug.Log("Food is ready!");
    }

    private void SpawnFood()
    {
        GameObject foodObj;

        if (foodPrefab != null && SceneManager.GetActiveScene().name == "GameScene")
        {
            Debug.Log("Hace comida");
            foodObj = Instantiate(foodPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            foodObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foodObj.transform.position = spawnPoint.position;
            foodObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            Renderer rend = foodObj.GetComponent<Renderer>();
            rend.material.color = GetRandomFoodColor();
        }

        foodObj.name = "Food";
        
        Food food = foodObj.GetComponent<Food>();
        if (food == null)
        {
            food = foodObj.AddComponent<Food>();
        }
        
        food.foodName = availableFoods[Random.Range(0, availableFoods.Length)];
        food.price = Random.Range(5f, 25f);

        Rigidbody rb = foodObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = foodObj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;

        readyFood = food;
    }

    private Color GetRandomFoodColor()
    {
        Color[] foodColors = {
            new Color(0.8f, 0.5f, 0.2f),
            new Color(1f, 0.3f, 0.3f),
            new Color(0.4f, 0.8f, 0.3f),
            new Color(1f, 0.9f, 0.5f)
        };
        return foodColors[Random.Range(0, foodColors.Length)];
    }

    public bool IsFoodReady()
    {
        return readyFood != null;
    }

    public float GetCookingProgress()
    {
        if (!isCooking) return readyFood != null ? 1f : 0f;
        return cookTimer / cookTime;
    }
}
