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
using System.Numerics;

namespace utils;

public record Vertex(Vector4 Position, Vector3 WorldCoordinates, Vector3 Color, Vector2 TexCoord, Vector3 Normal)
{
    public Vertex(Vector3 Position, Vector3 Color, Vector2 TexCoord, Vector3 Normal) : this(new Vector4(Position, 1), Vector3.Zero, Color, TexCoord, Normal)
    {
    }

    public static Vertex Lerp(Vertex a, Vertex b, float t)
        => new Vertex(
                Vector4.Lerp(a.Position, b.Position, t),
                Vector3.Lerp(a.WorldCoordinates, b.WorldCoordinates, t),
                Vector3.Lerp(a.Color, b.Color, t),
                Vector2.Lerp(a.TexCoord, b.TexCoord, t),
                Vector3.Lerp(a.Normal, b.Normal, t)
            );

    public static Vertex operator *(Vertex v, float x)
        => new Vertex(
                v.Position * x,
                v.WorldCoordinates * x,
                v.Color * x,
                v.TexCoord * x,
                v.Normal * x
            );

    public static Vertex operator *(float x, Vertex v)
        => v * x;

    public static Vertex operator +(Vertex a, Vertex b)
        => new Vertex(
                a.Position + b.Position,
                a.WorldCoordinates + b.WorldCoordinates,
                a.Color + b.Color,
                a.TexCoord + b.TexCoord,
                a.Normal + b.Normal
            );

    public static Vertex operator -(Vertex a, Vertex b)
        => new Vertex(
                a.Position - b.Position,
                a.WorldCoordinates - b.WorldCoordinates,
                a.Color - b.Color,
                a.TexCoord - b.TexCoord,
                a.Normal - b.Normal
            );
}
