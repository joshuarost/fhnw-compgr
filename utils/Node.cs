
using System;
using System.Collections.Generic;
using System.Numerics;

namespace utils;

public class Node()
{
    public Mesh? Mesh { get; set; }
    public List<(Node Node, Matrix4x4 Transform)> Children { get; } = [];

    public void Render(Matrix4x4 parentModel, Matrix4x4 viewProjection, RenderContext ctx)
    {
        // Accumulate transforms
        var modelMatrix = parentModel;
        var mvp = modelMatrix * viewProjection;

        // Normal matrix (for lighting)
        Matrix4x4 normalMatrix;
        if (Matrix4x4.Invert(modelMatrix, out var inv))
            normalMatrix = Matrix4x4.Transpose(inv);
        else
            normalMatrix = Matrix4x4.Identity; // fallback

        // Render this node's geometry
        if (Mesh?.Vertices != null && Mesh.Tris != null)
        {
            RenderTriangles([.. Mesh.Vertices], [.. Mesh.Tris], mvp, normalMatrix, ctx);
        }

        // Recursively render children
        foreach (var (childNode, localTransform) in Children)
        {
            var childModel = localTransform * modelMatrix;
            childNode.Render(childModel, viewProjection, ctx);
        }
    }

    private static void RenderTriangles(
        Vertex[] vertices,
        (int A, int B, int C)[] tris,
        Matrix4x4 mvp,
        Matrix4x4 normalMatrix,
        RenderContext ctx)
    {
        // Your rasterization code here
        foreach (var tri in tris)
        {
            var A = Project(VertexShader(vertices[tri.A], mvp, normalMatrix));
            var B = Project(VertexShader(vertices[tri.B], mvp, normalMatrix));
            var C = Project(VertexShader(vertices[tri.C], mvp, normalMatrix));
            ctx.Rasterize(A, B, C);
        }
    }

    private static Vertex Project(Vertex v)
    {
        var invW = 1f / v.Position.W;
        return v with
        {
            Position = new Vector4(v.Position.X * invW, v.Position.Y * invW, v.Position.Z * invW, 1f)
        };
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
}