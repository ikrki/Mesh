using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mesh.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection;
using OpenTK.Compute.OpenCL;

namespace Mesh
{
    public class Window : GameWindow
    {
        private float[] _vertices;
        private uint[] _indices;
        private float[] _lineVertices;
        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;
        private int _lineArrayObject;
        private int _lineBufferObject;
        private Shader _shader;
        private Shader _lineShader;
        private Camera _camera;
        private int[,] nums;
        private readonly int fpsLimit=60;
        private int frameTime;
        Stopwatch stopWatch;
        private bool _firstMove = true;
        private Vector2 _lastPos;
        private Matrix4 _model;
        private readonly float _radius=2f;
        private const float _speed = 0.005f;
        private void readData(string fileName)
        {
            StreamReader reader = new StreamReader(fileName); 
            string line=reader.ReadLine();
            string[] words = line.Split(' ');
            int n = words.Length;
            int m = 0;
            for (; line != null; m++,line= reader.ReadLine());
            reader.Close();
            nums = new int[m, n];
            reader = new StreamReader(fileName);
            for(int i = 0; i < m; i++)
            {
                line = reader.ReadLine();
                words = line.Split(' ');
                for(int j = 0; j < n; j++)
                {
                    nums[i,j] = int.Parse(words[j]);
                }
            }
            reader.Close();
            //now we get nums[m,n]

            //each quad has 4 vertices, each vertice has 3 pos 1 color(as an input of vertex shader)
            //vertices: posx posy posz color 16*cur+0,1,2,3
            //indices: 6*cur+0,1,2,1,2,3
            _vertices = new float[(m - 1) * (n - 1) * 16];
            _indices = new uint[(m - 1) * (n - 1) * 6];
            _lineVertices = new float[6 * (m * (n - 1) + n * (m - 1) + 3)];

            _lineVertices[0] = 1.0f;
            _lineVertices[1] = 0.0f;
            _lineVertices[2] = 0.0f;
            _lineVertices[3] = 1.0f;
            _lineVertices[4] = 0.0f;
            _lineVertices[5] = 1.0f;

            _lineVertices[6] = 0.0f;
            _lineVertices[7] = 0.0f;
            _lineVertices[8] = 1.0f;
            _lineVertices[9] = 1.0f;
            _lineVertices[10] = 0.0f;
            _lineVertices[11] = 1.0f;

            _lineVertices[12] = 0.0f;
            _lineVertices[13] = 0.0f;
            _lineVertices[14] = 1.0f;
            _lineVertices[15] = 0.0f;
            _lineVertices[16] = 1.0f;
            _lineVertices[17] = 1.0f;

            int index = 18;
            for(int i = 0; i < m; i++)
            {
                for(int j = 0; j < n - 1; j++)
                {
                    _lineVertices[index + 0] = i / (float)m;
                    _lineVertices[index + 1] = nums[i, j] / 256.0f;
                    _lineVertices[index + 2] = j / (float)n;
                    _lineVertices[index + 3] = i / (float)m;
                    _lineVertices[index + 4] = nums[i, j+1] / 256.0f;
                    _lineVertices[index + 5] = (j + 1) / (float)n;
                    index += 6;
                }
            }
            for (int i = 0; i < m-1; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    _lineVertices[index + 0] = i / (float)m;
                    _lineVertices[index + 1] = nums[i, j] / 256.0f;
                    _lineVertices[index + 2] = j / (float)n;
                    _lineVertices[index + 3] = (i + 1) / (float)m;
                    _lineVertices[index + 4] = nums[i + 1, j] / 256.0f;
                    _lineVertices[index + 5] = j / (float)n;
                    index += 6;
                }
            }
            for (int i = 0; i < m - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    int cur = i * (n - 1) + j;

                    //vertices
                    _vertices[16 * cur + 0] = i / (float)m;//posx
                    _vertices[16 * cur + 1] = nums[i, j] / 256.0f;//posy
                    _vertices[16 * cur + 2] = j / (float)n;//posz        

                    _vertices[16 * cur + 4 ] = (i + 1) / (float)m;
                    _vertices[16 * cur + 5] = nums[i + 1, j] / 256.0f;
                    _vertices[16 * cur + 6] = j / (float)n;

                    _vertices[16 * cur + 8] = i / (float)m;      
                    _vertices[16 * cur + 9] = nums[i, j + 1] / 256.0f;
                    _vertices[16 * cur + 10] = (j + 1) / (float)n;

                    _vertices[16 * cur + 12] = (i + 1) / (float)m;
                    _vertices[16 * cur + 13] = nums[i + 1, j + 1] / 256.0f;
                    _vertices[16 * cur + 14] = (j + 1) / (float)n;

                    float avgHeight = (_vertices[16 * cur + 1] + _vertices[16 * cur + 5] 
                        + _vertices[16 * cur + 9] + _vertices[16 * cur + 13]) / 4;
                    _vertices[16 * cur + 3] = _vertices[16 * cur + 7]
                        = _vertices[16 * cur + 11] = _vertices[16 * cur + 15] = avgHeight;//color

                    //indices
                    _indices[6 * cur + 0] = (uint)(4 * cur + 0);
                    _indices[6 * cur + 1] = (uint)(4 * cur + 1);
                    _indices[6 * cur + 2] = (uint)(4 * cur + 2);
                    _indices[6 * cur + 3] = (uint)(4 * cur + 1);
                    _indices[6 * cur + 4] = (uint)(4 * cur + 2);
                    _indices[6 * cur + 5] = (uint)(4 * cur + 3);
                }
            }
        }
        public Window(int width, int height, string title) : 
            base(GameWindowSettings.Default, new NativeWindowSettings() 
            { Size = (width, height), Title = title }) { }
        protected override void OnLoad()
        {
            readData("Data/test.txt");
            frameTime = 1000 / fpsLimit;
            base.OnLoad();
            GL.ClearColor(0.9f, 0.9f, 0.9f, 1.0f);

            _lineBufferObject = GL.GenBuffer();
            _lineArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_lineArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _lineVertices.Length * sizeof(float), _lineVertices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            _vertexBufferObject = GL.GenBuffer();
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 4 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _lineShader= new Shader("Shaders/line.vert", "Shaders/line.frag");
            _shader.Use();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.LineSmooth);

            GL.LineWidth(5.0f);
            stopWatch = new Stopwatch();
            _camera = new Camera((2f,0f,0f), Size.X / (float)Size.Y);
            _model = Matrix4.Identity * Matrix4.CreateRotationX(0);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);       
            
            //quads       
            _shader.SetMatrix4("model", _model);
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.Use();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BindVertexArray(_vertexArrayObject); 
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            //lines
            _lineShader.SetMatrix4("model", _model);
            _lineShader.SetMatrix4("view", _camera.GetViewMatrix());
            _lineShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _lineShader.Use();
            GL.BindBuffer(BufferTarget.ArrayBuffer,_lineBufferObject);
            GL.BindVertexArray(_lineArrayObject);
            GL.DrawArrays(PrimitiveType.Lines,0, _lineVertices.Length/3);

            SwapBuffers();

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            if (frameTime > ts.Milliseconds)
            {
                Thread.Sleep(frameTime - ts.Milliseconds);
            }
            stopWatch.Restart();
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            var input = KeyboardState;
            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            if(IsMouseButtonReleased(MouseButton.Left))
            {
                _firstMove= true;
            }
            if(IsMouseButtonDown(MouseButton.Left))
            {
                if (_firstMove)
                {
                    _lastPos = new Vector2(MouseState.X, MouseState.Y);
                    _firstMove = false;
                }
                else
                {
                    Vector2 currentPos = new Vector2(MouseState.X, MouseState.Y);
                    var deltaX = currentPos.X - _lastPos.X;
                    var deltaY = currentPos.Y - _lastPos.Y;
                    _lastPos = currentPos;
                    var cameraPos = _camera.Position;
                    double alpha = MathHelper.Asin((double)(cameraPos.Y / _radius));
                    double beta;
                    if (cameraPos.Z == 0) beta =MathHelper.PiOver2;
                    else beta = MathHelper.Atan((double)(cameraPos.X / cameraPos.Z));

                    if (cameraPos.Z < 0 && cameraPos.X > 0) beta += MathHelper.Pi;
                    if (cameraPos.Z < 0 && cameraPos.X < 0) beta -= MathHelper.Pi;

                    alpha += deltaY * _speed;
                    if(alpha > MathHelper.PiOver2)alpha = MathHelper.DegreesToRadians(89);
                    if (alpha < -MathHelper.PiOver2) alpha = MathHelper.DegreesToRadians(-89);
                    beta -= deltaX * _speed;
                    _camera.Position = new Vector3(
                        (float)(_radius * MathHelper.Cos(alpha) * MathHelper.Sin(beta)),
                        (float)(_radius * MathHelper.Sin(alpha)),
                        (float)(_radius * MathHelper.Cos(alpha) * MathHelper.Cos(beta))
                        );
                    /*Console.WriteLine(
                        _camera.Position.X.ToString()+" "
                        +_camera.Position.Y.ToString()+" "
                        +_camera.Position.Z.ToString()
                        );*/
                }
            }
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _camera.Fov -= e.OffsetY;
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
            GL.DeleteProgram(_shader.Handle);
            base.OnUnload();
        }
    }
}
