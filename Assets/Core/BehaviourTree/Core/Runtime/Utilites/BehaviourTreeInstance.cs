using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [AddComponentMenu("BehaviourTreeInstance")]
    public class BehaviourTreeInstance : MonoBehaviour
    {
        public enum TickMode
        {
            None, // Use this update method to manually update the tree by calling ManualTick()
            FixedUpdate,
            Update,
            LateUpdate
        };

        public enum StartMode
        {
            None, // Use this start method to manually start the tree by calling StartBehaviour()
            OnEnable,
            OnAwake,
            OnStart
        };

        // The main behaviour tree asset
        [Tooltip("BehaviourTree asset to instantiate during Awake")]
        public BehaviourTree behaviourTree;

        [Tooltip("When to update this behaviour tree in the frame")]
        public TickMode tickMode = TickMode.Update;

        [Tooltip("When to start this behaviour tree")]
        public StartMode startMode = StartMode.OnStart;

        [Tooltip("Run behaviour tree validation at startup (Can be disabled for release)")]
        public bool validate = true;

        [Tooltip("Override / set blackboard key values for this behaviour tree instance")]
        public List<BlackboardKeyValuePair> blackboardOverrides = new List<BlackboardKeyValuePair>();
        
        // Runtime tree instance
        private BehaviourTree _runtimeTree;

        // Storage container object to hold game object subsystems
        private BehaviourTreeContext _behaviourTreeContext;

        // Profile markers
        private static readonly ProfilerMarker ProfileUpdate = new("BehaviourTreeInstance.Update");

        // Tree state from last tick
        private Node.State _treeState = Node.State.Running;

        public BehaviourTree RuntimeTree => _runtimeTree;
        
        private void OnEnable()
        {
            if (startMode == StartMode.OnEnable)
            {
                StartBehaviour(behaviourTree);
            }
        }

        private void Awake()
        {
            if (startMode == StartMode.OnAwake)
            {
                StartBehaviour(behaviourTree);
            }
        }

        private void Start()
        {
            if (startMode == StartMode.OnStart)
            {
                StartBehaviour(behaviourTree);
            }
        }

        private void ApplyBlackboardOverrides()
        {
            foreach (var pair in blackboardOverrides)
            {
                // Find the key from the new behaviour tree instance
                var targetKey = _runtimeTree.blackboard.Find(pair.key.name);
                var sourceKey = pair.value;
                if (targetKey != null && sourceKey != null)
                {
                    targetKey.CopyFrom(sourceKey);
                }
            }
        }

        private void InternalUpdate(float tickDelta)
        {
            if (_runtimeTree != null)
            {
                ProfileUpdate.Begin();
                if (_runtimeTree.lastTickState == Node.State.Success)
                {
                    _behaviourTreeContext.TickResults.Clear();
                    return;
                }
                _treeState = _runtimeTree.Tick(tickDelta);
                ProfileUpdate.End();
            }
        }

        private void FixedUpdate()
        {
            if (tickMode == TickMode.FixedUpdate)
            {
                InternalUpdate(Time.fixedDeltaTime);
            }
        }

        private void Update()
        {
            if (tickMode == TickMode.Update)
            {
                InternalUpdate(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (tickMode == TickMode.LateUpdate)
            {
                InternalUpdate(Time.deltaTime);
            }
        }

        public void ManualTick(float tickDelta)
        {
            if (tickMode != TickMode.None)
            {
                Debug.LogWarning(
                    $"Manually ticking the behaviour tree while in {tickMode} will cause duplicate updates");
            }

            InternalUpdate(tickDelta);
        }

        private void StartBehaviour(BehaviourTree tree)
        {
            var isValid = ValidateTree(tree);
            if (isValid)
            {
                InstantiateTree(tree);
            }
            else
            {
                _runtimeTree = null;
            }
        }

        private void InstantiateTree(BehaviourTree tree)
        {
            _behaviourTreeContext = CreateBehaviourTreeContext();
            _runtimeTree = tree.Clone();
            _runtimeTree.Bind(_behaviourTreeContext);
            ApplyBlackboardOverrides();
        }

        private BehaviourTreeContext CreateBehaviourTreeContext()
        {
            var baseContext = new BehaviourTreeContext();
            baseContext.Register(new MonoBehaviourContext(transform));
            return baseContext;
        }

        private bool ValidateTree(BehaviourTree tree)
        {
            if (!tree)
            {
                Debug.LogWarning($"No BehaviourTree assigned to {name}, assign a behaviour tree in the inspector");
                return false;
            }

            var isValid = true;
            if (validate)
            {
                string cyclePath;
                isValid = !IsRecursive(tree, out cyclePath);

                if (!isValid)
                {
                    Debug.LogError($"Failed to create recursive behaviour tree. Found cycle at: {cyclePath}");
                }
            }

            return isValid;
        }

        private bool IsRecursive(BehaviourTree tree, out string cycle)
        {
            // Check if any of the subtree nodes and their decendents form a circular reference, which will cause a stack overflow.
            var treeStack = new List<string>();
            var referencedTrees = new HashSet<BehaviourTree>();

            var cycleFound = false;
            var cyclePath = "";

            Action<Node> traverse = null;
            traverse = (node) =>
            {
                if (!cycleFound)
                {
                    if (node is SubTree subtree && subtree.treeAsset != null)
                    {
                        treeStack.Add(subtree.treeAsset.name);
                        if (referencedTrees.Contains(subtree.treeAsset))
                        {
                            var index = 0;
                            foreach (var tree in treeStack)
                            {
                                index++;
                                if (index == treeStack.Count)
                                {
                                    cyclePath += $"{tree}";
                                }
                                else
                                {
                                    cyclePath += $"{tree} -> ";
                                }
                            }

                            cycleFound = true;
                        }
                        else
                        {
                            referencedTrees.Add(subtree.treeAsset);
                            BehaviourTree.Traverse(subtree.treeAsset.rootNode, traverse);
                            referencedTrees.Remove(subtree.treeAsset);
                        }

                        treeStack.RemoveAt(treeStack.Count - 1);
                    }
                }
            };
            treeStack.Add(tree.name);

            referencedTrees.Add(tree);
            BehaviourTree.Traverse(tree.rootNode, traverse);
            referencedTrees.Remove(tree);

            treeStack.RemoveAt(treeStack.Count - 1);
            cycle = cyclePath;
            return cycleFound;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!_runtimeTree)
            {
                return;
            }

            BehaviourTree.Traverse(_runtimeTree.rootNode, (n) =>
            {
                if (n.drawGizmos)
                {
                    n.OnDrawGizmos();
                }
            });
        }

        public BlackboardKey<T> FindBlackboardKey<T>(string keyName)
        {
            if (_runtimeTree)
            {
                return _runtimeTree.blackboard.Find<T>(keyName);
            }

            return null;
        }

        public void SetBlackboardValue<T>(string keyName, T value)
        {
            if (_runtimeTree)
            {
                _runtimeTree.blackboard.SetValue(keyName, value);
            }
        }

        public T GetBlackboardValue<T>(string keyName)
        {
            if (_runtimeTree)
            {
                return _runtimeTree.blackboard.GetValue<T>(keyName);
            }

            return default(T);
        }
    }
}