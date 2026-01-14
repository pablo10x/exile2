using Exile.Inventory.Examples;
using Salvage.ClothingCuller.Components;
using UnityEngine;

namespace core.player {

    public enum ClothingSlots {
        Tshirt,
        Pants,
        Hoodie,
        Shoes,
        Suit
    }

    public class CharacterEquipmentManager : MonoBehaviour {

        [SerializeField] private Occludee       body;
        [SerializeField] private ClothingCuller _clothingCuller;

   
      

        public void OnStartClient() {
            //base.OnStartClient();
         
            _clothingCuller.Register(body, false);

          
        }

       
        
        private void Start() {
                _clothingCuller.Register(body, false);
        }


        public void EquipBodyItem(ItemCloth item) {
            _clothingCuller.Register(item.ItemPrefab);
        }

   

   
    }
}