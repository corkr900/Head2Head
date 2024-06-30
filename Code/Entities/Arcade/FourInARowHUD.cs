using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities.Arcade {
	public class FourInARowHUD : ArcadeHUD {

		private enum GameState {
			ChoosingDifficulty = 0,
			MyTurn = 1,
			OpponentTurn = 2,
			WinP1 = 3,
			WinP2 = 4,
			Tie = 5,
		}

		private enum Difficulty {
			Easy = 0,
			Normal = 1,
			Hard = 2,
		}

		private enum CellState {
			Empty = 0,
			P1,
			P2,
		}

		private struct GameEndState {
			public CellState Winner;
			public int XStart;
			public int YStart;
			public int XEnd;
			public int YEnd;
		}

		private static readonly int BoardWidth = 7;
		private static readonly int BoardHeight = 6;
		private static readonly int NeededForWin = 4;
		private static readonly float CircleSize = 60;
		private static readonly float CellSize = 90;
		private static readonly float CircleThickness = 5f;
		private static readonly float LineThickness = 6f;
		private static Color ColorBG = Calc.HexToColor("8D856D");
		private static Color ColorEmpty = Calc.HexToColor("666666");
		private static Color ColorP1 = Calc.HexToColor("2222cc");
		private static Color ColorP2 = Calc.HexToColor("aa2222");

		private GameState gameState = GameState.ChoosingDifficulty;
		private int hoveredColumn = 3;
		private List<GameEndState> endStates = new List<GameEndState>();
		private AI opponentAI = null;
		private BoardState boardState = new BoardState(BoardWidth, BoardHeight);

		public FourInARowHUD() {
			Tag = Tags.HUD;
			hoveredColumn = 1;
		}

		public override void Update() {
			base.Update();

			if (Input.MenuCancel.Pressed) {
				CloseThis();
			}
			else if (gameState == GameState.ChoosingDifficulty) {
				if (Input.MenuUp.Pressed && hoveredColumn > 0) {
					hoveredColumn--;
				}
				else if (Input.MenuDown.Pressed && hoveredColumn < 2) {
					hoveredColumn++;
				}
				if (Input.MenuConfirm.Pressed) {
					ChooseDifficulty(new Difficulty[] {
						Difficulty.Easy,
						Difficulty.Normal,
						Difficulty.Hard,
					}[hoveredColumn], Scene);
				}
			}
			else if (gameState == GameState.MyTurn) {
				if (Input.MenuConfirm.Pressed) {
					MakeMove(hoveredColumn, CellState.P1, GameState.OpponentTurn);
				}
				else if (Input.MenuLeft.Pressed && hoveredColumn > 0) {
					hoveredColumn--;
				}
				else if (Input.MenuRight.Pressed && hoveredColumn < BoardWidth - 1) {
					hoveredColumn++;
				}
			}
			else if (gameState == GameState.OpponentTurn) {
				int? res = opponentAI?.TryGetResult();
				if (res != null) {
					MakeMove(res.Value, CellState.P2, GameState.MyTurn);
				}
			}
			else {
				if (Input.MenuConfirm.Pressed) {
					gameState = GameState.ChoosingDifficulty;
					hoveredColumn = 1;
				}
			}
		}

		private void CloseThis() {
			opponentAI?.RemoveSelf();
			Close();
		}

		private void MakeMove(int col, CellState player, GameState nextState) {
			if (boardState == null) return;
			if (!boardState.DropPiece(col, player)) {
				Audio.Play("event:/classic/sfx5");
				return;
			}
			Audio.Play("event:/classic/sfx2");
			List<GameEndState> wins = boardState.GetWins();
			if (wins.Count == 0) {
				if (boardState.IsFull) {
					gameState = GameState.Tie;
					opponentAI?.RemoveSelf();
					opponentAI = null;
					endStates = wins;
				}
				else {
					gameState = nextState;
					if (gameState == GameState.OpponentTurn) {
						InitiateOpponentTurn();
					}
				}
			}
			else {
				gameState = CellToEndingGameState(wins[0].Winner);
				if (gameState == GameState.WinP1) Audio.Play("event:/classic/sfx55");
				else Audio.Play("event:/classic/sfx37");
				opponentAI?.RemoveSelf();
				opponentAI = null;
				endStates = wins;
			}
		}

		public override void Render() {
			base.Render();

			Draw.Rect(Vector2.Zero, 1920, 1080, Color.Black * 0.5f);

			Vector2 center = new Vector2(1920, 1080) * 0.5f;
			Vector2 rendSize = new Vector2(BoardWidth, BoardHeight) * CellSize;
			Vector2 topleft = center - 0.5f * rendSize;
			Draw.Rect(topleft, rendSize.X, rendSize.Y, ColorBG);
			for (int x = 0; x < BoardWidth; x++) {
				for (int y = 0; y < BoardHeight; y++) {
					Vector2 position = topleft + CellCenterOffset(x, y);
					Color color = PlayerColor(boardState[x, y]);
					Draw.Circle(position, CircleSize * 0.5f, color, 5f, 4);
				}
			}

			if (gameState == GameState.ChoosingDifficulty) {
				Draw.Rect(center.X - rendSize.X * 0.3f, topleft.Y + 60f + 100f * hoveredColumn, rendSize.X * 0.6f, 80f, ColorP1);
				ActiveFont.DrawOutline(Dialog.Clean("Head2Head_Minigame_Difficulty_Easy"), new Vector2(center.X, topleft.Y + 50f),
					new Vector2(0.5f, 0f), Vector2.One * 1.5f, Color.White, 2f, Color.Black);
				ActiveFont.DrawOutline(Dialog.Clean("Head2Head_Minigame_Difficulty_Normal"), new Vector2(center.X, topleft.Y + 150f),
					new Vector2(0.5f, 0f), Vector2.One * 1.5f, Color.White, 2f, Color.Black);
				ActiveFont.DrawOutline(Dialog.Clean("Head2Head_Minigame_Difficulty_Hard"), new Vector2(center.X, topleft.Y + 250f),
					new Vector2(0.5f, 0f), Vector2.One * 1.5f, Color.White, 2f, Color.Black);
			}

			if (gameState == GameState.MyTurn) {
				Vector2 position = topleft + new Vector2(0.5f + hoveredColumn, -1) * CellSize;
				Color color = ColorP1;
				Draw.Circle(position, CircleSize * 0.5f, color, CircleThickness, 4);
			}

			if (gameState == GameState.OpponentTurn) {
				Vector2 progressBarSize = new Vector2(CellSize * 4f, CellSize);
				Vector2 position = center + new Vector2((rendSize.X + progressBarSize.X) * 0.5f + 30f, 0);
				Color color = ColorP2;
				Vector2 progressBarPosition = position + new Vector2(-progressBarSize.X * 0.5f, progressBarSize.Y + CircleSize);

				Draw.Circle(position, CircleSize * 0.5f, color, CircleThickness, 4);
				Draw.Rect(progressBarPosition, progressBarSize.X, progressBarSize.Y, Color.LightGray);
				Draw.Rect(progressBarPosition, progressBarSize.X * opponentAI.ThinkingProgress, progressBarSize.Y, Color.DarkGreen);
			}

			if (gameState == GameState.WinP1 || gameState == GameState.WinP2 || gameState == GameState.Tie) {
				foreach (GameEndState winState in endStates) {
					Vector2 start = topleft + CellCenterOffset(winState.XStart, winState.YStart);
					Vector2 end = topleft + CellCenterOffset(winState.XEnd, winState.YEnd);
					Draw.Line(start, end, PlayerColor(winState.Winner), LineThickness);
				}

				string text = gameState switch {
					GameState.WinP1 => Dialog.Clean("Head2Head_Minigame_Result_Win"),
					GameState.WinP2 => Dialog.Clean("Head2Head_Minigame_Result_Lose"),
					_ => Dialog.Clean("Head2Head_Minigame_Result_Tie"),
				};
				ActiveFont.DrawOutline(text, new Vector2(center.X, topleft.Y), new Vector2(0.5f, 1f), Vector2.One * 3f, Color.White, 3f, Color.Black);
				ActiveFont.DrawOutline(Dialog.Clean("Head2Head_Minigame_PlayAgain"), new Vector2(center.X, topleft.Y + 250f),
					new Vector2(0.5f, 0f), Vector2.One * 1.5f, Color.White, 2f, Color.Black);
			}
		}

		public override void Added(Scene scene) {
			base.Added(scene);
		}

		private void ChooseDifficulty(Difficulty diff, Scene scene) {
			opponentAI = diff switch {
				Difficulty.Easy => new AI_Easy(CellState.P2),
				Difficulty.Normal => new AI_Normal(CellState.P2),
				Difficulty.Hard => new AI_Hard(CellState.P2),
				_ => new AI_Easy(CellState.P2),
			};
			(scene ?? Scene).Add(opponentAI);
			hoveredColumn = 3;
			boardState = new BoardState(BoardWidth, BoardHeight);
			gameState = GameState.MyTurn;
		}

		private bool IsGameInProgress(GameState st) {
			return st == GameState.MyTurn || st == GameState.OpponentTurn;
		}

		private Color PlayerColor(CellState st) {
			return st switch {
				CellState.P1 => ColorP1,
				CellState.P2 => ColorP2,
				_ => ColorEmpty
			};
		}

		private GameState CellToEndingGameState(CellState st) {
			return st switch {
				CellState.P1 => GameState.WinP1,
				CellState.P2 => GameState.WinP2,
				_ => GameState.Tie
			};
		}

		private Vector2 CellCenterOffset(int x, int y) {
			return new Vector2(0.5f + x, BoardHeight - 0.5f - y) * CellSize;
		}

		private void InitiateOpponentTurn() {
			if (opponentAI == null) {
				gameState = GameState.MyTurn;
				return;
			}
			opponentAI.Start(boardState);
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			CloseThis();
		}

		private class BoardState {
			public int Width { get; private set; }
			public int Height { get; private set; }
			public CellState[,] Board { get; private set; }

			public bool IsFull {
				get {
					for (int i = 0; i < Width; i++) {
						if (Board[i, Height - 1] == CellState.Empty) return false;
					}
					return true;
				}
			}

			public BoardState(int w, int h) {
				Width = w;
				Height = h;
				Board = new CellState[w, h];
			}

			private BoardState(CellState[,] board) {
				CellState[,] newBoard = new CellState[BoardWidth, BoardHeight];
				Array.Copy(board, newBoard, board.Length);
				Board = newBoard;
			}

			public BoardState Copy() {
				return new BoardState(Board) {
					Width = this.Width,
					Height = this.Height,
				};
			}

			public bool CanDropInColumn(int col) {
				return Board[col, BoardHeight - 1] == CellState.Empty;
			}

			public bool DropPiece(int column, CellState newState) {
				for (int y = 0; y < BoardHeight; y++) {
					if (Board[column, y] == CellState.Empty) {
						Board[column, y] = newState;
						return true;
					}
				}
				return false;
			}

			public List<GameEndState> GetWins(int? needed = null, bool stopAfterAny = false) {
				List<GameEndState> ret = new List<GameEndState>();
				int neededToWin = needed ?? NeededForWin;
				// Check for win (horizontal)
				for (int x = 0; x < BoardWidth - neededToWin + 1; x++) {
					for (int y = 0; y < BoardHeight; y++) {
						if (Board[x, y] == CellState.Empty) continue;
						bool isWin = true;
						for (int i = 0; i < neededToWin; i++) {
							if (Board[x + i, y] != Board[x, y]) {
								isWin = false;
								break;
							}
						}
						if (isWin) {
							ret.Add(new GameEndState() {
								Winner = Board[x, y],
								XStart = x,
								YStart = y,
								XEnd = x + neededToWin - 1,
								YEnd = y,
							});
							if (stopAfterAny) return ret;
						}
					}
				}
				// Check for win (vertical)
				for (int x = 0; x < BoardWidth; x++) {
					for (int y = 0; y < BoardHeight - neededToWin + 1; y++) {
						if (Board[x, y] == CellState.Empty) continue;
						bool isWin = true;
						for (int i = 0; i < neededToWin; i++) {
							if (Board[x, y + i] != Board[x, y]) {
								isWin = false;
								break;
							}
						}
						if (isWin) {
							ret.Add(new GameEndState() {
								Winner = Board[x, y],
								XStart = x,
								YStart = y,
								XEnd = x,
								YEnd = y + neededToWin - 1,
							});
							if (stopAfterAny) return ret;
						}
					}
				}
				// Check for win (diagonal UR)
				for (int x = 0; x < BoardWidth - neededToWin + 1; x++) {
					for (int y = 0; y < BoardHeight - neededToWin + 1; y++) {
						if (Board[x, y] == CellState.Empty) continue;
						bool isWin = true;
						for (int i = 0; i < neededToWin; i++) {
							if (Board[x + i, y + i] != Board[x, y]) {
								isWin = false;
								break;
							}
						}
						if (isWin) {
							ret.Add(new GameEndState() {
								Winner = Board[x, y],
								XStart = x,
								YStart = y,
								XEnd = x + neededToWin - 1,
								YEnd = y + neededToWin - 1,
							});
							if (stopAfterAny) return ret;
						}
					}
				}
				// Check for win (diagonal DR)
				for (int x = 0; x < BoardWidth - neededToWin + 1; x++) {
					for (int y = neededToWin - 1; y < BoardHeight; y++) {
						if (Board[x, y] == CellState.Empty) continue;
						bool isWin = true;
						for (int i = 0; i < neededToWin; i++) {
							if (Board[x + i, y - i] != Board[x, y]) {
								isWin = false;
								break;
							}
						}
						if (isWin) {
							ret.Add(new GameEndState() {
								Winner = Board[x, y],
								XStart = x,
								YStart = y,
								XEnd = x + neededToWin - 1,
								YEnd = y - neededToWin + 1,
							});
							if (stopAfterAny) return ret;
						}
					}
				}

				return ret;
			}

			public CellState this[int x, int y] {
				get => Board[x, y];
				set {
					Board[x, y] = value;
				}
			}

			public bool IsWinningMove(int x, CellState forColor, int? winThreshold = null) {
				BoardState board = Copy();
				board.DropPiece(x, forColor);
				return board.GetWins(winThreshold, true).Count > 0;
			}
		}

		private abstract class AI : Entity {
			protected BoardState Board;
			protected int? result = null;
			public readonly CellState myColor;
			private Thread worker;
			protected CancellationTokenSource cnclTknSrc = new CancellationTokenSource();
			public float ThinkingProgress { get; protected set; }

			protected AI(CellState color) {
				myColor = color;
			}

			internal void Start(BoardState board) {
				if (worker != null) {
					cnclTknSrc.Cancel();
					worker.Join();
				}
				ThinkingProgress = 0;
				cnclTknSrc = new CancellationTokenSource();
				result = null;
				Board = board.Copy();
				worker = new Thread(() => {
					try {
						LogicProcess(Board);
					}
					catch (Exception ex) {
						// Ignore; Probably a thread abort
					}
				});
				worker.Start();
			}

			internal int? TryGetResult() {
				return worker.IsAlive ? null : result;
			}

			protected abstract void LogicProcess(BoardState state);

			public override void Removed(Scene scene) {
				base.Removed(scene);
				cnclTknSrc.Cancel();
			}

			public override void SceneEnd(Scene scene) {
				base.SceneEnd(scene);
				cnclTknSrc.Cancel();
			}
		}

		private class AI_Easy : AI {
			public AI_Easy(CellState color) : base(color) {

			}

			protected override void LogicProcess(BoardState state) {
				List<int> choices = new List<int>();
				int blockMove = -1;
				for (int i = 0; i < BoardWidth; i++) {
					if (Board.IsWinningMove(i, myColor)) {
						result = i;
						return;
					}
					else if (Board.CanDropInColumn(i)) {
						if (Board.IsWinningMove(i, myColor == CellState.P1 ? CellState.P2 : CellState.P1)) {
							blockMove = i;
						}
						else choices.Add(i);
					}
					ThinkingProgress = i / (state.Width - 1);
				}
				if (blockMove >= 0) {
					result = blockMove;
				}
				else if (choices.Count > 0) {
					result = Random.Shared.Choose(choices);
				}
				else {
					// That's weird... no valid moves???
					result = 0;
				}
			}
		}

		private class AI_Normal : AI {
			public AI_Normal(CellState color) : base(color) {

			}

			protected override void LogicProcess(BoardState state) {
				float[] scores = ScoreMoves(Board, myColor, 4, 4);
				float maxScore = scores.Max();
				List<int> choices = new List<int>();
				for (int i = 0; i < BoardWidth; i++) {
					if (maxScore - scores[i] < 2) {
						choices.Add(i);
					}
				}
				if (choices.Count > 0) {
					result = Random.Shared.Choose(choices);
				}
				else {
					result = 0;
				}
			}

			private float[] ScoreMoves(BoardState state, CellState player, int lookahead, int maxLookahead) {
				float[] scores = new float[state.Width];
				for (int i = 0; i < state.Width; i++) {
					scores[i] = ScoreMove(state, player, i, lookahead, maxLookahead);
					if (cnclTknSrc.IsCancellationRequested) {
						return scores;
					}
					if (lookahead == maxLookahead) {
						ThinkingProgress = (i + 1) / (float)(maxLookahead + 2);
					}
					else {
						float numIterations = (float)Math.Pow(state.Width, maxLookahead + 1);
						ThinkingProgress += 1f / (numIterations + 1f);
					}
				}
				return scores;
			}

			private float ScoreMove(BoardState state, CellState player, int move, int lookahead, int maxLookahead) {
				CellState otherPlayer = player == CellState.P1 ? CellState.P2 : CellState.P1;
				if (!state.CanDropInColumn(move)) return -9999f;
				if (lookahead > 0) {
					BoardState nextState = state.Copy();
					nextState.DropPiece(move, player);
					if (nextState.GetWins(stopAfterAny: true).Count > 0) return 1000f;
					float[] recurScores = ScoreMoves(nextState, otherPlayer, lookahead - 1, maxLookahead);
					return recurScores.Max() * -0.95f;
				}
				else {
					if (state.IsWinningMove(move, player)) return 1000f;
					if (state.IsWinningMove(move, otherPlayer)) return 900f;
					return 0f;
				}
			}
		}

		private class AI_Hard : AI {
			public AI_Hard(CellState color) : base(color) {

			}

			protected override void LogicProcess(BoardState state) {
				float[] scores = ScoreMoves(Board, myColor, 5, 5);
				float maxScore = scores.Max();
				List<int> choices = new List<int>();
				for (int i = 0; i < BoardWidth; i++) {
					if (maxScore - scores[i] <= 1f) {
						choices.Add(i);
					}
				}
				if (choices.Count > 0) {
					result = Random.Shared.Choose(choices);
				}
				else {
					result = 0;
				}
			}

			private float[] ScoreMoves(BoardState state, CellState player, int lookahead, int maxLookahead) {
				float[] scores = new float[state.Width];
				for (int i = 0; i < state.Width; i++) {
					scores[i] = ScoreMove(state, player, i, lookahead, maxLookahead);
					if (lookahead > 2 && cnclTknSrc.IsCancellationRequested) {
						throw new Exception();
					}
					if (lookahead == maxLookahead) {
						ThinkingProgress = (i + 1) / (float)(maxLookahead + 2);
					}
					else {
						float numIterations = (float)Math.Pow(state.Width, maxLookahead + 1);
						ThinkingProgress += 1f / (numIterations + 1f);
					}
				}
				return scores;
			}

			private float ScoreMove(BoardState state, CellState player, int move, int lookahead, int maxLookahead) {
				CellState otherPlayer = player == CellState.P1 ? CellState.P2 : CellState.P1;
				if (!state.CanDropInColumn(move)) return -9999f;
				BoardState nextState = state.Copy();
				nextState.DropPiece(move, player);
				if (nextState.GetWins(stopAfterAny: true).Count > 0) return 1000f;
				if (lookahead > 0) {
					float[] recurScores = ScoreMoves(nextState, otherPlayer, lookahead - 1, maxLookahead);
					return recurScores.Max() * -0.99f;
				}
				else {
					return AnalyzeState(state, player, otherPlayer);
				}
			}

			private float AnalyzeState(BoardState state, CellState player, CellState otherPlayer) {
				float score = 0;
				// Look for threes
				List<GameEndState> threes = state.GetWins(NeededForWin - 1);
				score += 50f * threes.Count((GameEndState st) => st.Winner == player);
				score -= 60f * threes.Count((GameEndState st) => st.Winner != player);
				// Look for squares
				for (int x = 0; x < BoardWidth - 1; x++) {
					for (int y = 0; y < BoardHeight - 1; y++) {
						float sqrVal = state[x, y] == CellState.Empty ? 0f
							: state[x, y] == player ? 1f
							: 0f;
						score += 5f * sqrVal * sqrVal;
					}
				}
				// Look for stacked force win condition
				for (int x = 0; x < BoardWidth; x++) {
					for (int y = 0; y < BoardHeight - 1; y++) {
						if (state[x, y] != CellState.Empty) continue;
						state[x, y] = player;
						if (state.GetWins(stopAfterAny: true).Count > 0) {
							state[x, y] = otherPlayer;
							if (state.GetWins(stopAfterAny: true).Count > 0) {
								// Contested spot; either player moving here wins the game. Severe score punishment
								score -= 100f;
								continue;
							}
							state[x, y + 1] = player;
							if (state.GetWins(stopAfterAny: true).Count > 0) {
								// Stacked win spots; Strong favor to this state
								score += 120f;
								continue;
							}
							state[x, y + 1] = CellState.Empty;
						}
						state[x, y] = CellState.Empty;
					}
				}
				return score;
			}
		}

	}
}
