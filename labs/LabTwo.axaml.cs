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
    private readonly Mesh cube = Mesh.CreateCube(
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1)
        );

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
        Span<uint> span = new(pixels, HEIGHT * stride);
        span.Fill(0xFF000000);

        RenderCube(pixels, stride);
        MainImage.InvalidateVisual();
        DebugLabel.Text = $"Angle: {angle:F2}";
    }

    private unsafe void RenderCube(uint* pixels, int stride)
    {
        angle += 0.02f; // Speed
        var MVP = CreateMVPMatrix(angle);

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

            var (min, max) = BoundingBox(p1, p2, p3);
            for (int y = (int)min.Y; y <= (int)max.Y; y++)
            {
                for (int x = (int)min.X; x <= (int)max.X; x++)
                {
                    var uv = TriangleIntersection(p1, p2, p3, new Vector2(x, y));
                    if (!IsPointInTriangle(uv, new Vector2(x, y)))
                        continue;
                    // Use barycentric weights to interpolate vertex colors: A*(1-u-v) + B*u + C*v
                    var color = Vector3.Lerp(
                        cube.Vertices[tri.A].Color,
                        Vector3.One,
                        uv.X);
                    pixels[y * stride + x] = Color.Vector3ToPixel(color);
                }
            }
        });
    }

    private static Vector3 VertexShader()
    {
        return Vector3.Zero;
    }


    private static (Vector2 x, Vector2 y) BoundingBox(Vector2 a, Vector2 b, Vector2 c)
    {
        var minX = MathF.Min(a.X, MathF.Min(b.X, c.X));
        var minY = MathF.Min(a.Y, MathF.Min(b.Y, c.Y));
        var maxX = MathF.Max(a.X, MathF.Max(b.X, c.X));
        var maxY = MathF.Max(a.Y, MathF.Max(b.Y, c.Y));
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }

    private static Vector2 TriangleIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var ab = b - a;
        var ac = c - a;
        var inverse = 1 / (ab.X * ac.Y - ab.Y * ac.X);
        var a1 = (p.X - a.X) * new Vector2(ac.Y, -ab.Y);
        var b1 = (p.Y - a.Y) * new Vector2(-ac.X, ab.X);
        return inverse * (a1 + b1);
    }

    private static bool IsPointInTriangle(Vector2 uv, Vector2 p)
    {
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