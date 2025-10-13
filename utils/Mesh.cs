/*
 * Copyright (c) 2013 - 2025 Simon Felix
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *  Redistributions of source code must retain the above copyright notice,
 *   this list of conditions and the following disclaimer.
 *  Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 *  Neither the name of FHNW nor the names of its contributors may
 *   be used to endorse or promote products derived from this software without
 *   specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Numerics;

namespace utils;

public class Mesh
{
    public readonly List<Vertex> Vertices = new();
    public readonly List<(int A, int B, int C)> Tris = new();

    private Mesh() { }

    public static Mesh CreateCube(Vector3 _colorFront, Vector3 _colorBack, Vector3 _colorTop, Vector3 _colorBottom, Vector3 _colorRight, Vector3 _colorLeft)
    {
        var m = new Mesh();
        AddCube(m.Vertices, m.Tris, _colorFront, _colorBack, _colorTop, _colorBottom, _colorRight, _colorLeft);
        return m;
    }

    public static Mesh CreateSphere(int _patches, Vector3 _color)
    {
        var m = new Mesh();
        AddSphere(m.Vertices, m.Tris, _patches, _color);
        return m;
    }

    public static void AddCube(List<Vertex> _vertices, List<(int A, int B, int C)> _tris, Vector3 _colorFront, Vector3 _colorBack, Vector3 _colorTop, Vector3 _colorBottom, Vector3 _colorRight, Vector3 _colorLeft)
    {
        //front
        _vertices.Add(new Vertex(new Vector3(-1, -1, -1), _colorFront, new Vector2(0, 0), -Vector3.UnitZ));
        _vertices.Add(new Vertex(new Vector3(1, -1, -1), _colorFront, new Vector2(1, 0), -Vector3.UnitZ));
        _vertices.Add(new Vertex(new Vector3(1, 1, -1), _colorFront, new Vector2(1, 1), -Vector3.UnitZ));
        _vertices.Add(new Vertex(new Vector3(-1, 1, -1), _colorFront, new Vector2(0, 1), -Vector3.UnitZ));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));

        //back
        _vertices.Add(new Vertex(new Vector3(1, -1, 1), _colorBack, new Vector2(0, 0), Vector3.UnitZ));
        _vertices.Add(new Vertex(new Vector3(-1, -1, 1), _colorBack, new Vector2(1, 0), Vector3.UnitZ));
        _vertices.Add(new Vertex(new Vector3(-1, 1, 1), _colorBack, new Vector2(1, 1), Vector3.UnitZ));
        _vertices.Add(new Vertex(new Vector3(1, 1, 1), _colorBack, new Vector2(0, 1), Vector3.UnitZ));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));

        //top
        _vertices.Add(new Vertex(new Vector3(-1, 1, -1), _colorTop, new Vector2(0, 1), Vector3.UnitY));
        _vertices.Add(new Vertex(new Vector3(1, 1, -1), _colorTop, new Vector2(1, 1), Vector3.UnitY));
        _vertices.Add(new Vertex(new Vector3(1, 1, 1), _colorTop, new Vector2(1, 0), Vector3.UnitY));
        _vertices.Add(new Vertex(new Vector3(-1, 1, 1), _colorTop, new Vector2(0, 0), Vector3.UnitY));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));

        //bottom
        _vertices.Add(new Vertex(new Vector3(1, -1, -1), _colorBottom, new Vector2(0, 1), -Vector3.UnitY));
        _vertices.Add(new Vertex(new Vector3(-1, -1, -1), _colorBottom, new Vector2(1, 1), -Vector3.UnitY));
        _vertices.Add(new Vertex(new Vector3(-1, -1, 1), _colorBottom, new Vector2(1, 0), -Vector3.UnitY));
        _vertices.Add(new Vertex(new Vector3(1, -1, 1), _colorBottom, new Vector2(0, 0), -Vector3.UnitY));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));

        //right
        _vertices.Add(new Vertex(new Vector3(1, -1, -1), _colorRight, new Vector2(0, 1), Vector3.UnitX));
        _vertices.Add(new Vertex(new Vector3(1, -1, 1), _colorRight, new Vector2(1, 1), Vector3.UnitX));
        _vertices.Add(new Vertex(new Vector3(1, 1, 1), _colorRight, new Vector2(1, 0), Vector3.UnitX));
        _vertices.Add(new Vertex(new Vector3(1, 1, -1), _colorRight, new Vector2(0, 0), Vector3.UnitX));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));

        //left
        _vertices.Add(new Vertex(new Vector3(-1, -1, 1), _colorLeft, new Vector2(0, 1), -Vector3.UnitX));
        _vertices.Add(new Vertex(new Vector3(-1, -1, -1), _colorLeft, new Vector2(1, 1), -Vector3.UnitX));
        _vertices.Add(new Vertex(new Vector3(-1, 1, -1), _colorLeft, new Vector2(1, 0), -Vector3.UnitX));
        _vertices.Add(new Vertex(new Vector3(-1, 1, 1), _colorLeft, new Vector2(0, 0), -Vector3.UnitX));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));
    }

    public static void AddSphere(List<Vertex> _vertices, List<(int A, int B, int C)> _tris, int _patches, Vector3 _color)
    {
        for (var x = 0; x < _patches; x++)
            for (var y = 0; y < _patches; y++)
            {
                var x1 = (x * 2f / _patches) - 1f;
                var x2 = ((x + 1) * 2f / _patches) - 1f;
                var y1 = (y * 2f / _patches) - 1f;
                var y2 = ((y + 1) * 2f / _patches) - 1f;

                AddPatch(new Vector3(x1, y1, -1),
                         new Vector3(x2, y1, -1),
                         new Vector3(x2, y2, -1),
                         new Vector3(x1, y2, -1), false);
                AddPatch(new Vector3(x1, y1, 1),
                         new Vector3(x2, y1, 1),
                         new Vector3(x2, y2, 1),
                         new Vector3(x1, y2, 1), true);
                AddPatch(new Vector3(x1, -1, y1),
                         new Vector3(x2, -1, y1),
                         new Vector3(x2, -1, y2),
                         new Vector3(x1, -1, y2), true);
                AddPatch(new Vector3(x1, 1, y1),
                         new Vector3(x2, 1, y1),
                         new Vector3(x2, 1, y2),
                         new Vector3(x1, 1, y2), false);
                AddPatch(new Vector3(-1, x1, y1),
                         new Vector3(-1, x2, y1),
                         new Vector3(-1, x2, y2),
                         new Vector3(-1, x1, y2), false);
                AddPatch(new Vector3(1, x1, y1),
                         new Vector3(1, x2, y1),
                         new Vector3(1, x2, y2),
                         new Vector3(1, x1, y2), true);

                void AddPatch(Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4, bool _flipNormal)
                {
                    var n1 = Vector3.Normalize(_p1);
                    var n2 = Vector3.Normalize(_p2);
                    var n3 = Vector3.Normalize(_p3);
                    var n4 = Vector3.Normalize(_p4);
                    static Vector2 ToTex(Vector3 p) => new Vector2((float)(Math.Atan2(p.X, p.Y) / Math.PI / 2) + 0.5f, (float)(Math.Acos(p.Z) / Math.PI));
                    var t1 = ToTex(n1);
                    var t2 = ToTex(n2);
                    var t3 = ToTex(n3);
                    var t4 = ToTex(n4);
                    if (t1.X > 0.75f || t2.X > 0.75f || t3.X > 0.75f || t4.X > 0.75f)
                    {
                        if (t1.X < 0.25f)
                            t1.X++;
                        if (t2.X < 0.25f)
                            t2.X++;
                        if (t3.X < 0.25f)
                            t3.X++;
                        if (t4.X < 0.25f)
                            t4.X++;
                    }
                    _vertices.Add(new Vertex(n1, _color, t1, n1));
                    _vertices.Add(new Vertex(n2, _color, t2, n2));
                    _vertices.Add(new Vertex(n3, _color, t3, n3));
                    _vertices.Add(new Vertex(n4, _color, t4, n4));
                    if (_flipNormal)
                    {
                        _tris.Add((_vertices.Count - 3, _vertices.Count - 4, _vertices.Count - 2));
                        _tris.Add((_vertices.Count - 2, _vertices.Count - 4, _vertices.Count - 1));
                    }
                    else
                    {
                        _tris.Add((_vertices.Count - 4, _vertices.Count - 3, _vertices.Count - 2));
                        _tris.Add((_vertices.Count - 4, _vertices.Count - 2, _vertices.Count - 1));
                    }
                }
            }
    }
}
