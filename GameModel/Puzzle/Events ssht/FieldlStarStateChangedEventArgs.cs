namespace RoboZZle.GameModel
{
    public class FieldStarStateChangedEventArgs : PuzzleFieldChangedEventArgs
	{
		public bool OldStarState { get; private set; }

		public bool NewStarState { get; private set; }

		public FieldStarStateChangedEventArgs(bool oldStarState, bool newStarState, Coord coord)
			: base(coord)
		{
			this.OldStarState = oldStarState;
			this.NewStarState = newStarState;
		}
	}
}