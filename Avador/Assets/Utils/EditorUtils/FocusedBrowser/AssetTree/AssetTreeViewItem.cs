using System;
using System.Threading.Tasks;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable VirtualMemberCallInConstructor

namespace Utils.Editor
{
    [Serializable]
    public class AssetTreeViewItem : TreeViewItem 
    {
        public string AssetPath;
        public static Texture2D matIcon;
        public bool IsFakeFolder;
        public float IconHueShift;

        private static Texture2D folderOpen;
        private static Texture2D folderClosed;
        private int _priority;

        public AssetTreeViewItem(int id, int depth, string displayName, string path, bool is_fake_folder = false, float hue_shift = 0f) : base(id, depth, displayName)
        {
            AssetPath = path;
            _priority = 1;
            IsFakeFolder = is_fake_folder;
 
            //Separate handling for fake folders
            if (IsFakeFolder)
            {
                _priority = 2;
                icon = folderClosed ??= EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                IconHueShift = hue_shift;
                if (hue_shift != 0f)
                    ApplyHueShift();
            }
            
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null)
            {
                //sometime this happens lol
                TrySetupDataInASec(path);
                return;
            }
            icon = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;
            // Set the icon based on the asset type
            if (AssetDatabase.IsValidFolder(AssetPath))
            {
                _priority = 2;
            } 
            else if (AssetPath.EndsWith(".asmdef"))
            {
                _priority = 0;
            }
        }

        private async void TrySetupDataInASec(string path)
        {
            int count = 0;
            await Task.Delay(50);
            
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            while (obj == null)
            {
                if (count > 20) //this will never happen but just in case, I don't want to be responsible for a bunch of async background infinite loops
                {
                    return;
                }
                await Task.Delay(50);
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                count++;
            }
            
            icon = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;
            // Set the icon based on the asset type
            if (AssetDatabase.IsValidFolder(AssetPath))
            {
                _priority = 2;
            } 
            else if (AssetPath.EndsWith(".asmdef"))
            {
                _priority = 0; 
            }
        }
        
        public static int Sort(AssetTreeViewItem a, AssetTreeViewItem b)
        {
            if (a._priority != b._priority)
            {
                return b._priority - a._priority;
            }

            return String.CompareOrdinal(a.displayName, b.displayName);
        }

        public void Expanded(bool state)
        {
            if (state)
            {
                icon = folderOpen ??= EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;
            }
            else
            {
                icon = folderClosed ??= EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            }
        }

        //Fake folders get their color shifted when right clicked. Some things are hardcoded for the sole use case (like hue shift amount and what the saturation gets set to)
        #region Color Shift
        public void ShiftHue()
        {
            IconHueShift += 0.3f; //0.6 is a magic number for how we want to switch hues. Picked because it'll wrap around a few times without overlapping.
            while (IconHueShift > 1f) //clamp between -1 and 1
                IconHueShift -= 2f;
            
            ApplyHueShift();
        }

        public void ApplyHueShift()
        {
            folderClosed ??= EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            if (Mathf.Abs(IconHueShift) < 0.01f) return;
            Texture2D readableTexture = CreateReadableTexture(folderClosed);
            HueShiftTexture(readableTexture, IconHueShift);
            icon = readableTexture;
        } 
        
        Texture2D CreateReadableTexture(Texture2D original)
        {
            // Create a new texture with the same dimensions and format as the original
            Texture2D readableTexture = new Texture2D(original.width, original.height, original.format, true);
        
            // Copy pixel data from the original texture
            Graphics.CopyTexture(original, readableTexture);

            return readableTexture;
        } 

        private void HueShiftTexture(Texture2D texture, float hueShift)
        {
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color color = pixels[i];
                float h, s, l;

                // Convert the color to HSL
                Color.RGBToHSV(color, out h, out s, out l);

                // Adjust the hue
                h += hueShift;
                h = Mathf.Repeat(h, 1f); // Ensure the hue stays within [0, 1]

                s = 0.7f;
                
                // Convert back to RGB
                color = Color.HSVToRGB(h, s, l);
                color.a = pixels[i].a;
                
                pixels[i] = color;
            }

            // Apply the modified pixels back to the texture
            texture.SetPixels(pixels);
            texture.Apply();
        }
        #endregion
    }
}