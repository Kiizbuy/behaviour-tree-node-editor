using UnityEngine;

namespace BehaviourTreeLogic
{
    public class EmptyActionNode : ActionNode
    {
        [SerializeField] private State _returnState = State.Success;
        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            return _returnState;
        }
    }
}