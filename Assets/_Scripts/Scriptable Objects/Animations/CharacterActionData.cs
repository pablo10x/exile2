using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "CNR/PlayerAction", fileName = "PlayerAction", order = 0)] public class CharacterActionData : ScriptableObject
{
    [FoldoutGroup("Action animation")] public AnimationClip AnimationClip;
    [FoldoutGroup("Action animation")] public AvatarMask    AvatarMask;
    [FoldoutGroup("Action animation")] public float         LayerFadeOut = 0.2f;
    [FoldoutGroup("Action animation")] public bool          forcewalk;
}