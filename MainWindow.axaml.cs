using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;
using System;
using utils;
using System.Numerics;
using Avalonia.Controls.Shapes;

namespace fhnw_compgr;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer timer = new();
    private float angle = 0;

    public MainWindow()
    {
        InitializeComponent();
        timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        timer.Tick += OnRenderFrame;
        timer.Start();
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        MainCanvas.Children.Clear();
        RenderCube();
    }

    private void RenderCube()
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

            var polygon = new Polygon
            {
                Points = [new Point(p1.X, p1.Y), new Point(p2.X, p2.Y), new Point(p3.X, p3.Y)],
                Stroke = Brushes.Black,
                Fill = Brushes.Honeydew,
                StrokeThickness = 1
            };

            MainCanvas.Children.Add(polygon);
        });
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
        var wHalf = (float)MainCanvas.Bounds.Width / 2;
        var hHalf = (float)MainCanvas.Bounds.Height / 2;
        return new Vector2(
            position.X * wHalf + wHalf,
            position.Y * wHalf + hHalf
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