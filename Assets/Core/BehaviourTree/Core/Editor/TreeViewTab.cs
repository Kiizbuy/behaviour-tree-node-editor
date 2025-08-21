using BehaviourTreeLogic;
using UnityEngine.UIElements;

public class TreeViewTab : Tab
{
    public SerializedBehaviourTree serializer;
    public BehaviourTreeView treeView;
    public bool isRuntimeTab = false;

    public TreeViewTab(BehaviourTree tree, StyleSheet styleSheet, string tabName) : base(tabName)
    {
        isRuntimeTab = tree.name.Contains("Clone");
        name = tabName;
        closeable = true;
        serializer = new SerializedBehaviourTree(tree);

        treeView = new BehaviourTreeView();
        treeView.styleSheets.Add(styleSheet);
        treeView.PopulateView(serializer);
        treeView.OnNodeSelected += OnNodeSelected;

        // TODO: Remove selection when closing the tab

        Add(treeView);

        closed += (tab) => { BehaviourTreeEditorWindow.Instance.OnTabClosed(tab); };
    }

    private void OnNodeSelected(NodeView node)
    {
        var window = BehaviourTreeEditorWindow.Instance;
        window.InspectNode(serializer, node);
    }

    public void Close()
    {
        RemoveFromHierarchy();
    }
}