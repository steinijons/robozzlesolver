using System;

namespace RoboZZle
{
	internal class StartDirectionChangedEventArgs : EventArgs
	{
		public int OldDirection { get; private set; }
		
		public int NewDirection { get; private set; }

		public StartDirectionChangedEventArgs(int oldDirection, int newDirection)
		{
			this.OldDirection = oldDirection;
			this.NewDirection = newDirection;
		}
	}
}