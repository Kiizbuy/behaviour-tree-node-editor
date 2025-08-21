using System;
using BehaviourTreeLogic;
using UnityEngine;

namespace Core.ECS.BehaviourTree.Core.Runtime.Examples.Nodes
{
    [Serializable]
    public class SimpleMoveToPoint : ActionNode, INodeValidation
    {
        [SerializeField] private float _stopDistance = 1f;
        [SerializeField] private float _moveSpeed;
        [SerializeField] private NodeProperty<Vector3> _point;
        private MonoBehaviourContext _monoBehaviourContext;
        
        protected override void OnStart()
        {
            _monoBehaviourContext = BehaviourTreeContext.Get<MonoBehaviourContext>();
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var transform = _monoBehaviourContext.Owner;
            var direction = _point.Value - transform.position;
            if (direction.magnitude <= _stopDistance)
            {
                return State.Success;
            }

            transform.position = Vector3.MoveTowards(transform.position, _point.Value, _moveSpeed * Time.deltaTime);
            return State.Running;
        }

        public bool IsValid(out string errorMessage)
        {
            var isValid = true;
            errorMessage = string.Empty;
            if (_moveSpeed <= 0)
            {
                errorMessage += "Move speed must be greater than zero!";
                isValid = false;
            }

            if (_stopDistance <= 0)
            {
                errorMessage += "Stop distance must be greater than zero!";
                isValid = false;
            }

            return isValid;
        }
    }
}