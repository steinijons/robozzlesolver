using System;
using AForge.Genetic;
using System.Diagnostics;

namespace RoboZZle.GameModel
{
	public class ProgramChromosome : ChromosomeBase
	{
		private readonly int slotCount;

		private readonly int funcCount;

		private readonly bool useRed;

		private readonly bool useGreen;

		private readonly bool useBlue;

		private readonly ProgramSlot[] program;

		private static readonly Random random = new Random();

		public ProgramChromosome(int slotCount, int funcCount, bool useRed, bool useGreen, bool useBlue)
		{
			Debug.Assert(slotCount > 0 && funcCount > 0 && funcCount <= 5);

			this.slotCount = slotCount;
			this.funcCount = funcCount;
			this.useRed = useRed;
			this.useGreen = useGreen;
			this.useBlue = useBlue;
			this.program = new ProgramSlot[slotCount];
			this.Generate();
		}

		public int SlotCount
		{
			get { return this.slotCount; }
		}

		public ProgramSlot GetSlot(int index)
		{
			return this.program[index];
		}

		public override void Generate()
		{
			for (int i = 0; i < this.slotCount; ++i)
			{
				ProgramAction action = this.GenerateRandomAction();
				FieldColor color = this.GenerateRandomColor();
				this.program[i].Action = action;
				this.program[i].Color = color;
			}
		}

		private FieldColor GenerateRandomColor()
		{
			int color = -1;
			while (color == -1 || (color == 1 && !useRed) || (color == 2 && !useGreen) || (color == 3 && !useBlue))
				color = random.Next(4);

			switch (color)
			{
				case 0:
					return FieldColor.None;
				case 1:
					return FieldColor.Red;
				case 2:
					return FieldColor.Green;
				case 3:
					return FieldColor.Blue;
				default:
					Debug.Fail("We should never get there");
					return FieldColor.None;
			}
		}

		private ProgramAction GenerateRandomAction()
		{
			int action = random.Next(this.funcCount + 4);
			switch (action)
			{
				case 0:
					return ProgramAction.None;
				case 1:
					return ProgramAction.Left;
				case 2:
					return ProgramAction.Right;
				case 3:
					return ProgramAction.Forward;
				case 4:
					return ProgramAction.F1;
				case 5:
					return ProgramAction.F2;
				case 6:
					return ProgramAction.F3;
				case 7:
					return ProgramAction.F4;
				case 8:
					return ProgramAction.F5;
				default:
					Debug.Fail("We should never get there");
					return ProgramAction.None;
			}
		}

		public override IChromosome CreateNew()
		{
			return new ProgramChromosome(this.slotCount, this.funcCount, this.useRed, this.useGreen, this.useBlue);
		}

		public override IChromosome Clone()
		{
			ProgramChromosome newChromosome = (ProgramChromosome)this.CreateNew();
			for (int i = 0; i < this.slotCount; ++i)
				newChromosome.program[i] = this.program[i];
			return newChromosome;
		}

		public override void Mutate()
		{
			bool mutateColor = random.Next(2) == 0;
			int randomPos = random.Next(this.slotCount);
			if (mutateColor)
			{
				FieldColor color = GenerateRandomColor();
				this.program[randomPos].Color = color;
			}
			else
			{
				ProgramAction action = this.GenerateRandomAction();
				this.program[randomPos].Action = action;
			}
		}

		public override void Crossover(IChromosome pair)
		{
			ProgramChromosome pairCasted = (ProgramChromosome)pair;

			int randomPos1 = random.Next(this.slotCount);
			int randomPos2 = random.Next(this.slotCount);
			for (int i = Math.Min(randomPos1, randomPos2); i <= Math.Max(randomPos1, randomPos2); ++i)
			{
				ProgramSlot temp = pairCasted.program[i];
				pairCasted.program[i] = this.program[i];
				this.program[i] = temp;
			}
		}
	}
}
