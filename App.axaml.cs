using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Numerics;

namespace fhnw_compgr;

public partial class App : Application
{

    private readonly int WIDTH = 400;
    private readonly int HEIGHT = 400;


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        Lab1();
    }

    private void Lab1()
    {
        var window = new Window
        {
            Width = WIDTH,
            Height = HEIGHT,
            Title = "LAB 1"
        };

        var image = new Image();
        window.Content = image;

        var bitmap = new WriteableBitmap(
            new PixelSize(WIDTH, HEIGHT),
            new Avalonia.Vector(96, 96), // Fully qualified to avoid ambiguity
            PixelFormat.Bgra8888
        );

        unsafe
        {
            using var fb = bitmap.Lock();
            uint* fstPxl = (uint*)fb.Address;

            // colors
            Vector3 left = SRGBToLinear(new(1f, 0f, 0f));   // Red
            Vector3 right = SRGBToLinear(new(0f, 1f, 0f));  // Green

            for (int x = 0; x < WIDTH; x++)
            {
                // Interpolation factor
                float tx = x / (float)(WIDTH - 1);
                Vector3 linear = Vector3.Lerp(left, right, tx);

                Vector3 srgb = LinearToSRGB(linear);
                for (int y = 0; y < HEIGHT; y++)
                {
                    int offset = y * (fb.RowBytes / 4) + x;
                    fstPxl[offset] = Vector3ToPixel(srgb);
                }
            }
        }

        image.Source = bitmap;
        window.Show();
        base.OnFrameworkInitializationCompleted();
    }


    // Returns a BGRA Pixel
    static uint Vector3ToPixel(Vector3 v, byte alpha = 255)
    {
        byte r = (byte)(Math.Clamp(v.X, 0, 1) * 255);
        byte g = (byte)(Math.Clamp(v.Y, 0, 1) * 255);
        byte b = (byte)(Math.Clamp(v.Z, 0, 1) * 255);
        return (uint)(b | (g << 8) | (r << 16) | (alpha << 24));
    }

    static Vector3 SRGBToLinear(Vector3 srgb)
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
}