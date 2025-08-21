using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTreeLogic.Utility;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace BehaviourTreeLogic
{
    public class CreateNodeWindow : ScriptableObject, ISearchWindowProvider
    {
        private Texture2D icon;
        private BehaviourTreeView treeView;
        private NodeView source;
        private bool isSourceParent;
        private BehaviourTreeEditorUtility.ScriptTemplate[] scriptFileAssets;

        private TextAsset GetScriptTemplate(int type)
        {
            var projectSettings = BehaviourTreeProjectSettings.GetOrCreateSettings();

            switch (type)
            {
                case 0:
                    if (projectSettings.scriptTemplateActionNode)
                    {
                        return projectSettings.scriptTemplateActionNode;
                    }

                    return BehaviourTreeEditorWindow.Instance.scriptTemplateActionNode;
                case 1:
                    if (projectSettings.scriptTemplateConditionNode)
                    {
                        return projectSettings.scriptTemplateConditionNode;
                    }

                    return BehaviourTreeEditorWindow.Instance.scriptTemplateConditionNode;
                case 2:
                    if (projectSettings.scriptTemplateCompositeNode)
                    {
                        return projectSettings.scriptTemplateCompositeNode;
                    }

                    return BehaviourTreeEditorWindow.Instance.scriptTemplateCompositeNode;
                case 3:
                    if (projectSettings.scriptTemplateDecoratorNode)
                    {
                        return projectSettings.scriptTemplateDecoratorNode;
                    }

                    return BehaviourTreeEditorWindow.Instance.scriptTemplateDecoratorNode;
                case 4:
                    if (projectSettings.scriptTemplateUtilityEvaluatorNode)
                    {
                        return projectSettings.scriptTemplateUtilityEvaluatorNode;
                    }

                    return BehaviourTreeEditorWindow.Instance.scriptTemplatUtilityEvaluatorNode;
            }

            Debug.LogError("Unhandled script template type:" + type);
            return null;
        }

        public void Initialise(BehaviourTreeView treeView, NodeView source, bool isSourceParent)
        {
            this.treeView = treeView;
            this.source = source;
            this.isSourceParent = isSourceParent;

            icon = new Texture2D(1, 1);
            icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            icon.Apply();

            scriptFileAssets = new BehaviourTreeEditorUtility.ScriptTemplate[]
            {
                new BehaviourTreeEditorUtility.ScriptTemplate
                    {templateFile = GetScriptTemplate(0), defaultFileName = "NewActionNode", subFolder = "Actions"},
                new BehaviourTreeEditorUtility.ScriptTemplate
                {
                    templateFile = GetScriptTemplate(1), defaultFileName = "NewConditionNode", subFolder = "Conditions"
                },
                new BehaviourTreeEditorUtility.ScriptTemplate
                {
                    templateFile = GetScriptTemplate(2), defaultFileName = "NewCompositeNode", subFolder = "Composites"
                },
                new BehaviourTreeEditorUtility.ScriptTemplate
                {
                    templateFile = GetScriptTemplate(3), defaultFileName = "NewDecoratorNode", subFolder = "Decorators"
                },
                new BehaviourTreeEditorUtility.ScriptTemplate
                {
                    templateFile = GetScriptTemplate(4), defaultFileName = "NewUtilityEvaluatorNode",
                    subFolder = "UtilityEvaluators"
                },
            };
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var editorWindow = BehaviourTreeEditorWindow.Instance;
            var screenPoint = context.screenMousePosition;
            var windowPoint = editorWindow.rootVisualElement
                .ChangeCoordinatesTo(
                    editorWindow.rootVisualElement.parent,
                    screenPoint - editorWindow.position.position);
            var graphPosition = treeView.contentViewContainer.WorldToLocal(windowPoint);

            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            #region Groups

            tree.Add(new SearchTreeEntry(new GUIContent("Create Group"))
            {
                level = 1,
                userData = (Action) (() => { treeView.CreateGroup("New Group", graphPosition); })
            });

            if (treeView.selection.OfType<NodeView>().Any())
            {
                tree.Add(new SearchTreeEntry(new GUIContent("Group Selected Nodes"))
                {
                    level = 1,
                    userData = (Action) (() =>
                    {
                        var group = treeView.CreateGroup("Grouped Nodes", graphPosition);
                        foreach (var nv in treeView.selection.OfType<NodeView>())
                        {
                            group.AddElement(nv);
                            treeView.AddNodeToGroupModel(nv, group);
                        }
                    })
                });

                // Remove from Group
                var inGroups = treeView.selection
                    .OfType<NodeView>()
                    .Select(nv => nv.GetFirstAncestorOfType<GroupView>())
                    .Where(g => g != null)
                    .Distinct()
                    .ToList();

                if (inGroups.Count > 0)
                {
                    tree.Add(new SearchTreeEntry(new GUIContent("Remove from Group"))
                    {
                        level = 1,
                        userData = (Action) (() =>
                        {
                            foreach (var gv in inGroups)
                            foreach (var nv in treeView.selection.OfType<NodeView>()
                                         .Where(nv => nv.GetFirstAncestorOfType<GroupView>() == gv))
                            {
                                gv.RemoveElement(nv);
                                treeView.RemoveNodeFromGroupModel(nv, gv);
                            }
                        })
                    });
                }
            }

            #endregion


            // Action nodes can only be added as children
            if (isSourceParent || source == null)
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Actions")) {level = 1});
                var types = TypeCache.GetTypesDerivedFrom<ActionNode>();
                foreach (var type in types)
                {
                    // Ignore condition types
                    if (!type.IsSubclassOf(typeof(ConditionNode)))
                    {
                        Action invoke = () => CreateNode(type, context);
                        tree.Add(new SearchTreeEntry(new GUIContent($"{type.Name}")) {level = 2, userData = invoke});
                    }
                }
            }

            // Condition nodes can only be added as children
            if (isSourceParent || source == null)
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Conditions")) {level = 1});
                var types = TypeCache.GetTypesDerivedFrom<ConditionNode>();
                foreach (var type in types)
                {
                    Action invoke = () => CreateNode(type, context);
                    tree.Add(new SearchTreeEntry(new GUIContent($"{type.Name}")) {level = 2, userData = invoke});
                }
            }

            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Composites")) {level = 1});
                {
                    var types = TypeCache.GetTypesDerivedFrom<CompositeNode>();
                    foreach (var type in types)
                    {
                        Action invoke = () => CreateNode(type, context);
                        tree.Add(new SearchTreeEntry(new GUIContent($"{type.Name}")) {level = 2, userData = invoke});
                    }
                }
            }

            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("UtilityEvaluators")) {level = 1});
                {
                    var types = TypeCache.GetTypesDerivedFrom<BaseUtilityEvaluator>();
                    foreach (var type in types)
                    {
                        Action invoke = () => CreateNode(type, context);
                        tree.Add(new SearchTreeEntry(new GUIContent($"{type.Name}")) {level = 2, userData = invoke});
                    }
                }
            }
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Decorators")) {level = 1});
                {
                    var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
                    foreach (var type in types)
                    {
                        Action invoke = () => CreateNode(type, context);
                        tree.Add(new SearchTreeEntry(new GUIContent($"{type.Name}")) {level = 2, userData = invoke});
                    }
                }
            }

            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Subtrees")) {level = 1});
                {
                    var behaviourTrees = BehaviourTreeEditorUtility.GetAssetPaths<BehaviourTree>();
                    behaviourTrees.ForEach(path =>
                    {
                        var fileName = System.IO.Path.GetFileName(path);

                        Action invoke = () =>
                        {
                            var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
                            var subTreeNodeView = CreateNode(typeof(SubTree), context);
                            var subtreeNode = subTreeNodeView.node as SubTree;
                            subtreeNode.treeAsset = tree;
                        };
                        tree.Add(new SearchTreeEntry(new GUIContent($"{fileName}")) {level = 2, userData = invoke});
                    });
                }
            }


            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("New Script ...")) {level = 1});

                Action createActionScript = () => CreateScript(scriptFileAssets[0], context);
                tree.Add(new SearchTreeEntry(new GUIContent($"New Action Script"))
                    {level = 2, userData = createActionScript});

                Action createConditionScript = () => CreateScript(scriptFileAssets[1], context);
                tree.Add(new SearchTreeEntry(new GUIContent($"New Condition Script"))
                    {level = 2, userData = createConditionScript});

                Action createCompositeScript = () => CreateScript(scriptFileAssets[2], context);
                tree.Add(new SearchTreeEntry(new GUIContent($"New Composite Script"))
                    {level = 2, userData = createCompositeScript});

                Action createDecoratorScript = () => CreateScript(scriptFileAssets[3], context);
                tree.Add(new SearchTreeEntry(new GUIContent($"New Decorator Script"))
                    {level = 2, userData = createDecoratorScript});
            }

            {
                Action invoke = () =>
                {
                    var newTree = BehaviourTreeEditorUtility.CreateNewTree();
                    if (newTree)
                    {
                        var subTreeNodeView = CreateNode(typeof(SubTree), context);
                        var subtreeNode = subTreeNodeView.node as SubTree;
                        subtreeNode.treeAsset = newTree;
                    }
                };
                tree.Add(new SearchTreeEntry(new GUIContent("     New Subtree ...")) {level = 1, userData = invoke});
            }


            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var invoke = (Action) searchTreeEntry.userData;
            invoke();
            return true;
        }

        public NodeView CreateNode(Type type, SearchWindowContext context)
        {
            var editorWindow = BehaviourTreeEditorWindow.Instance;

            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition =
                editorWindow.CurrentTreeView.contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeOffset = new Vector2(-75, -20);
            var nodePosition = graphMousePosition + nodeOffset;

            // #TODO: Unify this with CreatePendingScriptNode
            NodeView createdNode;
            if (source != null)
            {
                if (isSourceParent)
                {
                    createdNode = treeView.CreateNode(type, nodePosition, source);
                }
                else
                {
                    createdNode = treeView.CreateNodeWithChild(type, nodePosition, source);
                }
            }
            else
            {
                createdNode = treeView.CreateNode(type, nodePosition, null);
            }

            treeView.SelectNode(createdNode);
            return createdNode;
        }

        public void CreateScript(BehaviourTreeEditorUtility.ScriptTemplate scriptTemplate, SearchWindowContext context)
        {
            var editorWindow = BehaviourTreeEditorWindow.Instance;

            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition =
                editorWindow.CurrentTreeView.contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeOffset = new Vector2(-75, -20);
            var nodePosition = graphMousePosition + nodeOffset;

            BehaviourTreeEditorUtility.CreateNewScript(scriptTemplate, source, isSourceParent, nodePosition);
        }

        public static void Show(Vector2 mousePosition, NodeView source, bool isSourceParent = false)
        {
            var screenPoint = GUIUtility.GUIToScreenPoint(mousePosition);
            var searchWindowProvider = CreateInstance<CreateNodeWindow>();
            searchWindowProvider.Initialise(BehaviourTreeEditorWindow.Instance.CurrentTreeView, source, isSourceParent);
            var windowContext = new SearchWindowContext(screenPoint, 240, 320);
            SearchWindow.Open(windowContext, searchWindowProvider);
        }
    }
}