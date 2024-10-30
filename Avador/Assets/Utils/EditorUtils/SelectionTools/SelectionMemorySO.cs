using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utils.Editor
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Utils/SelectionMemory")]
	public class SelectionMemorySO : ScriptableObject
	{
		[Header("DOESN'T WORK ON SELECTIONS THAT HAVEN'T BEEN SAVED IN THE SCENE")]
		[FormerlySerializedAs("_MaxMemoryDepth")] public int MaxMemoryDepth = 16;//any more than that, and you are using it irresponsibly, I say.
		[Tooltip("selections larger than this size will be ignored")] public int MaxSelectionSize = 64; //you can click ctrl+a again

		public List<PersistentReferenceArrayWrapper> SelectionMemory = new();
		public List<PersistentReferenceArrayWrapper> SelectionRedoStack = new();
		
		[Serializable]
		public class PersistentReferenceArrayWrapper
		{
			public PersistentReference[] Array;
			
			public PersistentReference this[int key]
			{
				get
				{
					return Array[key];
				}
				set
				{
					Array[key] = value;
				}
			}
			
			// Implicit operator to assign a PersistentReference[] to PersistentReferenceArrayWrapper
			public static implicit operator PersistentReferenceArrayWrapper(PersistentReference[] array)
			{
				return new PersistentReferenceArrayWrapper { Array = array };
			}

			// Optional: Implicit operator to convert back from PersistentReferenceArrayWrapper to PersistentReference[]
			public static implicit operator PersistentReference[](PersistentReferenceArrayWrapper wrapper)
			{
				return wrapper.Array;
			}
		}
	}
}
