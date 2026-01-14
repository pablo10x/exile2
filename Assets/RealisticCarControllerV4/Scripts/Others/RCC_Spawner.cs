//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Spawns last saved vehicle with PlayerPrefs. Used on demo scene while selecting a player vehicle and loading it on the next scene.
/// </summary>
public class RCC_Spawner : RCC_Core {

    private void Start() {

        int selectedIndex = PlayerPrefs.GetInt("SelectedRCCVehicle", 0);

        RCC.SpawnRCC(RCC_DemoVehicles.Instance.vehicles[selectedIndex], transform.position, transform.rotation, true, true, true);

    }

}
