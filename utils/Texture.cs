using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media.Imaging;

using utils;

public static class Texture
{
    public static Vector3 BilinearSample(Bitmap texture, Vector2 uv)
    {
        uv.X -= MathF.Floor(uv.X);
        uv.Y -= MathF.Floor(uv.Y);

        int w = (int)texture.Size.Width;
        int h = (int)texture.Size.Height;

        float x = uv.X * (w - 1);
        float y = (1f - uv.Y) * (h - 1);

        int x0 = (int)MathF.Floor(x);
        int x1 = Math.Min(x0 + 1, w - 1);
        int y0 = (int)MathF.Floor(y);
        int y1 = Math.Min(y0 + 1, h - 1);

        Vector3 c00 = GetTexture(texture, new Vector2((float)x0 / w, 1f - (float)y0 / h));
        Vector3 c10 = GetTexture(texture, new Vector2((float)x1 / w, 1f - (float)y0 / h));
        Vector3 c01 = GetTexture(texture, new Vector2((float)x0 / w, 1f - (float)y1 / h));
        Vector3 c11 = GetTexture(texture, new Vector2((float)x1 / w, 1f - (float)y1 / h));

        float tx = x - x0;
        float ty = y - y0;

        Vector3 c0 = Vector3.Lerp(c00, c10, tx);
        Vector3 c1 = Vector3.Lerp(c01, c11, tx);
        return Vector3.Lerp(c0, c1, ty);
    }

    public unsafe static Vector3 GetTexture(Bitmap texture, Vector2 uv)
    {
        uv.X -= MathF.Floor(uv.X);
        uv.Y -= MathF.Floor(uv.Y);

        int w = (int)texture.Size.Width;
        int h = (int)texture.Size.Height;

        int x = (int)(uv.X * (w - 1));
        int y = (int)((1f - uv.Y) * (h - 1));

        byte[] buffer = new byte[4]; // Avalonia uses RGBA
        fixed (byte* p = buffer)
        {
            var rect = new PixelRect(x, y, 1, 1);
            int bufferSize = buffer.Length;   // 4
            int stride = 4;                   // 1 pixel * 4 bytes
            texture.CopyPixels(rect, (nint)p, bufferSize, stride);
        }

        float r = buffer[0] / 255f;
        float g = buffer[1] / 255f;
        float b = buffer[2] / 255f;
        return Color.SRGBToLinear(new Vector3(r, g, b));
    }
}