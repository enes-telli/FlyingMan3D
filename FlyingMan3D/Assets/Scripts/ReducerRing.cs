using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReducerRing : MonoBehaviour
{
    public int reductionFactor;

    private bool reductionHappened;

    private void OnTriggerEnter(Collider other)
    {
        GameObject root = other.transform.root.gameObject;

        if (root.CompareTag("Player"))
        {
            if (!reductionHappened)
            {
                for (int i = 0; i < reductionFactor && PlayerController.players.Count > 1; i++)
                {
                    Destroy(PlayerController.players[PlayerController.players.Count - 1].gameObject);
                    PlayerController.players.RemoveAt(PlayerController.players.Count - 1);
                }

                reductionHappened = true;
            }
        }
    }
}
