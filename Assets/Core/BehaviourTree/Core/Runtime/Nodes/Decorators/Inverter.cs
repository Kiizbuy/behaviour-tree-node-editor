using BehaviourTreeLogic.Attributes;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    public class Inverter : DecoratorNode
    {
        protected override void OnStart()
        {
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

            return child.Update() switch
            {
                State.Running => State.Running,
                State.Failure => State.Success,
                State.Success => State.Failure,
                _ => State.Failure
            };
        }
    }
}