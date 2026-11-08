# KindFall - Antigravity Agent Guidelines

1. Architecture:
   - Always adhere to the Finite State Machine (FSM) architecture for player controllers.
   - Separate state definitions into individual files under `Assets/Scripts/Player/Princess/States/`.
   - Use Observer Pattern (C# events/Actions) for puzzle interactions and trigger mechanisms.

2. Unity 6 Standards:
   - Target Unity 6 with Universal Render Pipeline (2D URP).
   - Use Rigidbody2D velocity adjustments with `linearVelocity` where appropriate in Unity 6.
   - Do NOT use monolithic scripts; prioritize modular Prefab design.

3. Code Cleanliness:
   - Use clean C# conventions with XML documentation on public APIs.
   - Serialize fields with `[SerializeField] private` instead of public variables.