namespace RoboZZle.GameModel
{
    public struct Coord
    {
        public static readonly Coord Zero = new Coord(0, 0);
		
        private int x;
        public int X
        {
            get { return this.x; }
            set { this.x = value; }
        }

        private int y;
        public int Y
        {
            get { return this.y; }
            set { this.y = value; }
        }

        public static bool operator ==(Coord coord1, Coord coord2)
        {
            return coord1.X == coord2.X && coord1.Y == coord2.Y;
        }

        public static bool operator !=(Coord coord1, Coord coord2)
        {
            return !(coord1 == coord2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return this == (Coord) obj;
        }

        public override int GetHashCode()
        {
            return this.X ^ this.Y;
        }

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}