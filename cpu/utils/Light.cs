using System.Numerics;

namespace utils;

public record Light(Vector3 position, Vector3 color)
{
    public Vector3 position = position;
    public Vector3 color = color;
}