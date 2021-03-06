﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGUI.Space
{
	public readonly struct Position
	{
		public int X { get; }
		public int Y { get; }

		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}

		public static Position Begin => new Position(0, 0);
		public static Position At(int x, int y) => new Position(x, y);

		public Position Next => new Position(X + 1, Y);
		public Position NextLine => new Position(0, Y + 1);
		public Position Move(int x, int y) => new Position(X + x, Y + y);
		public Position Move(Vector vector) => new Position(X + vector.X, Y + vector.Y);
		public Position Wrap(int width) => new Position(X % width, X / width);
		public Position UnWrap(int width) => new Position(X + Y * width, 0);
		public Vector AsVector() => new Vector(X, Y);
	}
}
