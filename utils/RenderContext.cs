using System;
using System.Numerics;

namespace utils;

public class RenderContext
{
    public int WIDTH, HEIGHT;
    public float[] zBuffer;
    public int stride;
    public unsafe uint* pixels;

    public readonly Light light = new(
        // Top of screen
        new Vector3(0, 5, 0),
        new Vector3(0.5f, 0.5f, 0.5f)
    );

    public readonly Vector3 eye = new(0, 0, -10);


    public unsafe RenderContext(int width, int height, uint* pixels, int stride)
    {
        WIDTH = width;
        HEIGHT = height;
        this.pixels = pixels;
        this.stride = stride;
        zBuffer = new float[WIDTH * HEIGHT];
    }

    public unsafe void Clear(uint color = 0xFF000000)
    {
        // Clear color buffer
        Span<uint> span = new(pixels, HEIGHT * stride);
        span.Fill(color);
        // Clear z-buffer
        Array.Fill(zBuffer, float.PositiveInfinity);
    }

    public unsafe void Rasterize(Vertex A, Vertex B, Vertex C)
    {
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
        int x0 = Math.Max(0, (int)MathF.Floor(min.X));
        int y0 = Math.Max(0, (int)MathF.Floor(min.Y));
        int x1 = Math.Min(WIDTH - 1, (int)MathF.Ceiling(max.X));
        int y1 = Math.Min(HEIGHT - 1, (int)MathF.Ceiling(max.Y));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
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
                if (zBuffer[y * WIDTH + x] < z)
                    continue;
                zBuffer[y * WIDTH + x] = z;
                var Q2 = FragmentShaderDiffuse(Q * z);
                pixels[y * stride + x] = Color.Vector3ToPixel(Q2);
            }
        }
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
}
