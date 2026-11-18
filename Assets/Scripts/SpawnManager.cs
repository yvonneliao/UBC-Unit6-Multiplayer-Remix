using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    public void GetSpawn(out Vector3 position, out Quaternion rotation)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        // - Returns a number between minimum and maximum (not included / exclusive)
        // - 0, 10 =>  [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
        Transform selected = spawnPoints[Random.Range(0, spawnPoints.Length)];

        position = selected.position;
        rotation = selected.rotation;
    }

}
