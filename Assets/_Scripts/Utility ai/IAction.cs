using UnityEngine;

namespace UtilityAI
{
    /// <summary>
    /// An action that an AI agent can perform. Returns a score in [0,1].
    /// </summary>
    public interface IAction
    {
        string Name { get; }
        float Score { get; }
        bool IsValid(AIContext context);
        float CalculateScore(AIContext context);
        void Execute(AIContext context);
    }
}
