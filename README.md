# Chess Game - C# WPF Application

A fully-featured chess game built with C# and WPF, featuring a complete chess engine with FEN notation support, move validation, and a modern user interface.

## 🎯 Features

* **Complete Chess Engine**: Full implementation of chess rules and move validation
* **FEN Notation Support**: Import/export chess positions using standard FEN notation
* **Modern WPF UI**: Clean, responsive user interface with piece highlighting
* **Move Validation**: Legal move calculation and game state management
* **Game State Tracking**: Castling rights, en passant, move counters, and game over detection
* **Asset Management**: High-quality piece images and custom cursors
* **Keyboard Shortcuts**: Quick access to game controls and FEN display
* **Chess AI**: Opponent powered by heuristic search algorithms (e.g., Negamax with alpha-beta pruning, piece-square tables for evaluation)
* **Move History**: Tracks all moves with the ability to replay, go to previous or next moves
* **Game Modes & Timers**: Select different timers (1, 5, 30 minutes) for bullet, blitz, and regular modes
* **Board Themes**: Switch between multiple board and piece themes from the ESC menu

## 📷 Screenshots

![Chess Game Interface](ChessUI/Assets/screenshots/Game.png)
![Pause Menu Display](ChessUI/Assets/screenshots/PauseMenu.png)
![Pawn Promotion](ChessUI/Assets/screenshots/Promotion.png)
![Game Over State](ChessUI/Assets/screenshots/Win.png)
![AI vs Player](ChessUI/Assets/screenshots/AIversusPlayer.mp4)

## 🚀 Getting Started

### Prerequisites

* **.NET 6.0 SDK** or later
* **Windows 10/11** (WPF application)
* **Visual Studio 2022** or **Visual Studio Code** (optional, for development)

### Installation

1. **Clone the repository**:

   ```bash
   git clone <your-repository-url>
   cd ChessCSharpProject
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
├── Chess.sln
├── ChessLogic/
│   ├── Board.cs
│   ├── GameState.cs
│   ├── Player.cs
│   ├── Position.cs
│   ├── Pieces/
│   │   ├── Piece.cs
│   │   ├── King.cs
│   │   ├── Queen.cs
│   │   ├── Rook.cs
│   │   ├── Bishop.cs
│   │   ├── Knight.cs
│   │   └── Pawn.cs
│   ├── Moves/
│   │   ├── Move.cs
│   │   ├── NormalMove.cs
│   │   ├── Castle.cs
│   │   ├── EnPassant.cs
│   │   ├── PawnPromotion.cs
│   │   └── DoublePawn.cs
│   └── AI/
│       ├── ChessAI.cs
│       ├── Negamax.cs
│       ├── AlphaBeta.cs
│       └── PST.cs
│   └── MoveHistory.cs            # Tracks moves and allows replay/navigation
└── ChessUI/
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── ControlMenu.xaml
    ├── GameOverMenu.xaml
    ├── PromotionMenu.xaml
    ├── Assets/
    │   ├── Board.png
    │   ├── [Piece][Color].png
    │   ├── Cursor[Color].cur
    │   └── icon.ico
    └── Images.cs
```

## 🎮 How to Play

### Basic Controls

* **Mouse Click**: Select pieces and make moves
* **Escape Key**: Open pause menu (choose timer modes and board themes)
* **F Key**: Display current FEN notation
* **R Key**: Restart game (from pause menu)
* **Move History Navigation**: Replay moves using Prev/Next buttons

### Game Features

* **Piece Selection**: Click on a piece to see legal moves highlighted
* **Move Execution**: Click on a highlighted square to make a move
* **Pawn Promotion**: Automatically prompts for piece selection
* **Game Over Detection**: Automatically detects checkmate, stalemate, and draw conditions
* **FEN Export**: Press F to see the current position in FEN notation
* **AI Opponent**: Play against the computer with heuristic evaluation
* **Timer Modes**: Bullet (1 min), Blitz (5 min), Regular (30 min)
* **Board Themes**: Switch between different board appearances

## 🔧 Technical Details

### FEN Notation Support

The application fully supports the standard chess FEN (Forsyth-Edwards Notation) format:

```
rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
```

### Architecture

* **ChessLogic**: Pure C# library with no external dependencies
* **ChessUI**: WPF application using standard Windows controls
* **Separation of Concerns**: Engine logic completely separated from UI
* **Event-Driven**: Clean event handling for game state changes
* **Chess AI**: Modular design allows adding multiple AI algorithms
* **Move History**: Centralized management for replay and navigation

## 📦 Dependencies

* **Microsoft.NET.Sdk** (6.0)
* **Microsoft.NET.Sdk.WindowsDesktop** (for WPF support)
* **No external chess libraries**

## 🚀 Development

### Building from Source

```bash
dotnet restore
dotnet build
dotnet build --configuration Release
dotnet clean
```

### Running Tests

```bash
dotnet test
dotnet test --project ChessLogic.Tests
```

### Code Quality

* **C# 6.0+** features
* **Nullable reference types** enabled
* **Implicit usings**
* **XML documentation**

## 🐛 Troubleshooting

* Check SDK installation and version
* Ensure WPF compatibility
* Verify assets are present
* Use Debug mode for detailed logs

## 📝 License

Open source. See LICENSE file.

## 🤝 Contributing

* Fork → Feature branch → Changes → Tests → Pull request

## 📞 Support

* Open an issue
* Review code comments

## 🎯 Future Enhancements

* [ ] **Enhanced AI**: Multiple difficulty levels
* [ ] **Game History Enhancements**: Extended replay, analysis tools
* [ ] **Network Play**: Multiplayer support
* [ ] **Opening Database**: Built-in openings
* [ ] **Analysis Tools**: Move evaluation
* [ ] **Save/Load Games**: Persistent storage
* [ ] **Additional Board Themes**

---

**Enjoy playing chess! ♟️**
