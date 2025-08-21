using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [Serializable]
    public class GroupData
    {
        public string guid;
        public string title;
        public Vector2 position;
        public List<string> nodeGuids = new List<string>();
    }
}