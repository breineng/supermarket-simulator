# Tech Context

_This document will detail the technologies used, development setup, and technical constraints._ 

## Key Libraries & Frameworks

### `com.sergeysychov.behaviour_inject` (BInject)
- **Type**: Dependency Injection (DI) Framework for Unity.
- **Version**: 1.0.8 (from package.json).
- **License**: MIT.
- **Compatibility**: Unity 2018.4+.
- **Purpose**: Provides a lightweight and performant way to manage dependencies in Unity projects. It supports injection into MonoBehaviour fields, properties, and methods, interface resolution, class autocomposition, factories, and event/command systems.
- **Key Features**:
    - `[Inject]` attribute for marking injection points.
    - `Context` class for registering and managing dependencies.
    - `Injector` MonoBehaviour for performing injections on a GameObject.
    - Support for multiple and hierarchical contexts.
    - Interface-based dependency resolution.
    - Autocomposition of objects with constructor injection.
    - `[Create]` attribute and `IInstantiator` for on-demand object creation.
- **Source**: Likely integrated as a Unity package (UPM).
- **Documentation**: `Packages/com.sergeysychov.behaviour_inject/README.md` and `https://github.com/sergeysychov/behaviour_inject#readme` 