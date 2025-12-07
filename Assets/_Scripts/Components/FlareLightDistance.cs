using UniRx;
using UnityEngine;

public class FlareLightDistance : MonoBehaviour {
    public LensFlare lensFlare;
    public Transform mainCamera;
    public float     maxDistance    = 100f;
    public float     maxBrightness  = 1f;
    public float     updateInterval = 0.2f;
    public bool      isLightOn      = true;

    private float maxDistanceSqr;

    private void Start() {
        maxDistanceSqr = maxDistance * maxDistance;

        Observable.Interval(System.TimeSpan.FromSeconds(updateInterval))
                  .Where(_ => isLightOn)
                  .Subscribe(_ => UpdateBrightness())
                  .AddTo(this);
    }

    private void UpdateBrightness() {
        float distanceSqr = (transform.position - mainCamera.position).sqrMagnitude;
        float factor      = 1f - Mathf.Clamp01(distanceSqr / maxDistanceSqr);
        lensFlare.brightness = maxBrightness * factor;
    }
}