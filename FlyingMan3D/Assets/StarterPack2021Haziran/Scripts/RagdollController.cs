using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace BhorGames
{
    public class RagdollController : MonoBehaviour
    {
        public List<Rigidbody> rigids;
        public Rigidbody body;
        public bool multiplyMass = false;
        public float multiplyBy = 3;
        private void Start()
        {
            rigids = transform.GetComponentsInChildren<Rigidbody>().ToList();
            DeactivateRagdoll();
        }
        [ContextMenu("DeActivate")]
        public void DeactivateRagdoll()
        {
            foreach (Rigidbody item in rigids)
            {
                // GetComponent<Animator>().enabled = true;
                item.velocity = Vector3.zero;
                item.isKinematic = true;
            }
        }
        [ContextMenu("Activate")]
        public void ActivateRagdoll()
        {
            foreach (Rigidbody item in rigids)
            {
                // if(multiplyMass)
                // item.drag = 1;
                GetComponent<Animator>().enabled = false;
                item.velocity = Vector3.zero;
                item.isKinematic = false;
            }
        }
        public void StickBodyToObject()
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        public void UnStickBodyToObject()
        {
            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}