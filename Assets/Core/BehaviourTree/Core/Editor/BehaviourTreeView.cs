using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;

namespace BehaviourTreeLogic
{
    [UxmlElement]
    public partial class BehaviourTreeView : GraphView
    {
        public Action<NodeView> OnNodeSelected;

        protected override bool canCopySelection => true;
        protected override bool canCutSelection => false;
        protected override bool canPaste => true;
        protected override bool canDuplicateSelection => true;
        protected override bool canDeleteSelection => true;

        public SerializedBehaviourTree serializer;
        private ToolbarSearchField searchToolbar;

        private bool dontUpdateModel = false;
        private bool suppressGroupEvents = false;

        [Serializable]
        private class CopyPasteData
        {
            public List<string> nodeGuids = new List<string>();
            public List<GroupData> groupData = new List<GroupData>();

            public void AddGraphElements(IEnumerable<GraphElement> elementsToCopy)
            {
                foreach (var element in elementsToCopy)
                {
                    var nodeView = element as NodeView;
                    if (nodeView != null && nodeView.node is not RootNode)
                    {
                        nodeGuids.Add(nodeView.node.guid);
                    }

                    var groupView = element as GroupView;
                    if (groupView != null)
                    {
                        var group = new GroupData
                        {
                            guid = groupView.Guid,
                            title = groupView.title,
                            nodeGuids = groupView.Children().OfType<NodeView>()
                                .Select(nv => nv.node.guid).ToList()
                        };
                        groupData.Add(group);
                    }
                }
            }
        }

        private class EdgeToCreate
        {
            public NodeView parent;
            public NodeView child;
            public string portName;
        };

        public BehaviourTreeView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new HierarchySelector());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var minimap = new MiniMap {anchored = true};
            minimap.SetPosition(new Rect(10, 10, 200, 140));
            Add(minimap);

            searchToolbar = new ToolbarSearchField
            {
                style =
                {
                    position = Position.Absolute,
                    right = 10,
                    top = 10
                }
            };

            var searchContainer = new VisualElement();
            searchContainer.style.position = Position.Absolute;
            searchContainer.style.right = 10;
            searchContainer.style.top = 10;
            searchContainer.style.flexDirection = FlexDirection.Row;
            searchContainer.style.alignItems = Align.Center;

            searchToolbar = new ToolbarSearchField();
            searchToolbar.style.width = 200;
            searchToolbar.RegisterValueChangedCallback(OnSearchValueChanged);
            searchContainer.Add(searchToolbar);

            var clearButton = new Button(() => searchToolbar.value = "") {text = "×"};
            clearButton.style.marginLeft = 5;
            searchContainer.Add(clearButton);

            Add(searchContainer);

            serializeGraphElements = (items) =>
            {
                var copyPasteData = new CopyPasteData();
                copyPasteData.AddGraphElements(items);
                var data = JsonUtility.ToJson(copyPasteData);
                return data;
            };

            elementsAddedToGroup += (grp, elems) =>
            {
                if (suppressGroupEvents) return;
                if (grp is GroupView gv)
                {
                    RecordMultipleChanges("Add Nodes to Group");
                    foreach (var nv in elems.OfType<NodeView>())
                        AddNodeToGroupModel(nv, gv);
                }
            };

