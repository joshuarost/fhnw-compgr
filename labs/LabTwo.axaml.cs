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
    private readonly Bitmap texture = new(AssetLoader.Open(new Uri("avares://fhnw-compgr/Assets/brick.jpg")));

    private readonly Mesh cube = Mesh.CreateCube(
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0)
        );
    private readonly Mesh sphere = Mesh.CreateSphere(16, new Vector3(0, 0, 1));

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

        var ctx = new RenderContext(WIDTH, HEIGHT, pixels, stride);
        ctx.Clear();

        angle += 0.05f; // Speed
        var M = CreateModelMatrix(angle);
        var MVP = CreateMVPMatrix(angle);
        var root = new Node { Mesh = cube, Texture = texture };
        root.Children.Add((new Node { Mesh = sphere }, Matrix4x4.CreateTranslation(1, 1, 1)));
        root.Render(M, MVP, ctx);

        MainImage.InvalidateVisual();
        DebugLabel.Text = $"Angle: {angle:F2}";
    }

    private static Matrix4x4 CreateModelMatrix(float angle)
    {
        var M = Matrix4x4.CreateRotationY(angle);
        M *= Matrix4x4.CreateRotationX(angle / 2);
        return M;
    }

    private static Matrix4x4 CreateMVPMatrix(float angle)
    {
        var M = CreateModelMatrix(angle);
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