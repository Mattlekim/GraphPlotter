using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Input;
namespace GraphPlotter
{
 

    public struct GraphDataPoint
    {
        public float Speed;
        public float SteeringAngle;
        public List<float> YawRates;
    }

    public class GraphData
    {
        public string Name;

        public List<GraphDataPoint> Points;

        public static implicit operator GraphData(string filePath)
        {
            GraphData data = new GraphData();


            string[,] _data = new string[50, 300];
            int nuberOfColums = 0;
            int lineNumber = 0;
            using (TextReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Split line by comma (basic CSV parsing)
                    string[] fields = line.Split(',');

                    if (fields.Length > 2)
                    {
                        nuberOfColums = fields.Length;
                        if (_data[0, lineNumber] == "0")
                            continue;
                        for (int i = 0; i < fields.Length; i++)
                            //if (lineNumber > 0)
                                _data[i, lineNumber] = fields[i];
                          
                        lineNumber++;
                    }
                    // Display each field
                    Console.WriteLine("Row:");
                    foreach (string field in fields)
                    {
                        Console.WriteLine($"  {field}");
                    }
                }
            }

            data.Points = new List<GraphDataPoint>();
            GraphDataPoint point = new GraphDataPoint();
            point.YawRates = new List<float>();
            for (int colums = 1; colums < nuberOfColums; colums++)
            {
                point.Speed = -1;
                point.YawRates.Clear();
                for (int row = 1; row < 300; row++)
                {
                    if (_data[colums, row] == string.Empty)
                        continue;
                  

                    point.Speed = Convert.ToSingle(_data[0, row]);
                    point.SteeringAngle = Convert.ToSingle(_data[colums, 0]);

                    point.YawRates.Add(Convert.ToSingle(_data[colums, row]));
                }

                GraphDataPoint p = new GraphDataPoint();
                p.Speed = point.Speed;
                p.SteeringAngle = point.SteeringAngle;
                p.YawRates = new List<float>();
                foreach (float f in point.YawRates)
                    p.YawRates.Add(f);
                data.Points.Add(p);
            }


            return data;

        }


    }

    public class Plotter : DrawableGameComponent
    {
        private Texture2D _dot;

        private int _width;
        private int _height;

        private Vector2 _position;

        private float _axisXMidPoint;

        private List<GraphData> _data = new List<GraphData>();

        private string _axisXLable = string.Empty, _axisYLable = string.Empty;

        SpriteBatch _spriteBatch;

        TireGripPrediction _gripPrediction;
        SpriteFont _font;
        private KeyboardState _kb, _lkb;

        public Plotter(Game game) : base(game)
        {
        }

        public void LoadData(string filepath)
        {
            if (File.Exists(filepath))
            {
                _data.Add(filepath);

                //now lets make the prediction alg
                _gripPrediction = new TireGripPrediction(_data[0]);
            }
        }

        public void SetPosition(Vector2 pos, int width, int height)
        {
            _position = pos;
            _width = width;
            _height = height;

            _axisXMidPoint = height / 2 + pos.Y;
        }

        protected override void LoadContent()
        {
            _dot = new Texture2D(this.GraphicsDevice, 1, 1);
            _dot.SetData<Color>(new Color[1] { Color.White });

            _spriteBatch = new SpriteBatch(this.GraphicsDevice);

            _font = Game.Content.Load<SpriteFont>("font");
            base.LoadContent();
        }

        private float _keyHoldTimeUp = 0, _keyHoldTimeDown = 0;
        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lkb = _kb;

            _kb = Keyboard.GetState();
            if (_data.Count == 0)
            {
                return;
            }

            if (_kb.IsKeyDown(Keys.Down))
                {
                if (_keyHoldTimeDown >= 1)
                    _angleToPredict--;
                _keyHoldTimeDown += dt;
                
            }
            else
                _keyHoldTimeDown =0;


            if (_kb.IsKeyDown(Keys.Up))
            {
                if (_keyHoldTimeUp >= 1)
                    _angleToPredict++;
                _keyHoldTimeUp += dt;
            }
            else
                _keyHoldTimeUp = 0;


            if (_kb.IsKeyDown(Keys.Down) && _lkb.IsKeyUp(Keys.Down))
                _angleToPredict--;

          

            if (_kb.IsKeyDown(Keys.Up) && _lkb.IsKeyUp(Keys.Up))
                _angleToPredict++;
          
            base.Update(gameTime);
        }

        private float _angleToPredict = 20;

        public const float YScale = 500f;
        public const float XScale = 30;      
        Color[] _colors = new Color[] { Color.Red, Color.Blue, Color.Gray, Color.Green, Color.Yellow, Color.White, Color.CornflowerBlue, Color.Chocolate, Color.Crimson, Color.Pink, Color.LightBlue, Color.LightGreen, Color.OrangeRed, Color.PaleGreen,
        Color.Salmon, Color.Brown, Color.Orange, Color.OrangeRed, Color.DarkCyan, Color.DarkGreen, Color.DarkOrange};
        public override void Draw(GameTime gameTime)
        {
            if (_data.Count == 0)
            {
                base.Draw(gameTime);
                return;
            }

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            _spriteBatch.Draw(_dot, new Rectangle((int)_position.X, (int)_position.Y, 2, _height), Color.White);
            _spriteBatch.Draw(_dot, new Rectangle((int)_position.X, (int)(_axisXMidPoint + _position.Y), _width, 2), Color.White);

           
               for (int sAngle = 0; sAngle < _data[0].Points.Count; sAngle++)
           //int sAngle = 25;
                for (int speed = 0; speed < _data[0].Points[sAngle].YawRates.Count - 1; speed++)
                    if (_data[0].Points[sAngle].YawRates[speed + 1] != 0)
                    {
                        _spriteBatch.DrawLine(_dot, new Vector2(speed * XScale + _position.X, _position.Y + _data[0].Points[sAngle].YawRates[speed] * YScale + _axisXMidPoint),
                            new Vector2((speed + 1) * XScale + _position.X, _position.Y + _data[0].Points[sAngle].YawRates[speed + 1] * YScale + _axisXMidPoint), Color.DarkRed, 3f);

                    //_spriteBatch.DrawLine(_dot, new Vector2(speed * XScale + _position.X, _position.Y + _gripPrediction.Predict(speed, _data[0].Points[sAngle].SteeringAngle) * YScale + _axisXMidPoint),
                     //       new Vector2((speed + 1) * XScale + _position.X, _position.Y + _gripPrediction.Predict(speed + 1, _data[0].Points[sAngle].SteeringAngle) * YScale + _axisXMidPoint), Color.White * .9f, 3f);
                    // _spriteBatch.Draw(_dot, new Vector2(speed * 10 + _position.X, _position.Y + _data[0].Points[sAngle].YawRates[speed] * YScale + _axisXMidPoint), _colors[sAngle]);
                }

            for (int x=0;x<300 - 1;x++)
            {
                _spriteBatch.DrawLine(_dot, new Vector2(x * XScale + _position.X, _position.Y + _gripPrediction.Predict(x, _angleToPredict) * YScale + _axisXMidPoint),
                            new Vector2((x + 1) * XScale + _position.X, _position.Y + _gripPrediction.Predict(x + 1, _angleToPredict) * YScale + _axisXMidPoint), Color.Blue, 3f);
            }
            _spriteBatch.DrawString(_font, $"Steering Angle {_angleToPredict}", new Vector2(1500, 50), Color.White, 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
