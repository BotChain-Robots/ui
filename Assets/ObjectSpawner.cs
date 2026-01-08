using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject objectPrefab;
    public Button spawnButton;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    [DllImport ("libcontrol")]
    private static extern int init();


    void Start()
    {
        spawnButton.onClick.AddListener(SpawnObject);
    }

    void SpawnObject()
    {
        init();

        Vector3 spawnPos = FindNonCollidingPosition();

        if (spawnPos != Vector3.zero)
        {
            GameObject obj = Instantiate(objectPrefab, spawnPos, Quaternion.identity);
            spawnedObjects.Add(obj);
        }
        else
        {
            Debug.LogWarning("No valid spawn position found!");
        }
    }

    Vector3 FindNonCollidingPosition()
    {
        float radius = 1.5f;
        int attempts = 50;

        for (int i = 0; i < attempts; i++)
        {
            Vector3 tryPos = new Vector3(Random.Range(-3f, 3f), 0.5f, Random.Range(-3f, 3f));
            Collider[] hits = Physics.OverlapSphere(tryPos, radius);

            if (hits.Length == 0)
                return tryPos;
        }

        return Vector3.zero; // failed
    }
}
