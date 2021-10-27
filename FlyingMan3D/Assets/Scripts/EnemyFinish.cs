using UnityEngine;

public class EnemyFinish : MonoBehaviour
{
    [HideInInspector] public bool IsDie;

    private GameObject target;
    private float minDistance;
    private int index;
    private Vector3 MoveDistance;
    private float StopDistance=0.2f;
    private float moveSpeed = 2.4f;
    private Collider[] colliders;

    private void Start()
    {
        colliders = GetComponentsInChildren<Collider>();
        for (int i = 1; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }
    }

    private GameObject NearestTarget()
    {
        index = 0;
        minDistance = float.MaxValue;
        if(PlayerController.players == null || PlayerController.players.Count == 0) return null;
        for (int i = 1; i < PlayerController.players.Count; i++)
        {
            if (minDistance > Distance(PlayerController.players[i].transform.position, transform.position))
            {
                minDistance = Distance(PlayerController.players[i].transform.position, transform.position);
                index = i;
            }
        }
        if (PlayerController.players.Count > 0)
        {
            target = PlayerController.players[index].gameObject;
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

    void Update()
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
