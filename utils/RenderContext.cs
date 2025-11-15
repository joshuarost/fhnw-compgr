using System;
using System.Numerics;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace utils;

public class RenderContext
{
    public int WIDTH, HEIGHT;
    public float[] zBuffer;
    public int stride;
    public unsafe uint* pixels;
    private readonly Bitmap texture = new(AssetLoader.Open(new Uri("avares://fhnw-compgr/Assets/brick.jpg")));

    public readonly Light light = new(
        // Top of screen
        new Vector3(0, 1, 0),
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

        // Perspective-correct depth setup
        // Define your near and far plane values (adjust as needed)
        float zNear = 0.1f;
        float zFar = 100f;

        float wA = A.Position.W;
        float wB = B.Position.W;
        float wC = C.Position.W;

        float zA = A.Position.Z;
        float zB = B.Position.Z;
        float zC = C.Position.Z;

        float zPrimeA = zFar * zNear / (zFar + zNear - zFar * zA);
        float zPrimeB = zFar * zNear / (zFar + zNear - zFar * zB);
        float zPrimeC = zFar * zNear / (zFar + zNear - zFar * zC);

        float invWA = 1f / wA;
        float invWB = 1f / wB;
        float invWC = 1f / wC;

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                // Barycentric coordinates
                var bary = TriangleIntersection(p1, p2, p3, new Vector2(x, y));
                if (!IsPointInTriangle(bary, new Vector2(x, y)))
                    continue;

                // Interpolate vertex attributes
                var a = 1 - bary.X - bary.Y;
                var b = bary.X;
                var c = bary.Y;

                var Q = A + b * (B - A) + c * (C - A); // Interpolated vertex

                // Perspective-correct depth interpolation
                float zPrimeOverW = a * (zPrimeA * invWA) + b * (zPrimeB * invWB) + c * (zPrimeC * invWC);
                float oneOverW = a * invWA + b * invWB + c * invWC;
                float zPrime = zPrimeOverW / oneOverW;

                // Depth test with zPrime
                if (zBuffer[y * WIDTH + x] < zPrime)
                    continue;
                zBuffer[y * WIDTH + x] = zPrime;

                var color = FragmentShader(Q);
                pixels[y * stride + x] = Color.Vector3ToPixel(color);
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

    private Vector3 FragmentShader(Vertex Q)
    {
        var N = Vector3.Normalize(-Q.Normal);
        // return N;
        var PL = Vector3.Normalize(light.position - Q.WorldCoordinates);
        var EP = Vector3.Normalize(eye - Q.WorldCoordinates);

        var cos0 = MathF.Max(0, Vector3.Dot(N, PL)); // flip normal

        if (cos0 <= 0)
            return Q.Color;

        var diffuse = light.color * Q.Color * cos0;
        // var texColor = Texture.GetTexture(texture, Q.TexCoord);
        var texColor = Texture.BilinearSample(texture, Q.TexCoord);
        diffuse *= texColor;

        var R = Vector3.Normalize(2 * cos0 * N - PL);
        var cosF = MathF.Max(0, Vector3.Dot(R, EP));

        if (cosF <= 0)
            return diffuse;

        var k = 10f;
        var spec = MathF.Pow(cosF, k);
        return diffuse + light.color * spec;
    }
}
