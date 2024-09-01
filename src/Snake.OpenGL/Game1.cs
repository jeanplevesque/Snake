using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
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
		private Texture2D _deathScreen;
		private float _deathScreenOpacity = 0;
		private SoundEffect _deathScreenSound;
		private bool _isDeathSoundPlaying;
		private Effect _grayScaleEffect;

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
			SetupGame();

			base.Initialize();
		}

		private void SetupGame()
		{
			var snakePosition = new Point(_mapSize.X / 2, _mapSize.Y / 2);

			_snake = new Snake(this, snakePosition, _mapSize);
			_food = new Food(this, snakePosition + new Point(2, 0));

			Components.Add(_food);
			Components.Add(_snake);
		}

		protected override void LoadContent()
		{
			_deathScreen = Content.Load<Texture2D>("you-died");
			_deathScreenSound = Content.Load<SoundEffect>("you-died-sound");
			_grayScaleEffect = Content.Load<Effect>("Gray-Scale");
			_grayScaleEffect.CurrentTechnique = _grayScaleEffect.Techniques["BasicColorDrawing"];
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			if (_snake.IsDead)
			{
				if (!_isDeathSoundPlaying)
				{
					var sound = _deathScreenSound.CreateInstance();
					sound.Volume = 0.3f;
					sound.Play();
					_isDeathSoundPlaying = true;
				}
				_deathScreenOpacity = Math.Clamp(_deathScreenOpacity + (float)gameTime.ElapsedGameTime.TotalSeconds * 0.3f, 0, 1);
				_grayScaleEffect.Parameters["percent"].SetValue(_deathScreenOpacity);

				if (Keyboard.GetState().IsKeyDown(Keys.Enter) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.Start))
				{
					_isDeathSoundPlaying = false;
					_deathScreenOpacity = 0;
					_grayScaleEffect.Parameters["percent"].SetValue(0);

					Components.Remove(_snake);
					Components.Remove(_food);
					SetupGame();
				}
			}

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
			GraphicsDevice.Clear(GrayScale(Color.OliveDrab, _deathScreenOpacity));

			_spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, _grayScaleEffect, null);
			base.Draw(gameTime);
			_spriteBatch.End();

			_spriteBatch.Begin();
			if (_snake.IsDead)
			{
				// Draw the death screen in the center of the screen.
				_spriteBatch.Draw(_deathScreen, new Vector2(_mapSize.X * Tiles.Size / 2, _mapSize.Y * Tiles.Size / 2), null, Color.White * _deathScreenOpacity, 0, new Vector2(_deathScreen.Width / 2, _deathScreen.Height / 2), 1, SpriteEffects.None, 0);
			}
			_spriteBatch.End();
		}

		private static Color GrayScale(Color color, float percentage)
		{
			var gray = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 256f;
			return Color.Lerp(color, new Color(gray, gray, gray, color.A), percentage);
		}
	}
}
