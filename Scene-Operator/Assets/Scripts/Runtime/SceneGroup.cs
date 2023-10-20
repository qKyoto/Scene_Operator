using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct SceneGroup
    {
        [SerializeField] private string _groupName;
        [SerializeField] private SceneAsset[] _sceneAssets;

        public string GroupName => _groupName;
        public IEnumerable<SceneAsset> SceneAssets => _sceneAssets;
    }
}