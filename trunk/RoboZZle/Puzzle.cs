using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace RoboZZle
{
	internal class Puzzle
	{
		private static readonly int[] shiftX = new[] { 0, 1, 0, -1 };

		private static readonly int[] shiftY = new[] { -1, 0, 1, 0 };

		private readonly int width;

		private readonly int height;

		private readonly FieldColor[,] colors;

		private readonly bool[,] stars;

		private int starsCount;

		private Coord startPos = new Coord(-1, -1);

		private int startDir = -1;

		public Puzzle(int width, int height)
		{
			Debug.Assert(width > 0 && height > 0);

			this.width = width;
			this.height = height;
			this.colors = new FieldColor[width, height];
			this.stars = new bool[width, height];
		}

		public event EventHandler<StartPositionChangedEventArgs> StartPositionChanged;

		public event EventHandler<StartDirectionChangedEventArgs> StartDirectionChanged;

		public event EventHandler<FieldColorChangedEventArgs> FieldColorChanged;

		public event EventHandler<FieldStarStateChangedEventArgs> FieldStarStateChanged;

		public Coord StartPosition
		{
			get { return this.startPos; }
			set
			{
				Coord oldPos = this.startPos;
				this.startPos = value;

				EventHandler<StartPositionChangedEventArgs> handler = this.StartPositionChanged;
				if (handler != null)
					handler(this, new StartPositionChangedEventArgs(oldPos, value));
			}
		}

		public int StartDirection
		{
			get { return this.startDir; }
			set
			{
				int oldDir = this.startDir;
				this.startDir = value;

				EventHandler<StartDirectionChangedEventArgs> handler = this.StartDirectionChanged;
				if (handler != null)
					handler(this, new StartDirectionChangedEventArgs(oldDir, value));
			}
		}

		public bool GetStar(Coord coord)
		{
			return this.stars[coord.X, coord.Y];
		}

		public void SetStar(Coord coord, bool isStar)
		{
			bool oldStarState = stars[coord.X, coord.Y];

			if (stars[coord.X, coord.Y])
				starsCount -= 1;

			stars[coord.X, coord.Y] = isStar;

			if (stars[coord.X, coord.Y])
				starsCount += 1;

			EventHandler<FieldStarStateChangedEventArgs> handler = this.FieldStarStateChanged;
			if (handler != null)
				handler(this, new FieldStarStateChangedEventArgs(oldStarState, isStar, coord));
		}

		public FieldColor GetColor(Coord coord)
		{
			return this.colors[coord.X, coord.Y];
		}

		public void SetColor(Coord coord, FieldColor color)
		{
			FieldColor oldColor = colors[coord.X, coord.Y];
			colors[coord.X, coord.Y] = color;

			EventHandler<FieldColorChangedEventArgs> handler = this.FieldColorChanged;
			if (handler != null)
				handler(this, new FieldColorChangedEventArgs(oldColor, color, coord));
		}

		public void Reset()
		{
			for (int x = 0; x < this.width; ++x)
				for (int y = 0; y < this.height; ++y)
				{
					Coord coord = new Coord(x, y);
					this.SetColor(coord, FieldColor.None);
					this.SetStar(coord, false);
				}

			this.StartPosition = Coord.Zero;
			this.StartDirection = 1;
		}

		public bool IsInValidConfiguration()
		{
			return
				this.starsCount > 0 &&
				this.startPos.X >= 0 && this.startPos.Y >= 0 && this.startPos.X < this.width && this.startPos.Y < this.height &&
				this.startDir >= 0 && this.startDir <= 3 &&
				this.colors[this.startPos.X, this.startPos.Y] != FieldColor.None;
		}

		public void GetColorsCount(out int red, out int green, out int blue)
		{
			red = green = blue = 0;

			foreach (FieldColor color in colors)
			{
				switch (color)
				{
					case FieldColor.Red:
						++red;
						break;
					case FieldColor.Green:
						++green;
						break;
					case FieldColor.Blue:
						++blue;
						break;
				}
			}
		}

		public bool CanBeSolvedWith(Program program)
		{
			Debug.Assert(this.IsInValidConfiguration());

			HashSet<ProgramState> stateSet = new HashSet<ProgramState>();
			ProgramState state = new ProgramState { Position = this.startPos, Dir = this.startDir, Func = 0, Instruction = 0 };
			bool[,] eatenStarsMask = new bool[this.width, this.height];
			int starsEaten = 0;

			// Check if we are staying on star
			if (this.stars[startPos.X, startPos.Y])
			{
			    starsEaten = 1;
			    eatenStarsMask[startPos.X, startPos.Y] = true;
			    if (this.starsCount == 1)
			        return true;
			}

			return CanBeSolvedWithDfs(stateSet, eatenStarsMask, ref state, ref starsEaten, program) == ExecutionResult.StarsEaten;
		}

		private ExecutionResult CanBeSolvedWithDfs(
			HashSet<ProgramState> stateSet, bool[,] eatenStarsMask, ref ProgramState state, ref int starsEaten, Program program)
		{
			Debug.Assert(state.Instruction == 0);

			for (int instr = 0; instr < program.GetInstructionCountInFunc(state.Func); ++instr)
			{
				state.Instruction = instr;

				// Loop check
				// BUG: that check skips good solutions for some tasks
				if (stateSet.Contains(state))
					return ExecutionResult.Fail;
				stateSet.Add(state);

				FieldColor currentColor = this.colors[state.Position.X, state.Position.Y];
				ProgramSlot slot = program.GetProgramSlot(state.Func, state.Instruction);
				if (slot.Action == ProgramAction.None)
					continue;
				if (slot.Color != FieldColor.None && slot.Color != currentColor)
					continue;

				if (slot.Action == ProgramAction.Forward || slot.Action == ProgramAction.Left || slot.Action == ProgramAction.Right)
				{
					switch (slot.Action)
					{
						case ProgramAction.Left:
							state.Dir = (state.Dir - 1 + 4) % 4;
							break;
						case ProgramAction.Right:
							state.Dir = (state.Dir + 1) % 4;
							break;
						case ProgramAction.Forward:
							Coord newPosition = state.Position;
							newPosition.X += shiftX[state.Dir];
							newPosition.Y += shiftY[state.Dir];
							state.Position = newPosition;

							if (state.Position.X < 0 || state.Position.Y < 0 ||
								state.Position.X >= this.width || state.Position.Y >= this.height)
								return ExecutionResult.Fail;
							if (colors[state.Position.X, state.Position.Y] == FieldColor.None)
								return ExecutionResult.Fail;

							// If star was eaten after this turn
							if (this.stars[state.Position.X, state.Position.Y] && !eatenStarsMask[state.Position.X, state.Position.Y])
							{
								eatenStarsMask[state.Position.X, state.Position.Y] = true;
								starsEaten += 1;
								if (starsEaten == this.starsCount)
									return ExecutionResult.StarsEaten;
							}

							break;
					}
				}
				else if (slot.Action == ProgramAction.F1 || slot.Action == ProgramAction.F2 ||
						 slot.Action == ProgramAction.F3 || slot.Action == ProgramAction.F4 ||
						 slot.Action == ProgramAction.F5)
				{
					ProgramState stateToPass = state;
					stateToPass.Instruction = 0;

					switch (slot.Action)
					{
						case ProgramAction.F1:
							stateToPass.Func = 0;
							break;
						case ProgramAction.F2:
							stateToPass.Func = 1;
							break;
						case ProgramAction.F3:
							stateToPass.Func = 2;
							break;
						case ProgramAction.F4:
							stateToPass.Func = 3;
							break;
						case ProgramAction.F5:
							stateToPass.Func = 4;
							break;
					}

					ExecutionResult result = CanBeSolvedWithDfs(stateSet, eatenStarsMask, ref stateToPass, ref starsEaten, program);
					state.Position = stateToPass.Position;
					state.Dir = stateToPass.Dir;
					if (result != ExecutionResult.StillWorking)
						return result;
				}
			}

			return ExecutionResult.StillWorking;
		}

		enum ExecutionResult
		{
			StarsEaten, StillWorking, Fail
		}

		struct ProgramState
		{
			public Coord Position { get; set; }

			public int Dir { get; set; }

			public int Func { get; set; }

			public int Instruction { get; set; }

			public override bool Equals(object obj)
			{
				if (obj == null || GetType() != obj.GetType())
					return false;

				ProgramState objCasted = (ProgramState)obj;
				return
					//this.Position == objCasted.Position &&
					this.Position.X == objCasted.Position.X &&
					this.Position.Y == objCasted.Position.Y &&
					this.Dir == objCasted.Dir && this.Func == objCasted.Func &&
					this.Instruction == objCasted.Instruction;
			}

			public override int GetHashCode()
			{
				return this.Position.X ^ this.Position.Y ^ this.Dir ^ this.Func ^ this.Instruction;
			}
		}
	}
}
