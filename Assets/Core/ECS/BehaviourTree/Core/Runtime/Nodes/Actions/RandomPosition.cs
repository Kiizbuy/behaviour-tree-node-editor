using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Node that write random point to blackboard")]
    public class RandomPosition : ActionNode
    {
        [Tooltip("Minimum bounds to generate point")]
        [BTHelp("Minimum bounds to generate point")]
        public Vector3 min = Vector2.one * -10;

        [Tooltip("Maximum bounds to generate point")]
        [BTHelp("Maximum bounds to generate point")]
        public Vector3 max = Vector2.one * 10;

        [Tooltip("Blackboard key to write the result to")]
        [BTHelp("Blackboard key to write the result to")]
        public NodeProperty<Vector3> result;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var pos = new Vector3
            {
                x = Random.Range(min.x, max.x),
                y = Random.Range(min.y, max.y),
                z = Random.Range(min.z, max.z)
            };
            result.Value = pos;
            return State.Success;
        }
    }
}