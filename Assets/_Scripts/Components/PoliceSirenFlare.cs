using UnityEngine;

public class PoliceSirenFlare : MonoBehaviour {
    public LensFlare redFlare;
    public LensFlare blueFlare;
    public Transform mainCamera;
    public float     flashSpeed    = 2.0f;
    public float     maxDistance   = 100f;
    public float     maxBrightness = 1f;
    public bool      isLightOn     = true;

    private float timer = 0f;
    private float maxDistanceSqr;

    void Start() {
        maxDistanceSqr = maxDistance * maxDistance;

        if (redFlare)
            redFlare.brightness = 0f;
        if (blueFlare)
            blueFlare.brightness = 0f;
    }

    void Update() {
        if (!isLightOn) return;

        timer += Time.deltaTime * flashSpeed;

        // Calculate distance attenuation
        float distanceSqr    = (transform.position - mainCamera.position).sqrMagnitude;
        float distanceFactor = Mathf.Clamp01(1f - (distanceSqr / maxDistanceSqr));

        // Current maximum possible brightness based on distance
        float currentMaxBrightness = maxBrightness * distanceFactor;

        // Alternate between red and blue flares with reduced max brightness
        float redPhase  = Mathf.PingPong(timer, 1.0f) * currentMaxBrightness;
        float bluePhase = Mathf.PingPong(timer + 0.5f, 1.0f) * currentMaxBrightness;

        // Apply brightness
        if (redFlare)
            redFlare.brightness = redPhase;
        if (blueFlare)
            blueFlare.brightness = bluePhase;
    }
}