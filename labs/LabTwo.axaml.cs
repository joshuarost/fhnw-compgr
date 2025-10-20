using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia;
using System;
using utils;
using System.Numerics;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace fhnw_compgr.labs;

public partial class LabTwo : Window
{
    private readonly int WIDTH = 600, HEIGHT = 600;
    private readonly DispatcherTimer timer = new();
    private float angle = 0;
    private readonly WriteableBitmap framebuffer;

    public LabTwo()
    {
        InitializeComponent();

        framebuffer = new WriteableBitmap(
            new PixelSize(WIDTH, HEIGHT),
            new Avalonia.Vector(96, 96), // Fully qualified to avoid ambiguity
            PixelFormat.Bgra8888
        );

        MainImage.Source = framebuffer;

        timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        timer.Tick += OnRenderFrame;
        timer.Start();
    }

    private unsafe void OnRenderFrame(object? sender, EventArgs e)
    {
        using var fb = framebuffer.Lock();
        uint* pixels = (uint*)fb.Address;
        int stride = fb.RowBytes / 4;

        // Clear screen
        for (int i = 0; i < HEIGHT * stride; i++)
            pixels[i] = 0xFF000000; // black

        RenderCube(pixels, stride);
        MainImage.InvalidateVisual();
        DebugLabel.Text = $"Angle: {angle:F2}";
    }

    private unsafe void SetPixel(uint* buffer, int x, int y, uint color, int stride)
    {
        if (x < 0 || y < 0 || x >= WIDTH || y >= HEIGHT) return;
        buffer[y * stride + x] = color;
    }

    private unsafe void RenderCube(uint* pixels, int stride)
    {
        angle += 0.02f; // Speed
        var MVP = CreateMVPMatrix(angle);

        var cube = Mesh.CreateCube(
            Vector3.One,
            Vector3.One,
            Vector3.One,
            Vector3.One,
            Vector3.One,
            Vector3.One
        );

        cube.Tris.ForEach(tri =>
        {
            var v1 = Project(VertexShader(cube.Vertices[tri.A], MVP));
            var v2 = Project(VertexShader(cube.Vertices[tri.B], MVP));
            var v3 = Project(VertexShader(cube.Vertices[tri.C], MVP));

            var p1 = ConvertToPixels(v1.Position);
            var p2 = ConvertToPixels(v2.Position);
            var p3 = ConvertToPixels(v3.Position);

            var face = Vector3.Cross(
              new Vector3(p1.X - p2.X, p1.Y - p2.Y, 0),
              new Vector3(p3.X - p1.X, p3.Y - p1.Y, 0)
            );

            if (face.Z < 0)
                return;

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (IsPointInTriangle(p1, p2, p3, new(x, y)))
                    {
                        SetPixel(pixels, x, y, 0xFFFF0000, stride); // BLACK
                    }
                }
            }
        });
    }

    private static Vector2 TriangleIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var ab = b - a;
        var ac = c - a;

        var inverse = 1 / (ab.X * ac.Y - ab.Y * ac.X);
        var matrix = new Vector2(ac.Y - ac.X, ab.Y - ab.X);
        p = new(p.X - a.X, p.Y - a.Y);
        return inverse * matrix * p;
    }

    private static bool IsPointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var uv = TriangleIntersection(a, b, c, p);
        return uv.X >= 0 && uv.Y >= 0 && (uv.X + uv.Y) < 1;
    }

    private static Vertex Project(Vertex v)
    {
        return 1 / v.Position.W * v;
    }

    private static Vertex VertexShader(Vertex v, Matrix4x4 mvp)
    {
        return v with
        {
            Position = Vector4.Transform(v.Position, mvp)
        };
    }

    private Vector2 ConvertToPixels(Vector4 position)
    {
        var wHalf = WIDTH / 2f;
        var hHalf = HEIGHT / 2f;
        return new Vector2(
            position.X * wHalf + wHalf,
            position.Y * hHalf + hHalf
        );
    }

    private static Matrix4x4 CreateMVPMatrix(float angle)
    {
        var M = Matrix4x4.CreateRotationY(angle);
        M *= Matrix4x4.CreateRotationX(angle / 2);

        var V = Matrix4x4.CreateLookAt(
            new(0, 0, -10),
            Vector3.Zero,
            new(0, -1, 0)
        );
        var zNear = 0.1f;
        var zFar = 100f;

        var P = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,
            1,
            zNear,
            zFar
        );

        return M * V * P;
    }
}