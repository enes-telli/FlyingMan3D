using UnityEngine;
using Cinemachine;

public class Finish : MonoBehaviour
{
    private CinemachineVirtualCamera finishCamera;
    private bool CanCalculate = false;

    private void Start()
    {
        finishCamera = GameObject.Find("FinishCamera").GetComponent<CinemachineVirtualCamera>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.root.CompareTag("Player"))
        {
            if (!CanCalculate)
            {
                finishCamera.Priority = 15;
                finishCamera.transform.position = new Vector3(0, 23, transform.position.z - 30f);

                CanCalculate = true;

                for (int i = 0; i < Spawner.enemies.Count; i++)
                {
                    Spawner.enemies[i].GetComponent<EnemyFinish>().enabled=true;
                    Spawner.enemies[i].GetComponent<Animator>().SetBool("CanAttack", true);
                }

                GameManager.Instance.isPlayerEntered = true;
            }

            Transform root = other.gameObject.transform.root.gameObject.transform;
            root.tag = "FreePlayer";

            root.GetComponent<PlayerMoveToFinish>().enabled = true ;
            root.GetComponent<CapsuleCollider>().enabled = true;

            GameObject hips = root.GetChild(0).gameObject.transform.GetChild(0).gameObject;
            root.gameObject.AddComponent<Rigidbody>();
            root.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            root.GetComponent<Animator>().enabled = true;
            Collider[] colliders = root.GetComponentsInChildren<Collider>();

            for (int i = 1; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
            Destroy(hips.GetComponent<TrailRenderer>());

            root.position = new Vector3(Random.Range(-8, 8), transform.position.y + root.transform.position.y, transform.position.z + Random.Range(-8f, 8f)); 
            hips.transform.localPosition = Vector3.zero;
        }
    }
}
