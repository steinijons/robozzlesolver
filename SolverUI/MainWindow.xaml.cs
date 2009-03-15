using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using RoboZZle.GameModel;

namespace RoboZZle
{
	public partial class MainWindow
	{
		private const int FieldSideSize = 18;

		private Puzzle puzzle;

		private ButtonAction lastAction = ButtonAction.None;

		private BackgroundWorker backgroundWorker;

		private Brush redBrush;

		private Brush greenBrush;

		private Brush blueBrush;

		private Brush noColorBrush;

		private bool isSolvingNow;

		public MainWindow()
		{
			this.InitializeComponent();
			this.LoadBackgroundWorker();
			this.LoadBrushes();
			this.CreateFieldAndPuzzle();
		}

		#region Initialization

		private void LoadBrushes()
		{
			this.redBrush = (Brush)this.FindResource("RedColor");
			this.greenBrush = (Brush)this.FindResource("GreenColor");
			this.blueBrush = (Brush)this.FindResource("BlueColor");
			this.noColorBrush = (Brush)this.FindResource("NoColor");
		}

		private void LoadBackgroundWorker()
		{
			this.backgroundWorker = (BackgroundWorker) this.FindResource("BackgroundWorker");
		}

		private void CreateFieldAndPuzzle()
		{
			for (int i = 0; i < FieldSideSize; ++i)
				for (int j = 0; j < FieldSideSize; ++j)
				{
					Button button = this.FindResource("FieldButton") as Button;
					Debug.Assert(button != null);

					button.Name = string.Format("Field_{0}_{1}", j, i);
					button.Click += fieldButton_Click;

					this.fieldGrid.Children.Add(button);
				}

			this.puzzle = new Puzzle(FieldSideSize, FieldSideSize);
			this.puzzle.FieldColorChanged += this.puzzle_FieldColorChanged;
			this.puzzle.FieldStarStateChanged += this.puzzle_FieldStarStateChanged;
			this.puzzle.StartPositionChanged += this.puzzle_StartPositionChanged;
			this.puzzle.StartDirectionChanged += this.puzzle_StartDirectionChanged;
			this.puzzle.Reset();
		}

		#endregion

		#region Helpers

