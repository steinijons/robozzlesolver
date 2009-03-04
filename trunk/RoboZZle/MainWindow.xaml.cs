using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace RoboZZle
{
	enum ButtonAction
	{
		None, MarkAsRed, MarkAsGreen, MarkAsBlue, MarkAsGray, MarkAsStar, MarkAsStart
	}

	public partial class MainWindow
	{
		private Puzzle puzzle;

		private ButtonAction lastAction = ButtonAction.None;

		private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

		private Brush redBrush, greenBrush, blueBrush, noColorBrush;

		const int FieldSideSize = 18;

		public MainWindow()
		{
			this.InitializeComponent();
			this.CreateFieldAndPuzzle();
			this.InitializeBackgroundWorker();
			this.LoadBrushes();

			this.SetStartPosition(0, 0);
			this.SetStartDirection(1);
		}

		private void LoadBrushes()
		{
			this.redBrush = (Brush) this.FindResource("RedColor");
			this.greenBrush = (Brush) this.FindResource("GreenColor");
			this.blueBrush = (Brush) this.FindResource("BlueColor");
			this.noColorBrush = (Brush) this.FindResource("NoColor");
		}

		private void InitializeBackgroundWorker()
		{
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;
			this.backgroundWorker.ProgressChanged += this.worker_ProgressChanged;
			this.backgroundWorker.RunWorkerCompleted += this.worker_RunWorkerCompleted;
			this.backgroundWorker.DoWork += this.worker_DoWork;
		}

		private void CreateFieldAndPuzzle()
		{
			this.puzzle = new Puzzle(FieldSideSize, FieldSideSize);
			for (int i = 0; i < FieldSideSize; ++i)
				for (int j = 0; j < FieldSideSize; ++j)
				{
					Button button = new Button { Background = Brushes.Gray, Name = string.Format("Field_{0}_{1}", j, i) };
					button.Click += fieldButton_Click;

					this.fieldGrid.Children.Add(button);
				}
		}

		private void LoadDescription(string fileName)
		{
			if (!File.Exists(fileName))
			{
				MessageBox.Show(string.Format("File not found: '{0}'", fileName), "Error!");
				return;
			}

			string[] lines = File.ReadAllLines(fileName);
			if (lines.Length == 0)
			{
				MessageBox.Show("Given file does not contain any data.", "Error!");
				return;
			}

			string[] widthAndHeight = lines[0].Split();
			if (widthAndHeight.Length != 2)
			{
				MessageBox.Show("Given file does not contain any data.", "Error!");
				return;
			}

			bool parseResult = true;
			int width, height;
			parseResult &= Int32.TryParse(widthAndHeight[0], out height);
			parseResult &= Int32.TryParse(widthAndHeight[1], out width);
			if (!parseResult || width <= 0 || height <= 0)
			{
				MessageBox.Show("Field size values are not correct.", "Error!");
				return;
			}

			if (lines.Length != height + 1)
			{
				MessageBox.Show("Incorrect line count in input file.", "Error!");
				return;
			}

			this.Reset();
			CellColor[] intToColor = new[] { CellColor.None, CellColor.Red, CellColor.Green, CellColor.Blue };
			for (int y = 0; y < height; ++y)
			{
				if (lines[y + 1].Length != width)
				{
					MessageBox.Show("Incorrect line count in input file.", "Error!");
					return;
				}

				for (int x = 0; x < width; ++x)
				{
					int number = lines[y + 1][x];
					bool hasStar = ((number & 4) != 0);
					CellColor cellColor = intToColor[number & 3];
					SetColor(x, y, cellColor);
					SetStar(x, y, hasStar);
				}
			}
		}

		#region Field control

		private void SetStartDirection(int direction)
		{
			int x, y;
			this.puzzle.GetStartPosition(out x, out y);
			Button button = this.FindButtonForCoords(x, y);
			Canvas canvas = (Canvas) button.Content;
			Polygon playerTriangle = (Polygon) canvas.Children[0];
			
			double angle = 90 * direction + 180;
			playerTriangle.RenderTransform = new RotateTransform(angle, 5, 5);

			this.puzzle.SetStartDirection(direction);
		}

		private void SetStartPosition(int x, int y)
		{
			// Reset star state into player position
			SetStar(x, y, false);

			int oldX, oldY;
			this.puzzle.GetStartPosition(out oldX, out oldY);
			this.puzzle.SetStartPosition(x, y);

			if (oldX != -1 && oldY != -1)
			{
				Button oldPlayerButton = this.FindButtonForCoords(oldX, oldY);
				oldPlayerButton.Content = null;
			}

			Button newPlayerButton = this.FindButtonForCoords(x, y);
			Canvas canvas = (Canvas) this.FindResource("PlayerTriangleHolder");
			newPlayerButton.Content = canvas;
		}

		private void Reset()
		{
			SetStartPosition(0, 0);
			SetStartDirection(0);
			
			for (int x = 0; x < FieldSideSize; ++x)
				for (int y = 0; y < FieldSideSize; ++y)
				{
					SetColor(x, y, CellColor.None);
					SetStar(x, y, false);
				}
		}

		private Button FindButtonForCoords(int x, int y)
		{
			string nameToFind = string.Format("Field_{0}_{1}", x, y);
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

		private void SetColor(int x, int y, CellColor color)
		{
			Button button = FindButtonForCoords(x, y);

			this.puzzle.SetColor(x, y, color);
			switch (color)
			{
				case CellColor.None:
					button.Background = this.noColorBrush;
					break;
				case CellColor.Red:
					button.Background = this.redBrush;
					break;
				case CellColor.Green:
					button.Background = this.greenBrush;
					break;
				case CellColor.Blue:
					button.Background = this.blueBrush;
					break;
			}
		}

		private void SetStar(int x, int y, bool isStar)
		{
			// No stars at player position
			int playerX, playerY;
			this.puzzle.GetStartPosition(out playerX, out playerY);
			if (x == playerX && y == playerY)
				return;

			Button button = FindButtonForCoords(x, y);

			this.puzzle.SetStar(x, y, isStar);
			if (!isStar)
				button.Content = null;
			else
			{
				Ellipse star = (Ellipse)this.FindResource("Star");
				button.Content = star;
			}
		}

		#endregion

		#region Event handlers

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
			SetStartDirection((puzzle.GetStartDirection() + 3) % 4);
		}

		private void rightButton_Click(object sender, RoutedEventArgs e)
		{
			SetStartDirection((puzzle.GetStartDirection() + 1) % 4);
		}

		private void fieldButton_Click(object sender, RoutedEventArgs e)
		{
			Button button = e.Source as Button;
			Debug.Assert(button != null);
			string[] coordsAsString = button.Name.Substring(6).Split('_');
			int x = Int32.Parse(coordsAsString[0]), y = Int32.Parse(coordsAsString[1]);

			switch (this.lastAction)
			{
				case ButtonAction.MarkAsRed:
					SetColor(x, y, CellColor.Red);
					break;
				case ButtonAction.MarkAsGreen:
					SetColor(x, y, CellColor.Green);
					break;
				case ButtonAction.MarkAsBlue:
					SetColor(x, y, CellColor.Blue);
					break;
				case ButtonAction.MarkAsGray:
					SetColor(x, y, CellColor.None);
					break;
				case ButtonAction.MarkAsStar:
					SetStar(x, y, !this.puzzle.GetStar(x, y));
					break;
				case ButtonAction.MarkAsStart:
					SetStartPosition(x, y);
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
				MessageBox.Show("Given function slot descriptions are not correct.", "Error!");
				return;
			}

			if (!this.puzzle.IsInValidConfiguration())
			{
				MessageBox.Show("Puzzle configuration is not yet valid.", "Error!");
				return;
			}

			int redCount, greenCount, blueCount;
			this.puzzle.GetColorsCount(out redCount, out greenCount, out blueCount);
			bool useRed = redCount > 0, useGreen = greenCount > 0, useBlue = blueCount > 0;
			if ((redCount == 0 && greenCount == 0) || (redCount == 0 && blueCount == 0) || (greenCount == 0 && blueCount == 0))
				useRed = useGreen = useBlue = false;

			this.solveButton.IsEnabled = false;
			this.resetButton.IsEnabled = false;
			this.loadButton.IsEnabled = false;
			this.cancelButton.IsEnabled = true;

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
			this.Reset();
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
				MessageBox.Show("No solution found because you have stopped solver.", "No solution found!");
			else if (e.Result == null)
				MessageBox.Show("Unfortunately solver could not find any solutions.", "No solution found!");
			else
				MessageBox.Show(e.Result.ToString(), "Solution found!");

			this.solveButton.IsEnabled = true;
			this.resetButton.IsEnabled = true;
			this.loadButton.IsEnabled = true;
			this.cancelButton.IsEnabled = false;
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.iterationsCompletedMessage.Text = string.Format("{0} iterations", e.ProgressPercentage);
		}

		#endregion

		#endregion
	}
}
