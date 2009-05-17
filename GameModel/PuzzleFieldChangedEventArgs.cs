using System;

namespace RoboZZle.GameModel
{
    public class PuzzleFieldChangedEventArgs : EventArgs
	{
		public Coord Coord { get; private set; }

		public PuzzleFieldChangedEventArgs(Coord coord)
		{
			this.Coord = coord;
		}
	}
}