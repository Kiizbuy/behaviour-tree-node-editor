using System.Collections.Generic;
using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    public abstract class Node
    {
        public enum State
        {
            Running,
            Failure,
            Success
        }

        [SerializeField] private string title;
        [HideInInspector] public bool started = false;
        [HideInInspector] public string guid = System.Guid.NewGuid().ToString();
        [HideInInspector] public Vector2 position;
        [HideInInspector] public BehaviourTreeContext BehaviourTreeContext;
        [HideInInspector] public Blackboard blackboard;
        [TextArea] public string description;

        [Tooltip("When enabled, the nodes OnDrawGizmos will be invoked")]
        [BTHelp("When enabled, the nodes OnDrawGizmos will be invoked")]
        public bool drawGizmos = false;

        public string Title => string.IsNullOrEmpty(title) ? GetType().Name : title;

        public virtual void OnInit()
        {
            // Nothing to do here
        }

        //For default actions utility always equal zero
        public virtual float GetUtility() => 0f;

        public State Update()
        {
            if (!started)
            {
                OnStart();
                started = true;
            }

            var state = OnUpdate();
#if UNITY_EDITOR
            BehaviourTreeContext.TickResults[guid] = state;
#endif
            if (state != State.Running)
            {
                OnStop();
                started = false;
            }

            return state;
        }

        public void Abort()
        {
            BehaviourTree.Traverse(this, (node) =>
            {
                node.started = false;
                node.OnStop();
            });
        }

        public virtual void OnDrawGizmos()
        {
        }

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract State OnUpdate();

        protected virtual void Log(string message)
        {
            Debug.Log($"[{GetType().Name}] - {message}");
        }

        public Node Clone()
        {
            var clone = NodeCloneUtility.DeepClone(this);

            switch (clone)
            {
                case DecoratorNode decorator:
                    decorator.child = null;
                    break;

                case RootNode root:
                    root.child = null;
                    break;

                case CompositeNode composite:
                    composite.children = new List<Node>();
                    break;
            }

            return clone;
        }
    }
}