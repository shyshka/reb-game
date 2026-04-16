# RebelGame

A browser-based strategy game built with Blazor WebAssembly. Earn money and energy by building and managing solar stations and other structures.

## Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- Modern web browser (Chrome, Firefox, Safari, Edge)

### Running Locally
```bash
cd RebelGame
dotnet watch run
```

Open `https://localhost:5001` in your browser.

## Project Structure
```
RebelGame/
├── Pages/           # Routable game screens
├── Components/      # Reusable UI components
├── Services/        # Game logic and state management
├── Models/          # Data classes
├── wwwroot/         # Static assets (HTML, CSS, images)
└── RebelGame.csproj # Project configuration
```

## Features
- **Solar Stations**: Build and upgrade to generate passive income
- **Persistent Progress**: Game state saves automatically to browser storage
- **Offline Play**: Continues to generate resources even when the tab is closed
- **Mobile-Ready**: Responsive design for future mobile/desktop ports

## Development
For detailed development guidelines, see [.github/copilot-instructions.md](.github/copilot-instructions.md)

### Common Commands
- **Development**: `dotnet watch run` (hot reload enabled)
- **Build**: `dotnet build`
- **Production Build**: `dotnet publish -c Release`
- **Format Code**: `dotnet format`

## Future Plans
- Additional building types and progression
- Multiplayer leaderboards
- Mobile app (via .NET Maui)
- Desktop app (via .NET Maui)

## License
MIT
