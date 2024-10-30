using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

//Shader graph doesn't work in UI on overlay canvases. This can be fixed by converting the shader graph to an actual HLSL file, and making some changes.
//This is for regular unlit shaders (and don't forget to make them transparent if they should be) - NOT sprite shaders. Sort converted shaders by z-value in their transform on your overlay canvas.
//This script crudely generates HLSL shaders from .shadergraph files, and makes the follow changes: 1) removes tags on the first pass, 2) removes all secondary passes.
//This makes things work in UI, but I don't know enough about shaders to have made a mature solution, so your shader may break if it needed the secondary passes.
//Technique credit to Matt Whiting from https://www.youtube.com/watch?v=GY3aHVDBAno
public class ShaderGraphConverter : EditorWindow 
{
	private Object[] selectedObjects;
	private bool allShaderGraphsSelected = false;

	[MenuItem("Tools/Shader Graph Converter")]
	public static void ShowWindow()
	{
		GetWindow<ShaderGraphConverter>("Shader Graph Converter");
	}

	private void OnGUI()
	{
		GUILayout.Label("Shader Graph Converter", EditorStyles.boldLabel);

		if (Selection.objects.Length > 0)
		{
			selectedObjects = Selection.objects;
			allShaderGraphsSelected = selectedObjects.All(obj => obj != null && AssetDatabase.GetAssetPath(obj).EndsWith(".shadergraph"));
		}
		else
		{
			selectedObjects = null;
			allShaderGraphsSelected = false;
		}

		if (allShaderGraphsSelected)
		{
			GUILayout.Label("<color=green>All selected files are .shadergraph files</color>", new GUIStyle(GUI.skin.label) { richText = true });
		}
		else
		{
			GUILayout.Label("<color=red>Select .shadergraph files</color>", new GUIStyle(GUI.skin.label) { richText = true });
		}

		if (GUILayout.Button("Convert") && allShaderGraphsSelected)
		{
			foreach (var obj in selectedObjects)
			{
				string path = AssetDatabase.GetAssetPath(obj);
				ConvertShaderGraphToHLSL(path);
			}
		}
		GUILayout.BeginVertical();
		GUILayout.Label("<color=green>//Shader graph doesn't work in UI on overlay canvases.\n This can be fixed by converting the shader graph to an actual HLSL file, and making some changes.\n//This is for regular unlit shaders (and don't forget to make them transparent if they should be) - NOT sprite shaders.\nSort converted shaders by z-value in their transform on your overlay canvas.\n//This script crudely generates HLSL shaders from .shadergraph files, and makes the follow changes:\n 1) removes tags on the first pass, 2) removes all secondary passes.\n//This makes things work in UI, but I don't know enough about shaders to have made a mature solution,\n so your shader may break if it needed the secondary passes.</color>",new GUIStyle(GUI.skin.label) { richText = true });
		GUILayout.EndVertical();
	}

