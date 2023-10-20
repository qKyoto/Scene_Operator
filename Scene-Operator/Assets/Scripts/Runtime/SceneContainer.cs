using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    //[CreateAssetMenu(fileName = "Scene Container", menuName = "Scene Manager/Scene Container")]
    public class SceneContainer : ScriptableObject
    {
        [SerializeField] private List<SceneDataElement> _sceneDataElements;

        public IEnumerable<SceneDataElement> SceneDataElements => _sceneDataElements;
    }
}