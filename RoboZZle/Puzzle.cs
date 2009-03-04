using System.Collections.Generic;
using System.Diagnostics;

namespace RoboZZle
{
	internal class Puzzle
	{
		private static readonly int[] shiftX = new[] { 0, 1, 0, -1 };

		private static readonly int[] shiftY = new[] { -1, 0, 1, 0 };

		private readonly int width;

		private readonly int height;

		private readonly CellColor[,] colors;

		private readonly bool[,] stars;

		private int starsCount;

		private int startX = -1;

		private int startY = -1;

		private int startDir = -1;

		public Puzzle(int width, int height)
		{
			Debug.Assert(width > 0 && height > 0);

			this.width = width;
			this.height = height;
			this.colors = new CellColor[width, height];
			this.stars = new bool[width, height];
		}

		public void SetStar(int x, int y, bool isStar)
		{
			if (stars[x, y])
				starsCount -= 1;

			stars[x, y] = isStar;

			if (stars[x, y])
				starsCount += 1;
		}

		public bool GetStar(int x, int y)
		{
			return stars[x, y];
		}

		public void SetColor(int x, int y, CellColor color)
		{
			colors[x, y] = color;
		}

		public void SetStartPosition(int x, int y)
		{
			this.startX = x;
			this.startY = y;
		}

		public void GetStartPosition(out int x, out int y)
		{
			x = this.startX;
			y = this.startY;
		}

		public void SetStartDirection(int dir)
		{
			this.startDir = dir;
		}

		public int GetStartDirection()
		{
			return this.startDir;
		}

		public bool IsInValidConfiguration()
		{
			return this.starsCount > 0 && this.startX >= 0 && this.startY >= 0 && this.startDir >= 0 &&
				   this.colors[this.startX, this.startY] != CellColor.None;
		}

		public void GetColorsCount(out int red, out int green, out int blue)
		{
			red = green = blue = 0;

			foreach (CellColor color in colors)
			{
				switch (color)
				{
					case CellColor.Red:
						++red;
						break;
					case CellColor.Green:
						++green;
						break;
					case CellColor.Blue:
						++blue;
						break;
				}
			}
		}

		public bool CanBeSolvedWith(Program program)
		{
			HashSet<ProgramState> stateSet = new HashSet<ProgramState>();
			ProgramState state = new ProgramState { X = this.startX, Y = this.startY, Dir = this.startDir, Func = 0, Instruction = 0 };
			bool[,] eatenStarsMask = new bool[this.width, this.height];
			int starsEaten = 0;

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

				CellColor currentColor = this.colors[state.X, state.Y];
				ProgramSlot slot = program.GetProgramSlot(state.Func, state.Instruction);
				if (slot.Action == ProgramAction.None)
					continue;
				if (slot.Color != CellColor.None && slot.Color != currentColor)
					continue;

				//MessageBox.Show(string.Format("({0},{1}), dir={2}", state.X, state.Y, state.Dir));

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
							state.X += shiftX[state.Dir];
							state.Y += shiftY[state.Dir];

							if (state.X < 0 || state.Y < 0 || state.X >= this.width || state.Y >= this.height)
								return ExecutionResult.Fail;
							if (colors[state.X, state.Y] == CellColor.None)
								return ExecutionResult.Fail;

							if (this.stars[state.X, state.Y] && !eatenStarsMask[state.X, state.Y])
							{
								eatenStarsMask[state.X, state.Y] = true;
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
					state.X = stateToPass.X;
					state.Y = stateToPass.Y;
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
			public int X { get; set; }

			public int Y { get; set; }

			public int Dir { get; set; }

			public int Func { get; set; }

			public int Instruction { get; set; }

			public override bool Equals(object obj)
			{
				if (obj == null || GetType() != obj.GetType())
					return false;

				ProgramState objCasted = (ProgramState)obj;
				return
					this.X == objCasted.X && this.Y == objCasted.Y &&
					this.Dir == objCasted.Dir && this.Func == objCasted.Func &&
					this.Instruction == objCasted.Instruction;
			}

			public override int GetHashCode()
			{
				return this.X ^ this.Y ^ this.Dir ^ this.Func ^ this.Instruction;
			}
		}
	}
}
