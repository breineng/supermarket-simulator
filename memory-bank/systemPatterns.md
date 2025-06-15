# System Patterns

_This document will outline the system architecture, key technical decisions, and design patterns._

## `com.sergeysychov.behaviour_inject` (BInject) - Key System Patterns

The `BInject` package employs several key patterns and architectural decisions:

1.  **Core DI Container (`Context` class):**
    *   Manages dependency registration (instances, types for autocomposition, factories).
    *   Handles dependency resolution, supporting hierarchical lookups (parent contexts).
    *   Performs autocomposition of types, including constructor selection (prefers `[Inject]` attribute, then fewest parameters) and circular dependency detection.
    *   Integrates an event system and a command system.
    *   Self-registers `IEventDispatcher` and `IInstantiator` services.

2.  **Reflection with Caching (`ReflectionCache`):**
    *   Essential for performance. Scans types for members marked with `[Inject]`, `[Create]`, and `[InjectEvent]` attributes.
    *   Caches information about injectable members (fields, properties, methods) and event handlers to avoid repeated reflection.
    *   Respects excluded namespaces defined in `Settings` to reduce scope.

3.  **MonoBehaviour Integration:**
    *   `Injector` (MonoBehaviour): The primary component placed on GameObjects to trigger injection. It finds a `Context` (named or via hierarchy) and injects dependencies into other MonoBehaviours on the same GameObject.
    *   `HierarchyContext` (MonoBehaviour): Allows a GameObject to be associated with a specific named `Context`, enabling `Injector`s to find contexts by traversing the scene hierarchy.

4.  **Service Abstraction and Resolution Strategies:**
    *   `IDependency` Interface: Implemented by classes like `SingleDependency`, `SingleAutocomposeDependency`, `FactoryDependency`, defining different strategies for how dependencies are stored, scoped (singleton/transient implied by factory), and resolved.
    *   `IMemberInjection` Interface: Implemented by `FieldInjection`, `PropertyInjection`, `MethodInjection` to handle the specifics of injecting into different member types.
    *   `IEventHandler` Interface: Implemented by `MethodEventHandler`, `DelegateEventHandler` for different event handling strategies.

5.  **Configuration via ScriptableObject (`Settings`):**
    *   Global settings (predefined context names, namespaces to exclude from reflection) are managed via a `ScriptableObject` asset (`BInjectSettings.asset`), making configuration Unity-editor friendly.
    *   An installer script (`BInjectInstaller`) ensures this asset is automatically created from a template if missing.

6.  **Event System:**
    *   Each `Context` has an `EventManager`.
    *   Events are plain C# objects.
    *   `Injector` components act as `EventTransmitter`s, forwarding events from their `Context` to relevant handlers on the MonoBehaviours they manage.
    *   Dependencies registered in a `Context` can also directly receive events if they have methods/delegates marked with `[InjectEvent]`.
    *   Supports event type inheritance for handlers (via `InjectEventAttribute.Inherit`).

7.  **Command Pattern Integration:**
    *   `ICommand` interface with an `Execute()` method.
    *   Commands can be registered to execute in response to specific event types.
    *   Commands themselves are autocomposed by the `Context` and can also receive the triggering event via `[InjectEvent]` if they have handlers.

8.  **Lifecycle Management:**
    *   `Context.Destroy()`: Disposes `IDisposable` dependencies, unregisters from global registry (if applicable), and raises `OnContextDestroyed`.
    *   `Injector` and `HierarchyContext` GameObjects are destroyed if their associated `Context` is destroyed.
    *   Dependencies registered as `MonoBehaviour`s can have their event reception suppressed by their `Injector` if they are also direct dependencies of the context, to avoid double event delivery.

9.  **Global vs. Local Contexts:**
    *   Contexts can be global (registered in `ContextRegistry` by name) or local (standalone, not globally discoverable by name).

10. **Custom Editor Tooling:**
    *   Custom inspectors for `Injector` and `HierarchyContext` simplify their configuration in the Unity Editor (e.g., selecting named contexts from a dropdown). 