using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "generic")]
    public class TileBehaviour
    {
        private static Dictionary<string, Type> s_tileBehaviours = new Dictionary<string, Type>();

        public static void RegisterBehavioursFrom(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                foreach (TileBehaviourAttribute behaviour in type.GetCustomAttributes(typeof(TileBehaviourAttribute), false))
                {
                    s_tileBehaviours.Add(behaviour.Name, type);
                }
            }
        }

        public static TileBehaviour CreateFromName(string name, Tile tile, KeyValuePairs kvp)
        {
            if (s_tileBehaviours.ContainsKey(name))
            {
                try
                {
                    var type = s_tileBehaviours[name];
                    var constructor = type.GetConstructor(new Type[] { typeof(Tile), typeof(KeyValuePairs) });
                    return (TileBehaviour)constructor.Invoke(new object[] { tile, kvp });
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }
            else
            {
                App.Log("Error: Unrecognised tile behaviour " + name);
                return new TileBehaviour(tile, kvp);
            }
        }

        public readonly Tile Tile;

        public TileBehaviour(Tile tile, KeyValuePairs kvp)
        {
            Tile = tile;
        }

        public virtual void OnInit(ILevel level, TileCoordinates coordinates)
        {
        }

        public virtual void OnShutdown(ILevel level, TileCoordinates coordinates)
        {
        }

        public virtual Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            return null;
        }

        public virtual void OnLevelStart(ILevel level, TileCoordinates coordinates)
        {
        }

        public virtual void OnSteppedOn(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
        }

        public virtual void OnSteppedOff(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
        }

        public virtual void OnNeighbourChanged(ILevel level, TileCoordinates coordinates)
        {
        }

        public virtual bool GetPowerOutput(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return false;
        }

        public virtual bool AcceptsPower(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return false;
        }

        public virtual bool CanPlaceOnSide(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            if (Tile.AllowPlacement || direction != Direction.Up)
            {
                if (Tile.IsSolidOnSide(level, coordinates, direction) &&
                    Tile.IsOpaqueOnSide(level, coordinates, direction))
                {
                    var neighbour = coordinates.Move(direction);
                    if (direction == Direction.Up && Tile.Height > 1)
                    {
                        neighbour = coordinates.Move(Direction.Up, Tile.Height);
                    }
                    if (level.Tiles[neighbour].IsReplaceable(level, neighbour) &&
                       !level.Tiles[neighbour].IsXRay(level, neighbour))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual Model GetModel(ILevel level, TileCoordinates coordinates)
        {
            if (level.InEditor && Tile.EditorModel != null)
            {
                return Tile.EditorModel;
            }
            else
            {
                if (((coordinates.X + coordinates.Z) & 0x1) == 1)
                {
                    return Tile.AltModel;
                }
                else
                {
                    return Tile.Model;
                }
            }
        }

        public virtual FlatDirection GetModelDirection(ILevel level, TileCoordinates coordinates)
        {
            return Tile.GetDirection(level, coordinates);
        }

        public virtual bool ShouldRenderModelGroup(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return true;
        }

        public virtual bool ShouldRenderGroupShadows(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return true;
        }

        public virtual void OnRender(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
            var model = GetModel(level, coordinates);
            if (model != null)
            {
                var direction = GetModelDirection(level, coordinates);
                RenderModel(level, coordinates, output, textures, model, direction);
            }
        }

        public virtual void OnRenderLiquid(ILevel level, TileCoordinates coordinates, Geometry output)
        {
        }

        public virtual void OnRenderShadows(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            var model = GetModel(level, coordinates);
            if (model != null)
            {
                var direction = GetModelDirection(level, coordinates);
                RenderModelShadows(level, coordinates, output, model, direction);
            }
        }

        protected void RenderModel(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures, Model model, FlatDirection direction)
        {
            // Determine culling info
            var leftDir = direction.RotateLeft();
            var rightDir = direction.RotateRight();
            var frontDir = direction;
            var backDir = direction.Opposite();

            var cullLeft = IsFaceHidden(level, coordinates, leftDir.ToDirection());
            var cullRight = IsFaceHidden(level, coordinates, rightDir.ToDirection());
            var cullTop = IsFaceHidden(level, coordinates, Direction.Up);
            var cullBottom = IsFaceHidden(level, coordinates, Direction.Down);
            var cullFront = IsFaceHidden(level, coordinates, frontDir.ToDirection());
            var cullBack = IsFaceHidden(level, coordinates, backDir.ToDirection());

            RenderModel(level, coordinates, output, textures, model, direction, cullLeft, cullRight, cullTop, cullBottom, cullFront, cullBack);
        }

        protected void RenderModel(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures, Model model, FlatDirection direction, bool cullLeft, bool cullRight, bool cullTop, bool cullBottom, bool cullFront, bool cullBack)
        {
            // Build the transform
            var transform = Tile.BuildTransform(coordinates, direction);

            // Build the geometry
            for (int i = 0; i < model.GroupCount; ++i)
            {
                if (ShouldRenderModelGroup(level, coordinates, model.GetGroupName(i)))
                {
                    var material = model.GetGroupMaterial(i);
                    var diffuseTexture = material.DiffuseTexture;
                    var diffuseTextureArea = textures.GetTextureArea(diffuseTexture);
                    if (!diffuseTextureArea.HasValue)
                    {
                        App.Log("Error: Atlas {0} does not contain diffuse texture {1}", textures.Path, diffuseTexture);
                        diffuseTextureArea = textures.GetTextureArea("defaults/default.png");
                    }
                    var specularTexture = material.SpecularTexture;
                    var specularTextureArea = textures.GetTextureArea(specularTexture);
                    if (!specularTextureArea.HasValue)
                    {
                        App.Log("Error: Atlas {0} does not contain specular texture {1}", textures.Path, specularTexture);
                        specularTextureArea = textures.GetTextureArea("defaults/default.png");
                    }
                    var normalTexture = material.NormalTexture;
                    var normalTextureArea = textures.GetTextureArea(normalTexture);
                    if (!normalTextureArea.HasValue)
                    {
                        App.Log("Error: Atlas {0} does not contain normal texture {1}", textures.Path, normalTexture);
                        normalTextureArea = textures.GetTextureArea("defaults/default.png");
                    }
                    var emissiveTexture = material.EmissiveTexture;
                    var emissiveTextureArea = textures.GetTextureArea(emissiveTexture);
                    if (!emissiveTextureArea.HasValue)
                    {
                        App.Log("Error: Atlas {0} does not contain emissive texture {1}", textures.Path, emissiveTexture);
                        emissiveTextureArea = textures.GetTextureArea("defaults/default.png");
                    }
                    output.AddGeometry(
                        model.GetGroupGeometry(i),
                        ref transform,
                        diffuseTextureArea.Value,
                        specularTextureArea.Value,
                        normalTextureArea.Value,
                        emissiveTextureArea.Value,
                        cullLeft, cullRight,
                        cullTop, cullBottom,
                        cullFront, cullBack
                    );
                }
            }
        }

        protected void RenderModelShadows(ILevel level, TileCoordinates coordinates, Geometry output, Model model, FlatDirection direction)
        {
            // Determine culling info
            var leftDir = direction.RotateLeft();
            var rightDir = direction.RotateRight();
            var frontDir = direction;
            var backDir = direction.Opposite();

            var cullLeft = IsFaceHidden(level, coordinates, leftDir.ToDirection());
            var cullRight = IsFaceHidden(level, coordinates, rightDir.ToDirection());
            var cullTop = IsFaceHidden(level, coordinates, Direction.Up);
            var cullBottom = IsFaceHidden(level, coordinates, Direction.Down);
            var cullFront = IsFaceHidden(level, coordinates, frontDir.ToDirection());
            var cullBack = IsFaceHidden(level, coordinates, backDir.ToDirection());

            RenderModelShadows(level, coordinates, output, model, direction, cullLeft, cullRight, cullTop, cullBottom, cullFront, cullBack);
        }

        protected void RenderModelShadows(ILevel level, TileCoordinates coordinates, Geometry output, Model model, FlatDirection direction, bool cullLeft, bool cullRight, bool cullTop, bool cullBottom, bool cullFront, bool cullBack)
        {
            // Build the transform
            var transform = Tile.BuildTransform(coordinates, direction);

            // Build the geometry
            for (int i = 0; i < model.GroupCount; ++i)
            {
                if (ShouldRenderGroupShadows(level, coordinates, model.GetGroupName(i)))
                {
                    output.AddShadowGeometry(
                        model.GetGroupGeometry(i),
                        ref transform,
                        cullLeft, cullRight,
                        cullTop, cullBottom,
                        cullFront, cullBack
                    );
                }
            }
        }

        protected bool IsFaceHidden(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            var tiles = level.Tiles;
            if (direction.IsFlat())
            {
                var adjoiningPos = coordinates.Move(direction);
                var adjoiningSide = direction.Opposite();
                for (int i = 0; i < Tile.Height; ++i)
                {
                    var tile = tiles[adjoiningPos];
                    if (tile.IsHidden(level, adjoiningPos) || !tiles[adjoiningPos].IsOpaqueOnSide(level, adjoiningPos, adjoiningSide))
                    {
                        return false;
                    }
                    adjoiningPos = adjoiningPos.Above();
                }
                return true;
            }
            else if (direction == Direction.Up)
            {
                var adjoiningPos = coordinates.Move(Direction.Up, Tile.Height);
                var tile = tiles[adjoiningPos];
                return !tile.IsHidden(level, adjoiningPos) && tile.IsOpaqueOnSide(level, adjoiningPos, Direction.Down);
            }
            else //if(direction == Direction.Down)
            {
                var adjoiningPos = coordinates.Below();
                var tile = tiles[adjoiningPos];
                return !tile.IsHidden(level, adjoiningPos) && tile.IsOpaqueOnSide(level, adjoiningPos, Direction.Up);
            }
        }

        public virtual bool? IsOpaqueOnSide(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return null;
        }
    }
}

