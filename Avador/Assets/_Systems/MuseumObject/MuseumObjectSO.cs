using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MuseumObjectSO", menuName = "ScriptableObjects/MuseumObjectSO")]
public class MuseumObjectSO : ScriptableObject
{
    [HideInInspector] public int Id;
    public string ObjectName;
    [TextArea(15, 20)]
    public string Description;
    public Sprite MainImage;
    public Sprite[] OtherImages;
}
