using BehaviourTreeLogic.Attributes;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Node that allows you to compare the value of a Blackboard element by key")]
    public class CompareProperty : ActionNode
    {
        [BTHelp("ID Blackboard element to compare")]
        public BlackboardKeyValuePair pair;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var source = pair.value;
            var destination = pair.key;

            if (source != null && destination != null)
            {
                if (destination.Equals(source))
                {
                    return State.Success;
                }
            }

            return State.Failure;
        }
    }
}