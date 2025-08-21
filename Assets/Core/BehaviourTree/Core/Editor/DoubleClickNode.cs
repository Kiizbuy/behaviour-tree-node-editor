using UnityEditor;
using UnityEngine.Device;
using UnityEngine.UIElements;

namespace BehaviourTreeLogic
{
    public class DoubleClickNode : MouseManipulator
    {
        private double _time;
        private double _doubleClickDuration = 0.3;

        public DoubleClickNode()
        {
            _time = EditorApplication.timeSinceStartup;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStopManipulation(evt))
                return;

            var clickedElement = evt.target as NodeView;
            if (clickedElement == null)
            {
                var ve = evt.target as VisualElement;
                clickedElement = ve.GetFirstAncestorOfType<NodeView>();
                if (clickedElement == null)
                    return;
            }

            var duration = EditorApplication.timeSinceStartup - _time;
            if (duration < _doubleClickDuration)
            {
                OnDoubleClick(evt, clickedElement);
                evt.StopImmediatePropagation();
            }

            _time = EditorApplication.timeSinceStartup;
        }

        private void OpenScriptForNode(NodeView clickedElement)
        {
            var script = BehaviourTreeEditorUtility.GetNodeScriptPath(clickedElement);
            if (script)
            {
                AssetDatabase.OpenAsset(script);
                BehaviourTreeEditorWindow.Instance.CurrentTreeView.RemoveFromSelection(clickedElement);
            }
        }

        private void OpenSubtree(NodeView clickedElement)
        {
            var subtreeNode = clickedElement.node as SubTree;
            var treeToFocus = subtreeNode.treeAsset;
            if (Application.isPlaying)
            {
                treeToFocus = subtreeNode.treeInstance;
            }

            if (treeToFocus != null)
            {
                BehaviourTreeEditorWindow.Instance.NewTab(treeToFocus, true, treeToFocus.name);
            }
        }

        private void OnDoubleClick(MouseDownEvent evt, NodeView clickedElement)
        {
            if (clickedElement.node is SubTree)
            {
                OpenSubtree(clickedElement);
            }
            else
            {
                OpenScriptForNode(clickedElement);
            }
        }
    }
}