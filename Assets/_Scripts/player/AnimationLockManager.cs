using System;
using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

public class AnimationLockManager
{
    private Character character;
    private bool isLocked;
    private AnimationLockType currentLockType;
    private Coroutine unlockCoroutine;

    public enum AnimationLockType
    {
        None,
        HardLanding,      // High fall landing
        Bandaging,        // Healing/medical
        VehicleEnter,     // Getting into vehicle
        VehicleExit,      // Getting out of vehicle
        Interaction,      // Using objects
        Custom            // For other use cases
    }

    public bool IsLocked => isLocked;
    public AnimationLockType CurrentLockType => currentLockType;

    public AnimationLockManager(Character character)
    {
        this.character = character;
    }

    /// <summary>
    /// Locks player control for the duration of an animation
    /// </summary>
    public void LockForAnimation(AnimancerState animState, AnimationLockType lockType, Action onUnlock = null)
    {
        if (isLocked && currentLockType != AnimationLockType.None)
        {
            // Already locked - decide if new lock should override
            if (ShouldOverrideLock(lockType))
            {
                ForceUnlock();
            }
            else
            {
                Debug.LogWarning($"Animation lock rejected: Already locked with {currentLockType}");
                return;
            }
        }

        isLocked = true;
        currentLockType = lockType;
        character.isFrozen = true;

        // Clear any existing unlock coroutine
        if (unlockCoroutine != null)
        {
            character.StopCoroutine(unlockCoroutine);
        }

        // Listen to animation end event
        if (animState != null)
        {
            animState.Events(character).OnEnd = () =>
            {
                Unlock();
                onUnlock?.Invoke();
            };
        }
    }

    /// <summary>
    /// Locks player control for a specific duration
    /// </summary>
    public void LockForDuration(float duration, AnimationLockType lockType, Action onUnlock = null)
    {
        if (isLocked && !ShouldOverrideLock(lockType))
        {
            Debug.LogWarning($"Animation lock rejected: Already locked with {currentLockType}");
            return;
        }

        Debug.Log($" playing lock for {duration} seconds");
        isLocked = true;
        currentLockType = lockType;
        character.isFrozen = true;

        if (unlockCoroutine != null)
        {
            character.StopCoroutine(unlockCoroutine);
        }

        unlockCoroutine = character.StartCoroutine(UnlockAfterDelay(duration, onUnlock));
    }

    private IEnumerator UnlockAfterDelay(float delay, Action onUnlock)
    {
        yield return new WaitForSeconds(delay);
        Unlock();
        onUnlock?.Invoke();
    }

    /// <summary>
    /// Unlocks player control
    /// </summary>
    public void Unlock()
    {
        if (!isLocked) return;

        isLocked = false;
        currentLockType = AnimationLockType.None;
        character.isFrozen = false;

        if (unlockCoroutine != null)
        {
            character.StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }
    }

    /// <summary>
    /// Force unlock regardless of current state
    /// </summary>
    public void ForceUnlock()
    {
        isLocked = false;
        currentLockType = AnimationLockType.None;
        character.isFrozen = false;

        if (unlockCoroutine != null)
        {
            character.StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }
    }

    /// <summary>
    /// Determines if a new lock should override the current one
    /// </summary>
    private bool ShouldOverrideLock(AnimationLockType newLockType)
    {
        // Define priority hierarchy (higher number = higher priority)
        int GetPriority(AnimationLockType type)
        {
            return type switch
            {
                AnimationLockType.HardLanding => 5,
                AnimationLockType.VehicleEnter => 4,
                AnimationLockType.VehicleExit => 4,
                AnimationLockType.Bandaging => 3,
                AnimationLockType.Interaction => 2,
                AnimationLockType.Custom => 1,
                _ => 0
            };
        }

        return GetPriority(newLockType) > GetPriority(currentLockType);
    }

    /// <summary>
    /// Check if player input should be blocked
    /// </summary>
    public bool ShouldBlockInput()
    {
        return isLocked;
    }
}
