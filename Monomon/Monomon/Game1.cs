using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Monomon;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Render target for pixel-perfect scaling
    private RenderTarget2D _gameRenderTarget;

    // Resolution settings
    private const int VIRTUAL_WIDTH = 240;  // Base resolution width
    private const int VIRTUAL_HEIGHT = 160; // Base resolution height
    private int _scaleFactor = 3;           // How much to scale the base resolution
    private Rectangle _destinationRectangle;

    Texture2D _playerspritesheet;
    AnimationManager _animationManager;

    // Player state
    Vector2 _playerPosition;
    string _currentAnimation;
    KeyboardState _previousKeyboardState;

    // Player collision properties
    Rectangle _playerHitbox;
    const int PLAYER_WIDTH = 16;
    const int PLAYER_HEIGHT = 8;
    const int DISPLAY_TILESIZE = 8;

    private Dictionary<Vector2, int> main;
    private Dictionary<Vector2, int> colision;
    Texture2D _textureAtlas;
    Texture2D _hitboxTexture;
    Texture2D _debugTexture;

    private bool _showDebug = true;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        main = LoadMap("tileset_main.csv");
        colision = LoadMap("tileset_colision.csv");

        
        _graphics.PreferredBackBufferWidth = VIRTUAL_WIDTH * _scaleFactor;
        _graphics.PreferredBackBufferHeight = VIRTUAL_HEIGHT * _scaleFactor;
    }

    protected override void Initialize()
    {
        _gameRenderTarget = new RenderTarget2D(
            GraphicsDevice,
            VIRTUAL_WIDTH,
            VIRTUAL_HEIGHT,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents
        );


        UpdateDestinationRectangle();

        _playerPosition = new Vector2(30, 30);
        _currentAnimation = "idle";
        _playerHitbox = new Rectangle(
            (int)_playerPosition.X,
            (int)_playerPosition.Y + PLAYER_HEIGHT,
            PLAYER_WIDTH,
            PLAYER_HEIGHT
        );

        base.Initialize();
    }


    private void UpdateDestinationRectangle()
    {

        float outputAspect = _graphics.PreferredBackBufferWidth / (float)_graphics.PreferredBackBufferHeight;
        float preferredAspect = VIRTUAL_WIDTH / (float)VIRTUAL_HEIGHT;

        if (outputAspect <= preferredAspect)
        {

            int presentHeight = (int)(_graphics.PreferredBackBufferWidth / preferredAspect);
            int barHeight = (_graphics.PreferredBackBufferHeight - presentHeight) / 2;

            _destinationRectangle = new Rectangle(
                0, barHeight, _graphics.PreferredBackBufferWidth, presentHeight);
        }
        else
        {

            int presentWidth = (int)(_graphics.PreferredBackBufferHeight * preferredAspect);
            int barWidth = (_graphics.PreferredBackBufferWidth - presentWidth) / 2;

            _destinationRectangle = new Rectangle(
                barWidth, 0, presentWidth, _graphics.PreferredBackBufferHeight);
        }
    }

    private Dictionary<Vector2, int> LoadMap(string filePath)
    {
        Dictionary<Vector2, int> result = new();

        StreamReader reader = new StreamReader(filePath);
        int y = 0;
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            string[] items = line.Split(',');

            for (int i = 0; i < items.Length; i++)
            {
                if (int.TryParse(items[i], out int value))
                {
                    if (value > 0)
                    {
                        result[new Vector2(i, y)] = value;
                    }
                }

            }
            y++;
        }
        reader.Close();
        return result;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

       
        _playerspritesheet = Content.Load<Texture2D>("player1");

        
        _animationManager = new AnimationManager(10, 10, new Vector2(16, 16));

        
        _animationManager.SetAnimation(0, 0, 0);

        
        _animationManager.SetAnimationSpeed(15); // Faster animation

        _textureAtlas = Content.Load<Texture2D>("tileMap");
        _hitboxTexture = Content.Load<Texture2D>("colisionmap");

        
        _debugTexture = new Texture2D(GraphicsDevice, 1, 1);
        _debugTexture.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Get current keyboard state
        KeyboardState keyboardState = Keyboard.GetState();

        // Toggle debug visualization with F1
        if (keyboardState.IsKeyDown(Keys.F1) && _previousKeyboardState.IsKeyUp(Keys.F1))
            _showDebug = !_showDebug;

        // Toggle fullscreen with F11
        if (keyboardState.IsKeyDown(Keys.F11) && _previousKeyboardState.IsKeyUp(Keys.F11))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
            UpdateDestinationRectangle();
        }

        // Change scale factor with + and - keys
        if (keyboardState.IsKeyDown(Keys.OemPlus) && _previousKeyboardState.IsKeyUp(Keys.OemPlus))
        {
            _scaleFactor = MathHelper.Clamp(_scaleFactor + 1, 1, 8);
            UpdateWindowSize();
        }
        if (keyboardState.IsKeyDown(Keys.OemMinus) && _previousKeyboardState.IsKeyUp(Keys.OemMinus))
        {
            _scaleFactor = MathHelper.Clamp(_scaleFactor - 1, 1, 8);
            UpdateWindowSize();
        }

        // Handle player movement and animation
        HandleInput(keyboardState);

        // Update animation manager
        _animationManager.Update();

        // Store current keyboard state for next frame
        _previousKeyboardState = keyboardState;

        // Update the hitbox position to match player (bottom half)
        _playerHitbox.X = (int)_playerPosition.X;
        _playerHitbox.Y = (int)_playerPosition.Y + PLAYER_HEIGHT;

        base.Update(gameTime);
    }

    private void UpdateWindowSize()
    {
        _graphics.PreferredBackBufferWidth = VIRTUAL_WIDTH * _scaleFactor;
        _graphics.PreferredBackBufferHeight = VIRTUAL_HEIGHT * _scaleFactor;
        _graphics.ApplyChanges();
        UpdateDestinationRectangle();
    }

    private void HandleInput(KeyboardState keyboardState)
    {
        float speed = 2.0f;
        string lastDirection = "down"; // Default direction

        if (_currentAnimation.StartsWith("walk_"))
            lastDirection = _currentAnimation.Substring(5);
        else if (_currentAnimation.StartsWith("idle_"))
            lastDirection = _currentAnimation.Substring(5);

        // Store the original position for collision checking
        Vector2 newPosition = _playerPosition;

        // Handle movement based on key presses - one direction at a time
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
        {
            newPosition.Y -= speed;
            SetPlayerAnimation("walk_up");
        }
        else if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
        {
            newPosition.Y += speed;
            SetPlayerAnimation("walk_down");
        }
        else if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            newPosition.X -= speed;
            SetPlayerAnimation("walk_left");
        }
        else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            newPosition.X += speed;
            SetPlayerAnimation("walk_right");
        }
        else
        {
            SetPlayerAnimation("idle_" + lastDirection);
        }

        // Check if new position would cause a collision
        if (!WouldCollide(newPosition))
        {
            _playerPosition = newPosition;
        }
    }

    private bool WouldCollide(Vector2 newPosition)
    {
        // Create a hitbox at the new position (bottom half of player)
        Rectangle newHitbox = new Rectangle(
            (int)newPosition.X,
            (int)newPosition.Y + PLAYER_HEIGHT,
            PLAYER_WIDTH,
            PLAYER_HEIGHT
        );

        // Check collision against each collision tile
        foreach (var tile in colision)
        {
            Rectangle tileRect = new Rectangle(
                (int)tile.Key.X * DISPLAY_TILESIZE,
                (int)tile.Key.Y * DISPLAY_TILESIZE,
                DISPLAY_TILESIZE,
                DISPLAY_TILESIZE
            );

            if (newHitbox.Intersects(tileRect))
            {
                return true; // Collision detected
            }
        }

        return false; // No collision
    }

    private void SetPlayerAnimation(string animationName)
    {
        // Only change animation if we're switching to a different one
        if (_currentAnimation != animationName)
        {
            _currentAnimation = animationName;

            // Set the appropriate animation based on name
            switch (animationName)
            {
                case "idle_down":
                    // Static idle down frame: frame 0 on row 0
                    _animationManager.SetStaticFrame(0, 0);
                    break;

                case "idle_up":
                    // Static idle up frame: frame 0 on row 1
                    _animationManager.SetStaticFrame(1, 0);
                    break;

                case "idle_left":
                    // Static idle left frame: frame 0 on row 2
                    _animationManager.SetStaticFrame(6, 0);
                    break;

                case "idle_right":
                    // Static idle right frame: frame 0 on row 3
                    _animationManager.SetStaticFrame(8, 0);
                    break;

                case "idle":
                    // Using the first method: Single frame with SetAnimation
                    // This will use the first frame on row 0
                    _animationManager.SetAnimation(0, 0, 0);
                    break;

                case "walk_down":
                    // Walking down: frames 0-2 on row 0
                    _animationManager.SetAnimation(2, 3, 0);
                    _animationManager.SetAnimationSpeed(15); // Faster for walking
                    break;

                case "walk_up":
                    // Walking up: frames 0-2 on row 1
                    _animationManager.SetAnimation(4, 5, 0);
                    _animationManager.SetAnimationSpeed(15);
                    break;

                case "walk_left":
                    // Walking left: frames 0-2 on row 2
                    _animationManager.SetAnimation(6, 7, 0);
                    _animationManager.SetAnimationSpeed(10);
                    break;

                case "walk_right":
                    // Walking right: frames 0-2 on row 3
                    _animationManager.SetAnimation(8, 9, 0);
                    _animationManager.SetAnimationSpeed(10);
                    break;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        // First render to our render target at the virtual resolution
        GraphicsDevice.SetRenderTarget(_gameRenderTarget);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp // This ensures pixel-perfect scaling
        );

        int num_tiles = 16;
        int pixel_tilesize = 8;

        // Draw main tilemap
        foreach (var tile in main)
        {
            Rectangle drect = new((int)tile.Key.X * DISPLAY_TILESIZE,
                                  (int)tile.Key.Y * DISPLAY_TILESIZE,
                                  DISPLAY_TILESIZE,
                                  DISPLAY_TILESIZE);

            int x = tile.Value % num_tiles;
            int y = tile.Value / num_tiles;
            Rectangle src = new(
                x * pixel_tilesize,
                y * pixel_tilesize,
                pixel_tilesize,
                pixel_tilesize
            );
            _spriteBatch.Draw(_textureAtlas, drect, src, Color.White);
        }

        /*
        if (_showDebug)
        {
            foreach (var tile in colision)
            {
                Rectangle drect = new((int)tile.Key.X * DISPLAY_TILESIZE,
                                      (int)tile.Key.Y * DISPLAY_TILESIZE,
                                      DISPLAY_TILESIZE,
                                      DISPLAY_TILESIZE);

                int x = tile.Value % num_tiles;
                int y = tile.Value / num_tiles;                    
                Rectangle src = new(
                    x * pixel_tilesize,
                    y * pixel_tilesize,
                    pixel_tilesize,
                    pixel_tilesize
                );
                _spriteBatch.Draw(_hitboxTexture, drect, src, Color.Red * 0.5f); // Semi-transparent red
            }
        }
        */

        _spriteBatch.Draw(
            _playerspritesheet,
            new Rectangle(
                (int)_playerPosition.X,
                (int)_playerPosition.Y,
                PLAYER_WIDTH,
                PLAYER_WIDTH),
            _animationManager.GetFrame(),
            Color.White
        );

        /*
        if (_showDebug)
        {
            _spriteBatch.Draw(
                _debugTexture,
                _playerHitbox,
                Color.Green * 0.3f // Semi-transparent green
            );
        }
        */
        _spriteBatch.End();


        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_gameRenderTarget, _destinationRectangle, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}