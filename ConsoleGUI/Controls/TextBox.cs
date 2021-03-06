﻿using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using ConsoleGUI.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGUI.Controls
{
	public class TextBox : Control, IInputListener
	{
		private string _text = string.Empty;
		public string Text
		{
			get => _text ?? string.Empty;
			set => Setter
				.Set(ref _text, value)
				.Then(Initialize);
		}

		private int _caretStart;
		public int CaretStart
		{
			get => Math.Min(Math.Max(_caretStart, 0), TextLength);
			set => Setter
				.Set(ref _caretStart, value)
				.Then(Redraw);
		}

		private int _caretEnd;
		public int CaretEnd
		{
			get => Math.Min(Math.Max(_caretEnd, CaretStart), TextLength);
			set => Setter
				.Set(ref _caretEnd, value)
				.Then(Redraw);
		}

		private int TextLength => Text?.Length ?? 0;
		private Size TextSize => new Size(TextLength, 1);
		private Size EditorSize => CaretEnd >= TextLength ? TextSize.Expand(1, 0) : TextSize;

		public override Character this[Position position]
		{
			get
			{
				if (!EditorSize.Contains(position)) return Character.Empty;

				var character = position.X >= TextLength
					? (char?)null
					: Text[position.X];

				if (position.X == CaretStart && position.X == CaretEnd)
					return new Character(character, background: new Color(70, 70, 70));

				if (position.X >= CaretStart && position.X < CaretEnd)
					return new Character(character, background: Color.White, foreground: Color.Black);

				return new Character(character);
			}
		}

		void IInputListener.OnInput(InputEvent inputEvent)
		{
			using (Freeze())
			{
				string newText = null;

				switch (inputEvent.Key.Key)
				{
					case ConsoleKey.LeftArrow when inputEvent.Key.Modifiers.HasFlag(ConsoleModifiers.Control):
						CaretStart = Math.Max(0, CaretStart - 1);
						break;
					case ConsoleKey.LeftArrow:
						CaretStart = CaretEnd = Math.Max(0, CaretStart - 1);
						break;
					case ConsoleKey.RightArrow when inputEvent.Key.Modifiers.HasFlag(ConsoleModifiers.Control):
						CaretEnd = Math.Min(Text.Length, CaretEnd + 1);
						break;
					case ConsoleKey.RightArrow:
						CaretStart = CaretEnd = Math.Min(Text.Length, CaretEnd + 1);
						break;
					case ConsoleKey.Delete when CaretStart != CaretEnd:
					case ConsoleKey.Backspace when CaretStart != CaretEnd:
						newText = $"{Text.Substring(0, CaretStart)}{Text.Substring(CaretEnd)}";
						CaretEnd = CaretStart;
						break;
					case ConsoleKey.Backspace when CaretStart > 0:
						newText = $"{Text.Substring(0, CaretStart - 1)}{Text.Substring(CaretStart)}";
						CaretStart = CaretEnd = CaretStart - 1;
						break;
					case ConsoleKey.Delete when CaretStart < TextLength:
						newText = $"{Text.Substring(0, CaretStart)}{Text.Substring(CaretStart + 1)}";
						break;
					case ConsoleKey key when char.IsControl(inputEvent.Key.KeyChar):
						return;
					default:
						newText = $"{Text.Substring(0, CaretStart)}{inputEvent.Key.KeyChar}{Text.Substring(CaretEnd)}";
						CaretStart = CaretEnd = CaretStart + 1;
						break;
				}

				if (newText != null)
					Text = newText;

				inputEvent.Handled = true;
			}
		}

		protected override void Initialize()
		{
			using (Freeze())
			{
				Resize(EditorSize);
			}
		}
	}
}
