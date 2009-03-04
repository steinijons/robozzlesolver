using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RoboZZle
{
	class Program
	{
		private readonly ProgramSlot[][] programCode;

		public Program(ProgramSlot[][] programCode)
		{
			this.programCode = new ProgramSlot[programCode.Length][];
			for (int i = 0; i < programCode.Length; ++i)
				this.programCode[i] = (ProgramSlot[])programCode[i].Clone();
		}

		public override string ToString()
		{
			string result = String.Empty;

			for (int i = 0; i < programCode.Length; ++i)
			{
				if (programCode[i] == null)
					continue;

				result += string.Format("F{0}: ", i + 1);
				for (int j = 0; j < programCode[i].Length; ++j)
				{
					if (programCode[i][j].Action == ProgramAction.None)
						break;
					result += programCode[i][j].Action;
					if (programCode[i][j].Color != CellColor.None)
						result += string.Format("({0})", programCode[i][j].Color);
					result += " ";
				}

				if (i != programCode.Length - 1)
					result += " |  ";
			}

			return result;
		}

		public ProgramSlot GetProgramSlot(int func, int number)
		{
			return this.programCode[func][number];
		}

		public int GetInstructionCountInFunc(int func)
		{
			return this.programCode[func].Length;
		}

		public static void GeneratePrograms(
			bool useRed, bool useGreen, bool useBlue, IList<int> funcSlotCount, Predicate<Program> shouldStopPredicate)
		{
			Debug.Assert(funcSlotCount.Count <= 5 && !funcSlotCount.Contains(0));

			List<CellColor> availableColors = new List<CellColor> { CellColor.None };
			if (useRed) availableColors.Add(CellColor.Red);
			if (useGreen) availableColors.Add(CellColor.Green);
			if (useBlue) availableColors.Add(CellColor.Blue);

			List<ProgramAction> availableActions =
				new List<ProgramAction> { ProgramAction.Forward, ProgramAction.Left, ProgramAction.Right };
			if (funcSlotCount.Count > 0) availableActions.Add(ProgramAction.F1);
			if (funcSlotCount.Count > 1) availableActions.Add(ProgramAction.F2);
			if (funcSlotCount.Count > 2) availableActions.Add(ProgramAction.F3);
			if (funcSlotCount.Count > 3) availableActions.Add(ProgramAction.F4);
			if (funcSlotCount.Count > 4) availableActions.Add(ProgramAction.F5);

			ProgramSlot[][] generatedCode = new ProgramSlot[funcSlotCount.Count][];
			for (int i = 0; i < funcSlotCount.Count; ++i)
				generatedCode[i] = new ProgramSlot[funcSlotCount[i]];

			GenerateProgramsDfs(availableColors, availableActions, 0, generatedCode, shouldStopPredicate);
		}

		#region Code generation

		private static bool GenerateProgramsDfs(
			IEnumerable<CellColor> colors,
			IEnumerable<ProgramAction> actions,
			int func,
			ProgramSlot[][] generatedCode,
			Predicate<Program> shouldStopPredicate)
		{
			if (func >= generatedCode.Length)
			{
				if (!CheckIfCompleteCodeIsGood(generatedCode))
					return false;

				return shouldStopPredicate(new Program(generatedCode));
			}

			Predicate<ProgramSlot[]> funcCodePredicate = (
				code =>
				{
					generatedCode[func] = (ProgramSlot[])code.Clone();
					return GenerateProgramsDfs(colors, actions, func + 1, generatedCode, shouldStopPredicate);
				});

			return GenerateFuncCodeDfs(func, colors, actions, 0, new ProgramSlot[generatedCode[func].Length], funcCodePredicate);
		}

		private static bool GenerateFuncCodeDfs(
			int funcIndex,
			IEnumerable<CellColor> colors,
			IEnumerable<ProgramAction> actions,
			int depth,
			ProgramSlot[] funcCode,
			Predicate<ProgramSlot[]> funcCodePredicate)
		{
			foreach (ProgramAction action in actions)
				foreach (CellColor color in colors)
				{
					if (SkipDueToOptimizations(funcCode, funcIndex, depth, action, color))
						continue;

					funcCode[depth].Action = action;
					funcCode[depth].Color = color;

					if (funcCodePredicate(funcCode))
						return true;

					if (action != ProgramAction.None && depth < funcCode.Length - 1)
					{
						if (GenerateFuncCodeDfs(funcIndex, colors, actions, depth + 1, funcCode, funcCodePredicate))
							return true;
					}

					funcCode[depth].Action = ProgramAction.None;
					funcCode[depth].Color = CellColor.None;
				}

			return false;
		}

		#endregion

		#region Code checking

		private static bool SkipDueToOptimizations(ProgramSlot[] code, int funcIndex, int depth, ProgramAction action, CellColor color)
		{
			// Rotation code optimization
			if ((action == ProgramAction.Left || action == ProgramAction.Right) && depth > 0)
			{
				// Do not use consequent opposite rotations with the same color
				if (code[depth - 1].Color == color &&
					((code[depth - 1].Action == ProgramAction.Left && action == ProgramAction.Right) ||
					 (code[depth - 1].Action == ProgramAction.Right && action == ProgramAction.Left)))
				{
					return true;
				}

				// Consequent rotation count optimization
				int i = depth - 1;
				int sameCount = 1;
				while (i >= 0 && code[i].Color == color && code[i].Action == action)
				{
					sameCount += 1;
					i -= 1;
				}

				// Max 2 rotations right and 1 rotation left
				if ((sameCount > 2 && action == ProgramAction.Right) || (sameCount > 1 && action == ProgramAction.Left))
					return true;
			}

			// Recursion optimization - do not call function from itself in the first instruction
			if (depth == 0 && funcIndex == 0 && action == ProgramAction.F1 ||
				depth == 0 && funcIndex == 1 && action == ProgramAction.F2 ||
				depth == 0 && funcIndex == 2 && action == ProgramAction.F3 ||
				depth == 0 && funcIndex == 3 && action == ProgramAction.F4 ||
				depth == 0 && funcIndex == 4 && action == ProgramAction.F5)
			{
				return true;
			}

			return false;
		}

		private static bool CheckIfCompleteCodeIsGood(ProgramSlot[][] programCode)
		{
			// Check if program contains any forward instructions
			bool forwardInstructionFound = false;
			foreach (ProgramSlot[] funcCode in programCode)
				foreach (ProgramSlot programSlot in funcCode)
				{
					if (programSlot.Action == ProgramAction.Forward)
					{
						forwardInstructionFound = true;
						break;
					}
				}

			// Program with no forward instructions is bad
			if (!forwardInstructionFound)
				return false;

			return true;
		}

		#endregion
	}
}