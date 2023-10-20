using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct SceneDataElement
    {
        [SerializeField] private string _elementName;
        [SerializeField] private SceneAsset[] _sceneAssets;

        public string ElementName => _elementName;
        public IEnumerable<SceneAsset> SceneAssets => _sceneAssets;
    }
}