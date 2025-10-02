using System.Collections.Generic;
using UnityEngine;
using UtilityAI;

[CreateAssetMenu(menuName = "Utility AI/Action/Idle action", fileName = "IdleAction")]
public class IdleAction : ActionBase
{
    [Header("Idle Bounce Settings")] public float amplitude = 0.25f; // How high to bounce (meters)
    public float frequency = 1.5f; // How fast to bounce (cycles per second)
    public bool useLocalPosition = false; // Bounce in local or world space

    // Cache each agent's base Y so we don't drift over time.
    [System.NonSerialized] private readonly Dictionary<Transform, float> _baseY = new Dictionary<Transform, float>();

    public override bool IsValid(AIContext context)
    {
        return context != null && context.self != null;
    }

    public override void Execute(AIContext context) {
        if (context == null || context.self == null) return;

        var t = context.self;
        // Ensure base Y is stored per agent
        if (!_baseY.ContainsKey(t))
        {
            float startY = useLocalPosition ? t.localPosition.y : t.position.y;
            _baseY[t] = startY;
        }

        float baseY = _baseY[t];
        float time = Time.time;
        float offset = Mathf.Sin(time * Mathf.PI * 2f * Mathf.Max(0f, frequency)) * Mathf.Max(0f, amplitude);

        if (useLocalPosition)
        {
            Vector3 lp = t.localPosition;
            lp.y = baseY + offset;
            t.localPosition = lp;
        }
        else
        {
            Vector3 p = t.position;
            p.y = baseY + offset;
            t.position = p;
        }
    }
}
