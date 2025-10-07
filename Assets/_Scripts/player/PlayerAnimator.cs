using System;
using System.Collections.Generic;
using Animancer;
using core.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace core.player {
    public class PlayerAnimator : MonoBehaviour {
        #region Fields

        //get player
        //[SerializeField] private PlayerController _PlayerController;

        [SerializeField] private DirectionalAnimationSet8 _strafe_directionalAnimationSet;
        [SerializeField] private DirectionalAnimationSet8 _crouch_directionalAnimationSet;
        
        
        [FormerlySerializedAs("_AnimancerComponent")] public AnimancerComponent _animancer;

        [FoldoutGroup("locomotion")] public AnimationClip anim_idle;
        [FoldoutGroup("locomotion")] public AnimationClip crouch_idle;
        [FoldoutGroup("locomotion")] public AnimationClip anim_walk;
        [FoldoutGroup("locomotion")] public AnimationClip anim_run;


        public AnimationClip anim_turnLeft; // Assign in inspector
        public AnimationClip anim_turnRight;
        
        
        //vehicles
        [FoldoutGroup("Vehicles")] public AnimationClip anim_Vehicle_Passenger_sit;
        [FoldoutGroup("Vehicles")]                                                public AnimationClip Vehicle_driver_reversing;

        #region CombatMovement

        [FormerlySerializedAs("combat_BareFistPunches"), FoldoutGroup("Combat")] public List<CharacterActionData> combat_BareFistPunches_idle;
        [FoldoutGroup("Combat/BareFist")]                                                                          public CharacterActionData       combat_BareFistPunche_run;

        #endregion

        #region Jumping

        [Space] [FoldoutGroup("Jumping")] public AnimationClip anim_jump_start;
        [FoldoutGroup("Jumping")]         public AnimationClip anim_jump_inplace;
        [FoldoutGroup("Jumping")]         public AnimationClip anim_falling_frist;
        [FoldoutGroup("Jumping")]         public AnimationClip anim_falling_second;
        [FoldoutGroup("Jumping")]         public AnimationClip anim_falling_loop;
        [FoldoutGroup("Jumping")]         public AnimationClip anim_land_med;
        [FoldoutGroup("Jumping")]         public AnimationClip anim_land_low;

        #endregion

        private AnimancerLayer _BaseLayer;
        private AnimancerLayer _ActionLayer;

        [SerializeField] private AvatarMask _ActionLayerMask;

        // actions

        [BoxGroup("Actions")] [InspectorName("action_change_gear")] public CharacterActionData ACTION_DRIVER_CHANGE_GEAR;

        [BoxGroup("Actions")] [InspectorName("action_reverse")] public CharacterActionData ACTION_DRIVER_REVERSE;
        [BoxGroup("Actions")] [InspectorName("action_ROTATE_LEFT")] public CharacterActionData CHARACTER_ROTATE_LEFT;
        [BoxGroup("Actions")] [InspectorName("action_ROTATE_RIGHT")] public CharacterActionData CHARACTER_ROTATE_RIGHT;

        // mixers
        [SerializeField] private LinearMixerTransition driverMixerState;

        // Animation blending support
        [SerializeField] private LinearMixerTransition locomotionMixer;
        private bool isLocomotionMixerPlaying;

        #endregion

        private AnimationClip _lastStrafeClip = null;
        private void Awake() {



            _animancer.Graph.ApplyAnimatorIK = true;
            
            if (_animancer == null) {
                _animancer = GetComponent<AnimancerComponent>();
            }
            if (_animancer == null) {
                Debug.LogError("AnimancerComponent is missing on PlayerAnimator GameObject.");
                return;
            }
            _BaseLayer   = _animancer.Layers[0];
            _ActionLayer = _animancer.Layers[1];
            _ActionLayer.Mask = _ActionLayerMask;
            _ActionLayer.SetDebugName("Action Layer");

        
            _animancer.Play(anim_idle);
        }

        /// <summary>
        /// Updates the driver steering mixer.
        /// </summary>
        /// <param name="value">The steering value.</param>
        /// <param name="smoothTime">The smoothing time.</param>
        public void UpdateVehicleSteering_Mixer(float value, float smoothTime = 0.2f) {
            // if (!_animancer.IsPlaying(driverMixerState)) {
            //     _BaseLayer.Play(driverMixerState);
            // }

            _BaseLayer.Play(driverMixerState);


            float olds_teering = driverMixerState.State.Parameter;
           


            try {
                driverMixerState.State.Parameter = Mathf.Lerp(olds_teering, value, 1f * Time.deltaTime);
                // driverMixerState.Transition.State.Parameter = value;
            }
            catch (Exception e) {
                UnityEngine.Debug.Log($"{e}");
            }
        }

        /// <summary>
        /// Disables the driver steering mixer.
        /// </summary>
        public void DisableVehicleSteering_mixer() {
            if (_animancer.IsPlaying(driverMixerState)) {
                _animancer.Stop(driverMixerState);
            }
        }

        /// <summary>
        /// Plays an animation on the base layer.
        /// </summary>
        /// <param name="clip">The animation clip to play.</param>
        /// <param name="fade">The fade duration.</param>
        /// <param name="fadeMode">The fade mode.</param>
        /// <returns>The animancer state.</returns>
        public AnimancerState PlayAnimation(AnimationClip clip, float fade = 0.2f, FadeMode fadeMode = FadeMode.FixedSpeed) {

          
            return _BaseLayer.Play(clip, fade, fadeMode);
        }

        /// <summary>
        /// Plays a timed action animation on the action layer.
        /// </summary>
        /// <param name="action_data">The action data containing the animation clip and avatar mask.</param>
        /// <param name="fade">The fade duration.</param>
        /// <param name="fadeMode">The fade mode.</param>
        public void PlayTimedAction(CharacterActionData action_data, float fade = 0.2f, FadeMode fadeMode = FadeMode.FromStart) {
            //update mask 
            _ActionLayerMask = action_data.AvatarMask;

            
            
            AnimancerState state = _ActionLayer.Play(action_data.AnimationClip, fade, fadeMode);

            
            state.Events(this).OnEnd = () => {
                // _ActionLayer.DestroyStates();
                _ActionLayer.StartFade(0, fade);
            };
        }

        /// <summary>
        /// Plays an action animation on the action layer.
        /// </summary>
        /// <param name="clip">The animation clip to play.</param>
        /// <param name="fade">The fade duration.</param>
        /// <param name="fadeMode">The fade mode.</param>
        /// <returns>The animancer state.</returns>
        public AnimancerState PlayActionAnimation(AnimationClip clip, float fade = 0.2f, FadeMode fadeMode = FadeMode.FixedDuration) {
            if (_ActionLayer.IsPlayingClip(clip)) _ActionLayer.Stop();
            return _ActionLayer.Play(clip, fade, fadeMode);
        }

        /// <summary>
        /// Plays an action animation on the action layer.
        /// </summary>
        /// <param name="characterActionData">The character action data.</param>
        /// <param name="fadeMode">The fade mode.</param>
        /// <returns>The animancer state.</returns>
        public AnimancerState PlayActionAnimation(CharacterActionData characterActionData, FadeMode fadeMode = FadeMode.FixedDuration) {
            AnimancerLayer layer = _ActionLayer;

            _ActionLayerMask = characterActionData.AvatarMask;
            

            if (layer != null) {
               

                return layer.Play(characterActionData.AnimationClip, characterActionData.LayerFadeOut, fadeMode);
            }


            return null;
        }

        public void StopActionanimation() {
            _ActionLayer.Stop();
        }

        /// <summary>
        /// Returns true if the action layer is playing the given animation clip.
        /// </summary>
        /// <param name="characterActionData">The character action data.</param>
        /// <returns>True if the action layer is playing the given animation clip.</returns>
        public bool isActionPlaying(CharacterActionData characterActionData) {
            return _ActionLayer.IsPlayingClip(characterActionData.AnimationClip);
        }

        public bool isPlayingAnimation(AnimationClip clip) {
            return _BaseLayer.IsPlayingClip(clip);
        }

        /// <summary>
        /// Fades out the action layer.
        /// </summary>
        /// <param name="fadeDuration">The duration of the fade-out.</param>
        public void FadeOutActionLayer(float fadeDuration = 0.2f) {
            _ActionLayer.StartFade(0, fadeDuration);
        }

        /// <summary>
        /// Returns true if the ActionLayer's weight is greater than 0.
        /// </summary>
        public bool IsActionLayerPlaying() {
            return _ActionLayer.Weight > 0;
        }

        /// <summary>
        /// Plays a blended locomotion animation using the locomotion mixer
        /// </summary>
        /// <param name="category">The animation category (should be Locomotion)</param>
        /// <param name="blendParameter">The blend parameter (0 = idle, 0.5 = walk, 1 = run)</param>
        /// <returns>The mixer's AnimancerState</returns>
      
        // --- Scalable Animation System ---
        

        

       

        
        

        



        public void ResetStrafeAnimation() {
           
            _lastStrafeClip = null;
        }
        
        public void resetCrouchAnimation() {
           
            _lastStrafeClip = null;
        }
        public void UpdateStrafeAnimation(Vector2 Directions) {
            
            var strafeType = _strafe_directionalAnimationSet.Snap(Directions);
            var clip       = _strafe_directionalAnimationSet.Get(strafeType);

            if (_lastStrafeClip != clip) {
                _lastStrafeClip = clip;
             
              
                PlayAnimation(clip,0.2f,FadeMode.FromStart);
            }
        }

        public void UpdateCrouchAnimation(Vector2 Directions) {
            var strafeType = _crouch_directionalAnimationSet.Snap(Directions);
            var clip       = _crouch_directionalAnimationSet.Get(strafeType);

            if (_lastStrafeClip != clip) {
                _lastStrafeClip = clip;

                PlayAnimation(clip, 0.15f, FadeMode.FixedDuration);
            }
        }
        
        public void Update() {
         
          
        }
        /// <summary>
        /// Plays an animation with a slow-in effect: the animation starts at a lower speed and ramps up to normal speed over a short duration.
        /// </summary>
        /// <param name="clip">The animation clip to play.</param>
        /// <param name="fade">The fade duration.</param>
        /// <param name="fadeMode">The fade mode.</param>
        /// <param name="slowInDuration">How long (in seconds) to ramp up the speed.</param>
        /// <param name="initialSpeed">The starting speed (e.g., 0.3f).</param>
        /// <returns>The AnimancerState.</returns>
        public AnimancerState PlayAnimationWithSlowIn(AnimationClip clip, float fade = 0.2f, FadeMode fadeMode = FadeMode.FixedSpeed, float slowInDuration = 0.2f, float initialSpeed = 0.3f)
        {
            var state = _BaseLayer.Play(clip, fade, fadeMode);
            state.Speed = initialSpeed;
            StartCoroutine(RampUpAnimancerSpeed(state, slowInDuration));
            return state;
        }

        private System.Collections.IEnumerator RampUpAnimancerSpeed(AnimancerState state, float duration)
        {
            float elapsed = 0f;
            float startSpeed = state.Speed;
            while (elapsed < duration && state != null && state.IsPlaying)
            {
                elapsed += Time.deltaTime;
                state.Speed = Mathf.Lerp(startSpeed, 1f, elapsed / duration);
                yield return null;
            }
            if (state != null) state.Speed = 1f;
        }
    }
}

