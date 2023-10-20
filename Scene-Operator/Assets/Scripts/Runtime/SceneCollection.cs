using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    public class SceneCollection : ScriptableObject
    {
        [SerializeField] private List<SceneGroup> _sceneGroups;

        public IEnumerable<SceneGroup> SceneGroups => _sceneGroups;
    }
}