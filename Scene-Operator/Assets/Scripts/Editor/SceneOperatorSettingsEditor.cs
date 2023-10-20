using System;
using System.Data;
using Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public static class Constants
    {
        public const string EDITOR_SETTINGS_TITLE = "";
        public const string EDITOR_WINDOW_TITLE = "";
        public const string SO_FILTER = "t:ScriptableObject";
        public const string DROPDOWN_TITLE = "Current container";
        public const string SAVED_CONTAINER_KEY = "Saved Container";
        public const string SETTINGS_PATH_KEY = "";
    }
    
    public class SceneOperatorSettingsEditor : EditorWindow
    {
        private const string EDITOR_TITLE = "Settings";
        private const string PATH_KEY = "Path";

        [SerializeField] private StyleSheet _styleSheet;

        private TextField _newContainerName;
        private TextField _sourcePath;

        private string Path
        {
            get => EditorPrefs.GetString(PATH_KEY);
            set => EditorPrefs.SetString(PATH_KEY, value);
        }
        
        public static event Action PathChanged;

        private void CreateGUI()
        {
            //_styleSheet = Resources.Load<StyleSheet>("EditorSettingsStyles");
            rootVisualElement.styleSheets.Add(_styleSheet);
            rootVisualElement.AddToClassList("root-container");
            DrawPathField();
            DrawCreateNewContainerSection();
            
            
        }

        public static void OpenEditor()
        {
            EditorWindow editorWindow = GetWindow<SceneOperatorSettingsEditor>();
            //need apply window size
            editorWindow.titleContent = new GUIContent(EditorConstants.SETTINGS_EDITOR_TITLE);
        }

        private void DrawPathField()
        {
            VisualElement blockContent = new();
            blockContent.AddToClassList("visual-block");
            _sourcePath = new TextField { value = Path, label = "Source path"};
            _sourcePath.AddToClassList("TextField");
            _sourcePath.RegisterValueChangedCallback(OnPathChanged);

            Button choicePathButton = new();
            choicePathButton.AddToClassList("select-folder-button");
            choicePathButton.clicked += TryChoicePath;

            blockContent.Add(_sourcePath);
            blockContent.Add(choicePathButton);
            rootVisualElement.Add(blockContent);
        }

        private void TryChoicePath()
        {
            string path = EditorUtility.OpenFolderPanel("Select path", "", "");
            
            if (string.IsNullOrEmpty(path))
                return;

            _sourcePath.value = path;
        }

        private void DrawCreateNewContainerSection()
        {
            VisualElement blockContent = new();
            blockContent.AddToClassList("visual-block");
            
            _newContainerName = new TextField { label = "Container name" };
            Button addNewSceneContainerButton = new() { text = "+" };
            addNewSceneContainerButton.AddToClassList("create-container-button");
            addNewSceneContainerButton.clicked += TryCreateNewSceneContainer;
            
            blockContent.Add(_newContainerName);
            blockContent.Add(addNewSceneContainerButton);
            rootVisualElement.Add(blockContent);
        }

        private void OnPathChanged(ChangeEvent<string> changeEvent)
        {
            PathChanged?.Invoke();
            Debug.Log(changeEvent.newValue);
            //save new path
            //need trigger scene editor for redraw if window opened
            //EditorPrefs.SetString("");
        }
        
        private void TryCreateNewSceneContainer()
        {
            if (string.IsNullOrEmpty(_newContainerName.value))
            {
                Debug.LogWarning("Name invalid");
                return;
            }
            
            if (!System.IO.Directory.Exists(""))
            {
                Debug.LogWarning("Path invalid");
                return;
            }
            
            SceneCollection newCollection = CreateInstance<SceneCollection>();
            AssetDatabase.CreateAsset(newCollection, "");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newCollection;
        }
    }
}
