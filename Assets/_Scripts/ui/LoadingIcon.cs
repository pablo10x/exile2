using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class LoadingIcon : MonoBehaviour {

    public DOTweenAnimation [ ] animations;

    private void Start ()
    {
        
    }


    [Button("Enable Animations")]
    public void EnableAnimation ()
    {
        foreach ( var anim in animations ) {
            anim.DOPlay ();
        }
    }

    [Button("Disable Animations")]
    public void DisableAnimation ()
    {
        foreach ( var anim in animations ) {
            anim.DOKill ();
        }
    }
    
    
    #if UNITY_EDITOR
    public void OnValidate ()
    {
        if (animations is null ||  animations.Length <= 0) {
            animations = gameObject.GetComponentsInChildren <DOTweenAnimation> ();
        }
    }

    #endif
}
