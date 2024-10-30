using System;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Utils.Editor
{
    
    public static class RunBoostrapShortcut
    {
        [MenuItem("Tools/Play From Bootstrap _%h")]
        public static void RunMainScene()
        {
            if (!EditorApplication.isPlaying)
            {
                string currentSceneName = EditorSceneManager.GetActiveScene().path;
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenuKcp.unity");
                EditorApplication.isPlaying = true;
			
                EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
                {
                    if (state == PlayModeStateChange.EnteredEditMode)
                    {
                        try
                        {
                            EditorSceneManager.OpenScene(currentSceneName);
                        }
                        catch (Exception)
                        {
                            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
                        }
                    }
                };
            }
        }
    }
}