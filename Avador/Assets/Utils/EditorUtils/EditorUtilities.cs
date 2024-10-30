using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// neinReSharper disable All

namespace Utils.Editor
{
    public class EditorUtilities
    {
        [MenuItem("Inkblot/Shortcuts/Clear Console &c")]
        static void ClearConsole()
        {
            // This simply does "LogEntries.Clear()" the long way:
            var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }

        [MenuItem("Inkblot/Shortcuts/Toggle Window Lock &e")]
        static void SelectLockableInspector()
        {
            EditorWindow inspectorToBeLocked = EditorWindow.mouseOverWindow;// "EditorWindow.focusedWindow" can be used instead

            if (inspectorToBeLocked == null) return;

            Assembly editorAsm = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            Type type = null;

            switch (inspectorToBeLocked.GetType().Name)
            {
                case "InspectorWindow" :
                    type = editorAsm.GetType("UnityEditor.InspectorWindow");
                    break;
                case "ProjectBrowser" :
                    type = editorAsm.GetType("UnityEditor.ProjectBrowser");
                    break;
            }

            if (type == null)
            {
                return;
            }

            PropertyInfo propertyInfo = type.GetProperty("isLocked",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (propertyInfo == null)
            {
                return;
            }

            bool? value = (bool?)propertyInfo.GetValue(inspectorToBeLocked, null);
            propertyInfo.SetValue(inspectorToBeLocked, !value, null);

            inspectorToBeLocked.Repaint();
        }

        [MenuItem("Inkblot/Shortcuts/Toggle Inspector Mode &d")]//Change the shortcut here
        static void ToggleInspectorDebug()
        {
            EditorWindow targetInspector = EditorWindow.mouseOverWindow;// "EditorWindow.focusedWindow" can be used instead

            if (targetInspector != null && targetInspector.GetType().Name == "InspectorWindow")
            {
                Type type = Assembly.GetAssembly(typeof(UnityEditor.Editor))
                    .GetType("UnityEditor.InspectorWindow");//Get the type of the inspector window to find out the variable/method from
                FieldInfo field =
                    type.GetField("m_InspectorMode",
                        BindingFlags.NonPublic | BindingFlags.Instance);//get the field we want to read, for the type (not our instance)

                InspectorMode mode = (InspectorMode)field.GetValue(targetInspector);//read the value for our target inspector
                mode = (mode == InspectorMode.Normal ? InspectorMode.Debug : InspectorMode.Normal);//toggle the value
                //Debug.Log("New Inspector Mode: " + mode.ToString());

                MethodInfo
                    method = type.GetMethod("SetMode",
                        BindingFlags.NonPublic | BindingFlags.Instance);//Find the method to change the mode for the type
                method.Invoke(targetInspector, new object[] { mode });//Call the function on our targetInspector, with the new mode as an object[]

                targetInspector.Repaint();//refresh inspector
            }
        }

        [MenuItem("Inkblot/Shortcuts/Collapse All Components &f")]
        public static void CollapseComponents()
        {
            EditorWindow inspectorWindow = EditorWindow.mouseOverWindow;// "EditorWindow.focusedWindow" can be used instead

            if (inspectorWindow != null && inspectorWindow.GetType().Name == "InspectorWindow")
            {
                ActiveEditorTracker tracker = (ActiveEditorTracker)inspectorWindow.GetType().GetMethod("get_tracker")?.Invoke(inspectorWindow, null);

                if (tracker == null) return;

                var editors = tracker.activeEditors;
                for (int eidx = 0; eidx < editors.Length; ++eidx)
                {
                    tracker.SetVisible(eidx, 0);//to fold
                }
            }
        }

        [MenuItem("Inkblot/Shortcuts/Expand All Components &x")]
        public static void ExpandComponents()
        {
            EditorWindow inspectorWindow = EditorWindow.mouseOverWindow;// "EditorWindow.focusedWindow" can be used instead

            if (inspectorWindow != null && inspectorWindow.GetType().Name == "InspectorWindow")
            {
                ActiveEditorTracker tracker = (ActiveEditorTracker)inspectorWindow.GetType().GetMethod("get_tracker")?.Invoke(inspectorWindow, null);

                if (tracker == null) return;

                var editors = tracker.activeEditors;
                for (int eidx = 0; eidx < editors.Length; ++eidx)
                {
                    tracker.SetVisible(eidx, 1);//to unfold
                }
            }
        }
        
        struct TransformData {
            public Vector3      Position;
            public Quaternion   Rotation;
            public Vector3      LocalScale;

            public TransformData(Vector3 position, Quaternion rotation, Vector3 local_scale) {
                this.Position  = position;
                this.Rotation  = rotation;
                this.LocalScale = local_scale;
            }
        }

        private static TransformData _data;
        private static Vector3? _dataCenter;

        [MenuItem("Inkblot/Shortcuts/Copy Transform Values %&c", false, -101)]
        public static void CopyTransformValues() {
            if(Selection.gameObjects.Length == 0) return;
            var selectionTr = Selection.gameObjects[0].transform;
            _data = new TransformData(selectionTr.position, selectionTr.rotation, selectionTr.localScale);
        }

        [MenuItem("Inkblot/Shortcuts/Paste Transform Values %&v", false, -101)]
        public static void PasteTransformValues() {
            foreach(var selection in Selection.gameObjects) {
                Transform selectionTr = selection.transform;
                Undo.RecordObject(selectionTr, "Paste Transform Values");
                selectionTr.position = _data.Position;
                selectionTr.rotation = _data.Rotation;
                selectionTr.localScale = _data.LocalScale;
            }
        }

        [MenuItem("Inkblot/Shortcuts/Select Parent and Collapse &z", false, -101)]
        public static void SelectParentAndCollapse()
        {
            if (Selection.transforms.Length == 0) return;

            Transform parent = Selection.transforms[0].parent;
            if (parent == null) return;

            Undo.RecordObject(parent.gameObject, "Select Parent and Collapse");

            GameObject[] newSelection = new GameObject[]{parent.gameObject};
            
            Selection.selectionChanged += collapse;
            Selection.objects = newSelection;


            void collapse()
            {
				EditorCollapseAll.CollapseGameObjects(new MenuCommand(parent.gameObject));
				Selection.selectionChanged -= collapse;
            }
        }
        
        [MenuItem("Inkblot/Shortcuts/Destroy Children &t")] // %t means Alt+T
        private static void DestroyChildren()
        {
	        foreach (var obj in Selection.gameObjects)
	        {
		        if (obj is null)
		        {
			        return;
		        }
		        DestroyAllChildren(obj);
	        }
        }

        private static void DestroyAllChildren(GameObject parent)
        {
	        // Record the action for undo
	        Undo.RegisterCompleteObjectUndo(parent, "Destroy Children");

	        // Iterate through the children and destroy them
	        for (int i = parent.transform.childCount - 1; i >= 0; i--)
	        {
		        Transform child = parent.transform.GetChild(i);
		        Undo.DestroyObjectImmediate(child.gameObject);
	        }

        }
        
		#pragma warning disable 0618
        [MenuItem("Inkblot/Shortcuts/Revert Prefab &o")]
        static void RevertPrefab()
        {
	        GameObject[] selection = Selection.gameObjects;
	        if (selection.Length < 1) return;
	        Undo.RegisterCompleteObjectUndo(selection, "Revert Prefab");
	        foreach (GameObject go in selection)
	        {
		        if (PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance)
		        {
			        PrefabUtility.RevertPrefabInstance(go);
		        }
	        }
        }
		#pragma warning restore 0618
    }
    
    
}


public static class EditorCollapseAll //credit: https://gist.github.com/yasirkula/0b541b0865eba11b55518ead45fba8fc
{
	private const BindingFlags INSTANCE_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
	private const BindingFlags STATIC_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

