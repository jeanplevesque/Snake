using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace SnakeGame.OpenGL
{
	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private Point _mapSize;
		private Random _random = new Random();
		private Snake _snake;
		private Food _food;

		public Game1()
		{
			_mapSize = new Point(10, 10);
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = _mapSize.X * Tiles.Size,
				PreferredBackBufferHeight = _mapSize.Y * Tiles.Size
			};
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		public SpriteBatch SpriteBatch => _spriteBatch;

		protected override void Initialize()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);
			var snakePosition = new Point(_mapSize.X / 2, _mapSize.Y / 2);

			_snake = new Snake(this, snakePosition, _mapSize);
			_food = new Food(this, snakePosition + new Point(2, 0));

			Components.Add(_food);
			Components.Add(_snake);

			base.Initialize();
		}

		protected override void LoadContent()
		{
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			if (_snake.HeadPosition == _food.Position)
			{
				_snake.Eat();
				var availablePositions = Enumerable.Range(0, _mapSize.X * _mapSize.Y)
					.Select(i => new Point(i % _mapSize.X, i / _mapSize.X))
					.Except(_snake.GetAllPositionsOccupied())
					.Except(new[] { _food.Position })
					.ToArray();
				var newFoodPosition = availablePositions[_random.Next(0, availablePositions.Length)];
				_food.Position = newFoodPosition;
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.OliveDrab);

			_spriteBatch.Begin();
			base.Draw(gameTime);
			_spriteBatch.End();
		}
	}
}
