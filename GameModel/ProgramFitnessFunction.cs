using System.Collections.Generic;
using System.Linq;
using AForge.Genetic;
using System.Diagnostics;

namespace RoboZZle.GameModel
{
	public class ProgramFitnessFunction : IFitnessFunction
	{
		private readonly Puzzle puzzle;
		private readonly IList<int> funcSlotCount;
		private readonly int totalSlots;

		public ProgramFitnessFunction(Puzzle puzzle, IList<int> funcSlotCount)
		{
			Debug.Assert(puzzle != null && funcSlotCount != null && funcSlotCount.Count > 0);
			
			this.puzzle = puzzle;
			this.funcSlotCount = funcSlotCount;
			this.totalSlots = funcSlotCount.Sum();
		}
		
		public double Evaluate(IChromosome chromosome)
		{
			ProgramChromosome programChromosome = (ProgramChromosome) chromosome;
			Debug.Assert(this.totalSlots == programChromosome.SlotCount);
			Program program = this.ChromosomeToProgram(programChromosome);

			int starsEaten;
			this.puzzle.TrySolveWith(program, out starsEaten);
			return starsEaten + 1;
		}

		public Program ChromosomeToProgram(ProgramChromosome chromosome)
		{
			ProgramSlot[][] programCode = new ProgramSlot[this.funcSlotCount.Count][];
			int index = 0;
			for (int i = 0; i < this.funcSlotCount.Count; ++i)
			{
				programCode[i] = new ProgramSlot[this.funcSlotCount[i]];
				for (int j = 0; j < this.funcSlotCount[i]; ++j)
					programCode[i][j] = chromosome.GetSlot(index++);
			}

			return new Program(programCode);
		}
	}
}
