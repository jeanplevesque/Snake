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

		private const float _textureScale = 8f;
		private static readonly Random _random = new Random();

		private readonly Point _mapSize;
		private Texture2D _texture;
		private Texture2D _skinTexture;
		private SpriteBatch _spriteBatch;
		private bool _isMoving;
		private bool _isDead;
		private int _bodyLength;
		private Vector2 _virtualPosition;
		private Queue<BodyPart> _bodyPositions = new Queue<BodyPart>();
		private List<Rectangle> _skinSamples = new List<Rectangle>();
		private SnakeDirection _direction;
		private SnakeDirection _desiredDirection;
		private Point _neckPosition;
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
			_texture = Game.Content.Load<Texture2D>("Circle");
			_skinTexture = Game.Content.Load<Texture2D>("snake-skin");
			_spriteBatch = ((Game1)Game).SpriteBatch;
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
				_random.Next(_skinTexture.Width - size),
				_random.Next(_skinTexture.Height - size),
				size,
				size
			));
			_speed += 0.3f;
			_speed = Math.Min(_speed, 6);
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
				var direction = (headDelta < 0 ? position - bodyPart.PreviousPosition : bodyPart.Destination - position).ToVector2();
				direction.Normalize();
				var smoothPosition = Tiles.ToWorldPosition(position) + direction * headDelta;
				//_spriteBatch.Draw(_texture, new Rectangle((int)(smoothPosition.X - Tiles.Size / 2), (int)(smoothPosition.Y - Tiles.Size / 2), Tiles.Size, Tiles.Size), bodyColor);
				var angle =  MathF.Atan2(direction.Y, direction.X);
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

			_spriteBatch.Draw(_texture, new Rectangle((int)_virtualPosition.X - Tiles.Size / 2, (int)_virtualPosition.Y - Tiles.Size / 2, Tiles.Size, Tiles.Size), headColor);
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
