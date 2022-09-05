using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VSGames.Games
{
    public class KeyState
    {
        public bool IsDown;
        public bool IsJustDown;
        public bool IsJustUp;
    }

    public abstract class GameWindow : ToolWindowPane
    {
        private Stopwatch deltaTimeStopWatch = new Stopwatch();
        private Label fpsCounter;

        private WriteableBitmap bitmap; // Used to draw to the screen
        private bool bitmapLocked = false;
        private IntPtr bitmapBuffer;
        private System.Drawing.Bitmap regularBitmap;

        private Dictionary<Key, KeyState> keyStates = new Dictionary<Key, KeyState>();

        public event KeyEventHandler OnKeyDown;
        public event KeyEventHandler OnKeyUp;

        public int BufferWidth => bitmap.PixelWidth;
        public int BufferHeight => bitmap.PixelHeight;


        /// <param name="name">The name in the title bar</param>
        /// <param name="screenBufferSize">The size of the screen buffer (does not need to match the size of the window)</param>
        public GameWindow(string name, System.Drawing.Size screenBufferSize, Stretch stretchMode = Stretch.Fill) : base(null)
        {
            this.Caption = name;

            // Inputs
            var inputs = Enum.GetValues(typeof(Key));
            foreach (Key input in inputs)
            {
                if (!keyStates.ContainsKey(input)) // Lots of duplicate in Key
                    keyStates.Add(input, new KeyState());
            }
            keyStates.Remove(Key.None); // Will crash when calling IsDown

            Image image = new Image();
            image.Focusable = true;
            image.Focus();

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(image, EdgeMode.Unspecified);

            fpsCounter = new Label();
            fpsCounter.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            Grid grid = new Grid();
            grid.Children.Add(image);
            grid.Children.Add(fpsCounter);
            this.Content = grid;

            bitmap = new WriteableBitmap((int)screenBufferSize.Width, (int)screenBufferSize.Height, 96, 96, PixelFormats.Bgr32, null);

            image.Source = bitmap;

            image.Stretch = stretchMode;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Center;

            regularBitmap = new System.Drawing.Bitmap(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format32bppRgb, bitmap.BackBuffer);

            deltaTimeStopWatch.Start();
            CompositionTarget.Rendering += Loop;
        }
        
        private void Loop(object sender, EventArgs e)
        {
            // DeltaTime + FPS counter
            float deltaTime = (float)deltaTimeStopWatch.Elapsed.TotalSeconds;
            deltaTimeStopWatch.Restart();
            fpsCounter.Content = $"{(int)(1f / deltaTime)} fps";

            // Update Keys
            foreach(var pair in keyStates)
            {
                bool newVal = Keyboard.IsKeyDown(pair.Key);

                if (pair.Value.IsDown == false && newVal == true)
                    pair.Value.IsJustDown = true;
                else
                    pair.Value.IsJustDown = false;

                if(pair.Value.IsDown == true && newVal == false)
                    pair.Value.IsJustUp = true;
                else
                    pair.Value.IsJustUp = false;

                pair.Value.IsDown = newVal;
            }

            try
            {
                bitmap.Lock(); // Reserve the back buffer for updates
                bitmapLocked = true;

                bitmapBuffer = bitmap.BackBuffer;

                OnRender(deltaTime); // Draw

                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight)); // Make the whole bitmap dirty
            }
            finally
            {
                bitmapLocked = false;
                bitmap.Unlock(); // Release the back buffer and make it available for display
            }
        }

        protected abstract void OnRender(float deltaTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetPixel(int x, int y, System.Drawing.Color color)
        {
            if(bitmapLocked)
            {
                unsafe
                {
                    *((int*)(bitmapBuffer + (x * 4) + (y * bitmap.BackBufferStride))) = GetColorInt(color);
                }
            }
        }

        protected void DrawString(string str, System.Drawing.Font font, System.Drawing.Color color, System.Numerics.Vector2 pos)
        {
            if (bitmapLocked)
            {
                using (var g = System.Drawing.Graphics.FromImage(regularBitmap))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    g.DrawString(str, font, new System.Drawing.SolidBrush(color), pos.X, pos.Y);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetColorInt(System.Drawing.Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }

        protected KeyState GetKey(Key key) => keyStates.TryGetValue(key, out KeyState state) ? state : new KeyState();
    }
}
