using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Numerics;

namespace fhnw_compgr;

public partial class App : Application
{

    private int WIDTH = 400;
    private int HEIGHT = 400;


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
            byte* ptr = (byte*)fb.Address;
            int stride = fb.RowBytes;

            for (int x = 0; x < WIDTH; x++)
            {
                float tx = x / (float)(WIDTH - 1);

                // colors
                Vector3 left = new(1f, 0f, 0f);   // Red
                Vector3 right = new(0f, 1f, 0f);  // Green

                // Convert to linear RGB
                left = SRGBToLinear(left);
                right = SRGBToLinear(right);

                // Bilinear interpolation in linear space
                Vector3 linear = Vector3.Lerp(left, right, tx);

                // Convert back to sRGB
                Vector3 srgb = LinearToSRGB(linear);

                byte r = (byte)(Math.Clamp(srgb.X, 0, 1) * 255);
                byte g = (byte)(Math.Clamp(srgb.Y, 0, 1) * 255);
                byte b = (byte)(Math.Clamp(srgb.Z, 0, 1) * 255);
                byte a = 255;

                int offset = stride + x * 4;
                ptr[offset + 0] = b; // Blue
                ptr[offset + 1] = g; // Green
                ptr[offset + 2] = r; // Red
                ptr[offset + 3] = a; // Alpha
            }
        }

        image.Source = bitmap;
        window.Show();
        base.OnFrameworkInitializationCompleted();
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