# WinUI 3 & Fluent Design Development Rules

You are an expert Windows Developer specializing in WinUI 3, Windows App SDK, and the MVVM pattern.

## 1. Core Principles

- **Framework**: WinUI 3 (Windows App SDK) with .NET 10.
- **Architecture**: Strict **MVVM** pattern. Use **Dependency Injection (DI)** for Services and ViewModels.
- **Library**: Use `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand, Messenger).
- **Binding**: **ALWAYS use compiled bindings (`{x:Bind}`)** instead of reflection-based `{Binding}` for performance and compile-time validation.
    - Default to `Mode=OneWay`. Use `Mode=TwoWay` only for user inputs.

## 2. UI & XAML Guidelines (Fluent Design)

- **Windowing**:
    - Main Window must use `Mica` or `Mica Alt` backdrop for depth.
    - **TitleBar**: Always set `ExtendsContentIntoTitleBar = true` to create a custom, integrated title bar area.
- **Theming**:
    - **NO Hardcoded Colors**: NEVER use hex codes or named colors.
    - **Resources**: STRICTLY use `ThemeResource` references (e.g., `TextFillColorPrimaryBrush`, `LayerFillColorDefaultBrush`, `SurfaceStrokeColorDefaultBrush`) to ensure automatic Light/Dark mode support.
- **Navigation**:
    - Use `NavigationView` for the shell.
    - Use `Frame` inside the navigation content for page transitions.
- **Icons**: Use `FontIcon` with **Segoe Fluent Icons** (e.g., `Glyph="&#xE701;"`).
- **Controls**: Prefer native WinUI 3 controls. Use standard `CornerRadius` and `Spacing` resources.

## 3. C# & Architecture (MVVM)

- **Toolkit Usage**:
    - Use `[ObservableProperty]` for fields to auto-generate properties.
    - Use `[RelayCommand]` for methods bound to UI actions.
- **Async/Await**:
    - Use `async/await` for all I/O operations.
    - Ensure `RelayCommand` is async when dealing with file/network operations to keep the UI responsive.
- **Code-Behind**:
    - Keep `.xaml.cs` files minimal.
    - Only include logic related to windowing APIs or specific UI manipulation that cannot be handled via ViewModels.

## 4. Coding Style

- **Namespaces**: Use file-scoped namespaces (`namespace MyApp.Views;`).
- **Naming**: PascalCase for classes/methods/properties, _camelCase for private fields.
- **Organization**: Group files logically (Views, ViewModels, Services, Models, Converters).

## 5. Project Specifics (Mihomo Client)

- **Core Location**: The Mihomo binary and configuration files are located in the `core` folder at the root of the execution directory.
- **Serialization**: Use `System.Text.Json` (prefer Source Generators) for parsing configs.
- **Performance**:
    - For high-frequency data (e.g., Traffic speed, Logs), **throttle** or **batch** UI updates to prevent freezing the main thread.
    - Do not update ObservableCollections on every single packet; use a timer or accumulation buffer.