	private void ConvertShaderGraphToHLSL(string inputFile)
	{
		// Generate the shader code using reflection
		string shaderCode = GenerateShaderCode(inputFile);

		string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFile), "Converted_" + Path.GetFileNameWithoutExtension(inputFile) + ".shader");
		string[] lines = shaderCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

		// Modify the shader code as needed
		Regex shaderNamePattern = new Regex(@"Shader\s+""Shader Graphs/(.*)""");
		lines[0] = shaderNamePattern.Replace(lines[0], @"Shader ""Converted/$1""");

		// Identify and retain only the first pass
		Regex passStartPattern = new Regex(@"^\s*Pass\s*");
		Regex endHlslPattern = new Regex(@"^\s*ENDHLSL");
		Regex tagsPattern = new Regex(@"^\s*Tags");
		Regex endBlockPattern = new Regex(@"^\s*}");

		bool foundFirstPass = false;
		bool inFirstPass = false;

		bool addLine = true;
		bool deleteNextThenGo = false;
		bool deletedNowGo = false;

		bool removingTags = false;
		

		var sb = new StringBuilder();

		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].Trim();

			if (deletedNowGo)
			{
				addLine = true;
				deletedNowGo = false;
			}
			
			if (deleteNextThenGo)
			{
				addLine = false;
				deleteNextThenGo = false;
				deletedNowGo = true;
			}
			else if (passStartPattern.IsMatch(line))
			{
				if (!foundFirstPass)
				{
					foundFirstPass = true;
					inFirstPass = true;
					addLine = true;
				}
				else
				{
					addLine = false;
				}
			}
			else if (endHlslPattern.IsMatch(line))
			{
				if (inFirstPass)
				{
					addLine = true;
					inFirstPass = false;
					removingTags = true;
				}
				else
				{
					addLine = false;
					deleteNextThenGo = true;
				}
			}
			else if (inFirstPass)
			{
				//remove the tags
				if (tagsPattern.IsMatch(line))
				{
					addLine = false;
					removingTags = true;
				}

				if (removingTags && endBlockPattern.IsMatch(line))
				{
					addLine = false;
					deletedNowGo = true;
					removingTags = false;
				}
			}

			if (addLine)
			{
				sb.Append(line + '\n');
			}
		}

		// Write the modified shader code to the new file
		File.WriteAllText(outputFilePath, sb.ToString());
		Debug.Log("Shader conversion completed successfully. Output file: " + outputFilePath);
	}


	//All code below credit of Farl_Lee on these forums https://discussions.unity.com/t/how-to-get-shader-source-code-from-script/839046/6
	private static object GetGraphData(string shaderAssetPath)
	{
		var importer = AssetImporter.GetAtPath(shaderAssetPath);

		var textGraph = File.ReadAllText(importer.assetPath, Encoding.UTF8);
		var graphObjectType = Type.GetType("UnityEditor.Graphing.GraphObject, Unity.ShaderGraph.Editor")!;

		var graphObject = ScriptableObject.CreateInstance(graphObjectType);
		graphObject.hideFlags = HideFlags.HideAndDontSave;

		bool isSubGraph;
		var extension = Path.GetExtension(importer.assetPath).Replace(".", "");
		switch (extension)
		{
			case "shadergraph" :
				isSubGraph = false;
				break;
			case "ShaderGraph" :
				isSubGraph = false;
				break;
			case "shadersubgraph" :
				isSubGraph = true;
				break;
			default :
				throw new Exception($"Invalid file extension {extension}");
		}

		var assetGuid = AssetDatabase.AssetPathToGUID(importer.assetPath);

		var graphObject_graphProperty = graphObjectType.GetProperty("graph")!;
		var graphDataType = Type.GetType("UnityEditor.ShaderGraph.GraphData, Unity.ShaderGraph.Editor")!;
		var graphDataInstance = Activator.CreateInstance(graphDataType);
		graphDataType.GetProperty("assetGuid")!.SetValue(graphDataInstance, assetGuid);
		graphDataType.GetProperty("isSubGraph")!.SetValue(graphDataInstance, isSubGraph);
		graphDataType.GetProperty("messageManager")!.SetValue(graphDataInstance, null);
		graphObject_graphProperty.SetValue(graphObject, graphDataInstance);

		var multiJsonType = Type.GetType("UnityEditor.ShaderGraph.Serialization.MultiJson, Unity.ShaderGraph.Editor")!;
		var deserializeMethod = multiJsonType.GetMethod("Deserialize")!;
		var descrializeGenericMethod = deserializeMethod.MakeGenericMethod(graphDataType);
		descrializeGenericMethod.Invoke(null, new object[] { graphDataInstance, textGraph, null, false });

		graphDataType.GetMethod("OnEnable")!.Invoke(graphDataInstance, null);
		graphDataType.GetMethod("ValidateGraph")!.Invoke(graphDataInstance, null);

		return graphDataInstance;
	}

	private static string GenerateShaderCode(string shaderAssetPath, string shaderName = null)
	{
		Type generatorType =
			Type.GetType("UnityEditor.ShaderGraph.Generator, Unity.ShaderGraph.Editor")!;
		Type modeType =
			Type.GetType("UnityEditor.ShaderGraph.GenerationMode, Unity.ShaderGraph.Editor")!;

		shaderName ??= Path.GetFileNameWithoutExtension(shaderAssetPath);

		object graphData = GetGraphData(shaderAssetPath);

		object forReals = ((FieldInfo)modeType.GetMember("ForReals")[0]).GetValue(null);
		object generator = Activator.CreateInstance(
			generatorType,
			new object[] { graphData, null, forReals, shaderName, null, null, true }
		);
		object shaderCode = generatorType
			.GetProperty("generatedShader", BindingFlags.Public | BindingFlags.Instance)!
			.GetValue(generator);

		return (string)shaderCode;
	}
}
