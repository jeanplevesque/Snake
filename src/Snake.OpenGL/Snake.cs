using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SnakeGame.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGame
{
	public sealed class Snake : DrawableGameComponent
	{
		private enum SnakeDirection
		{
			Up,
			Down,
			Left,
			Right
		}

		private struct BodyPart
		{
			public BodyPart(Point position, Point destination, Point previousPosition)
			{
				Position = position;
				Destination = destination;
				PreviousPosition = previousPosition;
			}

			public Point Position { get; set; }
			public Point Destination { get; }
			public Point PreviousPosition { get; }
		}

		private const float _textureScale = 12f;
		private static readonly Random _random = new Random();

		private readonly Point _mapSize;
		private Texture2D _texture;
		private Texture2D _skinTexture;
		private Texture2D _headTexture;
		private SpriteBatch _spriteBatch;
		private bool _isMoving;
		private bool _isDead;
		private int _bodyLength;
		private Vector2 _virtualPosition;
		private Queue<BodyPart> _bodyPositions = new Queue<BodyPart>();
		private List<Rectangle> _skinSamples = new List<Rectangle>();
		private SnakeDirection _direction = SnakeDirection.Right;
		private SnakeDirection _desiredDirection = SnakeDirection.Right;
		private Point _neckPosition;
		private Point _skinTextureMaxIndices;
		private List<SoundEffect> _biteSoundEffects = [];
		/// <summary>
		/// In tiles per second.
		/// </summary>
		private float _speed { get; set; } = 3;

		public Snake(Game game, Point position, Point mapSize) : base(game)
		{
			HeadPosition = position;
			_mapSize = mapSize;
			_virtualPosition = Tiles.ToWorldPosition(HeadPosition);
		}

		public bool IsDead => _isDead;

		protected override void LoadContent()
		{
			var maskTexture = _texture = Game.Content.Load<Texture2D>("Circle");
			var skinTexture = Game.Content.Load<Texture2D>("snake-skin");
			_headTexture = Game.Content.Load<Texture2D>("snake-head");
			_spriteBatch = ((Game1)Game).SpriteBatch;

			// Use the circle texture to mask the skin texture in tiles.
			_skinTexture = new Texture2D(GraphicsDevice, skinTexture.Width, skinTexture.Height);
			var skinColors = new Color[skinTexture.Width * skinTexture.Height];
			var maskColors = new Color[maskTexture.Width * maskTexture.Height];
			skinTexture.GetData<Color>(skinColors);
			maskTexture.GetData<Color>(maskColors);
			var tileSize = (int)Math.Ceiling(Tiles.Size * _textureScale);
			for (int i = 0; i < skinColors.Length; ++i)
			{
				var skinPixelPosition = new Point(i % skinTexture.Width, i / skinTexture.Width);
				var skinTileIndex = new Point(skinPixelPosition.X / tileSize, skinPixelPosition.Y / tileSize);
				var skinRelativePixelPosition = new Point(skinPixelPosition.X % tileSize, skinPixelPosition.Y % tileSize);
				var tileUV = new Vector2(skinRelativePixelPosition.X / (float)tileSize, skinRelativePixelPosition.Y / (float)tileSize);

				var maskPixelPosition = tileUV * new Vector2(maskTexture.Width, maskTexture.Height);
				var maskPixel = maskColors[(int)maskPixelPosition.Y * maskTexture.Width + (int)maskPixelPosition.X];
				skinColors[i] = skinColors[i] * (maskPixel.A / 256f);
			}
			_skinTexture.SetData(skinColors);
			_skinTextureMaxIndices = new Point(skinTexture.Width / tileSize, skinTexture.Height / tileSize);

			for (int i = 0; i < 5; ++i)
			{
				_biteSoundEffects.Add(Game.Content.Load<SoundEffect>($"bite{i}"));
			}
		}

		/// <summary>
		/// In tiles.
		/// </summary>
		public Point HeadPosition { get; set; }

		public void Eat()
		{
			++_bodyLength;
			var size = (int)Math.Ceiling(Tiles.Size * _textureScale);
			_skinSamples.Add(new Rectangle(
				_random.Next(_skinTextureMaxIndices.X) * size,
				_random.Next(_skinTextureMaxIndices.Y) * size,
				size,
				size
			));
			_speed += 0.3f;
			_speed = Math.Min(_speed, 6);
			var soundEffect = _biteSoundEffects[_random.Next(_biteSoundEffects.Count)];
			var soundInstance = soundEffect.CreateInstance();
			soundInstance.Volume = 0.8f;
			soundInstance.Play();
		}

		public IEnumerable<Point> GetAllPositionsOccupied()
		{
			yield return HeadPosition;
			foreach (var position in _bodyPositions)
			{
				yield return position.Position;
			}
		}

		public void Die()
		{
			_isDead = true;
			_isMoving = false;
		}

		public override void Update(GameTime gameTime)
		{
			if (_isDead)
			{
				return;
			}

			Steer();

			if (_isMoving)
			{
				_virtualPosition += GetDirectionVector(_direction) * (_speed * Tiles.Size) * (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (_direction == SnakeDirection.Left || _direction == SnakeDirection.Right)
				{
					_virtualPosition.Y = HeadPosition.Y * Tiles.Size + Tiles.Size / 2;
				}
				else
				{
					_virtualPosition.X = HeadPosition.X * Tiles.Size + Tiles.Size / 2;
				}
			}
			var newPosition = Tiles.ToTilePosition(_virtualPosition);
			if (newPosition != HeadPosition)
			{
				_neckPosition = HeadPosition;
				if (_bodyPositions.Any(b => b.Position == newPosition))
				{
					Die();
					return;
				}

				if (newPosition.X < 0 ||
					newPosition.Y < 0 ||
					newPosition.X >= _mapSize.X ||
					newPosition.Y >= _mapSize.Y)
				{
					Die();
					return;
				}

				if (_bodyPositions.Count > 0)
				{
					_bodyPositions.Enqueue(new BodyPart(HeadPosition, newPosition, _bodyPositions.Last().Position));
				}
				else
				{
					_bodyPositions.Enqueue(new BodyPart(HeadPosition, newPosition, HeadPosition));
				}
				if (_bodyPositions.Count > _bodyLength)
				{
					_bodyPositions.Dequeue();
				}
				HeadPosition = newPosition;
			}
		}

		private void Steer()
		{
			var gamePadState = GamePad.GetState(PlayerIndex.One);

			if (Keyboard.GetState().IsKeyDown(Keys.Left) || gamePadState.IsButtonDown(Buttons.DPadLeft) || gamePadState.IsButtonDown(Buttons.LeftThumbstickLeft))
			{
				if (_bodyLength == 0 || _neckPosition.Y != HeadPosition.Y)
				{
					SetDesiredDirection(SnakeDirection.Left);
				}
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Right) || gamePadState.IsButtonDown(Buttons.DPadRight) || gamePadState.IsButtonDown(Buttons.LeftThumbstickRight))
			{
				if (_bodyLength == 0 || _neckPosition.Y != HeadPosition.Y)
				{
					SetDesiredDirection(SnakeDirection.Right);
				}
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Up) || gamePadState.IsButtonDown(Buttons.DPadUp) || gamePadState.IsButtonDown(Buttons.LeftThumbstickUp))
			{
				if (_bodyLength == 0 || _neckPosition.X != HeadPosition.X)
				{
					SetDesiredDirection(SnakeDirection.Up);
				}
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Down) || gamePadState.IsButtonDown(Buttons.DPadDown) || gamePadState.IsButtonDown(Buttons.LeftThumbstickDown))
			{
				if (_bodyLength == 0 || _neckPosition.X != HeadPosition.X)
				{
					SetDesiredDirection(SnakeDirection.Down);
				}
			}

			var headDeltaPosition = _virtualPosition - Tiles.ToWorldPosition(HeadPosition);
			var headDirection = GetDirectionVector(_direction);
			var dot = Vector2.Dot(headDeltaPosition, headDirection);
			var headDelta = headDeltaPosition.Length() * Math.Sign(dot);

			// Prevent the snake from turning at the wrong time, causing it to move backwards.
			if (headDelta > 0 && headDelta < Tiles.Size / 8.0)
			{
				if (_desiredDirection != _direction)
				{
					_direction = _desiredDirection;
				}
			}
		}

		private void SetDesiredDirection(SnakeDirection direction)
		{
			if (!_isMoving)
			{
				_direction = direction;
			}
			_isMoving = true;
			_desiredDirection = direction;
		}

		public override void Draw(GameTime gameTime)
		{
			//var headColor = _isDead ? Color.Gray : Color.DarkOrange;
			//var bodyColor = _isDead ? Color.DarkGray : Color.Orange;

			var headColor = Color.DarkOrange;
			var bodyColor = Color.Orange;

			var headDeltaPosition = _virtualPosition - Tiles.ToWorldPosition(HeadPosition);
			var headDirection = GetDirectionVector(_direction);
			var dot = Vector2.Dot(headDeltaPosition, headDirection);
			var headDelta = headDeltaPosition.Length() * Math.Sign(dot);

			int i = _bodyPositions.Count - 1;
			foreach (var bodyPart in _bodyPositions)
			{
				var position = bodyPart.Position;
				var direction1 = (position - bodyPart.PreviousPosition).ToVector2();
				var direction2 = (bodyPart.Destination - position).ToVector2();
				direction1.Normalize();
				direction2.Normalize();
				var direction = headDelta < 0 ? direction1 : direction2;
				var smoothPosition = Tiles.ToWorldPosition(position) + direction * headDelta;
				//_spriteBatch.Draw(_texture, new Rectangle((int)(smoothPosition.X - Tiles.Size / 2), (int)(smoothPosition.Y - Tiles.Size / 2), Tiles.Size, Tiles.Size), bodyColor);
				var textureDirection = Vector2.Lerp(direction1, direction2, headDelta / Tiles.Size + 0.5f);
				var angle = MathF.Atan2(textureDirection.Y, textureDirection.X);
				_spriteBatch.Draw(
					texture: _skinTexture,
					position: smoothPosition,
					sourceRectangle: _skinSamples[i],
					color: Color.White,
					rotation: angle,
					scale: 1 / _textureScale,
					origin: Vector2.One * 0.5f * Tiles.Size * _textureScale,
					effects: SpriteEffects.None,
					layerDepth: 0);
				--i;
			}

			//_spriteBatch.Draw(_texture, new Rectangle((int)_virtualPosition.X - Tiles.Size / 2, (int)_virtualPosition.Y - Tiles.Size / 2, Tiles.Size, Tiles.Size), headColor);
			var desiredDirection = GetDirectionVector(_desiredDirection);
			var headTextureDirection = _desiredDirection != _direction ? desiredDirection + headDirection : headDirection;
			var headTextureAngle = MathF.Atan2(headTextureDirection.Y, headTextureDirection.X) - MathHelper.Pi;
			_spriteBatch.Draw(
				texture: _headTexture,
				position: _virtualPosition,
				sourceRectangle: null,
				color: Color.White,
				rotation: headTextureAngle,
				scale: Tiles.Size / (float)_headTexture.Width * 1.25f,
				origin: Vector2.One * 0.5f * _headTexture.Width,
				effects: SpriteEffects.None,
				layerDepth: 0);

			// Uncomment to show the tiled masked snake skin atlas.
			//_spriteBatch.Draw(_skinTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1 / 4f, SpriteEffects.None, 0);
		}

		private static Vector2 GetDirectionVector(SnakeDirection direction)
		{
			return direction switch
			{
				SnakeDirection.Up => new Vector2(0, -1),
				SnakeDirection.Down => new Vector2(0, 1),
				SnakeDirection.Left => new Vector2(-1, 0),
				SnakeDirection.Right => new Vector2(1, 0),
				_ => throw new ArgumentOutOfRangeException(nameof(direction))
			};
		}
	}
}
