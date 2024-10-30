using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Utils.Editor
{
    public static class MaintainSelection
    {
        private static readonly List<string> _selectedObjectsName = new();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SaveSelection();
            }
            
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SaveSelection();
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestoreSelection();
            }
        }
        
        public static void RestoreSelection()
        {
            var result = new List<Object>();
            
            foreach (var name in _selectedObjectsName)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    result.Add(obj);
                }
            }
            
            Selection.objects = result.ToArray();
        }

        [CanBeNull]
        private static string GetObjectAsPath(Object obj)
        {
            if (obj is not GameObject go) return null;
            StringBuilder sb = new StringBuilder();
            while (go != null)
            {
                sb.Insert(0, go.name);
                sb.Insert(0, "/");
                go = go.transform.parent?.gameObject;
            }

            return sb.ToString();
        }

        public static void SaveSelection()
        {
            _selectedObjectsName.Clear();
            foreach (var obj in Selection.objects)
            {
                _selectedObjectsName.Add(GetObjectAsPath(obj));
            }
        }
    }
}
