using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("A node that will return FAILURE with a certain probability")]
    public class RandomFailure : ActionNode
    {
        [Range(0, 1)] [Tooltip("Percentage chance of failure")]
        public float ChanceOfFailure = 0.5f;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var value = Random.value;
            if (value <= ChanceOfFailure)
            {
                return State.Failure;
            }

            return State.Success;
        }
    }
}