	[MenuItem( "Inkblot/Shortcuts/Collapse Project Browser &w", priority = 1000 )]
	private static void CollapseFolders()
	{
		EditorWindow projectWindow = typeof( EditorWindow ).Assembly.GetType( "UnityEditor.ProjectBrowser" ).GetField( "s_LastInteractedProjectBrowser", STATIC_FLAGS ).GetValue( null ) as EditorWindow;
		if( projectWindow )
		{
			object assetTree = projectWindow.GetType().GetField( "m_AssetTree", INSTANCE_FLAGS ).GetValue( projectWindow );
			if( assetTree != null )
				CollapseTreeViewController( projectWindow, assetTree, (TreeViewState) projectWindow.GetType().GetField( "m_AssetTreeState", INSTANCE_FLAGS ).GetValue( projectWindow ) );

			object folderTree = projectWindow.GetType().GetField( "m_FolderTree", INSTANCE_FLAGS ).GetValue( projectWindow );
			if( folderTree != null )
			{
				object treeViewDataSource = folderTree.GetType().GetProperty( "data", INSTANCE_FLAGS ).GetValue( folderTree, null );
				int searchFiltersRootInstanceID = (int) typeof( EditorWindow ).Assembly.GetType( "UnityEditor.SavedSearchFilters" ).GetMethod( "GetRootInstanceID", STATIC_FLAGS ).Invoke( null, null );
				bool isSearchFilterRootExpanded = (bool) treeViewDataSource.GetType().GetMethod( "IsExpanded", INSTANCE_FLAGS, null, new System.Type[] { typeof( int ) }, null ).Invoke( treeViewDataSource, new object[] { searchFiltersRootInstanceID } );

				CollapseTreeViewController( projectWindow, folderTree, (TreeViewState) projectWindow.GetType().GetField( "m_FolderTreeState", INSTANCE_FLAGS ).GetValue( projectWindow ), isSearchFilterRootExpanded ? new int[1] { searchFiltersRootInstanceID } : null );

				// Preserve Assets and Packages folders' expanded states because they aren't automatically preserved inside ProjectBrowserColumnOneTreeViewDataSource.SetExpandedIDs
				// https://github.com/Unity-Technologies/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Editor/Mono/ProjectBrowserColumnOne.cs#L408-L420
				InternalEditorUtility.expandedProjectWindowItems = (int[]) treeViewDataSource.GetType().GetMethod( "GetExpandedIDs", INSTANCE_FLAGS ).Invoke( treeViewDataSource, null );

				TreeViewItem rootItem = (TreeViewItem) treeViewDataSource.GetType().GetField( "m_RootItem", INSTANCE_FLAGS ).GetValue( treeViewDataSource );
				if( rootItem.hasChildren )
				{
					foreach( TreeViewItem item in rootItem.children )
						EditorPrefs.SetBool( "ProjectBrowser" + item.displayName, (bool) treeViewDataSource.GetType().GetMethod( "IsExpanded", INSTANCE_FLAGS, null, new System.Type[] { typeof( int ) }, null ).Invoke( treeViewDataSource, new object[] { item.id } ) );
				}
			}
		}
	}

