using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Нода, которая позволяет вывести в лог информацию (нужно для отладки BehaviourTree)")]
    public class Log : ActionNode
    {
        [Tooltip("Message to log to the console")]
        public NodeProperty<string> message = new NodeProperty<string>();

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            Debug.Log($"{message.Value}");
            return State.Success;
        }
    }
}