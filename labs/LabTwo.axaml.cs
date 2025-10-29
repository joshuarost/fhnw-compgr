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
    private readonly Vector3 eye = new(0, 0, -10);
    private readonly Light light = new(
        // Top of screen
        new Vector3(0, 5, 0),
        new Vector3(0.5f, 0.5f, 0.5f)
    );
    private readonly Mesh cube = Mesh.CreateCube(
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0)
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

        // Z-buffering with float infinity
        var zBuffer = new float[WIDTH * HEIGHT];
        Array.Fill(zBuffer, float.PositiveInfinity);

        RenderCube(pixels, stride, zBuffer);
        MainImage.InvalidateVisual();
        DebugLabel.Text = $"Angle: {angle:F2}";
    }

    private unsafe void RenderCube(uint* pixels, int stride, float[] zBuffer)
    {
        angle += 0.05f; // Speed
        var MVP = CreateMVPMatrix(angle);
        var M = CreateMMatrix(angle);

        var angle2 = (float)(angle * 0.02);
        var MVP2 = CreateMVPMatrix(angle2);
        var M2 = CreateMMatrix(angle2);

        cube.Tris.ForEach(tri =>
        {
            var A = Project(VertexShader(cube.Vertices[tri.A], MVP, M));
            var B = Project(VertexShader(cube.Vertices[tri.B], MVP, M));
            var C = Project(VertexShader(cube.Vertices[tri.C], MVP, M));

            var p1 = ConvertToPixels(A.Position);
            var p2 = ConvertToPixels(B.Position);
            var p3 = ConvertToPixels(C.Position);

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
                    // Check if pixel is in triangle
                    var uv = TriangleIntersection(p1, p2, p3, new Vector2(x, y));
                    if (!IsPointInTriangle(uv, new Vector2(x, y)))
                        continue;
                    var Q = A + uv.X * (B - A) + uv.Y * (C - A);
                    // Transform vertex Q back to camera space
                    var zFar = 100f;
                    var zNear = 0.1f;
                    var z = zFar * zNear / zFar + (zFar - zNear) * Q.Position.Z;
                    var Q2 = FragmentShaderDiffuse(Q * z);
                    pixels[y * stride + x] = Color.Vector3ToPixel(Q2);
                }
            }
        });
    }

    private Vector3 FragmentShaderDiffuse(Vertex Q)
    {
        var PL = Vector3.Normalize(Q.WorldCoordinates - light.position);
        var cos0 = MathF.Max(0, Vector3.Dot(-Q.Normal, PL)); // flip normal
        if (cos0 < 0)
            return Vector3.Zero;
        if (Q.TexCoord == Vector2.Zero)
            return light.color * Q.Color * cos0;
        // Specular texture
        var r = 2 * (cos0 * Q.Normal) * Q.Normal - PL;
        var EP = Vector3.Normalize(eye - Q.WorldCoordinates);
        var cosF = Vector3.Dot(Vector3.Normalize(r), EP);
        if (cosF < 0)
            return light.color * Q.Color * cos0;
        var k = 10f;
        var spec = MathF.Pow(cosF, k);
        return light.color * (Q.Color * cos0 + new Vector3(spec, spec, spec));
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

    private static Vertex VertexShader(Vertex v, Matrix4x4 mvp, Matrix4x4 M)
    {
        Matrix4x4.Invert(M, out var invM);
        var normalMatrix = Matrix4x4.Transpose(invM);

        return v with
        {
            Position = Vector4.Transform(v.Position, mvp),
            Normal = Vector3.Normalize(Vector3.TransformNormal(v.Normal, normalMatrix)),
            WorldCoordinates = Vector4.Transform(v.Position, M).AsVector3(),
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

    private static Matrix4x4 CreateMMatrix(float angle)
    {
        var M = Matrix4x4.CreateRotationY(angle);
        M *= Matrix4x4.CreateRotationX(angle / 2);
        return M;
    }

    private static Matrix4x4 CreateMVPMatrix(float angle)
    {
        var M = CreateMMatrix(angle);
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