	[MenuItem( "Inkblot/Shortcuts/Collapse Hierarchy &q", priority = 40 )]
	public static void CollapseGameObjects( MenuCommand command )
	{
		// This happens when this button is clicked while multiple Objects were selected. In this case,
		// this function will be called once for each selected Object. We don't want that, we want
		// the function to be called only once
		if( command.context )
		{
			EditorApplication.update -= CallCollapseGameObjectsOnce;
			EditorApplication.update += CallCollapseGameObjectsOnce;

			return;
		}

		EditorWindow hierarchyWindow = typeof( EditorWindow ).Assembly.GetType( "UnityEditor.SceneHierarchyWindow" ).GetField( "s_LastInteractedHierarchy", STATIC_FLAGS ).GetValue( null ) as EditorWindow;
		if( hierarchyWindow )
		{
#if UNITY_2018_3_OR_NEWER
			object hierarchyTreeOwner = hierarchyWindow.GetType().GetField( "m_SceneHierarchy", INSTANCE_FLAGS ).GetValue( hierarchyWindow );
#else
			object hierarchyTreeOwner = hierarchyWindow;
#endif
			object hierarchyTree = hierarchyTreeOwner.GetType().GetField( "m_TreeView", INSTANCE_FLAGS ).GetValue( hierarchyTreeOwner );
			if( hierarchyTree != null )
			{
				List<int> expandedSceneIDs = new List<int>( 4 );
				foreach( string expandedSceneName in (IEnumerable<string>) hierarchyTreeOwner.GetType().GetMethod( "GetExpandedSceneNames", INSTANCE_FLAGS ).Invoke( hierarchyTreeOwner, null ) )
				{
					Scene scene = SceneManager.GetSceneByName( expandedSceneName );
					if( scene.IsValid() )
						expandedSceneIDs.Add( scene.GetHashCode() ); // GetHashCode returns m_Handle which in turn is used as the Scene's instanceID by SceneHierarchyWindow
				}

				CollapseTreeViewController( hierarchyWindow, hierarchyTree, (TreeViewState) hierarchyTreeOwner.GetType().GetField( "m_TreeViewState", INSTANCE_FLAGS ).GetValue( hierarchyTreeOwner ), expandedSceneIDs );
			}
		}
	}

	private static void CallCollapseGameObjectsOnce()
	{
		EditorApplication.update -= CallCollapseGameObjectsOnce;
		CollapseGameObjects( new MenuCommand( null ) );
	}

	private static void CollapseTreeViewController( EditorWindow editorWindow, object treeViewController, TreeViewState treeViewState, IList<int> additionalInstanceIDsToExpand = null )
	{
		object treeViewDataSource = treeViewController.GetType().GetProperty( "data", INSTANCE_FLAGS ).GetValue( treeViewController, null );
		List<int> treeViewSelectedIDs = new List<int>( treeViewState.selectedIDs );
		int[] additionalInstanceIDsToExpandArray;
		if( additionalInstanceIDsToExpand != null && additionalInstanceIDsToExpand.Count > 0 )
		{
			treeViewSelectedIDs.AddRange( additionalInstanceIDsToExpand );

			additionalInstanceIDsToExpandArray = new int[additionalInstanceIDsToExpand.Count];
			additionalInstanceIDsToExpand.CopyTo( additionalInstanceIDsToExpandArray, 0 );
		}
		else
			additionalInstanceIDsToExpandArray = new int[0];

		treeViewDataSource.GetType().GetMethod( "SetExpandedIDs", INSTANCE_FLAGS ).Invoke( treeViewDataSource, new object[] { additionalInstanceIDsToExpandArray } );
#if UNITY_2019_1_OR_NEWER
		treeViewDataSource.GetType().GetMethod( "RevealItems", INSTANCE_FLAGS ).Invoke( treeViewDataSource, new object[] { treeViewSelectedIDs.ToArray() } );
#else
		foreach( int treeViewSelectedID in treeViewSelectedIDs )
			treeViewDataSource.GetType().GetMethod( "RevealItem", INSTANCE_FLAGS ).Invoke( treeViewDataSource, new object[] { treeViewSelectedID } );
#endif

		editorWindow.Repaint();
	}
}