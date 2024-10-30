using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace Utils.Editor
{
	//USE F1 AND F3 TO MOVE BACK AND FORTH BETWEEN GAME OBJECTS YOU HAVE SELECTED. 
	//RECOMMENDED TO USE THE SHORTCUT TAB TO SET THE SHORTCUTS TO MOUSE SIDE BUTTONS.( find "Previous Selection" and "Next Selection" in the shortcut window)
	//NOTE THAT MOUSE SIDE BUTTONS ARE NOT DETECTED IF CLICKED TOO FAST WITH THE SHORTCUT EDITOR ;( ;( ;( Therefore each option is equipped with two shortcut 
	//options so you can bind both mouse keys and keyboard keys
	public static class MoveSelectionToPreviousOrNext
	{
		private static SelectionMemorySO _selectionMemorySO;

		private static int _MaxMemoryDepth => _selectionMemorySO.MaxMemoryDepth;
		private static int _MaxSelectionSize => _selectionMemorySO.MaxSelectionSize;
		
		private static List<SelectionMemorySO.PersistentReferenceArrayWrapper> _selectionMemory => _selectionMemorySO.SelectionMemory;
		private static List<SelectionMemorySO.PersistentReferenceArrayWrapper> _selectionRedoStack => _selectionMemorySO.SelectionRedoStack; //list for serialization purposes but used as a stack

		private static bool _justChangedSelection;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			LoadSO();
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			Selection.selectionChanged += OnSelectionChanged;
		}

		private static void OnSelectionChanged()
		{
			if (_selectionMemory.Count > 0)
				if (_justChangedSelection)
				{
					_justChangedSelection = false;
					return;//you selected the same object, maybe because selection was set to previous or next
				}

			if (Selection.gameObjects.Length == 0 || Selection.gameObjects.Length > _MaxSelectionSize) return;

			if (CompareSelectionToMemory()) return;
			
			_selectionMemory.Add(ToPersistentReferences(Selection.gameObjects));

			if (_selectionMemory.Count > _MaxMemoryDepth)
				_selectionMemory.RemoveAt(0);

			_selectionRedoStack.Clear();
		}
		
		private static bool CompareSelectionToMemory() //true if they are equal, false if not
		{
			if (_selectionMemory.Count == 0) return false; 
			
			PersistentReference[] MemoryPersistentRefArray = _selectionMemory[^1];
			PersistentReference[] SelectionPersistentRefArray = ToPersistentReferences(Selection.gameObjects);

			if (MemoryPersistentRefArray.Length != SelectionPersistentRefArray.Length) return false;

			for (int i = 0; i < MemoryPersistentRefArray.Length; i++)
			{
				if (MemoryPersistentRefArray[i].LocalId != SelectionPersistentRefArray[i].LocalId ||
				    MemoryPersistentRefArray[i].SceneName != SelectionPersistentRefArray[i].SceneName)
					return false;
			}

			return true;
		}
		
		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				LoadSO();
			}
            
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				LoadSO();
			}
		}

		[Shortcut("Custom/PreviousSelection")]
		[MenuItem("Inkblot/Shortcuts/PreviousSelection _F1")]
		private static void PreviousSelection()
		{
			if (_selectionMemory.Count <= 1)
			{
				return;
			}

			_justChangedSelection = true;
			Selection.objects = ToGameObjects(_selectionMemory[^2]);

			_selectionRedoStack.Add(_selectionMemory[^1]);
			_selectionMemory.RemoveAt(_selectionMemory.Count - 1);
		}
		
		[Shortcut("Custom/NextSelection")]
		[MenuItem("Inkblot/Shortcuts/NextSelection _F3")]
		private static void NextSelection()
		{
			if (_selectionRedoStack.Count < 1) return;

			PersistentReference[] objs = _selectionRedoStack[^1];
			_selectionRedoStack.RemoveAt(_selectionRedoStack.Count - 1);
			_selectionMemory.Add(objs);
			_justChangedSelection = true;
			Selection.objects = ToGameObjects(objs);
		}

		private static void PrintList(List<GameObject[]> list)
		{
			for (int index = list.Count - 1; index >= 0; index--)
			{
				GameObject[] objs = list[index];
				Debug.Log($"{index}: {objs[0].name}");
			}
		}

		private static PersistentReference[] ToPersistentReferences(GameObject[] game_objects)
		{
			var result = new PersistentReference[game_objects.Length];

			for (int i = 0; i < game_objects.Length; i++)
			{
				result[i] = new PersistentReference(game_objects[i]);
			}

			return result;
		}


		private static GameObject[] ToGameObjects(PersistentReference[] references)
		{
			var result = new GameObject[references.Length];

			for (int i = 0; i < references.Length; i++)
			{
				result[i] = references[i]?.GetGameObject();
			}

			return result;
		}

		private static void LoadSO()
		{
			if (_selectionMemorySO == null)
			{
				_selectionMemorySO =
					AssetDatabase.LoadAssetAtPath<SelectionMemorySO>("Assets/Utilities/EditorUtils/RandomTools/SelectionTools/SelectionMemory.asset");

				
				if (_selectionMemorySO != null) return;
				
				
				_selectionMemorySO = AssetDatabase.LoadAssetAtPath<SelectionMemorySO>(
					AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("SelectionMemory")
						.FirstOrDefault()));//search for guid -> get path -> load at path
				
				
				if (_selectionMemorySO != null) return;
				
				
				_selectionMemorySO = ScriptableObject.CreateInstance<SelectionMemorySO>();
				AssetDatabase.CreateAsset(_selectionMemorySO, "Assets/Utilities/EditorUtils/SelectionTools/SelectionMemory.asset");
				AssetDatabase.SaveAssets();
			}
		}
	}


	[Serializable]
	public class PersistentReference//this class holds a reference to a gameobject that persists through scene reloads and stuff
	{
		public string SceneName;
		public long LocalId;
		public GameObject UnderlyingObject;

		public PersistentReference(GameObject gameObject)
		{
			SceneName = gameObject.scene.name;
			LocalId = GetLocalIdentifierInFile(gameObject);
			UnderlyingObject = gameObject;
		}

		public GameObject GetGameObject()
		{
			if (UnderlyingObject is not null)
			{
				return UnderlyingObject;
			}
			
			var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName);
			if (!scene.isLoaded)
				return null;

			foreach (var rootObject in scene.GetRootGameObjects())
			{
				var result = FindGameObjectByLocalId(rootObject, LocalId);
				if (result != null)
					return result;
			}

			return null;
		}

		private static long GetLocalIdentifierInFile(GameObject obj)
		{
			if (obj == null)
				return 0;

			var inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
			var serializedObject = new SerializedObject(obj);
			inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

			var localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
			return localIdProp.longValue;
		}

		private static GameObject FindGameObjectByLocalId(GameObject rootObject, long localId)
		{
			var identifier = GetLocalIdentifierInFile(rootObject);
			if (identifier == localId)
				return rootObject;

			foreach (Transform child in rootObject.transform)
			{
				var result = FindGameObjectByLocalId(child.gameObject, localId);
				if (result != null)
					return result;
			}

			return null;
		}
	}

}
