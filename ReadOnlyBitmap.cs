using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSGame
{
    // Faster than Bitmap.GetPixel()
    internal class ReadOnlyBitmap
    {
        private Bitmap bitmap;
        private BitmapData bitmapData;

        public int Width => bitmapData.Width;
        public int Height => bitmapData.Height;

        public ReadOnlyBitmap(Bitmap _bitmap)
        {
            bitmap = _bitmap;
            bitmapData = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        }

        ~ReadOnlyBitmap()
        {
            bitmap.UnlockBits(bitmapData);
        }

        public Color GetPixel(int x, int y)
        {
            unsafe
            {
                int* ptr = (int*)bitmapData.Scan0;
                ptr += x; // PixelFormat.Format32bppArgb
                ptr += y * (bitmapData.Stride / sizeof(int));

                int color = *ptr;
                return Color.FromArgb((int)((color & 0xff000000) >> 24), (color & 0xff0000) >> 16, (color & 0xff00) >> 8, color & 0xff);
            }
        }
    }
}
