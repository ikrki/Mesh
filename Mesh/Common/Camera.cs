using OpenTK.Mathematics;
using System;

namespace Mesh.Common
{
    public class Camera
    {
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;
        private float _fov = MathHelper.PiOver2;
        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }
        public Vector3 Position { get; set; }
        public float AspectRatio { private get; set; }
        public Vector3 Up => _up;
        public Vector3 Right => _right;
        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, (0f,0f,0f), _up);
        }
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);
        }
    }
}