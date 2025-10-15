using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Numerics;

namespace fhnw_compgr.labs;

public partial class LabOne : Window
{
    static readonly Random rand = new();
    private readonly int WIDTH;
    private readonly int HEIGHT;

    public LabOne()
    {
        InitializeComponent();
        WIDTH = (int)Width;
        HEIGHT = (int)Height;

        var bitmap = new WriteableBitmap(
                    new PixelSize(WIDTH, HEIGHT),
                    new Avalonia.Vector(96, 96), // Fully qualified to avoid ambiguity
                    PixelFormat.Bgra8888
                );
        unsafe
        {
            using var fb = bitmap.Lock();
            uint* fstPxl = (uint*)fb.Address;
            Lab2(fb, fstPxl);
        }
        MainImage.Source = bitmap;
    }
    private unsafe void Lab2(ILockedFramebuffer? fb, uint* fstPxl)
    {
        Vector3 eye = new(0, 0, -4f);
        Vector3 lookAt = new(0, 0, 6);
        const float POV = 36;

        // string path = Path.Combine(Environment.CurrentDirectory, "Assets", "tile.jpg");
        var tileTexture = new Bitmap(AssetLoader.Open(new Uri("avares://fhnw-compgr/Assets/chess.png")));

        Sphere[] scene = [
            new Sphere(new(-1001f, 0, 0), 1000, new Vector3(1, 0, 0), Vector3.Zero), // Red
            new Sphere(new(1001f, 0, 0), 1000, new Vector3(0, 0, 1), Vector3.Zero),  // Blue
            new Sphere(new(0, 0, 1001), 1000, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero),    // Gray
            new Sphere(new(0, -1001, 0), 1000, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero),   // Gray
            new Sphere(new(0, 1001, 0), 1000, Vector3.One, 2 * Vector3.One),    // White
            new Sphere(new(-0.6f, -0.7f, -0.6f), 0.3f, new Vector3(1, 1, 0), Vector3.Zero, 1f), // Yellow
            new Sphere(new(0.3f, -0.4f, 0.3f), 0.6f, new Vector3(0, 1, 1), Vector3.Zero, 1f),   // Light Cyan
            new Sphere(new(0f, -0.7f, -0.2f), 0.2f, Vector3.Zero, Vector3.Zero, 0, tileTexture) // texture
        ];

        const int SAMPLES_PER_FRAME = 20;

        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                Vector3 pixelColor = Vector3.Zero;

                for (int s = 0; s < SAMPLES_PER_FRAME; s++)
                {
                    float ndcX = -(2f * (x + 0.5f) / WIDTH - 1f) * (WIDTH / (float)HEIGHT);
                    float ndcY = 2f * (y + 0.5f) / HEIGHT - 1f;
                    var ray = CreateEyeRay(eye, lookAt, POV, new(ndcX, ndcY));
                    pixelColor += ComputeColor(scene, ray.o, ray.d);
                }
                pixelColor /= SAMPLES_PER_FRAME;

                int offset = y * (fb!.RowBytes / 4) + x;
                fstPxl[offset] = Vector3ToPixel(pixelColor);
            }
        }
    }

    static EyeRay CreateEyeRay(Vector3 eye, Vector3 lookAt, float pov, Vector2 pixel)
    {
        var f = lookAt - eye;
        Vector3 up = new(0, 1, 0);
        var r = Vector3.Cross(f, up);
        var u = Vector3.Cross(f, r);

        float fov = pov * (float)Math.PI / 180f;
        float scale = (float)Math.Tan(fov / 2);
        float beta = scale * pixel.Y;
        float w = scale * pixel.X;

        var d = Vector3.Normalize(f) + beta * Vector3.Normalize(u) + w * Vector3.Normalize(r);

        return new EyeRay(eye, d);
    }

    static HitPoint? FindClosestHitPoint(Sphere[] scene, Vector3 o, Vector3 d)
    {
        float closestT = float.MaxValue;
        HitPoint? closestPoint = null;

        foreach (var sphere in scene)
        {
            Vector3 co = o - sphere.center;
            float a = Vector3.Dot(d, d);
            float b = 2 * Vector3.Dot(co, d);
            float c = Vector3.Dot(co, co) - sphere.r * sphere.r;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) // no intersection
                continue;

            float sqrtDisc = MathF.Sqrt(discriminant);
            float t1 = (-b - sqrtDisc) / (2 * a);
            float t2 = (-b + sqrtDisc) / (2 * a);

            float t = (t1 >= 0) ? t1 : t2;
            if (t >= 0 && t < closestT)
            {
                closestT = t;
                closestPoint = new HitPoint(o + t * d, sphere);
            }
        }

        return closestPoint;
    }

    static Vector3 ComputeColor(Sphere[] scene, Vector3 o, Vector3 d, int depth = 0)
    {
        if (depth > 5)
            return Vector3.Zero; // terminate recursion

        var hitpoint = FindClosestHitPoint(scene, o, d);
        if (!hitpoint.HasValue)
            return Vector3.Zero; // Background color (black)

        var sphere = hitpoint.Value.sphere;
        var n = Vector3.Normalize(hitpoint.Value.Normal);

        if (sphere.texture != null)
        {
            var uv = SphericalProjection(hitpoint.Value.Normal);
            var texture = GetTexture(sphere.texture, uv);
            sphere.diffuse = texture;
        }

        const float p = 0.2f;
        if ((float)rand.NextDouble() < p)
            return sphere.emission; // terminate

        var r = SampleRandomDirection(n);
        Vector3 Li = ComputeColor(scene, hitpoint.Value.position + n * 0.001f, r, depth + 1);
        var fr = BRDF(d, r, n, sphere);
        var pdf = 1f / (2f * MathF.PI);
        return sphere.emission + fr * (Vector3.Dot(r, n) / pdf) * Li;
    }

    static Vector3 SampleRandomDirection(Vector3 n)
    {
        // random number between -1 and 1
        float x = (float)(rand.NextDouble() * 2 - 1);
        float y = (float)(rand.NextDouble() * 2 - 1);
        float z = (float)(rand.NextDouble() * 2 - 1);
        Vector3 r = new(x, y, z);
        if (r.Length() > 1) // outside of unit sphere
            return SampleRandomDirection(n); // try again
        if (Vector3.Dot(r, n) < 0) // below the surface
            r = -r; // flip to the upper hemisphere
        return Vector3.Normalize(r);
    }

    static Vector3 BRDF(Vector3 d, Vector3 wo, Vector3 n, Sphere sphere)
    {
        var diffused = sphere.diffuse * (1.0f / MathF.PI);
        if (sphere.specular > 0)
        {
            var r = Vector3.Reflect(Vector3.Normalize(d), Vector3.Normalize(n));
            if (Vector3.Dot(Vector3.Normalize(wo), r) > 1f - 0.01f) // Almost aligned
                return diffused + (Vector3.One * 10f * sphere.specular);
        }
        return diffused;
    }

    static Vector2 SphericalProjection(Vector3 n)
    {
        var s = MathF.Atan2(n.X, n.Z);
        var t = MathF.Acos(n.Y);
        return new Vector2(s, t);
    }

    unsafe static Vector3 GetTexture(Bitmap texture, Vector2 uv)
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
        return SRGBToLinear(new Vector3(r, g, b));
    }


    unsafe void Lab1(ILockedFramebuffer? fb, uint* fstPxl)
    {
        // colors
        Vector3 left = new(1f, 0f, 0f);   // Red
        Vector3 right = new(0f, 1f, 0f);  // Green

        for (int x = 0; x < WIDTH; x++)
        {
            // Interpolation factor
            float tx = x / (float)(WIDTH - 1);
            Vector3 linear = Vector3.Lerp(left, right, tx);

            for (int y = 0; y < HEIGHT; y++)
            {
                // var linear = Experiment(x, y);
                // var linear = Experiment2(x, y);
                int offset = y * (fb.RowBytes / 4) + x;
                fstPxl[offset] = Vector3ToPixel(linear);
            }
        }
    }

    Vector3 Experiment(float x, float y)
    {
        var fx = MathF.Sin(x / 20 + MathF.Cos(y * 0.03f)) * 0.4f + 0.4f;
        var fy = MathF.Cos(y / 20 + MathF.Sin(x * 0.03f)) * 0.4f + 0.4f;
        var f = MathF.Tan((fx + fy) * MathF.PI / 2) * 0.4f + 0.4f;
        return SRGBToLinear(new Vector3(fx, fy, f));
    }

    Vector3 Experiment2(float x, float y)
    {
        var fx = MathF.Sin(x / 20 + MathF.Cos(y * 0.03f)) * 0.4f + 0.4f;
        var fy = MathF.Cos(y / 20 + MathF.Sin(x * 0.03f)) * 0.4f + 0.4f;
        var f = MathF.Tan((fx + fy) * MathF.PI / 2) * 0.4f + 0.4f;
        return SRGBToLinear(new Vector3(fx, fy, f));
    }

    // Returns a BGRA Pixel
    static uint Vector3ToPixel(Vector3 v, byte alpha = 255)
    {
        v = LinearToSRGB(v);
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

struct EyeRay(Vector3 o, Vector3 d)
{
    public Vector3 o = o;
    public Vector3 d = d;
}

struct Sphere(Vector3 center, float r, Vector3 diffuse, Vector3 emission, float specular = 0, Bitmap? texture = null)
{
    public Vector3 center = center;
    public float r = r;
    public Vector3 diffuse = diffuse;
    public Vector3 emission = emission;
    public float specular = specular;
    public Bitmap? texture = texture;
}

struct HitPoint(Vector3 position, Sphere sphere)
{
    public Vector3 position = position;
    public Sphere sphere = sphere;
    public readonly Vector3 Normal => Vector3.Normalize(position - sphere.center);
}