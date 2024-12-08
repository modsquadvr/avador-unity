using System.Collections.Generic;
using UnityEngine;

public class ContentProvider : MonoBehaviour
{
	public List<MuseumObjectSO> MuseumObjectSOs = new List<MuseumObjectSO>();
	[HideInInspector] public int CurrentObjectId = -1;
	public int MaximumId => MuseumObjectSOs.Count - 1;


	public void Start()
	{
		for (int i = 0; i < MuseumObjectSOs.Count; i++)
			MuseumObjectSOs[i].Id = i;
	}
}
