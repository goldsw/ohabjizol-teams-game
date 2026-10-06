using System;

namespace Ohabjizol.Managers
{
    /// <summary>
    /// A width and height choice. Applying it is <see cref="WindowManager"/>'s job.
    /// </summary>
    public struct Resolution
    {
        public Resolution(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be > 0.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be > 0.");

            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }
    }
}
