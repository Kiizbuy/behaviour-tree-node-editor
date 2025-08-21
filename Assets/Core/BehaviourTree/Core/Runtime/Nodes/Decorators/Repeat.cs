using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Node that allows you to repeat the execution of a child node")]    public class Repeat : DecoratorNode
    {
        [Tooltip("Restarts the child node on success")]
        [BTHelp("Restarts the child npde on success")]
        public bool restartOnSuccess = true;

        [Tooltip("Restarts the child node on failure")]
        [BTHelp("Restarts the  child node on failure")]
        public bool restartOnFailure = false;

        [Tooltip("Maximum number of times the subtree will be repeated. Set to 0 to loop forever")]
        [BTHelp("Maximum number of times the subtree will be repeated. Set to 0 to loop forever")]
        public int maxRepeats = 0;

        private int _iterationCount = 0;

        protected override void OnStart()
        {
            _iterationCount = 0;
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

            switch (child.Update())
            {
                case State.Running:
                    break;
                case State.Failure:
                    if (restartOnFailure)
                    {
                        _iterationCount++;
                        if (_iterationCount >= maxRepeats && maxRepeats > 0)
                        {
                            return State.Failure;
                        }
                        else
                        {
                            return State.Running;
                        }
                    }
                    else
                    {
                        return State.Failure;
                    }
                case State.Success:
                    if (restartOnSuccess)
                    {
                        _iterationCount++;
                        if (_iterationCount >= maxRepeats && maxRepeats > 0)
                        {
                            return State.Success;
                        }
                        else
                        {
                            return State.Running;
                        }
                    }
                    else
                    {
                        return State.Success;
                    }
            }

            return State.Running;
        }
    }
}