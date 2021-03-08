using Dan200.Core.Animation;
using Dan200.Core.Render;
using Dan200.Core.Utils;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class TileMap : ITileMap, IDisposable
    {
        private Level m_level;
        private int m_xOrigin;
        private int m_yOrigin;
        private int m_zOrigin;
        private Tile[,,] m_tiles;
        private TileState[,,] m_state;

        private TextureAtlas m_atlas;
        private Geometry[] m_geometry;
        private Dictionary<LiquidTileBehaviour, Geometry> m_liquidGeometry;
        private Geometry m_shadowGeometry;
        private bool m_geometryNeedsRebuild;

        private enum TileChangeType
        {
            Tile,
            SubState
        }

        private class TileChange
        {
            public TileChangeType Type;
            public float TimeStamp;
            public TileCoordinates Coordinates;
            public Tile OldTile;
            public FlatDirection OldDirection;
            public int OldSubState;
        }
        private Stack<TileChange> m_history;
        private float m_latestTimeStamp;

        public int MinX
        {
            get
            {
                return m_xOrigin;
            }
        }

        public int MaxX
        {
            get
            {
                return MinX + Width;
            }
        }

        public int MinY
        {
            get
            {
                return m_yOrigin;
            }
        }

        public int MaxY
        {
            get
            {
                return MinY + Height;
            }
        }

        public int MinZ
        {
            get
            {
                return m_zOrigin;
            }
        }

        public int MaxZ
        {
            get
            {
                return MinZ + Depth;
            }
        }

        public int Width
        {
            get
            {
                return m_tiles.GetLength(0);
            }
        }

        public int Height
        {
            get
            {
                return m_tiles.GetLength(1);
            }
        }

        public int Depth
        {
            get
            {
                return m_tiles.GetLength(2);
            }
        }

        public TileCoordinates Origin
        {
            get
            {
                return new TileCoordinates(m_xOrigin, m_yOrigin, m_zOrigin);
            }
            set
            {
                var xDiff = value.X - m_xOrigin;
                var yDiff = value.Y - m_yOrigin;
                var zDiff = value.Z - m_zOrigin;
                if (xDiff != 0 || yDiff != 0 || zDiff != 0)
                {
                    m_xOrigin += xDiff;
                    m_yOrigin += yDiff;
                    m_zOrigin += zDiff;
                    foreach (Entity entity in m_level.Entities)
                    {
                        if (entity is TileEntity)
                        {
                            var tileEntity = (TileEntity)entity;
                            tileEntity.Location = new TileCoordinates(
                                tileEntity.Location.X + xDiff,
                                tileEntity.Location.Y + yDiff,
                                tileEntity.Location.Z + zDiff
                            );
                        }
                    }
                    RequestRebuild();
                }
            }
        }

        public Tile this[TileCoordinates coordinates]
        {
            get
            {
                return GetTile(coordinates);
            }
        }

        public TileMap(Level level, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            m_level = level;
            m_xOrigin = minX;
            m_yOrigin = minY;
            m_zOrigin = minZ;
            m_tiles = new Tile[maxX - minX, maxY - minY, maxZ - minZ];
            m_state = new TileState[maxX - minX, maxY - minY, maxZ - minZ];
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    for (int z = 0; z < Depth; ++z)
                    {
                        m_tiles[x, y, z] = Tiles.Air;
                        m_state[x, y, z] = new TileState();
                    }
                }
            }

            m_history = new Stack<TileChange>();
            m_latestTimeStamp = 0.0f;

            m_atlas = TextureAtlas.Get("models/tiles");
            m_geometry = new Geometry[Enum.GetValues(typeof(RenderPass)).Length];
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                m_geometry[i] = new Geometry(Primitive.Triangles);
            }
            m_shadowGeometry = new Geometry(Primitive.Triangles);
            m_liquidGeometry = new Dictionary<LiquidTileBehaviour, Geometry>();
            m_geometryNeedsRebuild = true;
        }

        public void Dispose()
        {
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                var geometry = m_geometry[i];
                geometry.Dispose();
            }
            m_shadowGeometry.Dispose();
            foreach (var geometry in m_liquidGeometry.Values)
            {
                geometry.Dispose();
            }
        }

        public void RequestRebuild()
        {
            m_geometryNeedsRebuild = true;
        }

        public void RequestRebuild(TileCoordinates coordinates)
        {
            m_geometryNeedsRebuild = true;
        }

        public Tile GetTile(TileCoordinates coordinates)
        {
            if (coordinates.X >= MinX && coordinates.X < MaxX &&
                coordinates.Y >= MinY && coordinates.Y < MaxY &&
                coordinates.Z >= MinZ && coordinates.Z < MaxZ)
            {
                return m_tiles[coordinates.X - m_xOrigin, coordinates.Y - m_yOrigin, coordinates.Z - m_zOrigin];
            }
            return Tiles.Air;
        }

        public TileState GetState(TileCoordinates coordinates)
        {
            if (coordinates.X >= MinX && coordinates.X < MaxX &&
                coordinates.Y >= MinY && coordinates.Y < MaxY &&
                coordinates.Z >= MinZ && coordinates.Z < MaxZ)
            {
                return m_state[coordinates.X - m_xOrigin, coordinates.Y - m_yOrigin, coordinates.Z - m_zOrigin];
            }
            return new TileState();
        }

        private void SetSubStateInternal(TileCoordinates coordinates, int subState)
        {
            GetState(coordinates).SubState = subState;
            RequestRebuild(coordinates);
        }

        public bool SetSubState(TileCoordinates coordinates, int subState, bool notifyNeighbours)
        {
            var tile = GetTile(coordinates);
            int oldSubState = tile.GetSubState(m_level, coordinates);
            if (subState != oldSubState)
            {
                ExpandToFit(coordinates);
                SetSubStateInternal(coordinates, subState);
                if (m_latestTimeStamp > 0.0f)
                {
                    var change = new TileChange();
                    change.Type = TileChangeType.SubState;
                    change.TimeStamp = m_latestTimeStamp;
                    change.Coordinates = coordinates;
                    change.OldSubState = oldSubState;
                    m_history.Push(change);
                }
                if (notifyNeighbours)
                {
                    tile.NotifyNeighboursChanged(m_level, coordinates);
                }
                return true;
            }
            return false;
        }

        public bool SetTile(TileCoordinates coordinates, Tile tile, FlatDirection direction = FlatDirection.North, bool notifyNeighbours = true)
        {
            var oldTile = GetTile(coordinates);
            var oldDirection = oldTile.GetDirection(m_level, coordinates);
            if (tile != oldTile || direction != oldDirection)
            {
                int oldSubState = oldTile.GetSubState(m_level, coordinates);
                ExpandToFit(coordinates);
                SetTileInternal(coordinates, tile, direction, 0);
                if (m_latestTimeStamp > 0.0f)
                {
                    var change = new TileChange();
                    change.Type = TileChangeType.Tile;
                    change.TimeStamp = m_latestTimeStamp;
                    change.Coordinates = coordinates;
                    change.OldTile = oldTile;
                    change.OldDirection = oldDirection;
                    change.OldSubState = oldSubState;
                    m_history.Push(change);
                }
                if (notifyNeighbours)
                {
                    tile.NotifyNeighboursChanged(m_level, coordinates);
                }
                return true;
            }
            return false;
        }

        private void SetTileInternal(TileCoordinates coordinates, Tile tile, FlatDirection direction, int subState)
        {
            Tile oldTile = m_tiles[coordinates.X - m_xOrigin, coordinates.Y - m_yOrigin, coordinates.Z - m_zOrigin];
            oldTile.Shutdown(m_level, coordinates);

            m_tiles[coordinates.X - m_xOrigin, coordinates.Y - m_yOrigin, coordinates.Z - m_zOrigin] = tile;
            SetSubStateInternal(coordinates, subState);
            tile.Init(m_level, coordinates, direction);

            m_geometryNeedsRebuild = true;
        }

        public bool Raycast(Ray ray, out TileCoordinates o_hitCoords, out Direction o_hitSide, out float o_hitDistance, bool solidOpaqueSidesOnly)
        {
            // Get started
            int tileX = (int)Math.Floor(ray.Origin.X);
            int tileY = (int)Math.Floor(ray.Origin.Y / 0.5f);
            int tileZ = (int)Math.Floor(ray.Origin.Z);
            float subTileX = ray.Origin.X - (float)tileX;
            float subTileY = ray.Origin.Y - (float)tileY * 0.5f;
            float subTileZ = ray.Origin.Z - (float)tileZ;
            int stepX = (ray.Direction.X != 0.0f) ? ((ray.Direction.X > 0.0f) ? 1 : -1) : 0;
            int stepY = (ray.Direction.Y != 0.0f) ? ((ray.Direction.Y > 0.0f) ? 1 : -1) : 0;
            int stepZ = (ray.Direction.Z != 0.0f) ? ((ray.Direction.Z > 0.0f) ? 1 : -1) : 0;

            float distanceLeft = ray.Length;
            while (distanceLeft > 0.0f)
            {
                // Check for out of bounds
                if ((stepX < 0 && tileX < MinX) ||
                    (stepX > 0 && tileX >= MaxX) ||
                    (stepY < 0 && tileY < MinY) ||
                    (stepY > 0 && tileY >= MaxY) ||
                    (stepZ < 0 && tileZ < MinZ) ||
                    (stepZ > 0 && tileZ >= MaxZ))
                {
                    break;
                }

                // Calculate where to move to
                float distToEdgeX = (stepX != 0) ? (((stepX > 0) ? 1.0f - subTileX : 0.0f - subTileX) / ray.Direction.X) : 9999.0f;
                float distToEdgeY = (stepY != 0) ? (((stepY > 0) ? 0.5f - subTileY : 0.0f - subTileY) / ray.Direction.Y) : 9999.0f;
                float distToEdgeZ = (stepZ != 0) ? (((stepZ > 0) ? 1.0f - subTileZ : 0.0f - subTileZ) / ray.Direction.Z) : 9999.0f;

                Direction hitSide;
                if (distToEdgeX < distToEdgeY && distToEdgeX < distToEdgeZ)
                {
                    tileX += stepX;
                    subTileX = (stepX > 0) ? 0.0f : 1.0f;
                    subTileY += distToEdgeX * ray.Direction.Y;
                    subTileZ += distToEdgeX * ray.Direction.Z;
                    distanceLeft -= distToEdgeX;
                    hitSide = (stepX > 0) ? Direction.East : Direction.West;
                }
                else if (distToEdgeY < distToEdgeX && distToEdgeY < distToEdgeZ)
                {
                    tileY += stepY;
                    subTileX += distToEdgeY * ray.Direction.X;
                    subTileY = (stepY > 0) ? 0.0f : 0.5f;
                    subTileZ += distToEdgeY * ray.Direction.Z;
                    distanceLeft -= distToEdgeY;
                    hitSide = (stepY > 0) ? Direction.Down : Direction.Up;
                }
                else
                {
                    tileZ += stepZ;
                    subTileX += distToEdgeZ * ray.Direction.X;
                    subTileY += distToEdgeZ * ray.Direction.Y;
                    subTileZ = (stepZ > 0) ? 0.0f : 1.0f;
                    distanceLeft -= distToEdgeZ;
                    hitSide = (stepZ > 0) ? Direction.South : Direction.North;
                }

                // See if side we crossed is solid
                if (distanceLeft >= 0.0f)
                {
                    var coords = new TileCoordinates(tileX, tileY, tileZ);
                    var tile = GetTile(coords);
                    if (tile != Tiles.Air)
                    {
                        bool acceptable = true;
                        if (solidOpaqueSidesOnly)
                        {
                            acceptable = tile.CanPlaceOnSide(m_level, coords, hitSide);
                        }
                        if (acceptable)
                        {
                            o_hitDistance = ray.Length - distanceLeft;
                            o_hitCoords = coords;
                            o_hitSide = hitSide;
                            return true;
                        }
                    }
                }
            }

            o_hitDistance = default(float);
            o_hitCoords = default(TileCoordinates);
            o_hitSide = default(Direction);
            return false;
        }

        private void UndoChange(TileChange change)
        {
            ExpandToFit(change.Coordinates);
            if (change.Type == TileChangeType.Tile)
            {
                SetTileInternal(change.Coordinates, change.OldTile, change.OldDirection, change.OldSubState);
            }
            else
            {
                SetSubStateInternal(change.Coordinates, change.OldSubState);
            }
        }

        public bool Undo()
        {
            if (m_history.Count > 0)
            {
                // Undo the change
                var change = m_history.Pop();
                UndoChange(change);

                // Undo other changes with the same timestamp
                while (m_history.Count > 0 && m_history.Peek().TimeStamp >= change.TimeStamp)
                {
                    var change2 = m_history.Pop();
                    UndoChange(change2);
                }
                return true;
            }
            return false;
        }

        public void Update()
        {
            float now = m_level.TimeMachine.Time;
            if (now < m_latestTimeStamp)
            {
                // Wind time backward
                m_latestTimeStamp = now;
                while (m_history.Count > 0 && m_latestTimeStamp < m_history.Peek().TimeStamp)
                {
                    var change = m_history.Pop();
                    UndoChange(change);
                }
            }
            else if (now >= m_latestTimeStamp)
            {
                // Advance time forward
                m_latestTimeStamp = now;
            }
        }

        public bool RebuildIfNeeded()
        {
            if (m_geometryNeedsRebuild)
            {
                RebuildGeometry();
                m_geometryNeedsRebuild = false;
                return true;
            }
            return false;
        }

        public void Draw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            // Draw the geometry for the current pass
            var geometry = m_geometry[(int)pass];
            if (geometry.IndexCount > 0)
            {
                modelEffect.ModelMatrix = Matrix4.Identity;
                modelEffect.UVOffset = Vector2.Zero;
                modelEffect.UVScale = Vector2.One;
                modelEffect.DiffuseColour = Vector4.One;
                modelEffect.DiffuseTexture = m_atlas;
                modelEffect.SpecularColour = Vector3.One;
                modelEffect.SpecularTexture = m_atlas;
                modelEffect.NormalTexture = m_atlas;
                modelEffect.EmissiveColour = Vector3.One;
                modelEffect.EmissiveTexture = m_atlas;
                modelEffect.Bind();
                geometry.Draw();

                /*
                // Wireframe test code
                modelEffect.ModelMatrix = Matrix4.Identity;
                modelEffect.DiffuseColour = Vector4.One;
                modelEffect.DiffuseTexture = Texture.White;
                modelEffect.SpecularColour = Vector3.One;
                modelEffect.SpecularTexture = Texture.White;
                modelEffect.NormalTexture = Texture.Flat;
                modelEffect.Bind();
                using( var wireframe = geometry.ToWireframe( lines:false, normals:true, tangents:true, binormals:true ) )
                {
                    wireframe.Draw();
                }
                */
            }

            foreach (var pair in m_liquidGeometry)
            {
                var behaviour = pair.Key;
                var liquidGeometry = pair.Value;
                if (liquidGeometry.IndexCount > 0 && behaviour.Tile.RenderPass == pass)
                {
                    bool visible;
                    Matrix4 modelMatrix;
                    Vector2 uvOffset, uvScale;
                    Vector4 colour;
                    if (behaviour.Animation != null)
                    {
                        float cameraFOV;
                        var anim = LuaAnimation.Get(behaviour.Animation);
                        anim.Animate("Liquid", m_level.TimeMachine.RealTime, out visible, out modelMatrix, out uvOffset, out uvScale, out colour, out cameraFOV);
                    }
                    else
                    {
                        visible = true;
                        modelMatrix = Matrix4.Identity;
                        uvOffset = Vector2.Zero;
                        uvScale = Vector2.One;
                        colour = Vector4.One;
                    }
                    if (visible)
                    {
                        var surface = Texture.Get(behaviour.Texture, false);
                        surface.Wrap = true;

                        modelEffect.ModelMatrix = modelMatrix;
                        modelEffect.UVOffset = uvOffset;
                        modelEffect.UVScale = uvScale;
                        modelEffect.DiffuseColour = colour;
                        modelEffect.DiffuseTexture = surface;
                        modelEffect.SpecularColour = Vector3.One;
                        modelEffect.SpecularTexture = Texture.Black;
                        modelEffect.NormalTexture = Texture.Flat;
                        modelEffect.EmissiveColour = Vector3.One;
                        modelEffect.EmissiveTexture = Texture.Black;
                        modelEffect.Bind();
                        liquidGeometry.Draw();
                    }
                }
            }
        }

        public void DrawShadows(ShadowEffectInstance shadowEffect)
        {
            shadowEffect.ModelMatrix = Matrix4.Identity;
            shadowEffect.Bind();
            m_shadowGeometry.Draw();
        }

        private void RebuildGeometry()
        {
            // Clear
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                var geometry = m_geometry[i];
                geometry.Clear();
            }
            foreach (var liquidGeometry in m_liquidGeometry.Values)
            {
                liquidGeometry.Clear();
            }
            m_shadowGeometry.Clear();

            // Populate
            for (int x = MinX; x < MaxX; ++x)
            {
                for (int y = MinY; y < MaxY; ++y)
                {
                    for (int z = MinZ; z < MaxZ; ++z)
                    {
                        var here = new TileCoordinates(x, y, z);
                        var tile = GetTile(here);
                        if (!tile.IsExtension())
                        {
                            tile.Render(m_level, here, m_geometry[(int)tile.RenderPass], m_atlas);
                            if (tile.IsLiquid(m_level, here))
                            {
                                Geometry liquidGeometry;
                                var behaviour = tile.GetBehaviour(m_level, here) as LiquidTileBehaviour;
                                if (!m_liquidGeometry.TryGetValue(behaviour, out liquidGeometry))
                                {
                                    liquidGeometry = new Geometry(Primitive.Triangles);
                                    m_liquidGeometry.Add(behaviour, liquidGeometry);
                                }
                                tile.RenderLiquid(m_level, here, liquidGeometry);
                            }
                            if (tile.CastShadows && tile.RenderPass == RenderPass.Opaque)
                            {
                                tile.RenderShadows(m_level, here, m_shadowGeometry);
                            }
                        }
                    }
                }
            }

            // Rebuild
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                var geometry = m_geometry[i];
                geometry.Rebuild();
            }
            foreach (var liquidGeometry in m_liquidGeometry.Values)
            {
                liquidGeometry.Rebuild();
            }
            m_shadowGeometry.Rebuild();
        }

        public void ExpandToFit(TileCoordinates coordinates)
        {
            if (coordinates.X < MinX || coordinates.X >= MaxX ||
                coordinates.Y < MinY || coordinates.Y >= MaxY ||
                coordinates.Z < MinZ || coordinates.Z >= MaxZ)
            {
                Reframe(
                    Math.Min(coordinates.X, MinX), Math.Min(coordinates.Y, MinY), Math.Min(coordinates.Z, MinZ),
                    Math.Max(coordinates.X + 1, MaxX), Math.Max(coordinates.Y + 1, MaxY), Math.Max(coordinates.Z + 1, MaxZ)
                );
            }
        }

        private void Reframe(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            if (minX == MinX && minY == MinY && minZ == MinZ &&
                maxX == MaxX && maxY == MaxY && maxZ == MaxZ)
            {
                return;
            }

            int newWidth = maxX - minX;
            int newHeight = maxY - minY;
            int newDepth = maxZ - minZ;
            var newTiles = new Tile[newWidth, newHeight, newDepth];
            var newState = new TileState[newWidth, newHeight, newDepth];
            for (int x = 0; x < newWidth; ++x)
            {
                for (int y = 0; y < newHeight; ++y)
                {
                    for (int z = 0; z < newDepth; ++z)
                    {
                        int oldX = minX + x - m_xOrigin;
                        int oldY = minY + y - m_yOrigin;
                        int oldZ = minZ + z - m_zOrigin;
                        if (oldX >= 0 && oldX < Width && oldY >= 0 && oldY < Height && oldZ >= 0 && oldZ < Depth)
                        {
                            newTiles[x, y, z] = m_tiles[oldX, oldY, oldZ];
                            newState[x, y, z] = m_state[oldX, oldY, oldZ];
                        }
                        else
                        {
                            newTiles[x, y, z] = Tiles.Air;
                            newState[x, y, z] = new TileState();
                        }
                    }
                }
            }
            m_xOrigin = minX;
            m_yOrigin = minY;
            m_zOrigin = minZ;
            m_tiles = newTiles;
            m_state = newState;
        }

        public void Compress()
        {
            // Find the first and last non-empty tile on each axis
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int minZ = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            int maxZ = int.MinValue;
            for (int x = MinX; x < MaxX; ++x)
            {
                for (int y = MinY; y < MaxY; ++y)
                {
                    for (int z = MinZ; z < MaxZ; ++z)
                    {
                        var tile = this[new TileCoordinates(x, y, z)];
                        if (tile != Tiles.Air)
                        {
                            minX = Math.Min(x, minX);
                            minY = Math.Min(y, minY);
                            minZ = Math.Min(z, minZ);
                            maxX = Math.Max(x, maxX);
                            maxY = Math.Max(y, maxY);
                            maxZ = Math.Max(z, maxZ);
                        }
                    }
                }
            }

            // Crop the level to these tiles
            if (maxX >= minX && maxY >= minY && maxZ >= minZ)
            {
                Reframe(minX, minY, minZ, maxX + 1, maxY + 1, maxZ + 1);
            }
            else
            {
                Reframe(0, 0, 0, 0, 0, 0);
            }
        }
    }
}

