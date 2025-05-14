using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Monomon
{ 
    internal class ScaledSprite : Sprite
    {
        public Rectangle Rect
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, 100, 100);
            }
        }

        public ScaledSprite(Texture2D texture, Vector2 position) : base(texture, position)
        {
        }
    }
}