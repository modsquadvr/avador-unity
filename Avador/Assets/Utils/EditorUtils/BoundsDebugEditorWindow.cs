using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BoundsDebugEditorWindow
{
	private static List<Bounds> boundsList = new List<Bounds>();
	private static float gizmoDisplayTime = 5f;
	private static float gizmoStartTime;
	private static Color color = new Color(0f, 1f, 0f, 0.05f);
	
	[MenuItem("Inkblot/Print Selected Object Size")]
	public static void PrintObjectSize()
	{
		boundsList.Clear();
		foreach (GameObject obj in Selection.gameObjects)
		{
			if (obj != null)
			{
				Bounds bounds = GetMaxBounds(obj);
				Debug.Log($"GameObject: {obj.name}, Bounds Size: {bounds.size.x}, {bounds.size.y}, {bounds.size.z}");
				boundsList.Add(bounds);
			}
		}
		gizmoStartTime = Time.realtimeSinceStartup;
		SceneView.duringSceneGui += OnSceneGUI;
	}
	
	private static Bounds GetMaxBounds(GameObject parent)
	{
		var total = new Bounds(parent.transform.position, Vector3.zero);
		foreach (var child in parent.GetComponentsInChildren<Collider>())
		{
			total.Encapsulate(child.bounds);
		}
		return total;
	}
	
	 private static void OnSceneGUI(SceneView sceneView)
    {
        if (Time.realtimeSinceStartup - gizmoStartTime > gizmoDisplayTime)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            return;
        }
        
        foreach (var bounds in boundsList)
        {
            DrawGizmoCube(bounds);
        }
        
        SceneView.RepaintAll();
    }

    private static void DrawGizmoCube(Bounds bounds)
    {
        var center = bounds.center;
        var size = bounds.size;
        var halfSize = size / 2.0f;

        Vector3[] vertices = new Vector3[8]
        {
            center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
            center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
            center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
            center + new Vector3(-halfSize.x, halfSize.y, halfSize.z),
        };
        
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { vertices[0], vertices[1], vertices[2], vertices[3] }, color, Color.clear);
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { vertices[4], vertices[5], vertices[6], vertices[7] }, color, Color.clear);
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { vertices[0], vertices[1], vertices[5], vertices[4] }, color, Color.clear);
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { vertices[1], vertices[2], vertices[6], vertices[5] }, color, Color.clear);
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { vertices[2], vertices[3], vertices[7], vertices[6] }, color, Color.clear);
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { vertices[3], vertices[0], vertices[4], vertices[7] }, color, Color.clear);
    }
}