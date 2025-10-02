Utility AI Example

This folder contains a minimal, self-contained example showing how to use the Utility AI framework found in Assets/_Assets/Scripts/Utility ai.

How to try it quickly:
1) Create an empty GameObject in your scene called "Example Agent" and add the component ExampleSetup.
   - This script will automatically add AIContext and UtilityAIController to the same GameObject if they are not present.
   - It also creates two actions (MoveTowardsTarget and Eat) and two considerations (Hunger, Distance) at runtime and wires them up for you.
2) Create another GameObject as the target (e.g., a Cube) and assign it to the ExampleSetup's Target field.
3) Enter Play Mode. You should see:
   - If the agent is far from the target, it moves toward it.
   - When the agent gets close and is hungry, it will "Eat" (reducing its hunger).
   - Hunger slowly increases over time when not eating.

Notes:
- This example creates ScriptableObject instances at runtime for convenience. In production, you can create assets via the Create menu:
  Create -> Utility AI -> Consideration -> Hunger / Distance To Target
  Create -> Utility AI -> Action -> Utility Action
  ...or create your own concrete actions by inheriting from ActionBase.
- The UtilityAIController evaluates actions each Update and invokes the Execute method on the best-scoring one.
- You can add more considerations and actions by combining and weighting them.

Extending:
- Duplicate MoveTowardsTargetAction and customize its Execute logic (e.g., use NavMesh, animations, etc.).
- Add new considerations by inheriting from ConsiderationBase and overriding Score(AIContext).
- Replace runtime-created assets with reusable .asset files for designers to tweak.