            groupTitleChanged += (grp, title) =>
            {
                if (grp is GroupView gv)
                {
                    RenameGroupModel(gv.Guid, title);
                }
            };

            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.shiftKey && evt.keyCode == KeyCode.G)
                {
                    var mousePosition = this.WorldToLocal(Event.current.mousePosition);
                    CreateGroup("New Group", mousePosition);
                    evt.StopPropagation();
                }
            });

            elementsRemovedFromGroup += (grp, elems) =>
            {
                if (suppressGroupEvents) return;
                if (grp is GroupView gv)
                {
                    RecordMultipleChanges("Remove Nodes from Group");
                    foreach (var nv in elems.OfType<NodeView>())
                        RemoveNodeFromGroupModel(nv, gv);
                }
            };

            unserializeAndPaste = (_, data) =>
            {
                var window = BehaviourTreeEditorWindow.Instance;
                var targetView = window.CurrentTreeView;
                var targetTree = window.CurrentSerializer;

                targetTree.BeginBatch();

                targetView.ClearSelection();
                ClearSelection();

                var copyData = JsonUtility.FromJson<CopyPasteData>(data);
                var oldToNewMapping = new Dictionary<string, string>();
                var nodesToCopy = new List<NodeView>();
                var edgesToCreate = new List<EdgeToCreate>();

                foreach (var guid in copyData.nodeGuids)
                    if (FindNodeView(guid) is NodeView nv)
                        nodesToCopy.Add(nv);

                foreach (var guid in copyData.nodeGuids)
                {
                    if (FindNodeView(guid) is NodeView nv)
                    {
                        var parent = nv.NodeParent;
                        if (nodesToCopy.Contains(parent))
                            edgesToCreate.Add(new EdgeToCreate {parent = parent, child = nv});
                    }
                }

                foreach (var nv in nodesToCopy)
                {
                    var clone = targetTree.CloneNode(nv.node, nv.node.position + Vector2.one * 50);
                    var cloneView = targetView.CreateNodeView(clone);
                    targetView.AddToSelection(cloneView);
                    oldToNewMapping[nv.node.guid] = clone.guid;
                }

                foreach (var edge in edgesToCreate)
                {
                    var newParent = targetView.FindNodeView(oldToNewMapping[edge.parent.node.guid]);
                    var newChild = targetView.FindNodeView(oldToNewMapping[edge.child.node.guid]);


                    targetTree.AddChild(newParent.node, newChild.node);
                    targetView.AddChild(newParent, newChild);
                }

                foreach (var gd in copyData.groupData)
                {
                    var groupView = targetView.CreateGroup(gd.title, gd.position);
                    groupView.Guid = gd.guid;

                    foreach (var oldNodeGuid in gd.nodeGuids)
                    {
                        if (oldToNewMapping.TryGetValue(oldNodeGuid, out var newGuid)
                            && targetView.FindNodeView(newGuid) is NodeView childNV)
                        {
                            groupView.AddElement(childNV);
                            targetView.AddNodeToGroupModel(childNV, groupView);
                        }
                    }
                }

                targetTree.EndBatch();
            };

            canPasteSerializedData = (_) => true;
            viewTransformChanged += OnViewTransformChanged;

            RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    foreach (var group in graphElements.OfType<GroupView>())
                    {
                        var groupData = serializer?.tree?.groups.FirstOrDefault(g => g.guid == group.Guid);
                        if (groupData != null && groupData.position != group.GetPosition().position)
                        {
                            RecordGroupChange("Move Group");
                            groupData.position = group.GetPosition().position;
                            serializer?.ApplyChanges();
                        }
                    }
                }
            }, TrickleDown.TrickleDown);
        }

        private void RecordGroupChange(string operationName)
        {
            if (serializer == null || serializer.tree == null) return;

            Undo.RegisterCompleteObjectUndo(serializer.tree, operationName);
            EditorUtility.SetDirty(serializer.tree);
        }

        private void RecordMultipleChanges(string operationName)
        {
            if (serializer == null || serializer.tree == null) return;

            Undo.RegisterCompleteObjectUndo(serializer.tree, operationName);
            EditorUtility.SetDirty(serializer.tree);
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            var searchText = evt.newValue.ToLower();

            foreach (var node in nodes.ToList())
            {
                var nodeView = node as NodeView;
                if (nodeView != null)
                {
                    nodeView.RemoveFromClassList("highlight");
                    nodeView.RemoveFromClassList("strong-highlight");
                    nodeView.RemoveFromClassList("weak-highlight");
                    nodeView.RemoveFromClassList("in-group-highlight");

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        var nameMatch = nodeView.node.Title.ToLower().Contains(searchText.ToLower());
                        var descMatch = nodeView.node.description != null &&
                                        nodeView.node.description.ToLower().Contains(searchText.ToLower());
                        var isInGroup = graphElements.OfType<GroupView>().Any(g =>
                            g.ContainsElement(nodeView) && g.title.ToLower().Contains(searchText.ToLower()));

                        if (nameMatch && descMatch)
                        {
                            nodeView.AddToClassList("strong-highlight");
                        }
                        else if (nameMatch)
                        {
                            nodeView.AddToClassList("highlight");
                        }
                        else if (descMatch)
                        {
                            nodeView.AddToClassList("weak-highlight");
                        }
                        else if (isInGroup)
                        {
                            nodeView.AddToClassList("in-group-highlight");
                        }
                    }
                }
            }
        }

        private void OnViewTransformChanged(GraphView graphView)
        {
            var position = contentViewContainer.transform.position;
            var scale = contentViewContainer.transform.scale;
            serializer.SetViewTransform(position, scale);
        }

        public GroupView CreateGroup(string title, Vector2 position)
        {
            RecordGroupChange("Create Group");

            var group = new GroupView
            {
                title = title,
                Guid = GUID.Generate().ToString()
            };

            group.SetPosition(new Rect(position, new Vector2(200, 200)));
            AddElement(group);

            var groupData = new GroupData
            {
                guid = group.Guid,
                title = title,
                position = position
            };

            serializer.tree.groups.Add(groupData);
            serializer.ApplyChanges();

            return group;
        }

        public void AddNodeToGroupModel(NodeView nodeView, GroupView groupView)
        {
            var gd = serializer.tree.groups.FirstOrDefault(g => g.guid == groupView.Guid);
            if (gd != null && !gd.nodeGuids.Contains(nodeView.node.guid))
            {
                RecordGroupChange("Add Node to Group");
                gd.nodeGuids.Add(nodeView.node.guid);
                serializer.ApplyChanges();
            }
        }

        public void RemoveNodeFromGroupModel(NodeView nodeView, GroupView groupView)
        {
            var gd = serializer.tree.groups.FirstOrDefault(g => g.guid == groupView.Guid);
            if (gd != null && gd.nodeGuids.Remove(nodeView.node.guid))
            {
                RecordGroupChange("Remove Node from Group");
                serializer.ApplyChanges();
            }
        }

        public void RenameGroupModel(string guid, string newTitle)
        {
            var gd = serializer.tree.groups.FirstOrDefault(g => g.guid == guid);
            if (gd != null && gd.title != newTitle)
            {
                RecordGroupChange("Rename Group");
                gd.title = newTitle;
                serializer.ApplyChanges();
            }
        }

        public NodeView FindNodeView(Node node)
        {
            return node == null ? null : GetNodeByGuid(node.guid) as NodeView;
        }

        public NodeView FindNodeView(string guid)
        {
            return GetNodeByGuid(guid) as NodeView;
        }

        public void ClearView()
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;
        }

        public void ValidateNodes()
        {
            foreach (var nodeView in nodes.OfType<NodeView>())
            {
                nodeView.UpdateValidation();
            }
        }

        public void PopulateView(SerializedBehaviourTree tree)
        {
            if (serializer != null && serializer.tree != null)
            {
                Undo.RegisterCompleteObjectUndo(serializer.tree, "Populate View");
            }

            serializer = tree;
            suppressGroupEvents = true;
            ClearView();

            Debug.Assert(serializer.tree.rootNode != null);

            foreach (var groupData in serializer.tree.groups)
            {
                var groupView = new GroupView
                {
                    title = groupData.title,
                    Guid = groupData.guid
                };
                groupView.SetPosition(new Rect(groupData.position, new Vector2(200, 200)));
                AddElement(groupView);
            }

            serializer.tree.nodes.ForEach(n => CreateNodeView(n));

            serializer.tree.nodes.ForEach(n =>
            {
                var children = BehaviourTree.GetChildren(n);
                if (children.Count > 0)
                {
                    children.ForEach(c =>
                    {
                        var parentView = FindNodeView(n);
                        var childView = FindNodeView(c);

                        // if (parentView.node is RequirementActionNode requirementNode && childView != null)
                        // {
                        //     var isTrueAction = requirementNode.TrueNode == c;
                        //     var isFalseAction = requirementNode.FalseNode == c;
                        //
                        //     if (isTrueAction)
                        //     {
                        //         CreateEdgeRequirementView(parentView, childView, true);
                        //     }
                        //     else if (isFalseAction)
                        //     {
                        //         CreateEdgeRequirementView(parentView, childView, false);
                        //     }
                        //     else
                        //     {
                        //         Debug.LogWarning(
                        //             $"Child {c.GetType().Name} не найден в TrueNode/FalseNode у {requirementNode.GetType().Name}");
                        //     }
                        //
                        //     return;
                        // }

                        if (parentView == null || childView == null)
                            return;

                        Debug.Assert(parentView != null, "Invalid parent after deserialising");
                        Debug.Assert(childView != null,
                            $"Null child view after deserialising parent '{parentView.node.GetType().Name}'");
                        CreateEdgeView(parentView, childView);
                    });
                }
            });

            foreach (var groupData in serializer.tree.groups)
            {
                var groupView = GetGroupViewByGuid(groupData.guid);
                if (groupView != null)
                {
                    foreach (var nodeGuid in groupData.nodeGuids)
                    {
                        var nodeView = FindNodeView(nodeGuid);
                        if (nodeView != null)
                        {
                            groupView.AddElement(nodeView);
                        }
                    }
                }
            }

            contentViewContainer.transform.position = serializer.tree.viewPosition;
            contentViewContainer.transform.scale = serializer.tree.viewScale;

            suppressGroupEvents = false;

            if (serializer != null && serializer.tree != null)
            {
                EditorUtility.SetDirty(serializer.tree);
            }

            ValidateNodes();
        }

        private GroupView GetGroupViewByGuid(string guid)
        {
            return graphElements.OfType<GroupView>().FirstOrDefault(g => g.Guid == guid);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (dontUpdateModel)
            {
                return graphViewChange;
            }

            var blockedDeletes = new List<GraphElement>();

            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    var groupView = elem as GroupView;
                    if (groupView != null)
                    {
                        var groupData = serializer.tree.groups.FirstOrDefault(g => g.guid == groupView.Guid);
                        if (groupData != null)
                        {
                            RecordGroupChange("Delete Group");
                            serializer.tree.groups.Remove(groupData);
                            serializer.ApplyChanges();
                        }
                    }

                    var nodeView = elem as NodeView;
                    if (nodeView != null)
                    {
                        if (nodeView.node is not RootNode)
                        {
                            OnNodeSelected(null);
                            serializer.DeleteNode(nodeView.node);
                        }
                        else
                        {
                            blockedDeletes.Add(elem);
                        }
                    }

                    var edge = elem as Edge;
                    if (edge != null)
                    {
                        if (string.IsNullOrEmpty(edge.output.portName))
                        {
                            var parentView = edge.output.node as NodeView;
                            var childView = edge.input.node as NodeView;
                            serializer.RemoveChild(parentView.node, childView.node);
                        }
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    if (string.IsNullOrEmpty(edge.output.portName))
                    {
                        var parentView = edge.output.node as NodeView;
                        var childView = edge.input.node as NodeView;
                        serializer.AddChild(parentView.node, childView.node);
                    }
                });
            }

            nodes.ForEach((n) =>
            {
                var view = n as NodeView;
                view.SetupDataBinding();
                view.SortChildren();
            });

            foreach (var elem in blockedDeletes)
            {
                graphViewChange.elementsToRemove.Remove(elem);
            }

            ValidateNodes();
            return graphViewChange;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            CreateNodeWindow.Show(evt.mousePosition, null);
        }

        public NodeView CreateNode(Type type, Vector2 position, NodeView parentView)
        {
            serializer.BeginBatch();

            var node = serializer.CreateNode(type, position);
            if (parentView != null)
            {
                serializer.AddChild(parentView.node, node);
            }

            var nodeView = CreateNodeView(node);
            if (parentView != null)
            {
                AddChild(parentView, nodeView);
            }

            serializer.EndBatch();
            ValidateNodes();

            return nodeView;
        }

        public NodeView CreateNodeWithChild(Type type, Vector2 position, NodeView childView)
        {
            serializer.BeginBatch();

            var node = serializer.CreateNode(type, position);

            foreach (var connection in childView.input.connections)
            {
                var childParent = connection.output.node as NodeView;
                serializer.RemoveChild(childParent.node, childView.node);
            }

            serializer.AddChild(node, childView.node);

            var nodeView = CreateNodeView(node);
            if (nodeView != null)
            {
                AddChild(nodeView, childView);
            }

            serializer.EndBatch();
            return nodeView;
        }

        public NodeView CreateNodeView(Node node)
        {
            var nodeView = new NodeView(node, BehaviourTreeEditorWindow.Instance.nodeXml, this);
            AddElement(nodeView);
            return nodeView;
        }

        public void AddChild(NodeView parentView, NodeView childView)
        {
            if (parentView.output.capacity == Port.Capacity.Single)
            {
                RemoveElements(parentView.output.connections);
            }

            RemoveElements(childView.input.connections);

            CreateEdgeView(parentView, childView);
        }

        private void CreateEdgeView(NodeView parentView, NodeView childView)
        {
            var edge = parentView.output.ConnectTo(childView.input);
            AddElement(edge);
        }

        public void RemoveElements(IEnumerable<GraphElement> elementsToRemove)
        {
            dontUpdateModel = true;
            DeleteElements(elementsToRemove);
            dontUpdateModel = false;
        }

        public void UpdateNodeStates()
        {
            if (serializer == null || serializer.tree == null || serializer.tree.TreeBehaviourTreeContext == null)
            {
                return;
            }

            var tickResults = serializer.tree.TreeBehaviourTreeContext.TickResults;
            if (tickResults != null)
            {
                nodes.ForEach(n =>
                {
                    var view = n as NodeView;
                    view.UpdateState(tickResults);
                });
            }
        }

        public void SelectNode(NodeView nodeView)
        {
            ClearSelection();
            if (nodeView != null)
            {
                AddToSelection(nodeView);
            }
        }

        public void SelectNode(Node node)
        {
            var nodeView = FindNodeView(node);
            SelectNode(nodeView);
        }

        public void InspectNode(Node node)
        {
            var nodeView = FindNodeView(node);
            OnNodeSelected(nodeView);
        }

        internal void DeleteNodeView(Node n)
        {
            var nodeView = FindNodeView(n);
            if (nodeView != null)
            {
                if (nodeView.input != null)
                {
                    RemoveElements(nodeView.input.connections);
                }

                if (nodeView.output != null)
                {
                    RemoveElements(nodeView.output.connections);
                }

                RemoveElement(nodeView);
            }

            ValidateNodes();
        }

        public void ExpandSubtree(NodeView nodeView)
        {
            if (nodeView.node is SubTree)
            {
                BehaviourTree tree = null;
                var subtreeParent = nodeView.NodeParent.node;
                var subtree = nodeView.node as SubTree;
                tree = subtree.treeAsset;
                serializer.BeginBatch();
                serializer.RemoveChild(subtreeParent, subtree);
                serializer.DeleteTree(subtree);
                serializer.CloneTree(tree.rootNode.child, subtreeParent, subtree.position);
                serializer.EndBatch();
                PopulateView(serializer);
            }

            if (nodeView.node is SubTreeDecorator)
            {
                BehaviourTree tree = null;
                var subtreeParent = nodeView.NodeParent.node;
                var subtree = nodeView.node as SubTreeDecorator;
                tree = subtree.treeAsset;
                serializer.BeginBatch();
                serializer.RemoveChild(subtreeParent, subtree);
                serializer.DeleteTree(subtree);
                serializer.CloneTree(tree.rootNode.child, subtreeParent, subtree.position);
                serializer.EndBatch();
                PopulateView(serializer);
            }
        }

        public void CreateSubTree(NodeView nodeView)
        {
            var window = BehaviourTreeEditorWindow.Instance;
            InspectNode(null);

            var tree = BehaviourTreeEditorUtility.CreateNewTree();
            if (tree)
            {
                var subTreeRootParent = nodeView.NodeParent.node;
                var subTreeRoot = nodeView.node;

                {
                    var position = tree.rootNode.position;
                    var newTree = new SerializedBehaviourTree(tree);
                    newTree.BeginBatch();
                    newTree.CloneTree(subTreeRoot, tree.rootNode, position);
                    newTree.EndBatch();
                    window.NewTab(tree, false, tree.name);
                }

                {
                    serializer.BeginBatch();

                    var subTreeNode = serializer.CreateNode<SubTree>(subTreeRoot.position) as SubTree;
                    serializer.SetNodeProperty(subTreeNode, nameof(SubTree.treeAsset), tree);

                    serializer.RemoveChild(subTreeRootParent, subTreeRoot);
                    serializer.AddChild(subTreeRootParent, subTreeNode);
                    serializer.DeleteTree(subTreeRoot);

                    serializer.EndBatch();

                    PopulateView(serializer);
                    InspectNode(subTreeNode);
                    SelectNode(subTreeNode);
                }
            }
        }
    }
}