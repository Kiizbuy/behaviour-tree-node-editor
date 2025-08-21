using UnityEngine;

namespace BehaviourTreeLogic
{
    public struct MonoBehaviourContext
    {
        public readonly Transform Owner;

        public MonoBehaviourContext(Transform owner)
        {
            Owner = owner;
        }
    }
}