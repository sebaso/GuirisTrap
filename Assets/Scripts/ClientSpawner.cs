using System.Collections.Generic;
using UnityEngine;

public class ClientSpawner : MonoBehaviour
{
    [Header("Prefab & Points")]
    public GameObject clientPrefab;
    public Transform spawnPoint;
    public Transform entrancePoint;

    [Header("Timing")]
    public float spawnInterval = 12f;
    public int maxClients = 10;

    [Header("Queue")]
    public Vector3 queueDirection = Vector3.zero;
    public float queueSpacing = 1.2f;
    private float _timer;
    private readonly List<Client> _activeClients = new();

    void Start()
    {
        _timer = spawnInterval;

        if (RestaurantManager.Instance != null && entrancePoint != null)
        {
            RestaurantManager.Instance.entrancePoint = entrancePoint;

            Vector3 dir = queueDirection.sqrMagnitude > 0.001f
                ? queueDirection.normalized
                : (spawnPoint != null
                    ? (spawnPoint.position - entrancePoint.position).normalized
                    : Vector3.back);

            RestaurantManager.Instance.queueDirection = dir;
            RestaurantManager.Instance.queueSpacing   = queueSpacing;
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawnClient();
        }

        _activeClients.RemoveAll(c => c == null);
    }

    private void TrySpawnClient()
    {
        if (_activeClients.Count >= maxClients)
        {
            Debug.Log("[ClientSpawner] Max clients reached, skipping spawn.");
            return;
        }

        if (clientPrefab == null)
        {
            Debug.LogWarning("[ClientSpawner] clientPrefab is not assigned!");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject obj = Instantiate(clientPrefab, pos, Quaternion.identity);
        Client client = obj.GetComponent<Client>();

        if (client == null)
        {
            Debug.LogError("[ClientSpawner] Client prefab has no Client component!");
            Destroy(obj);
            return;
        }

        if (entrancePoint != null)
            client.SetEntrancePoint(entrancePoint);

        _activeClients.Add(client);
        Debug.Log($"[ClientSpawner] Spawned client. Active: {_activeClients.Count}");
    }
}
