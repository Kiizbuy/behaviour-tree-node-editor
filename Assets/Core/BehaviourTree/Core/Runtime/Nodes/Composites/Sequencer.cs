using System;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    public class Sequencer : CompositeNode
    {
        [UniqueId]
        public string PointerId;
        [NonSerialized] private int _currentActionIndex = 0;
        
        public override void OnInit()
        {
        }

        protected override void OnStart()
        {
            _currentActionIndex = 0;
        }

        protected override void OnStop()
        {
            _currentActionIndex = 0;
        }

        protected override State OnUpdate()
        {
            _currentActionIndex = Mathf.Clamp(_currentActionIndex, 0, children.Count - 1);

            if (_currentActionIndex >= children.Count)
            {
                return State.Success;
            }

            var currentNode = children[_currentActionIndex];
            var status = currentNode.Update();

            if (status == State.Success)
            {
                _currentActionIndex++;
                return (_currentActionIndex < children.Count) ? State.Running : State.Success;
            }

            if (status == State.Failure)
            {
                return State.Failure;
            }

            return State.Running;
        }
    }
}