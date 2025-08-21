#if UNITY_ADDRESSABLES

using System;
using UnityEngine.AddressableAssets;

namespace BehaviourTreeLogic
{
    [Serializable]
    public class AssetReferenceBehaviourTree : AssetReferenceT<BehaviourTree>
    {
        public AssetReferenceBehaviourTree(string guid) : base(guid)
        {
        }
        
        public override bool ValidateAsset(UnityEngine.Object obj)
        {
            var go = obj as BehaviourTree;
            return go != null;
        }
    }
}
#endif