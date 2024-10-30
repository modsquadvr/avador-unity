using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable PossibleNullReferenceException

namespace Utils.Editor
{
    public class FocusedBrowser : EditorWindow//, ISerializationCallbackReceiver
    {
        private TreeDataSO _treeData;
        private AssetTreeView _assetTreeView;
        private bool _deserialized;

        [MenuItem("Tools/Focused Browser")]
        public static void ShowWindow()
        {
            GetWindow<FocusedBrowser>("Focused Browser");
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged += EditorPlaymodeStateChanged;
            EditorSceneManager.sceneOpened += EditorSceneChanged;

            if (_treeData == null)
            {
                _treeData = AssetDatabase.LoadAssetAtPath<TreeDataSO>(
                    AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("FocusedBrowserData")
                        .FirstOrDefault()));//search for guid -> get path -> load at path
                if (_treeData == null)
                {
                    _treeData = CreateInstance<TreeDataSO>();
                    AssetDatabase.CreateAsset(_treeData, "Assets/Utilities/EditorUtils/FocusedBrowser/FocusedBrowserData.asset");
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void OnGUI()
        {
            /////////////// NULL CHECK ///////////////
            if (_treeData == null)
            {
                OnEnable();
                return;
            }

            if (_deserialized)
            {
                BuildTreeFromSO();
                _deserialized = false;
            }

            if (_assetTreeView == null)
            {
                BuildTreeFromSO();
            }

            /////////////// BUTTONS ///////////////
            if (GUILayout.Button("Reset Tree"))
            {
                _assetTreeView.Reset();
                _treeData.ItemsBones = new List<AssetItemBones>();
                _treeData.NextId = 1;
            }

            if (GUILayout.Button("Add Fake Folder")) 
            {
                _assetTreeView.AddFakeFolder();
            }
            
            /////////////// EVENT HANDLING ///////////////

            HandleEvents();

            /////////////// DRAW CALL ///////////////
            _assetTreeView.OnGUI(new Rect(0, 45, position.width, position.height - 45));
        }

        private void HandleEvents()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                Event e = Event.current;
                
                if (e.shift && e.keyCode == KeyCode.Delete)
                {
                    _assetTreeView.DeleteFromProject();
                }
                
                switch (Event.current.keyCode)
                {
                    case KeyCode.F2 :
                        _assetTreeView.BeginRename();
                        return;
                    case KeyCode.Return :
                        _assetTreeView.EnterPressed();
                        return;
                    case KeyCode.Delete :
                        _assetTreeView.RemoveSelection();
                        return;
                }
                
                if (e.alt && e.shift && e.control && e.keyCode == KeyCode.N)
                {
                    _assetTreeView.CreateScript();
                    e.Use();
                    return;
                }
                if (e.alt && e.shift && e.control && e.keyCode == KeyCode.P)
                {
                    _assetTreeView.CreatePrefab();
                    e.Use();
                    return;

                }
                if (e.shift && e.control && e.keyCode == KeyCode.N)
                {
                    _assetTreeView.CreateFolder();
                    e.Use();
                    return;
                }
                if (e.alt && e.shift && e.control && e.keyCode == KeyCode.S)
                {
                    _assetTreeView.CreateScene();
                    e.Use();
                    return;
                }
                if (e.alt && e.shift && e.control && e.keyCode == KeyCode.M)
                {
                    _assetTreeView.CreateMaterial();
                    e.Use();
                    return;
                }
                if (e.alt && e.shift && e.control && e.keyCode == KeyCode.D) 
                {
                    _assetTreeView.CreateAsmdef();
                    e.Use();
                    return;
                }

                if (e.alt && e.shift && e.control && e.keyCode == KeyCode.F)
                {
                    _assetTreeView.AddFakeFolder();
                    e.Use();
                    return;
                }
            }
        }

        private void Update()
        {
            if (_assetTreeView == null) return;
            
            if (_assetTreeView.NeedsRepaint)
            {
                _assetTreeView.NeedsRepaint = false;
                Repaint();
            }
        }
        
        private void OnDestroy()
        {
            SaveToSO();
        }

        private void BuildTreeFromSO()
        {
            if (_assetTreeView == null)
            {
                _assetTreeView = new AssetTreeView(new TreeViewState());
                _assetTreeView.BuildFromSO(_treeData);
            }
        }

        private void SaveToSO()
        {
            _assetTreeView.SaveToSO(_treeData);
            EditorUtility.SetDirty(_treeData);
            AssetDatabase.SaveAssets();
        }

        public void OnBeforeSerialize()
        {
            SaveToSO();
        }

        public void OnAfterDeserialize()
        {
            _deserialized = true;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        public void OnBeforeAssemblyReload()
        {
            SaveToSO();
        }

        public void OnAfterAssemblyReload()
        {
            BuildTreeFromSO();
        }

        private void EditorPlaymodeStateChanged(PlayModeStateChange play_mode_state_change)
        {
            if (play_mode_state_change == PlayModeStateChange.EnteredEditMode)
            {
                _assetTreeView.RedrawFakeFolderIcons();
                Repaint();
            }
        }
        
        private void EditorSceneChanged(Scene scene, OpenSceneMode mode)
        {
            _assetTreeView.RedrawFakeFolderIcons();
            Repaint();
        }
    }
}