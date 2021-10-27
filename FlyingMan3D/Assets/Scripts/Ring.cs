using UnityEngine;

public class Ring : MonoBehaviour
{
    private static float spawnGap = 1.8f;

    public static void DuplicatePlayer(GameObject root)
    {
        Vector3 randomPos = Random.onUnitSphere * spawnGap;

        GameObject duplicatedPlayer = Instantiate(root, root.transform.position + randomPos, root.transform.rotation);

        CopyTransformData(root.transform, duplicatedPlayer.transform);
    }

    public static void CopyTransformData(Transform sourceTransform, Transform targetTransform)
    {
        if (sourceTransform.childCount != targetTransform.childCount)
        {
            Debug.LogError("Players have different hierarchies!");
        }

        for (int i = 0; i < sourceTransform.childCount; i++)
        {
            var source = sourceTransform.GetChild(i);
            var target = targetTransform.GetChild(i);

            var rb = target.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = source.GetComponent<Rigidbody>().velocity;
            }

            CopyTransformData(source, target);
        }
    }
}
