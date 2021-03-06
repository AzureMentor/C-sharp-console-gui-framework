﻿using ConsoleGUI.Buffering;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using ConsoleGUI.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsoleGUI
{
	public static class ConsoleManager
	{
		private class ConsoleManagerDrawingContextListener : IDrawingContextListener
		{
			void IDrawingContextListener.OnRedraw(DrawingContext drawingContext)
			{
				if (_freezeLock.IsFrozen) return;
				Redraw();
			}

			void IDrawingContextListener.OnUpdate(DrawingContext drawingContext, Rect rect)
			{
				if (_freezeLock.IsFrozen) return;
				Update(rect);
			}
		}

		private static readonly ConsoleBuffer _buffer = new ConsoleBuffer();
		private static FreezeLock _freezeLock;

		private static DrawingContext _contentContext = DrawingContext.Dummy;
		private static DrawingContext ContentContext
		{
			get => _contentContext;
			set => Setter
				.SetDisposable(ref _contentContext, value)
				.Then(Initialize);
		}

		private static IControl _content;
		public static IControl Content
		{
			get => _content;
			set => Setter
				.Set(ref _content, value)
				.Then(BindContent);
		}

		private static bool _compatibilityMode;
		public static bool CompatibilityMode
		{
			get => _compatibilityMode;
			set => Setter
				.Set(ref _compatibilityMode, value)
				.Then(Redraw);
		}

		private static bool _dontPrintTheLastCharacter;
		public static bool DontPrintTheLastCharacter
		{
			get => _dontPrintTheLastCharacter;
			set => Setter
				.Set(ref _dontPrintTheLastCharacter, value)
				.Then(Redraw);
		}

		private static void Initialize()
		{
			var consoleSize = new Size(Console.WindowWidth, Console.WindowHeight);

			_buffer.Initialize(consoleSize);
			Console.Clear();

			_freezeLock.Freeze();
			ContentContext.SetLimits(consoleSize, consoleSize);
			_freezeLock.Unfreeze();

			Redraw();
		}

		private static void Redraw()
		{
			Update(ContentContext.Size.AsRect());
		}

		private static void Update(Rect rect)
		{
			rect = ClipRect(rect);

			Console.CursorVisible = false;

			foreach (var position in rect)
			{
				if (DontPrintTheLastCharacter &&
					position.X == Console.WindowWidth - 1 &&
					position.Y == Console.WindowHeight - 1)
					continue;

				var character = ContentContext[position];

				if (!_buffer.Update(position, character)) continue;

				var content = character.Content ?? ' ';
				var foreground = character.Foreground ?? Color.White;
				var background = character.Background ?? Color.Black;

				if (content == '\n') content = ' ';

				try
				{
					Console.SetCursorPosition(position.X, position.Y);

					if (CompatibilityMode)
					{
						Console.BackgroundColor = ColorConverter.GetNearestConsoleColor(background);
						Console.ForegroundColor = ColorConverter.GetNearestConsoleColor(foreground);
						Console.Write(content);
					}
					else
					{
						Console.Write($"\x1b[38;2;{foreground.Red};{foreground.Green};{foreground.Blue}m\x1b[48;2;{background.Red};{background.Green};{background.Blue}m{content}");
					}
				}
				catch (ArgumentOutOfRangeException)
				{ }
			}
		}

		public static void Setup()
		{
			SafeConsole.SetUtf8();
			ResizeBuffer(Console.WindowWidth, Console.WindowHeight);
			Initialize();
		}

		public static void Resize(in Size size)
		{
			SafeConsole.SetWindowSize(1, 1);
			ResizeBuffer(size.Width, size.Height);
			SafeConsole.SetWindowSize(size.Width, size.Height);
			Initialize();
		}

		private static void ResizeBuffer(int width, int height)
		{
			Console.SetCursorPosition(0, 0);
			SafeConsole.SetWindowPosition(0, 0);
			SafeConsole.SetBufferSize(width, height);
		}

		public static void ReadInput(IReadOnlyCollection<IInputListener> controls)
		{
			while (Console.KeyAvailable)
			{
				var key = Console.ReadKey(true);
				var inputEvent = new InputEvent(key);

				foreach (var control in controls)
				{
					control.OnInput(inputEvent);
					if (inputEvent.Handled) break;
				}
			}
		}

		private static void BindContent()
		{
			ContentContext = new DrawingContext(new ConsoleManagerDrawingContextListener(), Content);
		}

		private static Rect ClipRect(in Rect rect) => Rect.Intersect(rect, new Rect(0, 0, Console.WindowWidth, Console.WindowHeight));
	}
}
