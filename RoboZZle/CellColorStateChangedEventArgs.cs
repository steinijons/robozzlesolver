namespace RoboZZle
{
	class FieldColorChangedEventArgs : PuzzleFieldChangedEventArgs
	{
		public FieldColor OldColor { get; private set; }

		public FieldColor NewColor { get; private set; }

		public FieldColorChangedEventArgs(FieldColor oldColor, FieldColor newColor, Coord coord)
			: base(coord)
		{
			this.OldColor = oldColor;
			this.NewColor = newColor;
		}
	}
}