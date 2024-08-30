using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGame
{
	public static class Tiles
	{
		public const int Size = 48;

		public static Point ToTilePosition(Vector2 position)
		{
			return new Point((int)position.X / Size, (int)position.Y / Size);
		}

		public static Vector2 ToWorldPosition(Point position)
		{
			return new Vector2(position.X * Size, position.Y * Size);
		}
	}
}
