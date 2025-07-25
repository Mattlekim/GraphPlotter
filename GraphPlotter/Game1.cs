﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;


namespace GraphPlotter
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;


        private string[] _files;
        public static string PthToData = $"{Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)}\\MarvinsAIRA Refactored\\Calibration";
        private List<int> _selectedItem = new List<int>();
        private int _highlightedItem = -1;

        SpriteFont _font;
        Texture2D _dot;

        public static MouseState MouseState, LMouseState;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _selectedItem.Add(0); // Start with the first item selected
            Plotter plotter = new Plotter(this);
            plotter.SetPosition(new Vector2(50, 50), 1800, 900);
          //  plotter.LoadData(@"C:\Users\m_bri\OneDrive\Documents\MarvinsAIRA Refactored\Recordings\Super Formula Lights 324.csv");
            Components.Add(plotter);

            if (!System.IO.Directory.Exists(PthToData))
            {
                System.IO.Directory.CreateDirectory(PthToData);
            }
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
        public static bool IsLoaded { get; private set; } = false;
        private static bool InvalidFile = false; // Used to exit the game from the plotter

        protected override void Update(GameTime gameTime)
        {
            LMouseState = MouseState;
            MouseState = Mouse.GetState();

            if (IsLoaded)
            {
                base.Update(gameTime);
                return;
            }
            _lkb = _kb;
            _kb = Keyboard.GetState();

           
            //_selectedItem = -1;

            for (int i = 0; i < _files.Length; i++)
            {

                if (MouseState.X > 40 && MouseState.X < 640 && MouseState.Y > (50 + i * 35) && MouseState.Y < (50 + i * 35 + 33))
                {

                    _highlightedItem = i;
                   
                    
                    if (MouseState.LeftButton == ButtonState.Pressed & LMouseState.LeftButton == ButtonState.Released)
                    {
                        if (_kb.IsKeyUp(Keys.LeftShift) && _kb.IsKeyUp(Keys.Right))
                            _selectedItem.Clear();

                        _selectedItem.Add(i);
                        
                    }
                    break;
                }
            }

            if (MouseState.LeftButton == ButtonState.Pressed & LMouseState.LeftButton == ButtonState.Released)
            {
                if (new Rectangle(1680, 950, 200, 100).Contains(MouseState.Position)) //click load
                {
                    IsLoaded = true;
                    Plotter plotter = (Plotter)this.Components[0];
                    foreach (var item in _selectedItem)
                    {
                        plotter.LoadData(_files[item], _quickView);
                    }

                    LMouseState = MouseState; // Reset mouse state to avoid immediate re-triggering
                }
                if (new Rectangle(1800, 800, 50, 50).Contains(MouseState.Position))
                {
                    _quickView = !_quickView;
                }
            }
            if (_kb.IsKeyDown(Keys.Down) && _lkb.IsKeyUp(Keys.Down))
            {
                if (_selectedItem.Count > 1)
                    _selectedItem.RemoveRange(1, _selectedItem.Count - 2);
                if (_selectedItem.Count == 0)
                    _selectedItem.Add(0);
                else
                if (_selectedItem[0] < _files.Length - 1)
                {
                    _selectedItem[0]++;
                }
            }

            if (_kb.IsKeyDown(Keys.Up) && _lkb.IsKeyUp(Keys.Up))
            {
                if (_selectedItem.Count > 1)
                    _selectedItem.RemoveRange(1, _selectedItem.Count - 2);
                if (_selectedItem.Count == 0)
                    _selectedItem.Add(0);
                else
                if (_selectedItem[0] > 0)
                {
                    _selectedItem[0]--;
                }
            }

            if (_kb.IsKeyDown(Keys.Enter) & _lkb.IsKeyUp(Keys.Enter))
            {
                if (_selectedItem.Count == 1)
                {
                    IsLoaded = true;
                    Plotter plotter = (Plotter)this.Components[0];
                    plotter.LoadData(_files[_selectedItem[0]], _quickView);
                  
                }
            }
            

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        public static void ReturnToFileSelect()
        {
            InvalidFile = false;
            IsLoaded = false;

        }

        public static void SetInvalidFile()
        {
            InvalidFile = true;
            InvalidFileTimer = 4;
            IsLoaded = false;
        }
        float colorFade = .2f;
        private static float InvalidFileTimer = 0;

        private bool _quickView = false;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            if (!IsLoaded)
            {
                _spriteBatch.Begin();

                if (_files.Length == 0)
                {
                    _spriteBatch.DrawString(_font, "No File Exist!", new Vector2(50, 50 + 35), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }

                for (int i = 0; i < _files.Length; i++)
                {
                    colorFade = .2f;

                    if (_highlightedItem == i)
                        colorFade = .3f;

                    if (_selectedItem.Contains(i))
                        colorFade = .5f;
                    _spriteBatch.Draw(_dot, new Rectangle(40, (int)(50 + i * 35 + 1), 600, 33), Color.LightBlue * colorFade);


                    _spriteBatch.DrawString(_font, _files[i].Substring(_files[i].LastIndexOf("\\") + 1), new Vector2(50, 50 + i * 35), Color.White, 0f, Vector2.Zero, .8f, SpriteEffects.None, 0f);
                }
            
                
                if (InvalidFile)
                {
                    _spriteBatch.Draw(_dot, new Rectangle(980,280, 300,230), Color.Red * .2f);
                    _spriteBatch.DrawString(_font, "Invalid File", new Vector2(1030, 300), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                    _spriteBatch.DrawString(_font, "Please try a\ndifferent file.", new Vector2(1000, 370), Color.White, 0f, Vector2.Zero, .8f, SpriteEffects.None, 0f);

                    InvalidFileTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (InvalidFileTimer <= 0)
                        InvalidFile = false;    
                }


                if (new Rectangle(1680, 950, 200, 100).Contains(MouseState.Position))
                    _spriteBatch.Draw(_dot, new Rectangle(1680, 950, 200, 100), Color.Green * .4f);
                else
                    _spriteBatch.Draw(_dot, new Rectangle(1680, 950, 200, 100), Color.Green * .2f);


                _spriteBatch.DrawString(_font, "Quick View", new Vector2(1550, 800), Color.White);
                _spriteBatch.Draw(_dot, new Rectangle(1800, 800, 50, 50), Color.White);
                _spriteBatch.Draw(_dot, new Rectangle(1802, 802, 46, 46), Color.Black);

                if (_quickView)
                    _spriteBatch.Draw(_dot, new Rectangle(1802, 802, 46, 46), Color.Red);

                _spriteBatch.DrawString(_font, "Load", new Vector2(1730, 980), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                _spriteBatch.End();
            }

            


            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
