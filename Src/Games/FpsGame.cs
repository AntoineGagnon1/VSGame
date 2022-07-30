using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VSGames.Games
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("7cae38a8-a631-4ba5-a37e-098df7db38c2")]
    public class FpsGame : GameWindow
    {
        private static readonly double Deg2Rad = Math.PI / 180.0;

        private static readonly System.Drawing.Color BackColor = System.Drawing.Color.FromArgb(0, 0, 0);
        private static readonly System.Drawing.Color FloorColor = System.Drawing.Color.FromArgb(97, 97, 97);
        private static readonly int RenderDistance = 10; // In Cells
        private static readonly float FOV = 60; // In Degrees
        private static readonly int CellHeight = 1;

        private ReadOnlyBitmap wallTexture;

        // Empty = 0
        // Wall = 1
        private static int[,] map = new int[10, 10] {
            { 0,0,0,0,0,0,0,0,0,0 },
            { 0,1,0,1,0,1,0,1,0,0 },
            { 0,0,0,0,0,0,0,0,1,0 },
            { 0,1,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,1,0,0,0,1,0 },
            { 0,1,0,0,1,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,1,0 },
            { 0,1,0,0,0,0,0,0,0,0 },
            { 0,0,1,0,1,0,1,0,1,0 },
            { 0,0,0,0,0,0,0,0,0,0 }
        };

        private static readonly float RotSpeed = 50f;
        private static readonly float MoveSpeed = 2f;

        private float cameraRot = 0f; // In Degrees
        private Vector2 cameraPos = new Vector2(0, 5); // Starts at 0,5


        public FpsGame() : base("Shooter Game", new System.Drawing.Size(512, 256))
        {
            wallTexture = new ReadOnlyBitmap((System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile("Resources/Fps/Wall.png")); // From http://opengameart.org/content/dungeon-crawl-32x32-tiles
        }

        protected override void OnRender(float deltaTime)
        {
            // Update the Player
            if (GetKey(System.Windows.Input.Key.Right).IsDown)
                cameraRot += RotSpeed * deltaTime;
            else if (GetKey(System.Windows.Input.Key.Left).IsDown)
                cameraRot -= RotSpeed * deltaTime;

            Vector2 lookDir = Vector2.Normalize(new Vector2((float)Math.Cos(cameraRot * Deg2Rad), (float)Math.Sin(cameraRot * Deg2Rad)));
            if (GetKey(System.Windows.Input.Key.Up).IsDown)
                cameraPos += lookDir * MoveSpeed * deltaTime;
            else if (GetKey(System.Windows.Input.Key.Down).IsDown)
                cameraPos -= lookDir * MoveSpeed * deltaTime;

            RenderScene(cameraRot, cameraPos);
        }


        private void RenderScene(float cameraRotation, Vector2 cameraOffset)
        {
            for (int x = 0; x < BufferWidth; x++)
            {
                // Cast a ray
                float stepSize = 0.01f;


                float lookAngle = cameraRotation;
                lookAngle += (x - (BufferWidth / 2)) * (FOV / (float)BufferWidth);

                Vector2 lookDir = Vector2.Normalize(new Vector2((float)Math.Cos(lookAngle * Deg2Rad), (float)Math.Sin(lookAngle * Deg2Rad)));

                float dist = 0; // Distance traveled by the ray
                int hit = 0; // Id of the object that was hit
                bool outOfMap = false;
                float textureX = 0f; // Texture sample position
                while (dist <= RenderDistance)
                {
                    Vector2 checkPos = lookDir * dist + cameraOffset;

                    int checkX = (int)Math.Floor(checkPos.X);
                    int checkY = (int)Math.Floor(checkPos.Y);
                    if (checkX < 0 || checkX >= map.GetLength(0) || checkY < 0 || checkY >= map.GetLength(1))
                    {
                        outOfMap = true;
                        break;
                    }

                    if (map[checkX, checkY] != 0)
                    {
                        hit = map[checkX, checkY];

                        // Get the side the ray intersected
                        float tan = (float)Math.Atan2((checkX + 0.5f) - checkPos.X, (checkY + 0.5f) - checkPos.Y);

                        if (tan <= Math.PI * 0.25f && tan > -Math.PI * 0.25f)
                            textureX = (checkX + 1f) - checkPos.X; // Top
                        else if (tan <= -Math.PI * 0.25f && tan > -Math.PI * 0.75f)
                            textureX = (checkY + 1f) - checkPos.Y; // Left
                        else if (tan > Math.PI * 0.25f && tan <= Math.PI * 0.75f)
                            textureX = checkPos.Y - checkY; // Right
                        else if (tan > Math.PI * 0.75f || tan <= -Math.PI * 0.75f)
                            textureX = checkPos.X - checkX; // Bottom
                        else
                            textureX = 1;

                        break;
                    }

                    dist += stepSize;
                }

                dist *= (float)Math.Cos(((x - (BufferWidth / 2)) * (FOV / (float)BufferWidth)) * Deg2Rad); // Fish eye correction

                int height = (int)((float)(CellHeight / dist) * BufferHeight);
                int middle = BufferHeight / 2;
                int start = middle - height / 2;
                int end = start + height;

                float lightAmount = 1.0f - (dist / RenderDistance); // Shade based on distance

                for (int y = 0; y < BufferHeight; y++)
                {
                    // Get the color from the hit and distance
                    Color color = BackColor;

                    if ((y > middle && !outOfMap)
                        || (outOfMap && y > end)) // No floor when out of the map
                        color = FloorColor;

                    if (y >= start && y <= end)
                    {
                        float textureY = ((float)y - (float)start) / (float)height;

                        if (hit == 1)
                        {
                            // Get the color from the texure
                            color = wallTexture.GetPixel((int)Math.Round(textureX * (wallTexture.Width - 1)), (int)Math.Round(textureY * (wallTexture.Height - 1)));
                        }
                    }

                    // Set the pixel
                    SetPixel(x, y, color);
                }
            }
        }
    }
}
