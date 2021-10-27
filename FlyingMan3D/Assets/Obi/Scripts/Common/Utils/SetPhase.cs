using UnityEngine;

namespace Obi
{
    [RequireComponent(typeof(ObiActor))]
    public class SetPhase : MonoBehaviour
    {
        public int phase;
        private ObiActor act;

        private void Awake()
        {
            act = GetComponent<ObiActor>();
            act.OnBlueprintLoaded += Set;
            if (act.isLoaded) Set(act, null);
        }

        private void OnDestroy()
        {
            act.OnBlueprintLoaded -= Set;
        }

        private void OnValidate()
        {
            phase = Mathf.Clamp(phase, 0, (1 << 24) - 1);
        }

        private void Set(ObiActor actor, ObiActorBlueprint blueprint)
        {
            actor.SetPhase(phase);
        }
    }
}
