using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Runtime
{
    public class SceneLoader 
    {
        public void LoadSingleScene(string scenePath, OpenSceneMode openSceneMode = OpenSceneMode.Single)
        {
            EditorSceneManager.OpenScene(scenePath, openSceneMode);
        }

        public void LoadMultipleScenes(List<string> scenePaths)
        {
            foreach (string scenePath in scenePaths)
                LoadSingleScene(scenePath, OpenSceneMode.Additive);
        }
        
        public void UnloadSingleScene(string sceneName)
        {
            Scene unloadScene = SceneManager.GetSceneByName(sceneName); 
            EditorSceneManager.CloseScene(unloadScene, true);
        }

        public void UnloadMultipleScenes(List<string> sceneNames)
        {
            foreach (string sceneName in sceneNames)
                UnloadSingleScene(sceneName);
        }
    }
}
