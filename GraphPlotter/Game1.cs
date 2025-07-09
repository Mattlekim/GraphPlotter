using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;


namespace GraphPlotter
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;


        private string[] _files;
        public static string PthToData = $"{Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)}\\MarvinsAIRA Refactored\\Recordings";
        private int _selectedItem = 0;

        SpriteFont _font;
        Texture2D _dot;

        MouseState _mouseState, _lmouseState;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            Plotter plotter = new Plotter(this);
            plotter.SetPosition(new Vector2(50, 50), 1800, 900);
          //  plotter.LoadData(@"C:\Users\m_bri\OneDrive\Documents\MarvinsAIRA Refactored\Recordings\Super Formula Lights 324.csv");
            Components.Add(plotter);

            _files = System.IO.Directory.GetFiles(PthToData, "*.csv");
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");

            _dot = new Texture2D(GraphicsDevice, 1, 1);
            _dot.SetData<Color>(new Color[] { Color.White});
            // TODO: use this.Content to load your game content here
        }

        KeyboardState _kb, _lkb;
        private bool _isLoaded = false;
       
        protected override void Update(GameTime gameTime)
        {

            if (_isLoaded)
            {
                base.Update(gameTime);
                return;
            }
            _lkb = _kb;
            _kb = Keyboard.GetState();

            _lmouseState = _mouseState;
            _mouseState = Mouse.GetState();
            //_selectedItem = -1;

            for (int i = 0; i < _files.Length; i++)
            {

                if (_mouseState.X > 40 && _mouseState.X < 640 && _mouseState.Y > (50 + i * 35) && _mouseState.Y < (50 + i * 35 + 33))
                {
                    _selectedItem = i;
                    if (_mouseState.LeftButton == ButtonState.Pressed & _lmouseState.LeftButton == ButtonState.Released)
                    {
                        _isLoaded = true;
                        Plotter plotter = (Plotter)this.Components[0];
                        plotter.LoadData(_files[_selectedItem]);
                    }
                    break;
                }
            }

            if (_kb.IsKeyDown(Keys.Down) && _lkb.IsKeyUp(Keys.Down))
            {
                if (_selectedItem < _files.Length - 1)
                {
                    _selectedItem++;
                }
            }

            if (_kb.IsKeyDown(Keys.Up) && _lkb.IsKeyUp(Keys.Up))
            {
                if (_selectedItem > 0)
                {
                    _selectedItem--;
                }
            }



            if (_kb.IsKeyDown(Keys.Enter) & _lkb.IsKeyUp(Keys.Enter))
            {
                _isLoaded = true;  
                Plotter plotter = (Plotter)this.Components[0];
                plotter.LoadData(_files[_selectedItem]);
            }
            

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        float colorFade = .2f;
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            if (!_isLoaded)
            {
                _spriteBatch.Begin();

                for (int i = 0; i < _files.Length; i++)
                {
                    colorFade = .2f;
                    if (i == _selectedItem)
                        colorFade = .5f;
                    _spriteBatch.Draw(_dot, new Rectangle(40, (int)(50 + i * 35 + 1), 600, 33), Color.LightBlue * colorFade);


                    _spriteBatch.DrawString(_font, _files[i].Substring(_files[i].LastIndexOf("\\") + 1), new Vector2(50, 50 + i * 35), Color.White, 0f, Vector2.Zero, .8f, SpriteEffects.None, 0f);
                }
               
                _spriteBatch.End();
            }

       
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
