using Sirenix.OdinInspector;
using UnityEngine;

namespace core.Types {

    public abstract class Vehicle : MonoBehaviour {

        [ FoldoutGroup ("Vehicle info") ] public string vehicleName = "Vehicle";

        [ FoldoutGroup ("Vehicle info") ] public int vehicleSeats = 1;

    }

}
