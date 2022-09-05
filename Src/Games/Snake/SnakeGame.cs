using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VSGames.Games
{
    [Guid("550EFA51-F65E-469E-A121-A1610ACE46C2")]
    public class SnakeGame : GameWindow
    {
        private static readonly int GridSize = 8; // In pixels, should fully divide the size of the window

        private static readonly Color GridColor1 = Color.FromArgb(38,174,72);
        private static readonly Color GridColor2 = Color.FromArgb(93,218,116);
        private static readonly Color AppleColor = Color.FromArgb(255,47,0);
        private static readonly Color SnakeColor = Color.FromArgb(33,111,189);

        private static float UpdateDelta = 0.1f; // Update the snake every 0.1 seconds
        private enum SnakeDirection { Up, Down, Right, Left };
        private Vector2[] DirectionVectors = new Vector2[4] { new Vector2(0,-1), new Vector2(0, 1), new Vector2(1, 0), new Vector2(-1, 0) };

        // Store the positions of the snake body, each update cycle does :
        // 1. Add a new item to the front of the queue at the old front + the 
        //    direction of the snake
        // 2. Remove the item at the end of the queue IF the head of the snake
        //    is not on an apple
        private Queue<Vector2> snakePositions = new Queue<Vector2>();

        private SnakeDirection snakeDirection;
        private Vector2 lastHeadPos;

        private float lastUpdateDelta;

        private Vector2 applePos;
        private Random rng = new Random();

        private int score;

        // Positions with no snake in it
        private HashSet<Vector2> emptyPositions = new HashSet<Vector2>();

        public SnakeGame() : base("Snake", new System.Drawing.Size(256, 256), System.Windows.Media.Stretch.Uniform)
        {
            Reset();
            //MakeIcon("Icon.png");
        }

        protected override void OnRender(float deltaTime)
        {
            // User inputs
            if (GetKey(System.Windows.Input.Key.Up).IsDown)
                snakeDirection = SnakeDirection.Up;
            if (GetKey(System.Windows.Input.Key.Down).IsDown)
                snakeDirection = SnakeDirection.Down;
            if (GetKey(System.Windows.Input.Key.Right).IsDown)
                snakeDirection = SnakeDirection.Right;
            if (GetKey(System.Windows.Input.Key.Left).IsDown)
                snakeDirection = SnakeDirection.Left;

            lastUpdateDelta += deltaTime;

            if(lastUpdateDelta >= UpdateDelta)
            {
                lastUpdateDelta = 0f;
                if(!UpdateSnake())
                {
                    // Game over
                    VSGame.Settings.Default.SnakeScore = Math.Max(score, VSGame.Settings.Default.SnakeScore);
                    VSGame.Settings.Default.Save();

                    Reset();
                }

                // Draw the changes

                // Checkerboard pattern
                for (int x = 0; x < BufferWidth; x++)
                {
                    for (int y = 0; y < BufferHeight; y++)
                    {
                        SetPixel(x, y, ((x / GridSize) % 2 == (y / GridSize) % 2) ? GridColor1 : GridColor2);
                    }
                }

                // Draw the apple
                DrawGridSquare(applePos, AppleColor);

                // Draw the snake
                foreach(Vector2 pos in snakePositions)
                {
                    DrawGridSquare(pos, SnakeColor);
                }
                
                // Show the score
                DrawString($"Highscore : {VSGame.Settings.Default.SnakeScore}  Score : {score}", new Font("Tahoma", 8), Color.Black, Vector2.Zero);
            }

        }

        private void DrawGridSquare(Vector2 pos, Color color)
        {
            for(int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    SetPixel((int)(pos.X * GridSize) + x, (int)(pos.Y * GridSize) + y, color);
                }
            }
        }

        // Returns false for game over
        private bool UpdateSnake()
        {
            Vector2 newPos = lastHeadPos + DirectionVectors[(int)snakeDirection];
            lastHeadPos = newPos;

            bool ateItself = !emptyPositions.Contains(lastHeadPos);
            emptyPositions.Remove(lastHeadPos);
            snakePositions.Enqueue(newPos);

            // Is the snake on an apple ?
            if(lastHeadPos == applePos)
            {
                score++;
                GetNewApple();
            }
            else
            {
                Vector2 removed = snakePositions.Dequeue();
                emptyPositions.Add(removed);
            }

            // Game over ? (the snake ate itself or out-of-bounds)
            bool outOfBounds = lastHeadPos.X < 0 || lastHeadPos.Y < 0 
                               || lastHeadPos.X >= (BufferWidth / GridSize) || lastHeadPos.Y >= (BufferHeight / GridSize);
            return !(ateItself || outOfBounds);
        }
        
        // Change the position of the apple
        private void GetNewApple()
        {
            applePos = emptyPositions.ElementAt(rng.Next(0, emptyPositions.Count));
        }

        // Reset the map, the player, the score and the apple
        private void Reset()
        {
            emptyPositions.Clear();
            for (int x = 0; x < (BufferWidth / GridSize); x++)
            {
                for (int y = 0; y < (BufferHeight / GridSize); y++)
                {
                    emptyPositions.Add(new Vector2(x, y));
                }
            }

            score = 0;

            snakePositions.Clear();
            snakePositions.Enqueue(new Vector2(BufferWidth / GridSize / 2, BufferHeight / GridSize / 2));
            lastHeadPos = snakePositions.Peek();
            emptyPositions.Remove(lastHeadPos);

            snakeDirection = SnakeDirection.Up;

            GetNewApple();
        }

        // Used to make the icon for the game
        private void MakeIcon(string path)
        {
            char[,] map = new char[4, 4]
            {
                { ' ','S','S','S' },
                { ' ','S',' ',' ' },
                { ' ',' ',' ',' ' },
                { ' ','A',' ',' ' }
            };

            Bitmap bmp = new Bitmap(4 * GridSize, 4 * GridSize);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                for(int x = 0; x < 4; x++)
                {
                    for(int y = 0; y < 4; y++)
                    {
                        Color color = (x % 2 == y % 2) ? GridColor1 : GridColor2;

                        if (map[y, x] == 'S')
                            color = SnakeColor;
                        else if (map[y, x] == 'A')
                            color = AppleColor;

                        g.FillRectangle(new SolidBrush(color), x * GridSize, y * GridSize, GridSize, GridSize);
                    }
                }
            }

            bmp.Save(path);
        }
    }
}
