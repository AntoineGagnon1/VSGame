using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VSGames
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
    public class FpsGame : ToolWindowPane
    {
        Stopwatch stopWatch = new Stopwatch();
        WriteableBitmap bitmap;
        

        private static readonly double Deg2Rad = Math.PI / 180.0;

        private static readonly int BackColor = GetColorInt(0,0,0);
        private static readonly int FloorColor = GetColorInt(97,97,97);
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

        private float rotateInput = 0;
        private float moveInput = 0f;

        Label fpsCounter;

        public FpsGame() : base(null)
        {
            this.Caption = "GameView";

            wallTexture = new ReadOnlyBitmap((System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile("Resources/Fps/Wall.png")); // From http://opengameart.org/content/dungeon-crawl-32x32-tiles

            Image image = new Image();
            image.KeyDown += KeyDown;
            image.KeyUp += KeyUp;
            image.Focusable = true;
            image.Focus();

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

            fpsCounter = new Label();
            fpsCounter.Foreground = new SolidColorBrush(Color.FromArgb(255, 255,255,255));

            Grid grid = new Grid();
            grid.Children.Add(image);
            grid.Children.Add(fpsCounter);
            this.Content = grid;

            bitmap = new WriteableBitmap(
                512,
                256,
                96,
                96,
                PixelFormats.Bgr32,
                null);

            image.Source = bitmap;

            image.Stretch = Stretch.Fill;
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;

            
            stopWatch.Start();
            CompositionTarget.Rendering += Loop;
        }

        private void KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Right || e.Key == System.Windows.Input.Key.Left)
                rotateInput = 0f;

            if (e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Down)
                moveInput = 0f;
        }

        private void KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Right)
                rotateInput = 1f;
            else if (e.Key == System.Windows.Input.Key.Left)
                rotateInput = -1f;

            if (e.Key == System.Windows.Input.Key.Up)
                moveInput = 1f;
            else if (e.Key == System.Windows.Input.Key.Down)
                moveInput = -1f;
        }


        private void Loop(object sender, EventArgs e)
        {
            float deltaTime = (float)stopWatch.Elapsed.TotalSeconds;
            stopWatch.Restart();

            cameraRot += RotSpeed * rotateInput * deltaTime;
            Vector2 lookDir = Vector2.Normalize(new Vector2((float)Math.Cos(cameraRot * Deg2Rad), (float)Math.Sin(cameraRot * Deg2Rad)));
            cameraPos += lookDir * moveInput * MoveSpeed * deltaTime;

            RenderScene(cameraRot, cameraPos);
            fpsCounter.Content = $"{(int)(1f/deltaTime)} fps";
        }


        private void RenderScene(float cameraRotation, Vector2 cameraOffset)
        {
            try
            {
                // Reserve the back buffer for updates
                bitmap.Lock();

                unsafe
                {

                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        // Cast a ray
                        float stepSize = 0.01f;


                        float lookAngle = cameraRotation;
                        lookAngle += (x - (bitmap.PixelWidth / 2)) * (FOV / (float)bitmap.PixelWidth);

                        Vector2 lookDir = Vector2.Normalize(new Vector2((float)Math.Cos(lookAngle * Deg2Rad), (float)Math.Sin(lookAngle * Deg2Rad)));

                        float dist = 0; // Distance traveled by the ray
                        int hit = 0; // Id of the object that was hit
                        bool outOfMap = false;
                        float textureX = 0f; // Texture sample position
                        while(dist <= RenderDistance)
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

                        IntPtr pBackBuffer = bitmap.BackBuffer;
                        pBackBuffer += x * 4; // x offset

                        dist *= (float)Math.Cos(((x - (bitmap.PixelWidth / 2)) * (FOV / (float)bitmap.PixelWidth)) * Deg2Rad); // Fish eye correction

                        int height = (int)((float)(CellHeight / dist) * bitmap.PixelHeight);
                        int start = (bitmap.PixelHeight / 2) - height / 2;
                        int middle = bitmap.PixelHeight / 2;
                        int end = start + height;

                        float lightAmount = 1.0f - (dist / RenderDistance); // Shade based on distance

                        for (int y = 0; y < bitmap.PixelHeight; y++)
                        {
                            // Get the color from the hit and distance
                            int color = BackColor;

                            if ((y > middle && !outOfMap) 
                                || (outOfMap && y > end)) // No floor when out of the map
                                color = FloorColor;

                            if (y >= start && y <= end)
                            {
                                float textureY = ((float)y - (float)start) / (float)height;

                                if (hit == 1)
                                {
                                    // Get the color from the texure
                                    color = GetColorInt(wallTexture.GetPixel((int)Math.Round(textureX * (wallTexture.Width - 1)), (int)Math.Round(textureY * (wallTexture.Height - 1))));
                                }
                            }

                            // Set the color in the bitmap
                            *((int*)pBackBuffer) = color;
                            pBackBuffer += bitmap.BackBufferStride; // Go one line lower
                        }
                    }
                }

                // Specify the area of the bitmap that changed
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                // Release the back buffer and make it available for display
                bitmap.Unlock();
            }
        }


        private static int GetColorInt(byte r, byte g, byte b)
        {
            return (r << 16) | (g << 8) | b;
        }

        private static int GetColorInt(System.Drawing.Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }
    }
}
