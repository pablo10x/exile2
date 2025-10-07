
using Sirenix.OdinInspector;
using UnityEngine;

public enum Gender {

	Male, Female

}


[ CreateAssetMenu (fileName = "characterskin", menuName = "CNR/characterskin") ]
public class characterskin : ScriptableObject {

	public              int      SkinID;
	public              Gender   CharacterGender = Gender.Male;
	[ Required ] public Material skinMaterial;

	[ SerializeField ] [PreviewField(128)] private Sprite CharacterThumbnail;


#region body parts

	[ Required ] [ FoldoutGroup ("Body Parts") ]
	public Mesh Head;

	[ Required ] [ FoldoutGroup("Body Parts") ] public Mesh Neck;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh UpperBody;

	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh RightShoulder;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh RightArm;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh RightHand;

	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh LeftShoulder;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh LeftArm;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh LeftHand;


	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh LowerBody;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh LowerBodyShort;
	[ Required ] [ FoldoutGroup ("Body Parts") ] public Mesh Feet;

#endregion


}
