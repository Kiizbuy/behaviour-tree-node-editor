using System;
using Core.BehaviourTree.Debug;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Callbacks;
using Unity.Profiling;

namespace BehaviourTreeLogic
{
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        [Serializable]
        public class PendingScriptCreate
        {
            public bool pendingCreate = false;
            public string scriptName = "";
            public string sourceGuid = "";
            public bool isSourceParent = false;
            public Vector2 nodePosition;

            public void Reset()
            {
                pendingCreate = false;
                scriptName = "";
                sourceGuid = "";
                isSourceParent = false;
                nodePosition = Vector2.zero;
            }
        }

        public class BehaviourTreeEditorAssetModificationProcessor : AssetModificationProcessor
        {
            private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
            {
                if (HasOpenInstances<BehaviourTreeEditorWindow>())
                {
                    var wnd = GetWindow<BehaviourTreeEditorWindow>();
                    var tabs = wnd.tabView.Query<TreeViewTab>().ToList();
                    foreach (var tab in tabs)
                    {
                        if (AssetDatabase.GetAssetPath(tab.serializer.tree) == path)
                        {
                            tab.Close();
                        }
                    }
                }

                return AssetDeleteResult.DidNotDelete;
            }
        }

        private static readonly ProfilerMarker editorUpdate = new ProfilerMarker("BehaviourTree.EditorUpdate");
        public static BehaviourTreeEditorWindow Instance;
        public BehaviourTreeProjectSettings settings;
        public VisualTreeAsset behaviourTreeXml;
        public VisualTreeAsset nodeXml;
        public StyleSheet behaviourTreeStyle;
        public TextAsset scriptTemplateActionNode;
        public TextAsset scriptTemplateConditionNode;
        public TextAsset scriptTemplateCompositeNode;
        public TextAsset scriptTemplateDecoratorNode;
        public TextAsset scriptTemplatUtilityEvaluatorNode;

        public BehaviourTreeView CurrentTreeView
        {
            get
            {
                var activeTab = tabView?.activeTab as TreeViewTab;
                if (activeTab != null)
                {
                    return activeTab.treeView;
                }

                return null;
            }
        }

        public BehaviourTree CurrentTree
        {
            get
            {
                var activeTab = tabView?.activeTab as TreeViewTab;
                if (activeTab != null)
                {
                    return activeTab.serializer.tree;
                }

                return null;
            }
        }

        public SerializedBehaviourTree CurrentSerializer
        {
            get
            {
                var activeTab = tabView?.activeTab as TreeViewTab;
                if (activeTab != null)
                {
                    return activeTab.serializer;
                }

                return null;
            }
        }

        public InspectorView inspectorView;
        public BlackboardView blackboardView;
        public OverlayView overlayView;
        public ToolbarMenu toolbarMenu;
        public Label versionLabel;
        public NewScriptDialogView newScriptDialog;
        public TabView tabView;
        public BehaviourTreeEditorWindowState windowState;

        [SerializeField] public PendingScriptCreate pendingScriptCreate = new PendingScriptCreate();


        [MenuItem("Window/AI/BehaviourTree")]
        public static void OpenWindow()
        {
            var wnd = GetWindow<BehaviourTreeEditorWindow>();
            wnd.titleContent = new GUIContent("BehaviourTreeEditor");
            wnd.minSize = new Vector2(800, 600);
        }

        public static void OpenWindow(BehaviourTree tree)
        {
            var wnd = GetWindow<BehaviourTreeEditorWindow>();
            wnd.titleContent = new GUIContent("BehaviourTreeEditor");
            wnd.minSize = new Vector2(800, 600);
            wnd.NewTab(tree, true, tree.name);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is BehaviourTree)
            {
                OpenWindow(Selection.activeObject as BehaviourTree);
                return true;
            }

