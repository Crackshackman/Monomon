using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monomon
{
    public class Sprite
    {
        public Texture2D Texture { get; }
        public Vector2 Position { get; set; }

        public Sprite(Texture2D texture, Vector2 position)
        {
            Texture = texture;
            Position = position;
        }
    }
}