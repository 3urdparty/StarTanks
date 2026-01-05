using Microsoft.Xna.Framework;

namespace SpaceTanks
{
    public sealed class Camera2D
    {
        public Vector2 Position; 
        public float Zoom = 1f;
        public float Rotation = 0f;

        public Matrix GetTransform()
        {
            return Matrix.CreateTranslation(new Vector3(-Position, 0f))
                * Matrix.CreateRotationZ(Rotation)
                * Matrix.CreateScale(Zoom, Zoom, 1f);
        }
    }
}
