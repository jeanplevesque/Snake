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
		_texture = Game.Content.Load<Texture2D>("Circle");
		_spriteBatch = ((Game1)Game).SpriteBatch;
	}

	public override void Update(GameTime gameTime)
	{
	}

	public override void Draw(GameTime gameTime)
	{
		_spriteBatch.Draw(_texture, new Rectangle(Position.X * Tiles.Size, Position.Y * Tiles.Size, Tiles.Size, Tiles.Size), Color.Red);
	}
}
