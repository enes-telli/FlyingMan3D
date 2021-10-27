using UnityEngine;

public class AdditiveRing : MonoBehaviour
{
    public int addition;

    private bool additionHappened;

    private void OnTriggerEnter(Collider other)
    {
        GameObject root = other.transform.root.gameObject;
        
        if (root.CompareTag("Player"))
        {
            if (!additionHappened)
            {
                for (int i = 0; i < addition; i++)
                {
                    Ring.DuplicatePlayer(root);
                }

                additionHappened = true;
            }
        }
    }
}
