using System.Numerics;
using Avalonia.Media.Imaging;

struct Scene(Sphere[] objects, Vector3 eye, Vector3 lookAt, float pov)
{
    public Sphere[] objects = objects;
    public Vector3 eye = eye;
    public Vector3 lookAt = lookAt;
    public float pov = pov;
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