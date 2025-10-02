namespace UtilityAI
{
    /// <summary>
    /// A consideration returns a normalized utility score in [0,1].
    /// </summary>
    public interface IConsideration
    {
        string Name { get; }
        float Evaluate(AIContext context);
        float Weight { get; }
    }
}
