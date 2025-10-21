using UnityEngine;

public class PlayerSpawnSystem : MonoBehaviour
{
    public static PlayerSpawnSystem Instance;

    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetSpawnPoint(int index)
    {
        if (index < spawnPoints.Length)
            return spawnPoints[index];
        return spawnPoints[0];
    }
}
