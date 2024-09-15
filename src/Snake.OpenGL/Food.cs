using Microsoft.Xna.Framework.Graphics;
using SnakeGame.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGame;

public sealed class Food : DrawableGameComponent
{
	private Texture2D _texture;
	private SpriteBatch _spriteBatch;

	public Food(Game game, Point position) : base(game)
	{
		Position = position;
	}

	public Point Position { get; set; }

	protected override void LoadContent()
	{
		_texture = Game.Content.Load<Texture2D>("apple");
		_spriteBatch = ((Game1)Game).SpriteBatch;
	}

	public override void Update(GameTime gameTime)
	{
	}

	public override void Draw(GameTime gameTime)
	{
		//_spriteBatch.Draw(_texture, new Rectangle(Position.X * Tiles.Size, Position.Y * Tiles.Size, Tiles.Size, Tiles.Size), Color.Red);
		_spriteBatch.Draw(
				texture: _texture,
				position: new Vector2((Position.X + 0.5f) * Tiles.Size, (Position.Y + 0.5f) * Tiles.Size),
				sourceRectangle: null,
				color: Color.White,
				rotation: 0,
				scale: Tiles.Size / (float)_texture.Width * 1.25f,
				origin: new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f),
				effects: SpriteEffects.None,
				layerDepth: 0);
	}
}
