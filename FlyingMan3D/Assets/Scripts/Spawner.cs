using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using TMPro;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public enum RingType
    {
        Additive, Multiplier, Reducer
    }

    [Serializable]
    public class Ring
    {
        public RingType ringType;
        public int effect;
    }

    [Serializable]
    public class UpperRing
    {
        public Ring[] insideRings = new Ring[2];
    }

    public static Spawner Instance;
    public static List<GameObject> enemies;
    [SerializeField]
    private GameObject Platform;

    [SerializeField] private GameObject EnemyPrefab;
    [SerializeField] private float enemyCount;
    [SerializeField] private UpperRing[] rings;
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject finish;
    [SerializeField] private float timeDif = 0.5f;

    private float g, averageTime;
    private float velY, velZ;
    private float xPos, yPos, zPos;
    
    private int ringCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        enemies = new List<GameObject>();
    }

    void Start()
    {
        Platform.GetComponent<Renderer>().sharedMaterial.color = GameManager.Instance.ColorArray[SceneManager.GetActiveScene().buildIndex].PlatformColor;
        ringPrefab.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.color = GameManager.Instance.ColorArray[SceneManager.GetActiveScene().buildIndex].RingColor;
        ringPrefab.transform.GetChild(0).gameObject.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial.color = GameManager.Instance.ColorArray[SceneManager.GetActiveScene().buildIndex].RingTransColor;
        ringPrefab.transform.GetChild(1).gameObject.GetComponent<Renderer>().sharedMaterial.color = GameManager.Instance.ColorArray[SceneManager.GetActiveScene().buildIndex].RingColor;
        ringPrefab.transform.GetChild(1).gameObject.transform.GetChild(1).GetComponent<Renderer>().sharedMaterial.color = GameManager.Instance.ColorArray[SceneManager.GetActiveScene().buildIndex].RingTransColor;
        g = -Physics.gravity.y;
        ringCount = rings.Length;
    }

    public void SpawnObjects(Vector3 velocity)
    {
        velY = velocity.y;
        velZ = velocity.z;
        averageTime = velY / g;
        yPos = playerTransform.position.y + (velY * averageTime) - (0.5f * g * averageTime * averageTime);
        zPos = playerTransform.position.z + velZ * averageTime;

        float finishTime = averageTime * 2;
        float finishZPos = playerTransform.position.z + velZ * finishTime;
        GameObject finishGO = Instantiate(finish, new Vector3(0, 0, finishZPos), Quaternion.identity);

        float angle = 360 / enemyCount;

        for (int i = 0; i < enemyCount; i++)
        {
            float rotation = angle * i;
            GameObject enemy = Instantiate(EnemyPrefab, finishGO.transform.position, Quaternion.Euler(0f, 180f, 0f));
            enemy.transform.Rotate(0, rotation, 0);
            enemy.transform.Translate(new Vector3(0, 0, -16f));
            enemies.Add(enemy);
        }
        
        float t;
        
        if (ringCount % 2 == 0)
        {
            t = averageTime - timeDif / 2 - timeDif * (ringCount - 2) / 2;
        }
        else
        {
            t = averageTime - timeDif * (ringCount / 2);
        }
        
        for (int i = 0; i < ringCount; i++)
        {
            GameObject currentRing = Instantiate(ringPrefab, CalculateRingPosition(t, i), Quaternion.identity);

            for (int j = 0; j < 2; j++)
            {
                Ring currentInsideRing = rings[i].insideRings[j];
                GameObject currentGO = currentRing.transform.GetChild(j).gameObject;

                DetermineRingType(currentInsideRing, currentGO);
            }
            t += timeDif;
        }
    }

    private void DetermineRingType(Ring insideRing, GameObject currentChildGO)
    {
        if (insideRing.ringType == RingType.Additive)
        {
            currentChildGO.AddComponent<AdditiveRing>();
            currentChildGO.GetComponent<AdditiveRing>().addition = insideRing.effect;
            currentChildGO.GetComponentInChildren<TextMeshPro>().text = "+" + insideRing.effect;
        }
        else if (insideRing.ringType == RingType.Multiplier)
        {
            currentChildGO.AddComponent<MultiplierRing>();
            currentChildGO.GetComponent<MultiplierRing>().multiplier = insideRing.effect;
            currentChildGO.GetComponentInChildren<TextMeshPro>().text = "x" + insideRing.effect;
        }
        else if (insideRing.ringType == RingType.Reducer)
        {
            currentChildGO.AddComponent<ReducerRing>();
            currentChildGO.GetComponent<ReducerRing>().reductionFactor = insideRing.effect;
            currentChildGO.GetComponentInChildren<TextMeshPro>().text = "-" + insideRing.effect;
        }
    }

    Vector3 CalculateRingPosition(float t, int i)
    {
        xPos = (i == 0) ? Random.Range(-10f, 10f) : Mathf.Clamp(xPos + 2f, -20f, 20f);

        yPos = playerTransform.position.y + (velY * t) - (0.5f * g * t * t);
        zPos = playerTransform.position.z + velZ * t;

        return new Vector3(xPos, yPos, zPos);
    }
}
