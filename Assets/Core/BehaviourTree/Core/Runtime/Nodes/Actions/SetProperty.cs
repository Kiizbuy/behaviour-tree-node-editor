using BehaviourTreeLogic.Attributes;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Node that allows you to write to a Blackboard element")]
    public class SetProperty : ActionNode
    {
        public BlackboardKeyValuePair pair;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            pair.WriteValue();

            return State.Success;
        }
    }
}