using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Timeout node")]
    public class Timeout : DecoratorNode
    {
        [Tooltip("Returns failure after this amount of time if the subtree is still running.")]
        public float duration = 1.0f;

        private float _startTime;

        protected override void OnStart()
        {
            _startTime = Time.time;
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (child == null)
            {
                return State.Failure;
            }

            if (Time.time - _startTime > duration)
            {
                return State.Failure;
            }

            return child.Update();
        }
    }
}