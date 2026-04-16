# Copilot Instructions for RebelGame

## Project Overview
RebelGame is a browser-based strategy game built with **Blazor WebAssembly** using C#. Players earn money and energy, with the initial feature being solar stations that generate income. The game runs entirely in the browser using WebAssembly, with persistent storage via browser local storage.

**Tech Stack:**
- Frontend: Blazor WebAssembly (C# running in browser via WASM)
- Styling: CSS with responsive design for eventual mobile/desktop ports
- Storage: Browser LocalStorage for game state persistence
- Target: Web browser initially, designed for easy migration to mobile/desktop (Maui)

---

## Build, Test, and Lint Commands

### Development
```bash
cd RebelGame
dotnet watch run
```
Starts the dev server with hot reload on `https://localhost:5001`

### Build
```bash
dotnet build
```
Compiles the project to verify no errors.

### Build for Production
```bash
dotnet publish -c Release
```
Creates optimized WebAssembly output in `bin/Release/net8.0/publish/wwwroot/`. Deploy the `wwwroot` folder to any static host.

### Run Tests (when added)
```bash
dotnet test
```

### Format Code
```bash
dotnet format
```
Auto-formats C# code following .NET conventions.

---

## High-Level Architecture

### Game State Management
- **GameState** (singleton service): Central state container for player resources (money, energy), buildings, and progression
- **Persistence Layer**: Serializes GameState to LocalStorage on changes, loads on startup
- **Change Notifications**: GameState uses events to notify UI components when state updates (prevents unnecessary re-renders)

### Component Structure
- **Pages**: Top-level routable components (`Home.razor`, gameplay screens)
- **Components**: Reusable UI pieces (building buttons, resource displays, timers)
- **Services**: Business logic (GameService, StorageService, BuildingService)
- **Models**: Data classes (Building, Resource, Player)

### Game Loop
The game uses a tick-based system:
1. Timer fires every 100ms (configurable)
2. GameService updates all building production
3. GameState notifies subscribers of changes
4. UI components re-render only if their bound data changed
5. State auto-saves to LocalStorage

### Building System
Each building (e.g., SolarStation) has:
- Production rate (money/energy per tick)
- Cost to build
- Upgrade path
- Visual representation

Buildings are stored in a list in GameState; the UI dynamically renders building cards based on this list.

---

## Key Conventions

### File Organization
- **Pages/**: Routable .razor components (automatically handle routing via `@page` directive)
- **Components/**: Reusable UI components (buttons, panels, resource displays)
- **Services/**: Stateless or singleton services for business logic
- **Models/**: Plain C# classes for data (buildings, resources, player state)
- **Styles/**: CSS files (one per component or page, same name as .razor file with `.css` extension)

### Naming Conventions
- **Components**: PascalCase (e.g., `BuildingCard.razor`, `ResourceDisplay.razor`)
- **Services**: PascalCase ending in `Service` (e.g., `GameService`, `StorageService`)
- **Pages**: PascalCase describing the page (e.g., `GameBoard.razor`, `Settings.razor`)
- **CSS Classes**: lowercase with hyphens (e.g., `.building-card`, `.resource-counter`)

### Razor Conventions
- Use `@code { }` block for component logic
- Use `@inject` for service injection
- Keep component markup clean; move complex logic to methods in `@code` block
- Event handlers: prefix with `Handle` (e.g., `HandleBuildClick`)
- Use `@key` for list rendering to improve performance with dynamic lists

### Game Data Serialization
- All game state must be JSON-serializable (use System.Text.Json)
- Avoid circular references; use IDs to link related objects
- Time-based production: store only the last tick timestamp, calculate pending production when needed

### Component Communication
- Parent → Child: Use `@parameters`
- Child → Parent: Use event callbacks (`EventCallback<T>`)
- Sibling/Cross-component: Use `GameService` events or a message bus
- Avoid direct component-to-component references

### Production Calculations
- Always calculate production based on delta time (time elapsed since last update), not tick count
- This allows the game to remain balanced even if the browser tab is inactive
- Store `lastUpdateTick` timestamp, calculate: `production = rate * (now - lastUpdateTick) / 1000`

### LocalStorage Keys
- Use prefixed, hierarchical keys: `game_state_*`, `game_buildings_*`, `game_player_*`
- Always version your storage format (e.g., `game_state_v1`) to handle future migrations

### Error Handling
- Wrap async operations in try-catch blocks
- Log errors to browser console in dev; could add error tracking later
- Always provide user-friendly error messages in the UI (e.g., "Failed to save game")

### Performance Notes
- Use `ShouldRender()` in components to prevent unnecessary re-renders
- Bind only to values that actually change (avoid binding to objects that update frequently)
- Debounce LocalStorage writes if saving happens very frequently (combine multiple changes into one save)

---

## Future Considerations for Mobile/Desktop Port

When migrating to .NET Maui (for mobile/desktop):
- Keep all game logic in shared C# classes
- Only UI layers should change (.razor → Maui views)
- LocalStorage → SecureStorage or database
- Browser APIs → Maui equivalents
- Current responsive CSS will help mobile UX design

---

## Testing Strategy

**Unit Tests** (when added):
- Test GameService calculation logic
- Test Building production rates
- Test state serialization/deserialization

**Integration Tests** (when added):
- Test game state changes from user actions
- Test persistence (save/load cycle)

**Manual Testing Areas** (until automated):
- Build building, verify it generates money/energy
- Close and reopen game, verify state persists
- Play for extended time, verify no memory leaks
- Test on mobile browsers for responsive UX
