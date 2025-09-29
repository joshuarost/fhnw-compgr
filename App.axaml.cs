using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Numerics;
using Avalonia.Media;

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

    private void Lab2()
    {
        Vector3 eye = new(0, 0, -4f);
        Vector3 lookAt = new(0, 0, 6);
        const float POV = 36;




        Sphere[] scene = new Sphere[] {
    new(new(-1001f, 0, 0), 1000, new(1, 1, 0, 0)),
    new Sphere(new(1001f, 0, 0), 1000, new(1, 0, 1, 0)),
    new Sphere(new(0, 0, 1001), 1000, new(1, 1, 0, 0)),
    new Sphere(new(0, -1001, 0), 1000, new(1, 1, 0, 0)),
    new Sphere(new(0, 1001, 0), 1000, new(1, 1, 0, 0)),
    new Sphere(new(-0.6f, -0.7f, -0.6f), 0.3f, new(1, 1, 1, 0)),
    new Sphere(new(0.3f, -0.4f, 0.3f), 0.6f, new(1, 0, 1, 1)),
    };
    }

    static EyeRay CreateEyeRay(Vector3 eye, Vector3 lookAt, float pov, Vector2 pixel)
    {
        var f = lookAt - eye;
        Vector3 up = new(0, 1, 0);
        var r = Vector3.Cross(f, up);
        var u = Vector3.Cross(f, r);

        float beta = (float)Math.Tan(pov / 2 * pixel.Y);
        float w = (float)Math.Tan(pov / 2 * pixel.X);

        var d = Vector3.Normalize(f) + beta * Vector3.Normalize(u) + w * Vector3.Normalize(r);

        return new EyeRay(eye, d);
    }

    static Vector3 FindClosestHitPoint(Sphere[] scene, Vector3 o, Vector3 d)
    {
        return new(0, 0, 0);
    }

    static Color ComputeColor(Sphere[] scene, Vector3 o, Vector3 d)
    {
        return new(1, 1, 1, 1);
    }

    void Lab1()
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




class EyeRay
{
    public Vector3 o;
    public Vector3 d;

    public EyeRay(Vector3 o, Vector3 d)
    {
        this.o = o;
        this.d = d;
    }
}

class Sphere
{
    public Vector3 center;
    public float r;
    public Color color;

    public Sphere(Vector3 center, float r, Color color)
    {
        this.center = center;
        this.r = r;
        this.color = color;
    }
}