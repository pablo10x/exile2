using Animancer;
using FishNet.Object;
using JetBrains.Annotations;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace core.player {
    public class CombatHandler : NetworkBehaviour {
        [SerializeField] private Character      _character;
       [SerializeField] private PlayerAnimator _playerAnimator;

        private float meleCeooldown = 0.3f;

        //currentWeapon
        [CanBeNull] private Weapon _currentWeapon;

        private void Awake() {
            _character      ??= GetComponent<Character>();
            _playerAnimator ??= GetComponent<PlayerAnimator>();
        }

        private void Start() {
            Observable.EveryUpdate()
                      .Where(_ => Input.GetKeyDown(KeyCode.F))                   // Replace with your input key or condition
                      .ThrottleFirst(System.TimeSpan.FromSeconds(meleCeooldown)) // Ensures the method is only called once per cooldown period
                      .Subscribe(_ => Attack())
                      .AddTo(this); // Automatically disposes the subscription when the GameObject is destroyed
        }

        private void Update() { }

        /// <summary>
        ///  bareFist or holding a gun this method will be called
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        public void Attack() {
            if (_currentWeapon is null) {
                //bare fist animations
                // _playerAnimator.PlayTimedAction();

                var r = Random.Range(0, _playerAnimator.combat_BareFistPunches_idle.Count);

                AnimancerState animationState;
                switch (_character.CurrentCharacterState) {
                    case Character.CharacterState.Idle:
                        animationState              =  _playerAnimator.PlayActionAnimation(_playerAnimator.combat_BareFistPunches_idle[r].AnimationClip, 0.1f, FadeMode.FixedSpeed);
                        animationState.Events(this).OnEnd += () => { _playerAnimator.PlayAnimation(_playerAnimator.anim_idle); };
                        break;

                    default:
                        // animationState = _playerAnimator.PlayActionAnimation(_playerAnimator.combat_BareFistPunche_run.AnimationClip);
                        animationState              =  _playerAnimator.PlayActionAnimation(_playerAnimator.combat_BareFistPunche_run);
                        animationState.Events(this).OnEnd += () => { _playerAnimator.FadeOutActionLayer(); };
                        break;
                }


                if (_character.CurrentCharacterState == Character.CharacterState.Idle) { }
                //pick random animation
            }
            else {
                // get weapon data
                //shoot
            }
        }
    }
}