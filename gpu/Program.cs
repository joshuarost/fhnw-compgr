using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenTK;                   //add "OpenTK" as NuGet reference
using OpenTK.Graphics.OpenGL4;  //add "OpenTK" as NuGet reference
using OpenTK.Windowing.Common;  //add "OpenTK" as NuGet reference
using OpenTK.Windowing.Desktop; //add "OpenTK" as NuGet reference
using OpenTK.Windowing.GraphicsLibraryFramework;

using var w = new GameWindow(
    new GameWindowSettings() { },
    new NativeWindowSettings()
    {
        API = ContextAPI.OpenGL,
        Flags = ContextFlags.ForwardCompatible | ContextFlags.Debug,
        SrgbCapable = true,
        ClientSize = new OpenTK.Mathematics.Vector2i(720, 480),
        Title = "ComGr",
        APIVersion = new Version(4, 1),
    });

int hProgram = 0;
int vaoTriangle = 0;
int vboTriangleIndices = 0;

int[] triangleIndices = [0, 1, 2];
float[] triangleVertices =
[
    0.0f, -0.5f, 0.0f,
    0.5f,  0.5f, 0.0f,
    -0.5f,  0.5f, 0.0f
];

w.Load += () =>
{
    //set up opengl
    if (GLFW.ExtensionSupported("GL_KHR_debug"))
    {
        GL.Arb.DebugMessageCallback([DebuggerHidden] (source​, type​, id​, severity​, length​, message​, userParam) =>
        {
            var msg = Marshal.PtrToStringAnsi(message, length);
            if (type == DebugType.DebugTypeError)
                throw new InvalidOperationException(msg);
            Console.WriteLine(msg);
        }, IntPtr.Zero);

        GL.Enable(EnableCap.DebugOutput);
        if (Debugger.IsAttached)
            GL.Enable(EnableCap.DebugOutputSynchronous);
    }

    GL.Enable(EnableCap.FramebufferSrgb);
    GL.ClearColor(0.5f, 0.5f, 0.5f, 0);
    //GL.ClearDepth(1);
    //GL.Disable(EnableCap.DepthTest);
    //GL.DepthFunc(DepthFunction.Less);
    //GL.Disable(EnableCap.CullFace);
    //GL.Enable(EnableCap.CullFace);

    //load, compile and link shaders
    //see https://www.khronos.org/opengl/wiki/Vertex_Shader
    var VertexShaderSource = """
		#version 400 core

		in vec3 inPos;
		uniform mat4 inMatrix;
		uniform float inTime;
		out float fromVertexShaderToFragmentShader;

		void main()
		{
			gl_Position = vec4(inPos, 1.0) + vec4(sin(inTime) * 0.5, cos(inTime) * 0.5, 0.0, 0.0);
			fromVertexShaderToFragmentShader = inPos.x + 0.5;
		}
	""";
    var hVertexShader = GL.CreateShader(ShaderType.VertexShader);
    GL.ShaderSource(hVertexShader, VertexShaderSource);
    GL.CompileShader(hVertexShader);
    GL.GetShader(hVertexShader, ShaderParameter.CompileStatus, out int status);
    if (status != (int)All.True)
        throw new Exception(GL.GetShaderInfoLog(hVertexShader));

    //see https://www.khronos.org/opengl/wiki/Fragment_Shader
    var FragmentShaderSource = """
		#version 400 core

		out vec4 outColor;
		in float fromVertexShaderToFragmentShader;

		void main()
		{
			outColor = vec4(fromVertexShaderToFragmentShader, 0.75, 0.0, 1.0);
		}
	""";
    var hFragmentShader = GL.CreateShader(ShaderType.FragmentShader);
    GL.ShaderSource(hFragmentShader, FragmentShaderSource);
    GL.CompileShader(hFragmentShader);
    GL.GetShader(hFragmentShader, ShaderParameter.CompileStatus, out status);
    if (status != (int)All.True)
        throw new Exception(GL.GetShaderInfoLog(hFragmentShader));

    //link shaders to a program
    hProgram = GL.CreateProgram();
    GL.AttachShader(hProgram, hFragmentShader);
    GL.AttachShader(hProgram, hVertexShader);
    GL.LinkProgram(hProgram);
    GL.GetProgram(hProgram, GetProgramParameterName.LinkStatus, out status);
    if (status != (int)All.True)
        throw new Exception(GL.GetProgramInfoLog(hProgram));

    //upload model vertices to a vbo
    var vboTriangleVertices = GL.GenBuffer();
    GL.BindBuffer(BufferTarget.ArrayBuffer, vboTriangleVertices);
    GL.BufferData(BufferTarget.ArrayBuffer, triangleVertices.Length * sizeof(float), triangleVertices, BufferUsageHint.StaticDraw);

    // upload model indices to a vbo
    vboTriangleIndices = GL.GenBuffer();
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
    GL.BufferData(BufferTarget.ElementArrayBuffer, triangleIndices.Length * sizeof(int), triangleIndices, BufferUsageHint.StaticDraw);

    //set up a vao
    vaoTriangle = GL.GenVertexArray();
    GL.BindVertexArray(vaoTriangle);
    var posAttribIndex = GL.GetAttribLocation(hProgram, "inPos");
    if (posAttribIndex != -1)
    {
        GL.EnableVertexAttribArray(posAttribIndex);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboTriangleVertices);
        GL.VertexAttribPointer(posAttribIndex, 3, VertexAttribPointerType.Float, false, 0, 0);
    }

    //check for errors during all previous calls
    var error = GL.GetError();
    if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
        throw new Exception(error.ToString());
};

double time = 0;
w.UpdateFrame += fea =>
{
    //process logic

    time += fea.Time;
};

w.RenderFrame += fea =>
{
    //clear screen and z-buffer
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    //switch to our shader
    GL.UseProgram(hProgram);

    //set uniform values
    GL.Uniform1(GL.GetUniformLocation(hProgram, "inTime"), (float)time);

    var matrix = Matrix4x4.Identity;
    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "inMatrix"), 1, false, ref matrix.M11);

    //render our model
    GL.BindVertexArray(vaoTriangle);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
    GL.DrawElements(PrimitiveType.Triangles, triangleIndices.Length, DrawElementsType.UnsignedInt, 0);

    //display
    w.SwapBuffers();

    var error = GL.GetError();
    if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
        throw new Exception(error.ToString());
};

w.FramebufferResize += rea => GL.Viewport(0, 0, rea.Width, rea.Height);

w.Run();