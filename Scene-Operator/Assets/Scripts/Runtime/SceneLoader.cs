using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Runtime
{
    public class SceneLoader 
    {
        public void Load(string scenePath, OpenSceneMode openSceneMode = OpenSceneMode.Single)
        {
            EditorSceneManager.OpenScene(scenePath, openSceneMode);
        }

        public void Unload(string sceneName)
        {
            Scene unloadScene = SceneManager.GetSceneByName(sceneName); 
            EditorSceneManager.CloseScene(unloadScene, true);
        }
    }
}