            return false;
        }

        public void CreateGUI()
        {
            Instance = this;
            settings = BehaviourTreeProjectSettings.GetOrCreateSettings();
            windowState = settings.windowState;
            var root = rootVisualElement;

            TryLoadStyles();

            var visualTree = behaviourTreeXml;
            visualTree.CloneTree(root);

            var styleSheet = behaviourTreeStyle;
            root.styleSheets.Add(styleSheet);

            inspectorView = root.Q<InspectorView>();
            blackboardView = root.Q<BlackboardView>();
            toolbarMenu = root.Q<ToolbarMenu>();
            overlayView = root.Q<OverlayView>("OverlayView");
            newScriptDialog = root.Q<NewScriptDialogView>("NewScriptDialogView");
            tabView = root.Q<TabView>();
            tabView.activeTabChanged -= OnTabChanged;
            tabView.activeTabChanged += OnTabChanged;

            versionLabel = root.Q<Label>("Version");

            toolbarMenu.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                toolbarMenu.menu.MenuItems().Clear();
                var behaviourTrees = BehaviourTreeEditorUtility.GetAssetPaths<BehaviourTree>();
                behaviourTrees.ForEach(path =>
                {
                    var fileName = System.IO.Path.GetFileName(path);
                    toolbarMenu.menu.AppendAction($"{fileName}", (a) =>
                    {
                        var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
                        NewTab(tree, true, tree.name);
                    });
                });
                if (EditorApplication.isPlaying)
                {
                    toolbarMenu.menu.AppendSeparator();
                    foreach (var tuple in MainDebugBrainProvider.Get())
                    {
                        toolbarMenu.menu.AppendAction(
                            tuple.title,
                            (a) => { NewTab(tuple.brain, true, tuple.title); });
                    }
                }

                toolbarMenu.menu.AppendSeparator();
                toolbarMenu.menu.AppendAction("New Tree...", (a) => OnToolbarNewAsset());
            });


            overlayView.OnTreeSelected -= t => NewTab(t, true, t.name);
            overlayView.OnTreeSelected += t => NewTab(t, true, t.name);

            newScriptDialog.style.visibility = Visibility.Hidden;

            windowState.Restore(this);

            if (pendingScriptCreate != null && pendingScriptCreate.pendingCreate)
            {
                CreatePendingScriptNode();
            }
        }

        private void TryLoadStyles()
        {
            if (behaviourTreeXml == null)
            {
                behaviourTreeXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(settings.GetBehaviourTreeXmlPath());
            }

            if (behaviourTreeStyle == null)
            {
                behaviourTreeStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(settings.GetBehaviourTreeStylePath());
            }

            if (nodeXml == null)
            {
                nodeXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(settings.GetNodeXmlPath());
            }
        }

        private void OnTabChanged(Tab previous, Tab current)
        {
            var newTab = current as TreeViewTab;
            inspectorView.Clear();
            blackboardView?.Bind(newTab.serializer);
            if (!newTab.isRuntimeTab)
            {
                windowState.TabChanged(tabView.selectedTabIndex);
            }
        }

        public void OnTabClosed(Tab tab)
        {
            var treeTab = tab as TreeViewTab;
            if (!treeTab.isRuntimeTab)
            {
                windowState.TabClosed(treeTab);
            }
        }

        private void CreatePendingScriptNode()
        {
            // #TODO: Unify this with CreateNodeWindow.CreateNode

            if (CurrentTreeView == null)
            {
                return;
            }

            var source = CurrentTreeView.GetNodeByGuid(pendingScriptCreate.sourceGuid) as NodeView;
            var nodeType = Type.GetType($"{pendingScriptCreate.scriptName}, Assembly-CSharp");
            if (nodeType != null)
            {
                NodeView createdNode;
                if (source != null)
                {
                    if (pendingScriptCreate.isSourceParent)
                    {
                        createdNode = CurrentTreeView.CreateNode(nodeType, pendingScriptCreate.nodePosition, source);
                    }
                    else
                    {
                        createdNode =
                            CurrentTreeView.CreateNodeWithChild(nodeType, pendingScriptCreate.nodePosition, source);
                    }
                }
                else
                {
                    createdNode = CurrentTreeView.CreateNode(nodeType, pendingScriptCreate.nodePosition, null);
                }

                CurrentTreeView.SelectNode(createdNode);
                BehaviourTreeEditorUtility.OpenScriptInEditor(createdNode);
            }

            pendingScriptCreate.Reset();
        }

        private void OnUndoRedo()
        {
            if (CurrentTree != null)
            {
                CurrentSerializer.serializedObject.Update();
                CurrentTreeView.PopulateView(CurrentSerializer);
            }
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall += OnExitPlayMode;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall += OnEnterPlayMode;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    inspectorView?.Clear();
                    break;
            }
        }

        private void CloseRuntimeTabs()
        {
            var tabs = tabView.Query<TreeViewTab>().ToList();
            foreach (var tab in tabs)
            {
                if (tab.isRuntimeTab)
                {
                    tab.Close();
                }
            }
        }

        private void OnEnterPlayMode()
        {
            OnSelectionChange();
        }

        private void OnExitPlayMode()
        {
            OnSelectionChange();
            CloseRuntimeTabs();
        }

        private void OnSelectionChange()
        {
            foreach (var tuple in MainDebugBrainProvider.Get())
            {
                var tabName = tuple.title;
                var existingTab = tabView.Q<TreeViewTab>(tabName);
                if (existingTab != null)
                {
                    tabView.activeTab = existingTab;
                    return;
                }

                NewTab(tuple.brain, true, tabName);
            }
        }

        public void NewTab(BehaviourTree newTree, bool focus, string tabName)
        {
            var existingTab = tabView.Q<TreeViewTab>(tabName);
            if (existingTab != null)
            {
                if (focus)
                {
                    tabView.activeTab = existingTab;
                }

                return;
            }

            var newTab = new TreeViewTab(newTree, behaviourTreeStyle, tabName);
            tabView.Add(newTab);

            windowState.TabOpened(newTab);

            if (focus)
            {
                tabView.activeTab = newTab;
                overlayView?.Hide();
            }
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                editorUpdate.Begin();
                CurrentTreeView?.UpdateNodeStates();
                editorUpdate.End();
            }
        }

        private void OnToolbarNewAsset()
        {
            var tree = BehaviourTreeEditorUtility.CreateNewTree();
            if (tree)
            {
                NewTab(tree, true, tree.name);
            }
        }

        public void InspectNode(SerializedBehaviourTree serializer, NodeView node)
        {
            inspectorView.UpdateSelection(serializer, node);
        }
    }
}