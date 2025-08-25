# Chess Game - C# WPF Application

A fully-featured chess game built with C# and WPF, featuring a complete chess engine with FEN notation support, move validation, and a modern user interface.

## 🎯 Features

- **Complete Chess Engine**: Full implementation of chess rules and move validation
- **FEN Notation Support**: Import/export chess positions using standard FEN notation
- **Modern WPF UI**: Clean, responsive user interface with piece highlighting
- **Move Validation**: Legal move calculation and game state management
- **Game State Tracking**: Castling rights, en passant, move counters, and game over detection
- **Asset Management**: High-quality piece images and custom cursors
- **Keyboard Shortcuts**: Quick access to game controls and FEN display

## 📷 Screenshots

![Chess Game Interface](ChessUI/Assets/screenshots/Game.png)

### Move Highlighting

![Pause Menu Display](ChessUI/Assets/screenshots/PauseMenu.png)

### Promotion Dialog

![Pawn Promotion](ChessUI/Assets/screenshots/Promotion.png)

### Game Over Screen

![Game Over State](ChessUI/Assets/screenshots/Win.png)

## 🚀 Getting Started

### Prerequisites

- **.NET 6.0 SDK** or later
- **Windows 10/11** (WPF application)
- **Visual Studio 2022** or **Visual Studio Code** (optional, for development)

### Installation

1. **Clone the repository**:

   ```bash
   git clone <your-repository-url>
   cd ChessCsharpProject
   ```
2. **Build the solution**:

   ```bash
   dotnet build
   ```
3. **Run the application**:

   ```bash
   dotnet run --project ChessUI
   ```

## 🏗️ Project Structure

```
ChessCsharpProject/
├── Chess.sln                    # Solution file
├── ChessLogic/                  # Core chess engine library
│   ├── Board.cs                 # Chess board representation
│   ├── GameState.cs             # Game state management
│   ├── Player.cs                 # Player enumeration
│   ├── Postion.cs               # Position representation
│   ├── Pieces/                  # Chess piece implementations
│   │   ├── Piece.cs             # Abstract base class
│   │   ├── King.cs              # King piece logic
│   │   ├── Queen.cs             # Queen piece logic
│   │   ├── Rook.cs              # Rook piece logic
│   │   ├── Bishop.cs            # Bishop piece logic
│   │   ├── Knight.cs            # Knight piece logic
│   │   └── Pawn.cs              # Pawn piece logic
│   └── Moves/                   # Move type implementations
│       ├── Move.cs               # Abstract move class
│       ├── NormalMove.cs         # Standard piece moves
│       ├── Castle.cs             # Castling moves
│       ├── EnPassant.cs          # En passant captures
│       ├── PawnPromotion.cs      # Pawn promotion
│       └── DoublePawn.cs         # Pawn double moves
└── ChessUI/                     # WPF user interface
    ├── MainWindow.xaml          # Main game window
    ├── MainWindow.xaml.cs       # Main window logic
    ├── ControlMenu.xaml         # Pause/control menu
    ├── GameOverMenu.xaml        # Game over dialog
    ├── PromotionMenu.xaml       # Pawn promotion dialog
    ├── Assets/                  # Game assets
    │   ├── Board.png            # Chess board background
    │   ├── [Piece][Color].png   # Piece images
    │   ├── Cursor[Color].cur    # Custom cursors
    │   └── icon.ico             # Application icon
    └── Images.cs                # Asset management
```

## 🎮 How to Play

### Basic Controls

- **Mouse Click**: Select pieces and make moves
- **Escape Key**: Open pause menu
- **F Key**: Display current FEN notation
- **R Key**: Restart game (from pause menu)

### Game Features

- **Piece Selection**: Click on a piece to see legal moves highlighted
- **Move Execution**: Click on a highlighted square to make a move
- **Pawn Promotion**: Automatically prompts for piece selection
- **Game Over Detection**: Automatically detects checkmate, stalemate, and draw conditions
- **FEN Export**: Press F to see the current position in FEN notation

## 🔧 Technical Details

### FEN Notation Support

The application fully supports the standard chess FEN (Forsyth-Edwards Notation) format:

```
rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
```

**Components:**

1. **Placement**: Board piece positions
2. **StartPlayer**: Current player turn (w/b)
3. **Castling**: Available castling rights (KQkq)
4. **EnPassant**: En passant target square (- or square)
5. **HalfMoveClock**: Moves since last capture/pawn advance
6. **FullMoveNumber**: Complete move counter

### Architecture

- **ChessLogic**: Pure C# library with no external dependencies
- **ChessUI**: WPF application using standard Windows controls
- **Separation of Concerns**: Engine logic completely separated from UI
- **Event-Driven**: Clean event handling for game state changes

## 📦 Dependencies

### Required Packages

- **Microsoft.NET.Sdk** (6.0)
- **Microsoft.NET.Sdk.WindowsDesktop** (for WPF support)

### Target Frameworks

- **ChessLogic**: `net9.0`
- **ChessUI**: `net9.0-windows`

### External Dependencies

- **None** - All chess logic is implemented from scratch
- **WPF** - Built-in Windows Presentation Foundation

## 🚀 Development

### Building from Source

```bash
# Restore packages
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Clean build artifacts
dotnet clean
```

### Running Tests

```bash
# Run all tests (if test project exists)
dotnet test

# Run specific test project
dotnet test --project ChessLogic.Tests
```

### Code Quality

- **C# 6.0+** features used throughout
- **Nullable reference types** enabled
- **Implicit usings** enabled for cleaner code
- **XML documentation** for all public methods

## 🐛 Troubleshooting

### Common Issues

1. **Build Errors**:

   - Ensure .NET 6.0 SDK+ is installed
   - Run `dotnet --version` to verify installation
   - Try `dotnet clean` followed by `dotnet restore`
2. **Runtime Errors**:

   - Check Windows version compatibility
   - Ensure WPF is available on your system
   - Verify all asset files are present
3. **Performance Issues**:

   - Close other applications to free memory
   - Check Windows graphics drivers
   - Monitor system resources during gameplay

### Debug Mode

```bash
# Run with detailed logging
dotnet run --project ChessUI --configuration Debug

# Attach debugger
dotnet run --project ChessUI --configuration Debug --verbosity detailed
```

## 📝 License

This project is open source. Please check the LICENSE file for specific terms.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📞 Support

For issues, questions, or contributions:

- Create an issue in the repository
- Check existing documentation
- Review the code comments for implementation details

## 🎯 Future Enhancements

- [ ] **AI Opponent**: Computer player with different difficulty levels
- [ ] **Game History**: Move history and game replay
- [ ] **Network Play**: Multiplayer over network
- [ ] **Opening Database**: Built-in opening moves
- [ ] **Analysis Tools**: Move analysis and evaluation
- [ ] **Save/Load Games**: Persistent game storage
- [ ] **Themes**: Multiple board and piece themes

---

**Enjoy playing chess! ♟️**
