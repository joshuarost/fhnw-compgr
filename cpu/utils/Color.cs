using System;
using System.Numerics;

namespace utils;

public static class Color
{
    // Returns a BGRA Pixel
    public static uint Vector3ToPixel(Vector3 v, byte alpha = 255)
    {
        v = LinearToSRGB(v);
        byte r = (byte)(Math.Clamp(v.X, 0, 1) * 255);
        byte g = (byte)(Math.Clamp(v.Y, 0, 1) * 255);
        byte b = (byte)(Math.Clamp(v.Z, 0, 1) * 255);
        return (uint)(b | (g << 8) | (r << 16) | (alpha << 24));
    }

    static Vector3 LinearToSRGB(Vector3 linear)
    {
        const float boundry = 0.0031308f;
        Vector3 srgb = new();
        for (int i = 0; i < 3; i++)
        {
            float r = linear[i];
            if (r <= boundry)
                srgb[i] = r * 12.92f;
            else
                srgb[i] = 1.055f * MathF.Pow(r, 1 / 2.4f) - 0.055f;
        }
        return srgb;
    }

    public static Vector3 SRGBToLinear(Vector3 srgb)
    {
        const float boundry = 0.04045f;
        Vector3 linear = new();
        for (int i = 0; i < 3; i++)
        {
            float r = srgb[i];
            if (r <= boundry)
                linear[i] = r / 12.92f;
            else
                linear[i] = MathF.Pow((r + 0.055f) / 1.055f, 2.4f);
        }
        return linear;
    }
}