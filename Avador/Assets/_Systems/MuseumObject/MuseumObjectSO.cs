using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MuseumObjectSO", menuName = "ScriptableObjects/MuseumObjectSO")]
public class MuseumObjectSO : ScriptableObject
{
    [HideInInspector] public int Id;
    public Sprite MainImage;
    public List<Sprite> OtherImages;
}
