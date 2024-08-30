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

		private readonly Point _mapSize;
		private Texture2D _texture;
		private SpriteBatch _spriteBatch;
		private bool _isMoving;
		private bool _isDead;
		private int _bodyLength;
		private Vector2 _virtualPosition;
		private Queue<Point> _bodyPositions = new Queue<Point>();
		private SnakeDirection _direction;
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

		protected override void LoadContent()
		{
			_texture = Game.Content.Load<Texture2D>("Circle");
			_spriteBatch = ((Game1)Game).SpriteBatch;
		}

		/// <summary>
		/// In tiles.
		/// </summary>
		public Point HeadPosition { get; set; }

		public void Eat()
		{
			++_bodyLength;
			_speed += 0.3f;
			_speed = Math.Min(_speed, 6);
		}

		public IEnumerable<Point> GetAllPositionsOccupied()
		{
			yield return HeadPosition;
			foreach (var position in _bodyPositions)
			{
				yield return position;
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

			var gamePadState = GamePad.GetState(PlayerIndex.One);

			if (Keyboard.GetState().IsKeyDown(Keys.Left) || gamePadState.IsButtonDown(Buttons.DPadLeft) || gamePadState.IsButtonDown(Buttons.LeftThumbstickLeft))
			{
				_isMoving = true;
				_direction = SnakeDirection.Left;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Right) || gamePadState.IsButtonDown(Buttons.DPadRight) || gamePadState.IsButtonDown(Buttons.LeftThumbstickRight))
			{
				_isMoving = true;
				_direction = SnakeDirection.Right;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Up) || gamePadState.IsButtonDown(Buttons.DPadUp) || gamePadState.IsButtonDown(Buttons.LeftThumbstickUp))
			{
				_isMoving = true;
				_direction = SnakeDirection.Up;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Down) || gamePadState.IsButtonDown(Buttons.DPadDown) || gamePadState.IsButtonDown(Buttons.LeftThumbstickDown))
			{
				_isMoving = true;
				_direction = SnakeDirection.Down;
			}
			
			if (_isMoving)
			{
				_virtualPosition += GetDirectionVector(_direction) * (_speed * Tiles.Size) * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
			var newPosition = Tiles.ToTilePosition(_virtualPosition);
			if (newPosition != HeadPosition)
			{
				if (_bodyPositions.Any(b => b == newPosition))
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

				_bodyPositions.Enqueue(HeadPosition);
				if (_bodyPositions.Count > _bodyLength)
				{
					_bodyPositions.Dequeue();
				}
				HeadPosition = newPosition;
			}
		}

		public override void Draw(GameTime gameTime)
		{
			var headColor = _isDead ? Color.Gray : Color.DarkOrange;
			var bodyColor = _isDead ? Color.DarkGray : Color.Orange;

			_spriteBatch.Draw(_texture, new Rectangle(HeadPosition.X * Tiles.Size, HeadPosition.Y * Tiles.Size, Tiles.Size, Tiles.Size), headColor);
			foreach (var position in _bodyPositions)
			{
				_spriteBatch.Draw(_texture, new Rectangle(position.X * Tiles.Size, position.Y * Tiles.Size, Tiles.Size, Tiles.Size), bodyColor);
			}
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
