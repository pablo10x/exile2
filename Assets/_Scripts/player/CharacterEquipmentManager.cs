using System.Collections.Generic;
using Exile.Inventory.Examples;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Salvage.ClothingCuller.Components;
using Sirenix.OdinInspector;
using UnityEngine;

namespace core.player {

    public enum ClothingSlots {
        Tshirt,
        Pants,
        Hoodie,
        Shoes,
        Suit
    }

    public class CharacterEquipmentManager : NetworkBehaviour {

        [SerializeField] private Occludee       body;
        [SerializeField] private ClothingCuller _clothingCuller;

   
      

        public override void OnStartClient() {
            base.OnStartClient();
         
            _clothingCuller.Register(body, false);

          
        }

       
        
        private void Start() {
                _clothingCuller.Register(body, false);
        }

   

   
    }
}
