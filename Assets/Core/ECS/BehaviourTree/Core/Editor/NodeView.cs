using System.Collections.Generic;
using BehaviourTreeLogic.Utility;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;

namespace BehaviourTreeLogic
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public BehaviourTreeView treeView;
        public Node node;
        public Port input;
        public Port output;
        
        private Label validationLabel;
        private VisualElement errorContainer;
        
        public NodeView NodeParent
        {
            get
            {
                using var iter = input.connections.GetEnumerator();
                iter.MoveNext();
                return iter.Current?.output.node as NodeView;
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Create Subtree...", CreateSubtree);
            if (node is SubTree or SubTreeDecorator)
            {
                evt.menu.AppendAction("Expand Subtree...", ExpandSubtree);
            }
        }

        private void ExpandSubtree(DropdownMenuAction action)
        {
            treeView.ExpandSubtree(this);
        }

        private void CreateSubtree(DropdownMenuAction action)
        {
            treeView.CreateSubTree(this);
        }

        public NodeView(Node node, VisualTreeAsset nodeXml, BehaviourTreeView treeView) : base(
            AssetDatabase.GetAssetPath(nodeXml))
        {
            this.treeView = treeView;
            capabilities &= ~(Capabilities.Snappable); // Disable node snapping
            this.node = node;
            title = node.Title;
            viewDataKey = node.guid;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutputPorts();
            SetupClasses();
            SetupDataBinding();

            this.AddManipulator(new DoubleClickNode());
            this.treeView = treeView;
        }
        
        public void UpdateValidation()
        {
            if (node is INodeValidation validator)
            {
                var isValid = validator.IsValid(out var message);

                if (isValid)
                {
                    RemoveFromClassList("invalid");

                    if (errorContainer != null)
                    {
                        Remove(errorContainer);
                        errorContainer = null;
                        validationLabel = null;
                    }
                }
                else
                {
                    AddToClassList("invalid");

                    if (errorContainer == null)
                    {
                        errorContainer = new VisualElement();
                        errorContainer.style.backgroundColor = new Color(1f, 0.65f, 0f);
                        errorContainer.style.marginTop = 4;
                        errorContainer.style.paddingLeft = 6;
                        errorContainer.style.paddingRight = 6;
                        errorContainer.style.paddingTop = 2;
                        errorContainer.style.paddingBottom = 2;
                        errorContainer.style.borderBottomWidth = 1;
                        errorContainer.style.borderTopWidth = 1;
                        errorContainer.style.borderLeftWidth = 1;
                        errorContainer.style.borderRightWidth = 1;

                        validationLabel = new Label();
                        validationLabel.style.color = Color.black;
                        validationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        validationLabel.style.fontSize = 11;

                        errorContainer.Add(validationLabel);
                        Add(errorContainer);
                    }

                    validationLabel.text = message;
                }
            }
        }


        public void SetupDataBinding()
        {
            var serializer = treeView.serializer;
            var nodeProp = serializer.FindNode(serializer.Nodes, node);
            if (nodeProp == null)
            {
                return;
            }

            if (node is ActionNode)
            {
                var label = this.Q<Label>("title-label");
                PropertyBinder.BindLabel(label, () => node.Title);
                
                var descriptionLabel = this.Q<Label>("description");
                if (node is SubTree)
                {
                    var treeAssetProperty = nodeProp.FindPropertyRelative(nameof(SubTree.treeAsset));
                    SetSubTreeLabel(descriptionLabel, treeAssetProperty);
                    descriptionLabel.TrackPropertyValue(treeAssetProperty,
                        (property) => { SetSubTreeLabel(descriptionLabel, property); });
                }
                else
                {
                    var descriptionProp = nodeProp.FindPropertyRelative("description");
                    descriptionLabel.TrackPropertyValue(descriptionProp,
                        (property) => { SetDescriptionFieldVisible(descriptionLabel, property); });
                    descriptionLabel.BindProperty(descriptionProp);
                }
            }
            
            if (node is CompositeNode)
            {
                var descriptionProp = nodeProp.FindPropertyRelative("description");
                var descriptionLabel = this.Q<Label>("description");
                SetDescriptionFieldVisible(descriptionLabel, descriptionProp);
                descriptionLabel.TrackPropertyValue(descriptionProp,
                    (property) =>
                    {
                        SetDescriptionFieldVisible(descriptionLabel, property);
                    }
                );
                descriptionLabel.BindProperty(descriptionProp);
            }

            if (node is ConditionNode)
            {
                var invertProperty = nodeProp.FindPropertyRelative("invert");
                var icon = this.Q<VisualElement>("icon");
                icon.TrackPropertyValue(invertProperty, UpdateConditionNodeClasses);
            }
            if (node is SubTreeDecorator)
            {
                var descriptionLabel = this.Q<Label>("description");
                var treeAssetProperty = nodeProp.FindPropertyRelative(nameof(SubTree.treeAsset));
                SetSubTreeLabel(descriptionLabel, treeAssetProperty);
                descriptionLabel.TrackPropertyValue(treeAssetProperty,
                    (property) => { SetSubTreeLabel(descriptionLabel, property); });
            }
        }

        private void SetDescriptionFieldVisible(Label label, SerializedProperty property)
        {
            if (property.stringValue == null || property.stringValue.Length == 0)
            {
                label.style.display = DisplayStyle.None;
            }
            else
            {
                label.style.display = DisplayStyle.Flex;
            }
        }

        private void SetSubTreeLabel(Label label, SerializedProperty property)
        {
            if (property.objectReferenceValue == null)
            {
                label.text = "SubTree";
            }
            else
            {
                var treeAsset = property.objectReferenceValue as BehaviourTree;
                label.text = treeAsset.name;
            }
        }

        private void UpdateConditionNodeClasses(SerializedProperty obj)
        {
            if (obj.boolValue)
            {
                AddToClassList("invert");
            }
            else
            {
                RemoveFromClassList("invert");
            }
        }

        private void SetupClasses()
        {
            switch (node)
            {
                case ActionNode:
                {
                    AddToClassList("action");

                    if (node is ConditionNode conditionNode)
                    {
                        AddToClassList("condition");
                        if (conditionNode.invert)
                        {
                            AddToClassList("invert");
                        }
                    }

                    if (node is SubTree)
                    {
                        AddToClassList("subtree");
                    }

                 

                    break;
                }
                case CompositeNode:
                {
                    AddToClassList("composite");
                    switch (node)
                    {
                        case Sequencer:
                            AddToClassList("sequencer");
                            break;
                        case Selector:
                            AddToClassList("selector");
                            break;
                        case Parallel:
                            AddToClassList("parallel");
                            break;
                        case UtilitySelector:
                            AddToClassList("utility_selector");
                            break;
                    }
                    

                    break;
                }
                case DecoratorNode:
                    AddToClassList(node is SubTreeDecorator ? "subtree" : "decorator");
                    break;
                case RootNode:
                    AddToClassList("root");
                    break;
            }
        }

        private void CreateInputPorts()
        {
            switch (node)
            {
                case ActionNode:
                case CompositeNode:
                case DecoratorNode:
                    input = new NodePort(Direction.Input, Port.Capacity.Single);
                    break;
                case RootNode:
                    break;
            }

            if (input != null)
            {
                input.portName = "";
                input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(input);
            }
        }

        private void CreateOutputPorts()
        {
            switch (node)
            {
                case ActionNode:
                    // Actions have no outputs
                    break;
                case CompositeNode:
                    output = new NodePort(Direction.Output, Port.Capacity.Multi);
                    break;
                case DecoratorNode:
                case RootNode:
                    output = new NodePort(Direction.Output, Port.Capacity.Single);
                    break;
      
            }

            if (output != null)
            {
                output.portName = "";
                output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(output);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            title = node.Title;
            base.SetPosition(newPos);

            var serializer = treeView.serializer;
            var position = new Vector2(newPos.xMin, newPos.yMin);
            serializer.SetNodePosition(node, position);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            treeView.OnNodeSelected?.Invoke(this);
        }

        public void SortChildren()
        {
            if (node is CompositeNode composite)
            {
                composite.children.Sort(SortByHorizontalPosition);
            }
        }

        private int SortByHorizontalPosition(Node left, Node right)
        {
            return left.position.x < right.position.x ? -1 : 1;
        }

        public void UpdateState(Dictionary<string, Node.State> tickResults)
        {
            if (Application.isPlaying)
            {
                if (tickResults.TryGetValue(node.guid, out var tickResult))
                {
                    ApplyActiveNodeStateStyle(tickResult);
                }
                else
                {
                    ApplyInactiveNodeStateStyle();
                }
            }
        }

        private void ApplyActiveNodeStateStyle(Node.State state)
        {
            style.borderLeftWidth = 5;
            style.borderRightWidth = 5;
            style.borderTopWidth = 5;
            style.borderBottomWidth = 5;
            style.opacity = 1.0f;

            switch (state)
            {
                case Node.State.Success:
                    style.borderLeftColor = Color.green;
                    style.borderRightColor = Color.green;
                    style.borderTopColor = Color.green;
                    style.borderBottomColor = Color.green;
                    break;
                case Node.State.Failure:
                    style.borderLeftColor = Color.red;
                    style.borderRightColor = Color.red;
                    style.borderTopColor = Color.red;
                    style.borderBottomColor = Color.red;
                    break;
                case Node.State.Running:
                    style.borderLeftColor = Color.yellow;
                    style.borderRightColor = Color.yellow;
                    style.borderTopColor = Color.yellow;
                    style.borderBottomColor = Color.yellow;
                    break;
            }
        }

        private void ApplyInactiveNodeStateStyle()
        {
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;
            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            style.opacity = 0.5f;
        }
    }
}