		private void LoadDescription(string fileName)
		{
			if (!File.Exists(fileName))
			{
				MessageBox.Show(string.Format("File not found: '{0}'", fileName), "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			string[] lines = File.ReadAllLines(fileName);
			if (lines.Length == 0)
			{
				MessageBox.Show("Given file does not contain any data.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			string[] widthAndHeight = lines[0].Split();
			if (widthAndHeight.Length != 2)
			{
				MessageBox.Show("Given file does not contain any data.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			bool parseResult = true;
			int width, height;
			parseResult &= Int32.TryParse(widthAndHeight[0], out height);
			parseResult &= Int32.TryParse(widthAndHeight[1], out width);
			if (!parseResult || width <= 0 || height <= 0)
			{
				MessageBox.Show("Field size values are not correct.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (lines.Length != height + 1)
			{
				MessageBox.Show("Incorrect line count in input file.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			FieldColor[] intToColor = new[] { FieldColor.None, FieldColor.Red, FieldColor.Green, FieldColor.Blue };
			this.puzzle.Reset();
			for (int y = 0; y < height; ++y)
			{
				if (lines[y + 1].Length != width)
				{
					MessageBox.Show("Incorrect line count in input file.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				for (int x = 0; x < width; ++x)
				{
					int number = lines[y + 1][x];
					bool hasStar = ((number & 4) != 0);
					FieldColor cellColor = intToColor[number & 3];

					Coord coord = new Coord(x, y);
					this.puzzle.SetColor(coord, cellColor);
					this.puzzle.SetStar(coord, hasStar);
				}
			}
		}

		private Button FindButtonForCoords(Coord coord)
		{
			string nameToFind = string.Format("Field_{0}_{1}", coord.X, coord.Y);
			Button button = null;
			foreach (Button childButton in this.fieldGrid.Children)
			{
				if (childButton.Name == nameToFind)
				{
					button = childButton;
					break;
				}
			}

			Debug.Assert(button != null);
			return button;
		}

		public Shape FindPlayerTriangleInButton(Button button)
		{
			Grid grid = button.Content as Grid;
			Debug.Assert(grid != null);

			foreach (UIElement uiElement in grid.Children)
				if (uiElement is Canvas)
				{
					Canvas canvas = uiElement as Canvas;
					return canvas.Children[0] as Shape;
				}

			Debug.Fail("Execution should never reach that line!");
			return null;
		}

		public void AddPlayerTriangleToButton(Button button)
		{
			Grid grid = button.Content as Grid;
			Debug.Assert(grid != null);

			Canvas canvas = this.FindResource("PlayerTriangleHolder") as Canvas;
			Debug.Assert(canvas != null);
			grid.Children.Add(canvas);
		}

		public void RemovePlayerTriangleFromButton(Button button)
		{
			Grid grid = button.Content as Grid;
			Debug.Assert(grid != null);

			foreach (UIElement uiElement in grid.Children)
				if (uiElement is Canvas)
				{
					grid.Children.Remove(uiElement);
					return;
				}

			Debug.Fail("Execution should never reach that line!");
		}

		public void AddStarToButton(Button button)
		{
			Grid grid = button.Content as Grid;
			Debug.Assert(grid != null);

			Shape star = this.FindResource("Star") as Shape;
			Debug.Assert(star != null);
			grid.Children.Insert(0, star);	// Insert into beginning
		}

		public void RemoveStarFromButton(Button button)
		{
			Grid grid = button.Content as Grid;
			Debug.Assert(grid != null);

			foreach (UIElement uiElement in grid.Children)
				if (uiElement is Shape)
				{
					grid.Children.Remove(uiElement);
					return;
				}

			Debug.Fail("Execution should never reach that line!");
		}

		#endregion

		#region Event handlers

		#region Puzzle changing

		void puzzle_StartDirectionChanged(object sender, StartDirectionChangedEventArgs e)
		{
			Button button = this.FindButtonForCoords(this.puzzle.StartPosition);
			Shape playerTriangle = this.FindPlayerTriangleInButton(button);
			playerTriangle.RenderTransform = new RotateTransform(90 * e.NewDirection + 180, 5, 5);
		}

		void puzzle_StartPositionChanged(object sender, StartPositionChangedEventArgs e)
		{
			if (e.OldPosition == e.NewPosition)
				return;

			if (e.OldPosition.X != -1 && e.OldPosition.Y != -1)
			{
				Button oldPlayerButton = this.FindButtonForCoords(e.OldPosition);
				this.RemovePlayerTriangleFromButton(oldPlayerButton);
			}

			Button newPlayerButton = this.FindButtonForCoords(e.NewPosition);
			this.AddPlayerTriangleToButton(newPlayerButton);
			Shape playerTriangle = this.FindPlayerTriangleInButton(newPlayerButton);
			playerTriangle.RenderTransform = new RotateTransform(90 * this.puzzle.StartDirection + 180, 5, 5);
		}

		void puzzle_FieldStarStateChanged(object sender, FieldStarStateChangedEventArgs e)
		{
			if (e.OldStarState == e.NewStarState)
				return;

			Button button = this.FindButtonForCoords(e.Coord);
			bool isStar = this.puzzle.GetStar(e.Coord);

			if (!isStar)
				this.RemoveStarFromButton(button);
			else
				this.AddStarToButton(button);
		}

		void puzzle_FieldColorChanged(object sender, FieldColorChangedEventArgs e)
		{
			Button button = this.FindButtonForCoords(e.Coord);
			FieldColor color = this.puzzle.GetColor(e.Coord);

			switch (color)
			{
				case FieldColor.None:
					button.Background = this.noColorBrush;
					break;
				case FieldColor.Red:
					button.Background = this.redBrush;
					break;
				case FieldColor.Green:
					button.Background = this.greenBrush;
					break;
				case FieldColor.Blue:
					button.Background = this.blueBrush;
					break;
			}
		}

		#endregion

		#region Field editing

		private void redButton_Click(object sender, RoutedEventArgs e)
		{
			this.lastAction = ButtonAction.MarkAsRed;
		}

		private void greenButton_Click(object sender, RoutedEventArgs e)
		{
			this.lastAction = ButtonAction.MarkAsGreen;
		}

		private void blueButton_Click(object sender, RoutedEventArgs e)
		{
			this.lastAction = ButtonAction.MarkAsBlue;
		}

		private void noneButton_Click(object sender, RoutedEventArgs e)
		{
			this.lastAction = ButtonAction.MarkAsGray;
		}

		private void starButton_Click(object sender, RoutedEventArgs e)
		{
			this.lastAction = ButtonAction.MarkAsStar;
		}

		private void startButton_Click(object sender, RoutedEventArgs e)
		{
			this.lastAction = ButtonAction.MarkAsStart;
		}

		private void turnLeftButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.isSolvingNow)
				return;

			this.puzzle.StartDirection = (this.puzzle.StartDirection + 3) % 4;
		}

		private void rightButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.isSolvingNow)
				return;
			
			this.puzzle.StartDirection = (this.puzzle.StartDirection + 1) % 4;
		}

		private void fieldButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.isSolvingNow)
				return;
			
			Button button = e.Source as Button;
			Debug.Assert(button != null);
			string[] coordsAsString = button.Name.Substring(6).Split('_');
			int x = Int32.Parse(coordsAsString[0]), y = Int32.Parse(coordsAsString[1]);
			Coord coord = new Coord(x, y);

			switch (this.lastAction)
			{
				case ButtonAction.MarkAsRed:
					this.puzzle.SetColor(coord, FieldColor.Red);
					break;
				case ButtonAction.MarkAsGreen:
					this.puzzle.SetColor(coord, FieldColor.Green);
					break;
				case ButtonAction.MarkAsBlue:
					this.puzzle.SetColor(coord, FieldColor.Blue);
					break;
				case ButtonAction.MarkAsGray:
					this.puzzle.SetColor(coord, FieldColor.None);
					break;
				case ButtonAction.MarkAsStar:
					this.puzzle.SetStar(coord, !this.puzzle.GetStar(coord));
					break;
				case ButtonAction.MarkAsStart:
					this.puzzle.StartPosition = coord;
					break;
			}
		}

		#endregion

		#region Main operations

		private void solveButton_Click(object sender, RoutedEventArgs e)
		{
			bool parseResult = true;
			int n1, n2, n3, n4, n5;
			parseResult &= Int32.TryParse(this.func1Slots.Text, out n1);
			parseResult &= Int32.TryParse(this.func2Slots.Text, out n2);
			parseResult &= Int32.TryParse(this.func3Slots.Text, out n3);
			parseResult &= Int32.TryParse(this.func4Slots.Text, out n4);
			parseResult &= Int32.TryParse(this.func5Slots.Text, out n5);
			if (!parseResult || n1 < 1 || n2 < 0 || n3 < 0 || n4 < 0 || n5 < 0)
			{
				MessageBox.Show("Given function slot descriptions are not correct.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (!this.puzzle.IsInValidConfiguration())
			{
				MessageBox.Show("Puzzle configuration is not yet valid.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			int redCount, greenCount, blueCount;
			this.puzzle.GetColorsCount(out redCount, out greenCount, out blueCount);
			bool useRed = redCount > 0, useGreen = greenCount > 0, useBlue = blueCount > 0;
			if ((redCount == 0 && greenCount == 0) || (redCount == 0 && blueCount == 0) || (greenCount == 0 && blueCount == 0))
				useRed = useGreen = useBlue = false;

			int funcCount = (n1 > 0 ? 1 : 0) + (n2 > 0 ? 1 : 0) + (n3 > 0 ? 1 : 0) + (n4 > 0 ? 1 : 0) + (n5 > 0 ? 1 : 0);
			int slotsCount = n1 + n2 + n3 + n4 + n5;
			int colorsCount = 1 + (useRed ? 1 : 0) + (useGreen ? 1 : 0) + (useBlue ? 1 : 0);
			if (Math.Pow((3 + funcCount) * colorsCount, slotsCount) > 500000000)
				MessageBox.Show(
					"Note that solving puzzle with that configuration can take a lot of time.",
					"Warning!",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

			this.solveButton.IsEnabled = false;
			this.resetButton.IsEnabled = false;
			this.loadButton.IsEnabled = false;
			this.cancelButton.IsEnabled = true;
			this.isSolvingNow = true;

			List<int> slotSizes = new List<int> { n1, n2, n3, n4, n5 };
			slotSizes.RemoveAll(size => size == 0);
			this.backgroundWorker.RunWorkerAsync(new ArrayList { useRed, useGreen, useBlue, slotSizes });
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.backgroundWorker.CancelAsync();
		}

		private void resetButton_Click(object sender, RoutedEventArgs e)
		{
			this.puzzle.Reset();
		}

		private void loadButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			LoadDescription(dialog.FileName);
		}

		#endregion

		#region Background worker

		private void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			ArrayList parameters = e.Argument as ArrayList;
			Debug.Assert(parameters != null);
			bool useRed = (bool)parameters[0], useGreen = (bool)parameters[1], useBlue = (bool)parameters[2];
			List<int> slotSizes = parameters[3] as List<int>;

			int iterations = 0;
			Program.GeneratePrograms(
				useRed, useGreen, useBlue, slotSizes,
				program =>
				{
					iterations += 1;
					if (iterations % 10000 == 0)
						backgroundWorker.ReportProgress(iterations);

					if (this.puzzle.CanBeSolvedWith(program))
					{
						e.Result = program;
						return true;
					}

					if (backgroundWorker.CancellationPending)
					{
						e.Cancel = true;
						return true;
					}

					return false;
				});
		}

		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled)
				MessageBox.Show("No solution found because you have stopped solver.", "No solution found!", MessageBoxButton.OK, MessageBoxImage.Information);
			else if (e.Result == null)
				MessageBox.Show("Unfortunately solver could not find any solutions.", "No solution found!", MessageBoxButton.OK, MessageBoxImage.Information);
			else
				MessageBox.Show(e.Result.ToString(), "Solution found!", MessageBoxButton.OK, MessageBoxImage.Information);

			this.solveButton.IsEnabled = true;
			this.resetButton.IsEnabled = true;
			this.loadButton.IsEnabled = true;
			this.cancelButton.IsEnabled = false;
			this.isSolvingNow = false;
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.iterationsCompletedMessage.Text = string.Format("{0} iterations", e.ProgressPercentage);
		}

		#endregion

		#endregion
	}
}
