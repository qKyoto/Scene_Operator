using System;
using Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public class SceneOperatorSettingsEditor : EditorWindow
    {
        [SerializeField] private StyleSheet _styleSheet;

        private string Path
        {
            get => EditorPrefs.GetString(EditorConstants.COLLECTIONS_PATH_KEY);
            set => EditorPrefs.SetString(EditorConstants.COLLECTIONS_PATH_KEY, value);
        }
        
        public static event Action PathChanged;
        public static event Action NewCollectionCreated;

        public static void OpenEditor()
        {
            EditorWindow editorWindow = GetWindow<SceneOperatorSettingsEditor>();
            editorWindow.titleContent = new GUIContent(EditorConstants.SETTINGS_EDITOR_TITLE);
        }

        private void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(_styleSheet);
            rootVisualElement.AddToClassList("root-container");
            
            DrawPathField();
            DrawNewCollectionField();
        }

        private void DrawPathField()
        {
            VisualElement fieldContent = new();
            TextField sourcePath = new() { value = Path, label = "Source path"};
            Button choicePathButton = new();
            
            fieldContent.AddToClassList("visual-block");
            sourcePath.AddToClassList("TextField");
            choicePathButton.AddToClassList("select-folder-button");

            sourcePath.RegisterCallback<KeyDownEvent>(eventData =>
            {
                if (eventData.keyCode == KeyCode.Return) 
                    OnPathChanged(sourcePath.value);
            });
            choicePathButton.clicked += () =>
            {
                TryChoicePath(sourcePath);
            };

            fieldContent.Add(sourcePath);
            fieldContent.Add(choicePathButton);
            rootVisualElement.Add(fieldContent);
        }

        private void DrawNewCollectionField()
        {
            VisualElement blockContent = new();
            Button addNewSceneContainerButton = new() { text = "+" };
            TextField sceneCollectionName = new() { label = "Container name" };
            
            blockContent.AddToClassList("visual-block");
            addNewSceneContainerButton.AddToClassList("create-container-button");
            
            addNewSceneContainerButton.clicked += () =>
            {
                TryCreateNewSceneCollection(sceneCollectionName.value);
            };
            
            blockContent.Add(sceneCollectionName);
            blockContent.Add(addNewSceneContainerButton);
            rootVisualElement.Add(blockContent);
        }

        private void TryChoicePath(TextField field)
        {
            string path = EditorUtility.OpenFolderPanel("Select path", "", "");
            
            if (string.IsNullOrEmpty(path))
                return;
            
            field.value = path.CutText(Application.productName + "/");
            OnPathChanged(field.value);
        }

        private void OnPathChanged(string value)
        {
            if (Path == value)
                return;
            
            Path = value;
            PathChanged?.Invoke();
        }
        
        private void TryCreateNewSceneCollection(string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                Debug.LogWarning("Name invalid");
                return;
            }
            if (!System.IO.Directory.Exists(Path))
            {
                Debug.LogWarning("Path invalid");
                return;
            }
            
            SceneCollection newCollection = CreateInstance<SceneCollection>();
            newCollection.name = collectionName;
            AssetDatabase.CreateAsset(newCollection,  Path + "/" + collectionName + ".asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newCollection;
            
            NewCollectionCreated?.Invoke();
        }
    }
}