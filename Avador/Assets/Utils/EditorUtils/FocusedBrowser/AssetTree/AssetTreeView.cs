using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Utils.Editor 
{
    public class AssetTreeView : TreeView
    {
        public bool NeedsRepaint;
            
        private int _nextId;
        private TreeViewItem _relevantAssets;
        private bool _renaming;
        private HashSet<string> _addedPaths = new HashSet<string>();

        // INITIALIZATION //

        #region Initialization

        public AssetTreeView(TreeViewState state) : base(state)
        {
            Reload();
        }

        public void Reset()
        {
            _addedPaths = new HashSet<string>();
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            _nextId = 0;
            var root = new TreeViewItem { id = _nextId++, depth = -1, displayName = "Root" };

            _relevantAssets = new TreeViewItem(_nextId++, 0, "Relevant Assets");
            root.AddChild(_relevantAssets);
            SetExpanded(_relevantAssets.id, true);

            return root;
        }

        #endregion

        // BASIC STUFF + OVERRIDES //

        #region Basics

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is AssetTreeViewItem assetItem)
            {
                if (AssetDatabase.IsValidFolder(assetItem.AssetPath))
                    assetItem.Expanded(IsExpanded(args.item.id));
            }

            base.RowGUI(args);
        }

        override protected void SelectionChanged(IList<int> selected_ids)
        {
            List<Object> newUnitySelection = new List<Object>();

            foreach (int selectedId in selected_ids)
            {
                if (FindItem(selectedId, _relevantAssets) is AssetTreeViewItem item)
                {
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(item.AssetPath);
                    if (asset != null)
                    {
                        newUnitySelection.Add(asset);
                    }
                }
            }

            Selection.objects = newUnitySelection.ToArray();
        }

        #endregion

        // ADDING ITEMS //  

        #region AddItems

        public string GetDisplayNameFromPath(string path)
        {
            string[] splitPath = path.Split('/');
            string displayName = splitPath[^1];

            if (!AssetDatabase.IsValidFolder(path))
            {
                splitPath = displayName.Split('.');//reusing splitPath to remove the trailing ".filetype" at the end of the display name
                if (splitPath.Length > 1)
                {
                    splitPath[^1] = String.Empty;
                    displayName = String.Join('.', splitPath)
                        .Trim('.');//so much garbage ;( jumping through these hoops out of worry some name will have two '.'s
                }
            }

            return displayName;
        }

        //Adds an item to the tree, or adds a folder recursively to the tree
        public void AddItem(string asset_path, TreeViewItem parent = null)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(asset_path) == null)
            {
                Debug.Log("Not Valid Path!");
                return;
            }

            parent ??= _relevantAssets;

            AddItemRecursive(asset_path, parent);

            SetupDepthsFromParentsAndChildren(parent);
            SortParentRecursive(parent);
            SetExpanded(parent.id,
                false);//there is definitely a better way to do this, but without this I don't know how to have the new child visible right away
            SetExpanded(parent.id, true);
        }

        private void AddItemRecursive(string asset_path, TreeViewItem parent)
        {
            if (!_addedPaths.Add(asset_path))
            {
                if (AssetDatabase.IsValidFolder(asset_path))
                {
                    string[] dirChildrenGuidsQuick = AssetDatabase.FindAssets("", new[] { asset_path });
                    foreach (string item in dirChildrenGuidsQuick)
                    {
                        AddItemRecursive(AssetDatabase.GUIDToAssetPath(item), GetItemFromPath(asset_path));
                    }
                }
                return;
            }

            string displayName = GetDisplayNameFromPath(asset_path);

            AssetTreeViewItem newItem = new AssetTreeViewItem(_nextId++, 0, displayName, asset_path);
            parent.AddChild(newItem);

            if (!AssetDatabase.IsValidFolder(asset_path))
            {
                return;
            }

            string[] dirChildrenGuids = AssetDatabase.FindAssets("", new[] { asset_path });

            foreach (string item in dirChildrenGuids)
            {
                AddItemRecursive(AssetDatabase.GUIDToAssetPath(item), newItem);
            }
        }

        private void SortParentRecursive(TreeViewItem parent)
        {
            if (parent.children == null) return;

            parent.children.Sort(
                (x, y) => AssetTreeViewItem.Sort(x as AssetTreeViewItem, y as AssetTreeViewItem)
            );

            foreach (TreeViewItem child in parent.children)
            {
                SortParentRecursive(child);
            }
        }

        #endregion

        // OPENING ITEMS //

        #region OpeningItems

        protected override void DoubleClickedItem(int id)
        {
            AssetTreeViewItem item = FindItem(id, _relevantAssets) as AssetTreeViewItem;
            if (item == null)
            {
                //then it's a base folder (not asset)
                SetExpanded(id, !IsExpanded(id));
                return;
            }

            if (AssetDatabase.IsValidFolder(item.AssetPath))
            {
                SetExpanded(id, !IsExpanded(id));
            }

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(item.AssetPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
        }

        public void EnterPressed()
        {
            if (_renaming || _renamingNewFile) return;

            TreeViewItem selectedItem = FindItem(GetSelection()[0], rootItem);
            DoubleClickedItem(selectedItem.id);
        }

        #endregion

        // RENAMING ITEMS //

        #region RenamingItems

        public void BeginRename()
        {
            _renaming = true;
            TreeViewItem item = FindItem(GetSelection().FirstOrDefault(), _relevantAssets);
            BeginRename(item);
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (_renamingNewFile)
            {
                _renamingNewFile = false;
                RenameNewFileEnded(args);
                return;
            }

            _renaming = false;
            if (!args.acceptedRename) return;

            TreeViewItem item = FindItem(args.itemID, _relevantAssets);

            item.displayName = args.newName;

            if (item is not AssetTreeViewItem assetItem || assetItem.IsFakeFolder)
            {
                return;
            }

            _addedPaths.Remove(assetItem.AssetPath);
            string[] splitPath = assetItem.AssetPath.Split("/");
            string[] splitPathNoFile = splitPath[^1].Split(".");
            splitPathNoFile[0] = args.newName;
            splitPath[^1] = String.Join(".", splitPathNoFile);
            string newPath = String.Join("/", splitPath);

            AssetDatabase.RenameAsset(assetItem.AssetPath, args.newName);
            assetItem.AssetPath = newPath;
            _addedPaths.Add(newPath);
            AssetDatabase.Refresh();
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return true;
        }

        #endregion

        // DRAG AND DROP

        #region DragAndDrop

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (args.draggedItem != _relevantAssets)
                return true;

            return false;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            List<AssetTreeViewItem> items = new List<AssetTreeViewItem>();
            if (FindRows(args.draggedItemIDs) is not List<TreeViewItem> rows) return;

            foreach (TreeViewItem i in rows)
            {
                items.Add(i as AssetTreeViewItem);
            }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("List<AssetTreeViewItem>", items);

            DragAndDrop.paths = items.Select(x => x.AssetPath).ToArray();
            DragAndDrop.objectReferences = items.Where(x => !x.IsFakeFolder).Select(x => AssetDatabase.LoadAssetAtPath<Object>(x.AssetPath)).ToArray();

            DragAndDrop.StartDrag("Dragging AssetTreeViewItem");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition is DragAndDropPosition.OutsideItems)
            {
                string[] paths = DragAndDrop.paths;
                if (args.performDrop)
                {
                    foreach (string path in paths)
                    {
                        AddOrMoveItem(path, _relevantAssets);
                    }

                    DragAndDrop.AcceptDrag();
                    return DragAndDropVisualMode.Move;
                }

                return DragAndDropVisualMode.Copy;
            }

            if (args.dragAndDropPosition is DragAndDropPosition.UponItem or DragAndDropPosition.BetweenItems)
            {
                string[] paths = DragAndDrop.paths;
                if (args.performDrop)
                {
                    foreach (string path in paths)
                    {
                        AddOrMoveItem(path, args.parentItem);
                    }

                    DragAndDrop.AcceptDrag();
                    return DragAndDropVisualMode.Move;
                }

                return DragAndDropVisualMode.Copy;
            }

            return base.HandleDragAndDrop(args);
        }

        private void AddOrMoveItem(string path, TreeViewItem new_parent) 
        {
            if (new_parent is AssetTreeViewItem newParentAssetItem)
            {
                if (!AssetDatabase.IsValidFolder(newParentAssetItem.AssetPath) && !newParentAssetItem.IsFakeFolder)
                {
                    new_parent = new_parent.parent;//if our new parent isn't a folder, then send the new item its parent folder
                }
            }

            if (new_parent == rootItem) new_parent = _relevantAssets;
            
            if (!_addedPaths.Contains(path) && !path.Contains(_fakefolderString))
            {
                AddItem(path, new_parent);
                return;
            }
            
            TreeViewItem item;
            if (!path.Contains(_fakefolderString)) 
            {
                item = GetItemFromPathBasic(path); //it is a regular item, so get it regularly
            }
            else //it is a fake folder, so we'll have to get it like this:
            {
                string numberPart = path.Substring(_fakefolderString.Length);
                int.TryParse(numberPart, out int folderId);
                item = FindItem(folderId, _relevantAssets);
            }

            if (item == null || (new_parent as AssetTreeViewItem)?.AssetPath == path) return;
            if (GetAncestors(new_parent.id).Contains(item.id)) return;
            
            int oldParentId = item.parent.id;
            item.parent.children.Remove(item);
            item.parent = null;
            new_parent.AddChild(item);
            SetupDepthsFromParentsAndChildren(new_parent);
            SortParentRecursive(new_parent);
            SetExpanded(oldParentId,
                false);//there is definitely a better way to do this, but without this I don't know how to have the new child visible right away
            SetExpanded(oldParentId, true);
        }

        //Note: only gets build rows because it is meant for drag and drop, will return null if you are querying a now visible item
        private AssetTreeViewItem GetItemFromPathBasic(string path)
        {
            if (!_addedPaths.Contains(path))
            {
                return null;
            }

            IList<TreeViewItem> items = GetRows();
            foreach (TreeViewItem item in items)
            {
                if (item is AssetTreeViewItem assetItem)
                {
                    if (assetItem.AssetPath == path)
                        return assetItem;
                }
            }

            return null;
        }
        //a more expensive, more complete get item from path. 
        private AssetTreeViewItem GetItemFromPath(string path)
        {
            return GetItemFromPathRecursive(path, _relevantAssets);
        }
        private AssetTreeViewItem GetItemFromPathRecursive(string path, TreeViewItem item)
        {
            if (item == null || item.children == null) return null;
            foreach (AssetTreeViewItem child in item.children)
            {
                if (child.AssetPath == path)
                {
                    return child;
                }
                else
                {
                    var result = GetItemFromPathRecursive(path, child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        #endregion

        // DELETE ITEMS //

        #region DeleteItems

        public void RemoveSelection()
        {
            foreach (int id in GetSelection())
            {
                RemoveItem(id);
            }
        }

        private void RemoveItem(int id)
        {
            TreeViewItem item = FindItem(id, _relevantAssets);
            if (item == null) return;

            List<TreeViewItem> children = item.parent.children;
            foreach (TreeViewItem child in children)
            {
                if (child == item)
                {
                    if (child is AssetTreeViewItem assetChild)
                    {
                        ForgetAllChildrenPathsRecursive(assetChild);
                    }

                    children.Remove(child);
                    item.parent.children = children;
                    SetExpanded(item.parent.id, false);
                    SetExpanded(item.parent.id, true);
                    break;
                }
            }

            if (item.parent is AssetTreeViewItem parentAssetItem)
            {
                if (item.parent.children.Count == 0 && AssetDatabase.IsValidFolder(parentAssetItem.AssetPath))
                {
                    SetExpanded(parentAssetItem.id, false);
                }
            }
        }

        private void ForgetAllChildrenPathsRecursive(AssetTreeViewItem item)
        {
            if (item == null)
            {
                return;
            }

            _addedPaths.Remove(item.AssetPath);

            if (item.children == null)
            {
                return;
            }

            foreach (TreeViewItem child in item.children)
            {
                ForgetAllChildrenPathsRecursive(child as AssetTreeViewItem);
            }
        }

        public void DeleteFromProject()
        {
            foreach (int id in GetSelection())
            {
                DeleteItemFromProject(id);
            }
        }
        private void DeleteItemFromProject(int id)
        {
            TreeViewItem item = FindItem(id, _relevantAssets);
            //If it's just a regular item, remove it regularly
            if (item is not AssetTreeViewItem assetItem)
            {
                RemoveItem(id);
                return;
            }

            //if it's an asset item, delete it from the project

            AssetDatabase.DeleteAsset(assetItem.AssetPath);
            AssetDatabase.Refresh();
            ForgetAllChildrenPathsRecursive(assetItem);
        }

        #endregion

        // SAVING AND REBUILDING //

        #region SavingAndRebuilding

        public void SaveToSO(TreeDataSO so)
        {
            so.ItemsBones = new List<AssetItemBones>(); 
            so.NextId = _nextId;

            var createdBoneList = new List<AssetItemBones>();
            AddChildrenRecursive(_relevantAssets, ref createdBoneList);
            so.SetItemsBones(createdBoneList);
        }

        public void AddChildrenRecursive(TreeViewItem item, ref List<AssetItemBones> list) 
        {

            if (item is AssetTreeViewItem assetItem)
            {
                AssetItemBones bones = AssetItemBones.ConvertToBones(assetItem, IsExpanded(item.id)); 
                list.Add(bones);
            }

            if (item.children != null)
            {
                foreach (TreeViewItem child in item.children)
                {
                    AddChildrenRecursive(child, ref list);
                }
            }
        } 

        public void BuildFromSO(TreeDataSO so) 
        { 
            _nextId = so.NextId;
            foreach (AssetItemBones bone in so.ItemsBones)
            {
                _addedPaths.Add(bone.AssetPath); 
                var newItem = new AssetTreeViewItem(bone.Id, 0, bone.DisplayName, bone.AssetPath, bone.IsFakeFolder, bone.IconHueShift);
                TreeViewItem parent = FindItem(bone.ParentId, _relevantAssets);//Looks really risky lol but will be okay because of how the list is saved
                parent.AddChild(newItem);
                SetExpanded(newItem.id, bone.Expanded);
            }

            SetupDepthsFromParentsAndChildren(_relevantAssets);
        }

        #endregion

        // PING + REFRESH ON RIGHT CLICK //

        #region PingOnRightClick

        protected override void ContextClickedItem(int id) 
        {
            AssetTreeViewItem item = FindItem(id, _relevantAssets) as AssetTreeViewItem;
            if (item != null)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(item.AssetPath));
                if (AssetDatabase.IsValidFolder(item.AssetPath))
                {
                    AddItem(item.AssetPath, item.parent);
                    RemoveInvalidChildrenRecursive(item);
                    NeedsRepaint = true;
                }

                if (item.IsFakeFolder)
                {
                    RemoveInvalidChildrenRecursive(item);
                    item.ShiftHue();
                    NeedsRepaint = true;
                }
            }
        }
        private void RemoveInvalidChildrenRecursive(AssetTreeViewItem item)
        {
            if (item.children == null) return;

            List<TreeViewItem> childList = new(item.children);
            foreach (AssetTreeViewItem child in childList)
            {
                if (!child.IsFakeFolder && AssetDatabase.LoadAssetAtPath<Object>(child.AssetPath) == null)
                {
                    //if the asset path of this child has been removed from the project, remove the item from the tree
                    RemoveItem(child.id);
                }
                else
                {
                    RemoveInvalidChildrenRecursive(child);
                }
            }
        }

        #endregion

        // CREATING NEW ITEMS //

        #region CreatingNewItems

        private string GetFolderPath(string asset_path)
        {
            var splitPath = asset_path.Split('/');
            splitPath[^1] = String.Empty;
            return String.Join('/', splitPath);
        }

        private bool _renamingNewFile;
        private string _renamingNewFilePath;
        private TreeViewItem _proxyItem;

        private delegate bool CreationRenamingDelegate(string new_path);

        private CreationRenamingDelegate _creationRenamingDelegate;

        private void RenameNewFileEnded(RenameEndedArgs args)
        {
            TreeViewItem parent = _proxyItem.parent;
            RemoveItem(_proxyItem.id);

            if (!args.acceptedRename)
            {
                return;
            }

            string[] splitPath = _renamingNewFilePath.Split("/");
            string[] splitPathNoFile = splitPath[^1].Split(".");
            splitPathNoFile[0] = args.newName;
            splitPath[^1] = String.Join(".", splitPathNoFile);
            string newPath = String.Join("/", splitPath);

            // make sure this script doesn't already exist
            if (File.Exists(newPath))
            {
                Debug.Log("A file of that name + path already exists!");
                return;
            }

            // create the asset - if something goes wrong, don't add it to the tree
            bool success = _creationRenamingDelegate(newPath);
            if (!success) return;

            parent.AddChild(new AssetTreeViewItem(_proxyItem.id, _proxyItem.depth, args.newName, newPath));
            _addedPaths.Add(newPath);
            SortParentRecursive(parent);
            //add the script to the tree

            // SetupDepthsFromParentsAndChildren(parent);
            SetExpanded(parent.id, false);//there is definitely a better way to do this, but without this I don't know how to have the new child visible right away
            SetExpanded(parent.id, true);
        }

        private string CreateProxyItem(string display_name, Texture2D temp_icon)
        {
            TreeViewItem item = GetSelection().Count > 0 ? FindItem(GetSelection()[0], _relevantAssets) : null;
            string assetPath;
            TreeViewItem parent;
            if (item == null || item is not AssetTreeViewItem assetItem)
            {
                assetPath = "Assets/";
                parent = _relevantAssets;
            }
            else
            {
                if (AssetDatabase.IsValidFolder(assetItem.AssetPath))
                {
                    assetPath = assetItem.AssetPath + "/";
                    parent = assetItem;
                }
                else
                {
                    assetPath = GetFolderPath(assetItem.AssetPath);
                    parent = assetItem.parent;
                }
            }

            _proxyItem = new TreeViewItem(_nextId++, 1, display_name);
            _proxyItem.icon = temp_icon;
            parent.AddChild(_proxyItem);
            SetupDepthsFromParentsAndChildren(parent);
            SetExpanded(parent.id,
                false);//there is definitely a better way to do this, but without this I don't know how to have the new child visible right away
            SetExpanded(parent.id, true);
            _renamingNewFile = true;

            return assetPath;
        }

        public void CreateScript()
        {
            if (_renamingNewFile || _renaming) EndRename();
            
            const string scriptName = "BlankScript";
            string assetPath = CreateProxyItem(scriptName, EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D);
            _renamingNewFilePath = assetPath + scriptName + ".cs";
            _creationRenamingDelegate = WriteScript;
            BeginRename(_proxyItem);
        }
        private static bool WriteScript(string new_path)
        {
            using StreamWriter outfile = new StreamWriter(new_path);
            outfile.WriteLine("");
            AssetDatabase.Refresh();
            return true;
        }

        public void CreatePrefab()
        {
            if (_renamingNewFile || _renaming) EndRename();
            
            const string prefabName = "NewPrefab";
            string assetPath = CreateProxyItem(prefabName, EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D);
            _renamingNewFilePath = assetPath + prefabName + ".prefab";
            _creationRenamingDelegate = WritePrefab;
            BeginRename(_proxyItem);
        }
        private static bool WritePrefab(string new_path)
        {
            GameObject go = new GameObject();
            PrefabUtility.SaveAsPrefabAsset(go, new_path);
            Object.DestroyImmediate(go);
            return true;
        }

        public void CreateFolder()
        {
            if (_renamingNewFile || _renaming) EndRename();

            const string folderName = "NewFolder";
            string assetPath = CreateProxyItem(folderName, EditorGUIUtility.IconContent("Folder Icon").image as Texture2D);
            _renamingNewFilePath = assetPath + folderName;
            _creationRenamingDelegate = WriteFolder;
            BeginRename(_proxyItem);
        }
        private static bool WriteFolder(string new_path)
        {
            string[] splitPath = new_path.Split('/');
            string folderName = splitPath[^1];
            splitPath[^1] = string.Empty;
            string parentPath = String.Join('/', splitPath).Trim('/');
            AssetDatabase.CreateFolder(parentPath, folderName);
            AssetDatabase.Refresh();
            return true;
        }

        public void CreateScene()
        {
            if (_renamingNewFile || _renaming) EndRename();

            const string sceneName = "NewScene";
            string assetPath = CreateProxyItem(sceneName, EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image as Texture2D);
            _renamingNewFilePath = assetPath + sceneName + ".unity";
            _creationRenamingDelegate = WriteScene;
            BeginRename(_proxyItem);
        }
        private bool WriteScene(string new_path)
        {
            Scene scene;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            catch (InvalidOperationException)
            {
                 Debug.Log("FocusedBrowser cannot make a new scene when the open scene is untitled. Please save the open scene and try again.");
                 RemoveItem(_proxyItem.id);
                 return false;
            }
            EditorSceneManager.SaveScene(scene, new_path);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
            return true;
        }

        public void CreateMaterial()
        {
            if (_renamingNewFile || _renaming) EndRename();
            
            Texture2D matIcon = AssetTreeViewItem.matIcon;
            if (matIcon == null)
            {
                Material knownMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                matIcon = AssetPreview.GetMiniThumbnail(knownMaterial);
            }
            
            const string materialName = "NewMaterial";
            string assetPath = CreateProxyItem(materialName, matIcon);
            _renamingNewFilePath = assetPath + materialName + ".mat";
            _creationRenamingDelegate = WriteMaterial;
            BeginRename(_proxyItem);
        }
        private static bool WriteMaterial(string new_path)
        {
            Material mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, new_path);
            AssetDatabase.Refresh();
            return true;
        }

        public void CreateAsmdef()
        {
            if (_renamingNewFile || _renaming) EndRename();

            const string defName = "NewAsmDef";
            string assetPath = CreateProxyItem(defName, EditorGUIUtility.IconContent("AssemblyDefinitionAsset Icon").image as Texture2D);
            _renamingNewFilePath = assetPath + defName + ".asmdef";
            _creationRenamingDelegate = WriteAsmdef;
            BeginRename(_proxyItem);
        }
        private static bool WriteAsmdef(string new_path)
        {
            string name = new_path.Split('/')[^1];
            name = name.Remove(name.Length - 7);//remove ".asmdef
            
            using StreamWriter outfile = new StreamWriter(new_path);
            outfile.WriteLine("{");
            outfile.WriteLine($"  \"name\": \"{name}\",");
            outfile.WriteLine("  \"references\": [],");
            outfile.WriteLine("  \"includePlatforms\": [],");
            outfile.WriteLine("  \"excludePlatforms\": [],");
            outfile.WriteLine("  \"allowUnsafeCode\": false,");
            outfile.WriteLine("  \"overrideReferences\": false,");
            outfile.WriteLine("  \"precompiledReferences\": [],");
            outfile.WriteLine("  \"autoReferenced\": true,");
            outfile.WriteLine("  \"defineConstraints\": [],");
            outfile.WriteLine("  \"versionDefines\": [],");
            outfile.WriteLine("  \"noEngineReferences\": false");
            outfile.WriteLine("}");
            outfile.Close();
            
            AssetDatabase.ImportAsset(new_path);
            AssetDatabase.Refresh();

            return true;
        }
        
        
        #endregion
        
        // FAKE FOLDERS //
        #region Fake Folder Creation
        private static string _fakefolderString = "fakefolder";
        public void AddFakeFolder()
        {
            AssetTreeViewItem folder = new AssetTreeViewItem(_nextId, 0, "Folder", _fakefolderString + _nextId++, true);
            _relevantAssets.AddChild(folder);
            SetupDepthsFromParentsAndChildren(_relevantAssets);
            SetExpanded(_relevantAssets.id, false);
            SetExpanded(_relevantAssets.id, true);
        }

        public void RedrawFakeFolderIcons()
        {
            List<AssetTreeViewItem> fakeFolders = GetAllFakeFolders();
            foreach (var folder in fakeFolders)
            {
                folder.ApplyHueShift();
            }
        }
        private List<AssetTreeViewItem> GetAllFakeFolders()
        {
            List<AssetTreeViewItem> result = new();
            GetAllFakeFoldersRecursive(ref result, _relevantAssets);
            return result;
        }
        private void GetAllFakeFoldersRecursive(ref List<AssetTreeViewItem> result_list, TreeViewItem parent)
        {
            if (parent.children == null) return;

            foreach (AssetTreeViewItem child in parent.children)
            {
                if (child.IsFakeFolder)
                {
                    result_list.Add(child);
                }
              
                GetAllFakeFoldersRecursive(ref result_list, child);
            }
        }

        #endregion
    }
}