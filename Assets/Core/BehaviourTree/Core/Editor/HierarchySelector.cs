using UnityEngine.UIElements;

namespace BehaviourTreeLogic {
    public class HierarchySelector : MouseManipulator {

        protected override void RegisterCallbacksOnTarget() {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget() {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent evt) {
            if (!CanStopManipulation(evt))
                return; 
            
            var graphView = target as BehaviourTreeView;
            if (graphView == null)
                return;

            var clickedElement = evt.target as NodeView;
            if (clickedElement == null) {
                var ve = evt.target as VisualElement;
                clickedElement = ve.GetFirstAncestorOfType<NodeView>();
                if (clickedElement == null)
                    return;
            }

            var forceSelectNodeHierarchy = BehaviourTreeEditorWindow.Instance.settings.autoSelectNodeHierarchy;
            if (evt.ctrlKey || forceSelectNodeHierarchy) {
                graphView.ClearSelection();
                SelectChildren(evt, graphView, clickedElement);
            }
        }

        private void SelectChildren(MouseDownEvent evt, BehaviourTreeView graphView, NodeView clickedElement) {
            // Add children to selection so the root element can be moved
            BehaviourTree.Traverse(clickedElement.node, node => {
                var view = graphView.FindNodeView(node);
                graphView.AddToSelection(view);
            });

            // Insepctor should always focus on root element so this needs to be selected last
            graphView.RemoveFromSelection(clickedElement);
            graphView.AddToSelection(clickedElement);
        }
    }
}