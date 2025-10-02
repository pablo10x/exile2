# Utility AI — Examples

This folder contains small, focused examples that demonstrate how to use the Utility AI system and the optional FSM integration.

Contents:
- Utility AI Example/
  - ExampleSetup.cs — Wires a controller at runtime with two actions (MoveTowardsTarget and Eat).
  - MoveTowardsTargetAction.cs — Moves the agent toward a target with a stop distance.
  - EatAction.cs — Reduces hunger when close enough.
- FSM/
  - IdleStateSO.cs — Minimal state that logs and idles.
  - EatingStateSO.cs — Minimal state that logs when entering and during eating.
- ExampleSetup_FSM.cs — Shows how to add an AIStateMachine and drive states from actions via ActionBase.stateOnExecute.

See full guide: Assets/_Scripts/Utility ai/Documentation/UtilityAI_Documentation.html
