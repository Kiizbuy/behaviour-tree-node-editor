using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeLogic
{
    public class GroupView : Group
    {
        public string Guid;
        private bool _suppressUndo;

        public GroupView()
        {
            var label = new Label("[SHIFT + Drag node outside] - Remove node from group\n[SHIFT + Drag node inside] - Add node to group")
            {
                style =
                {
                    fontSize = 10,
                    unityTextAlign = TextAnchor.LowerRight,
                    marginTop = 5,
                    marginBottom = 5
                }
            };
            Add(label);
        }
        
        public void SetPositionWithUndo(Rect newPos, SerializedBehaviourTree serializer)
        {
            if (_suppressUndo) return;

            var groupData = serializer.tree.groups.FirstOrDefault(g => g.guid == Guid);
            if (groupData != null && groupData.position != newPos.position)
            {
                Undo.RegisterCompleteObjectUndo(serializer.tree, "Move Group");
                groupData.position = newPos.position;
                serializer.ApplyChanges();
            }

            _suppressUndo = true;
            base.SetPosition(newPos);
            _suppressUndo = false;
        }
    }
}