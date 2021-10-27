using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveToFinish : MonoBehaviour
{

    public GameObject target;
    [SerializeField]
    private GameObject SmokePrefab;
    [SerializeField]
    private GameObject RagdollPrefab;
    [SerializeField]
    private GameObject EnemyRagdollPrefab;

    private Vector3 MoveDistance;
    private float moveSpeed = 2.4f;
    private float StopDistance = 0.2f;
    private bool CanMove = false;
    private bool IsDie = false;

    void Start()
    {
        
        if (gameObject.CompareTag("Enemy"))
        {
            CanMove = true;
        }
    }

    void Update()
    {
        if (CanMove)
        {
            if (target == null)
            {
                target = NearestTarget();
            }
            else
            {
                try
                {
                    MoveDistance = target.transform.position - transform.position;
                    MoveDistance.y = 0f;
                    if (MoveDistance.magnitude > StopDistance)
                    {
                        transform.position += MoveDistance.normalized * moveSpeed * Time.deltaTime;
                        Quaternion targetRotation = Quaternion.LookRotation(MoveDistance);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * 120f);
                    }
                }
                catch
                {

                }
            }
        }
    }
    private GameObject NearestTarget()
    {
        float minDistance = float.MaxValue;
        int index = 0;

        for (int i = 1; i < Spawner.enemies.Count; i++)
        {
            if (minDistance > Distance(Spawner.enemies[i].transform.position, transform.position))
            {
                minDistance = Distance(Spawner.enemies[i].transform.position, transform.position);
                index = i;
            }
        }

        if (Spawner.enemies.Count > 0)
        {
            target = Spawner.enemies[index].gameObject;
        }
        else
        {
            target = null;
        }

        return target;
    }

    private float Distance(Vector3 v1, Vector3 v2)
    {
        return (v1 - v2).magnitude;
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.transform.root.gameObject.tag == "Enemy")
        {
            
            if (!collision.gameObject.transform.root.GetComponent<EnemyFinish>().IsDie && !IsDie)
            {
                if (GameManager.Instance.CanSmoke)
                {
                    GameManager.Instance.CanSmoke = false;
                    Instantiate(SmokePrefab, new Vector3(0f, 2f, transform.position.z), Quaternion.Euler(-90f, 0f, 0f));
                }
                collision.gameObject.transform.root.GetComponent<EnemyFinish>().IsDie = true;
                Spawner.enemies.Remove(collision.gameObject.transform.root.gameObject);              
                GameObject enemy = Instantiate(EnemyRagdollPrefab, transform.position, Quaternion.identity);
                enemy.layer = 8;
                Destroy(collision.gameObject.transform.root.gameObject);

                IsDie = true;
                PlayerController.players.Remove(gameObject.GetComponent<PlayerController>());
                GameObject self = Instantiate(RagdollPrefab, transform.position, Quaternion.identity);
                self.layer = 8;
                Destroy(gameObject);
                

            }
            
        }
        if (gameObject.transform.root.gameObject.tag != "Enemy" && collision.gameObject.tag == "Platform")
        {
            GetComponent<Animator>().SetBool("IsGround", true);
            CanMove = true;
        }

    }
}
