using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using core.Managers;
using core.player;
using core.Types;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace core.Vehicles {
    [Serializable]
    public struct CarSeat {
        public bool      isDriver;
        public Transform transform;
        public Transform exitpos;
        public bool      used;
        public Character character;

        public void FreetheSeat() {
            isDriver  = false;
            used      = false;
            character = null;
        }
    }

    [RequireComponent(typeof(RCC_CarControllerV3), typeof(AudioSource))]
    public class CarController : Vehicle {
        [FormerlySerializedAs("controllerV3"),SerializeField] public RCC_CarControllerV3 controllerV4;
        [SerializeField] private new Renderer            renderer;
        [SerializeField] internal Collider            VehicleCollider;

        [BoxGroup("Camera point")] public Transform cameraPoint;

        private AudioSource _audioSource;

        [FoldoutGroup("Sounds")]  public AudioClip openDoor;
        [FoldoutGroup("Sounds/Door")]                          public AudioClip closeDoor;
        [FoldoutGroup("Sounds/Honk")]                          public AudioClip HonkClip;

        [FoldoutGroup("Lights")] public GameObject light_stop;
        [FoldoutGroup("Lights")] public GameObject light_left_indicator;
        [FoldoutGroup("Lights")] public GameObject light_right_indicator;
        [FoldoutGroup("Lights")] public GameObject light_Headlight;
        [FoldoutGroup("Lights")] public GameObject light_Siren;

        //Seats
        [Header("seats")] public CarSeat[] passangerSeats;
        public                   CarSeat   driverSeat;

        private       float _zeroSpeedTime;
        private const float ZERO_SPEED_THRESHOLD = 3f;

        [BoxGroup("Misc")]
        //navigation
        public NavMeshAgent NavMeshAgent;

        public event Action<Character, CarSeat> OnPlayerEnterVehicle;
        public event Action<Character>          OnPlayerExitVehicle;

        private void Awake() {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start() {
            // if (controllerV3.engineRunning) {
            //     controllerV3.KillEngine();
            // }
        }

        public CarSeat? SetPlayerInVehicle(Character character, PlayerAnimator animator, bool driver = false) {
            if (GetFreeCarSeats()
                    .Count ==
                0 &&
                driverSeat.used) return null;

            character.playerCar = this;

            CarSeat seat = driver
                               ? SetDriverInVehicle(character, animator)
                               : SetPassengerInVehicle(character);


            character.navMeshAgent.enabled = false;
            fx_OpenDoorSound();

            character.motor.enabled = false;
            OnPlayerEnterVehicle?.Invoke(character, seat);

            return seat;
        }

        private CarSeat SetDriverInVehicle(Character character, PlayerAnimator animator) {
            if (driverSeat.used) return default;

            SetCharacterToSeat(character, driverSeat);
            driverSeat.used             = true;
            controllerV4.handbrakeInput = 0f;

            if (UiManager.Instance != null) {
                UiManager.Instance.ShowVehicleControllerPage(true);
            }

            character.motor.Capsule.enabled = false;

            // controllerV3.onGearChanged = _ => {
            //     if (!animator.isActionPlaying(animator.ACTION_DRIVER_CHANGE_GEAR))
            //         animator.PlayTimedAction(animator.ACTION_DRIVER_CHANGE_GEAR, 0.2f, FadeMode.FromStart);
            // };

            character.CurrentCharacterState = Character.CharacterState.InVehicleDriver;


            if (!controllerV4.engineRunning)
                controllerV4.StartEngine();
            return driverSeat;
        }

        private CarSeat SetPassengerInVehicle(Character character) {
            if (UiManager.Instance != null) {
                UiManager.Instance.ShowVehicleControllerPage(false);
            }

            character.motor.Capsule.enabled = false;
            character.CurrentCharacterState = Character.CharacterState.InVehiclePassenger;

            var freeSeats  = GetFreeCarSeats();
            var randomSeat = freeSeats[UnityEngine.Random.Range(0, freeSeats.Count)];
            SetCharacterToSeat(character, randomSeat);

            return randomSeat;
        }

        private void SetCharacterToSeat(Character character, CarSeat carSeat) {
            character.motor.SetPositionAndRotation(carSeat.transform.position, carSeat.transform.rotation);
            character.transform.parent = driverSeat.transform;
            character.motor.SetCapsuleCollisionsActivation(false);
            character.motor.SetGroundSolvingActivation(false);
            carSeat.character       = character;
            character.playerCarSeat = carSeat;
        }


        [Button("Update Behaviour")]
        private void UpdateBehaviour() {
           // controllerV4.CheckBehavior();
        }
        

        private void Update() {
           
        }

        private void FixedUpdate() {
           
        }

        private void CheckSpeed() {
            if (!IsVehicleOccupied()) return;

            if (controllerV4.speed < 4) {
                _zeroSpeedTime += Time.deltaTime;
                if (_zeroSpeedTime >= ZERO_SPEED_THRESHOLD) {
                    controllerV4.enabled = false;
                }
            }
            else {
                _zeroSpeedTime = 0f;
                if (!controllerV4.enabled) {
                    controllerV4.enabled = true;
                }
            }
        }

        public void RemovePlayerFromVehicle(Character character) {
            if (!IsVehicleOccupied()) {
                // Debug.Log($"Vehicle not occuiped returning");
                return;
            }

            // Find the seat the character is occupying
            CarSeat? seat = null;

            seat = character.playerCarSeat;

            // if (driverSeat.character == character) {
            //     seat = driverSeat;
            // }
            // else {
            //     // seat = passangerSeats.FirstOrDefault(s => s.character == character);
            //     seat = character.playerCarSeat;
            // }

            if (seat == null) {
                // Debug.Log("seat is null retuning");
                return; // Character not found in any seat
            }

            // --- Logic moved from Character.ExitVehicle ---

            // Reset player's transform and enable collision detection
            character.motor.transform.parent = null;
            character.transform.localScale   = Vector3.one;
            character.motor.SetPositionAndRotation(seat.Value.exitpos.position, Quaternion.identity);
            character.TogglePlayerCollisionDetection(true);
            character.motor.enabled = true;

            // Handle vehicle-specific actions

            if (seat.Value.isDriver) {
                controllerV4.KillEngine();
                driverSeat.used = false;
            }

            seat.Value.FreetheSeat();

            // Disable navigation and play exit sound
            character.navMeshAgent.enabled = false;
            fx_CloseDoorSound();


            //

           
                if (UiManager.Instance != null && character.orbitCamera != null) {
                    // Update UI
                    UiManager.Instance.ShowControllerPage();

                    // Update camera colliders
                    if (character.orbitCamera.IgnoredColliders.Contains(VehicleCollider))
                        character.orbitCamera.IgnoredColliders.Remove(VehicleCollider);
                }
            


            OnPlayerExitVehicle?.Invoke(character);


            //enable navmesh
            character.navMeshAgent.enabled = true;

            // Stop vehicle-related animations
            character.pa.FadeOutActionLayer();

            // Update character state
            character.invehicle             = false;
            character.CurrentCharacterState = Character.CharacterState.Idle;

            // --- End of moved logic ---
        }

        private bool IsVehicleOccupied() => passangerSeats.Any(seat => seat.used) || driverSeat.used;

        public void fx_OpenDoorSound() => _audioSource.PlayOneShot(openDoor);

        public void fx_CloseDoorSound() => _audioSource.PlayOneShot(closeDoor);

        public List<CarSeat> GetFreeCarSeats() =>
            passangerSeats.Where(seat => !seat.used)
                          .ToList();

        public void Honk(float duration = 0.2f) {
            if (HonkClip == null || _audioSource == null) return;

            // Start playing the honk sound
            _audioSource.clip = HonkClip;
            _audioSource.Play();

            // Stop the honk sound after the specified duration using UniRx
            Observable.Timer(TimeSpan.FromSeconds(duration))
                      .Subscribe(_ => _audioSource.Stop())
                      .AddTo(this); // Automatically disposes if the GameObject is destroyed
        }
    }
}