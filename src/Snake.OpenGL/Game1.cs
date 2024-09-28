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
		private Texture2D _grassTexture;
		private SpriteFont _font;

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
			_grassTexture = Content.Load<Texture2D>("grass");
			_deathScreen = Content.Load<Texture2D>("you-died");
			_deathScreenSound = Content.Load<SoundEffect>("you-died-sound");
			_grayScaleEffect = Content.Load<Effect>("Gray-Scale");
			_grayScaleEffect.CurrentTechnique = _grayScaleEffect.Techniques["BasicColorDrawing"];
			_font = Content.Load<SpriteFont>("font");
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
					sound.Volume = 0.5f;
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
			//GraphicsDevice.Clear(GrayScale(Color.OliveDrab, _deathScreenOpacity));

			_spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, _grayScaleEffect, null);
			_spriteBatch.Draw(_grassTexture, Vector2.UnitX * -200, null, new Color(Vector3.One * 0.8f), rotation: 0f, origin: Vector2.Zero, scale: 0.2f, effects: SpriteEffects.None, layerDepth: 0);
			base.Draw(gameTime);
			// Draw the score.
			if (!_snake.IsDead)
			{
				const float scale = 0.75f;
				var scoreText = $"score: {_snake.Length}";
				var scoreSize = _font.MeasureString(scoreText) * scale;
				_spriteBatch.DrawString(_font, scoreText, (_mapSize.ToVector2() * Tiles.Size - scoreSize - Vector2.One * 7).ToPoint().ToVector2(), Color.Black * 0.95f, 0, Vector2.Zero, Vector2.One * scale, SpriteEffects.None, 0, false);
				_spriteBatch.DrawString(_font, scoreText, (_mapSize.ToVector2() * Tiles.Size - scoreSize - Vector2.One * 8).ToPoint().ToVector2(), Color.White * 0.95f, 0, Vector2.Zero, Vector2.One * scale, SpriteEffects.None, 0, false);
			}
			_spriteBatch.End();

			if (_snake.IsDead)
			{
				_spriteBatch.Begin(sortMode: SpriteSortMode.Deferred);
				// Draw the death screen in the center of the screen.
				_spriteBatch.Draw(_deathScreen, new Vector2(_mapSize.X * Tiles.Size / 2, _mapSize.Y * Tiles.Size / 2), null, Color.White * _deathScreenOpacity, 0, new Vector2(_deathScreen.Width / 2, _deathScreen.Height / 2), 1, SpriteEffects.None, 0);

				// Draw the final score.
				var scoreText = $"final score: {_snake.Length}";
				var scoreSize = _font.MeasureString(scoreText);
				_spriteBatch.DrawString(_font, scoreText, (_mapSize.ToVector2() * Tiles.Size / 2 - scoreSize / 2 + Vector2.UnitY * _mapSize.Y * Tiles.Size * 0.25f + Vector2.One).ToPoint().ToVector2(), Color.Black * _deathScreenOpacity);
				_spriteBatch.DrawString(_font, scoreText, (_mapSize.ToVector2() * Tiles.Size / 2 - scoreSize / 2 + Vector2.UnitY * _mapSize.Y * Tiles.Size * 0.25f).ToPoint().ToVector2(), Color.White * _deathScreenOpacity);

				// Draw the restart text.
				const float restartScale = 0.75f;
				var restartText = "Press Enter to restart.";
				var restartSize = _font.MeasureString(restartText) * restartScale;
				_spriteBatch.DrawString(_font, restartText, (_mapSize.ToVector2() * Tiles.Size / 2 - restartSize / 2 + Vector2.UnitY * _mapSize.Y * Tiles.Size * 0.35f + Vector2.One).ToPoint().ToVector2(), Color.Black * _deathScreenOpacity, 0, Vector2.Zero, Vector2.One * restartScale, SpriteEffects.None, 0, false);
				_spriteBatch.DrawString(_font, restartText, (_mapSize.ToVector2() * Tiles.Size / 2 - restartSize / 2 + Vector2.UnitY * _mapSize.Y * Tiles.Size * 0.35f).ToPoint().ToVector2(), Color.White * _deathScreenOpacity, 0, Vector2.Zero, Vector2.One * restartScale, SpriteEffects.None, 0, false);
				_spriteBatch.End();
			}
		}

		private static Color GrayScale(Color color, float percentage)
		{
			var gray = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 256f;
			return Color.Lerp(color, new Color(gray, gray, gray, color.A), percentage);
		}
	}
}
