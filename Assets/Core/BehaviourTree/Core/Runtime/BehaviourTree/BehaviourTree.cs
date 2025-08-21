using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [CreateAssetMenu()]
    public class BehaviourTree : ScriptableObject
    {
        [SerializeReference] public RootNode rootNode;

        [SerializeReference] public List<Node> nodes = new List<Node>();
#if UNITY_EDITOR
        [HideInInspector] public List<GroupData> groups = new List<GroupData>();
#endif

        public Blackboard blackboard = new Blackboard();

        public BehaviourTreeContext TreeBehaviourTreeContext;
        [HideInInspector] public Node.State lastTickState;

        #region EditorProperties

        [HideInInspector] public Vector3 viewPosition = new Vector3(600, 300);
        [HideInInspector] public Vector3 viewScale = Vector3.one;

        #endregion

        public BehaviourTree()
        {
            rootNode = new RootNode();
            nodes.Add(rootNode);
        }

        private void OnEnable()
        {
            // Validate the behaviour tree on load, removing all null children
            nodes.RemoveAll(node => node == null);
            Traverse(rootNode, node =>
            {
                if (node is CompositeNode composite)
                {
                    composite.children.RemoveAll(child => child == null);
                }
            });
        }

        public Node.State Tick(float tickDelta)
        {
            TreeBehaviourTreeContext.TickDelta = tickDelta;
            lastTickState = rootNode.Update();
            return lastTickState;
        }

        public static List<Node> GetChildren(Node parent)
        {
            var children = new List<Node>();

            switch (parent)
            {
                case DecoratorNode {child: { }} decorator:
                    children.Add(decorator.child);
                    break;
                case RootNode { child: { } } rootNode:
                    children.Add(rootNode.child);
                    break;
                case CompositeNode composite:
                    return composite.children;
            }

            return children;
        }

        public static void Traverse(Node node, Action<Node> visiter)
        {
            if (node == null)
                return;

            visiter.Invoke(node);
            var children = GetChildren(node);
            children.ForEach((n) => Traverse(n, visiter));
        }

        public BehaviourTree Clone()
        {
            var tree = Instantiate(this);
            return tree;
        }

        public void Bind(BehaviourTreeContext behaviourTreeContext)
        {
            TreeBehaviourTreeContext = behaviourTreeContext;
            Traverse(rootNode, node =>
            {
                node.BehaviourTreeContext = behaviourTreeContext;
                node.blackboard = blackboard;
                node.OnInit();
            });
        }
    }
}