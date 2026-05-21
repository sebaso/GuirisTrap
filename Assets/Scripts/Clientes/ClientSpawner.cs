using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [Header("Group Spawning")]
    [Tooltip("Probability weights for group sizes [1, 2, 3, 4]")]
    public float[] groupSizeWeights = { 10f, 40f, 35f, 15f };
    public float groupMemberSpawnOffset = 0.3f;

    private float _timer;
    private readonly List<Client> _activeClients = new();
    private readonly List<ClientGroup> _activeGroups = new();

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
            RestaurantManager.Instance.queueSpacing = queueSpacing;
        }

        if (groupSizeWeights.Length != 4)
        {
            Debug.LogWarning("[ClientSpawner] Group size weights should have 4 values. Using defaults.");
            groupSizeWeights = new float[] { 10f, 40f, 35f, 15f };
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawnGroup();
        }

        _activeClients.RemoveAll(c => c == null);
        _activeGroups.RemoveAll(g => !g.IsValid || g.Members.Count == 0);
    }

    private void TrySpawnGroup()
    {
        int groupSize = DetermineGroupSize();
        if (_activeClients.Count + groupSize > maxClients)
        {
            Debug.Log($"[ClientSpawner] Cannot spawn group of {groupSize} - would exceed max clients ({_activeClients.Count}/{maxClients})");
            return;
        }

        if (clientPrefab == null)
        {
            Debug.LogWarning("[ClientSpawner] clientPrefab is not assigned!");
            return;
        }

        SpawnGroup(groupSize);
    }

    private int DetermineGroupSize()
    {
        float totalWeight = 0f;
        for (int i = 0; i < groupSizeWeights.Length; i++)
            totalWeight += groupSizeWeights[i];

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < groupSizeWeights.Length; i++)
        {
            cumulativeWeight += groupSizeWeights[i];
            if (randomValue <= cumulativeWeight)
                return i + 1;
        }

        return 2;
    }

    private void SpawnGroup(int groupSize)
    {
        ClientGroup group = new ClientGroup(groupSize);
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;

        List<Client> spawnedClients = new List<Client>();

        for (int i = 0; i < groupSize; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-groupMemberSpawnOffset, groupMemberSpawnOffset),
                0f,
                Random.Range(-groupMemberSpawnOffset, groupMemberSpawnOffset)
            );
            Vector3 spawnPos = basePos + offset;

            GameObject obj = Instantiate(clientPrefab, spawnPos, Quaternion.identity);
            Client client = obj.GetComponent<Client>();

            if (client == null)
            {
                Debug.LogError("[ClientSpawner] Client prefab has no Client component!");
                Destroy(obj);
                continue;
            }

            if (entrancePoint != null)
                client.SetEntrancePoint(entrancePoint);

            // Assign a random payout this client will pay when served
            client.money = Random.Range(10, 22);

            group.AddMember(client);
            _activeClients.Add(client);
            spawnedClients.Add(client);
        }

        if (group.IsFull)
        {
            _activeGroups.Add(group);
            Debug.Log($"[ClientSpawner] Spawned {group}. Total active clients: {_activeClients.Count}");
        }
        else
        {
            Debug.LogWarning($"[ClientSpawner] Group not fully spawned. Expected {groupSize}, got {group.Members.Count}");
        }
    }

    public int GetActiveGroupCount() => _activeGroups.Count;


    public List<ClientGroup> GetActiveGroups() => new List<ClientGroup>(_activeGroups);
}