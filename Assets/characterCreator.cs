using System.Collections.Generic;
using core.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


public struct characterThumbnail {

	public GameManager   prefab;
	public characterskin skin;
	public bool          isSelected;


}

public class characterCreator : MonoBehaviour {


	// refs to skins
	public List <characterskin> male_skins;
	public List <characterskin> female_skins;


	//ui refs

	
	[ FoldoutGroup ("UI REFS") ] [ SerializeField ] private Button bt_male_list;
	[ FoldoutGroup ("UI REFS") ] [ SerializeField ] private Button bt_female_list;


	public void AddSkin ( characterskin sk ) {

	}

}
