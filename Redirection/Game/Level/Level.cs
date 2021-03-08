using Dan200.Core.Assets;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Core.Utils;
using Dan200.Game.Game;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Game.Level
{
    public class Level : ILevel, IDisposable
    {
        private const int CURRENT_VERSION = 10;

        public static Level Load(LevelData data, LevelOptions options)
        {
            // Create the tile lookup
            var lookup = new TileLookup();
            for (int i = 0; i < data.TileLookup.Length; ++i)
            {
                string path = data.TileLookup[i];
                lookup.AddTile(Tile.Get(path), i);
            }

            // Create the level
            int width = data.Width;
            int height = data.Height;
            int depth = data.Depth;
            int xOrigin = data.XOrigin;
            int yOrigin = data.YOrigin;
            int zOrigin = data.ZOrigin;
            var level = new Level(xOrigin, yOrigin, zOrigin, xOrigin + width, yOrigin + height, zOrigin + depth);

            // Load the information
            level.Info.Path = data.Path;
            level.Info.ID = data.ID;
            level.Info.Title = data.Title;
            level.Info.MusicPath = data.Music;
            level.Info.SkyPath = data.Sky;
            level.Info.ScriptPath = data.Script;
            level.Info.ItemPath = data.Item;
            level.Info.IntroPath = data.Intro;
            level.Info.OutroPath = data.Outro;
            level.Info.TotalPlacements = data.ItemCount;
            level.Info.PlacementsLeft = data.ItemCount;
            level.Info.EverCompleted = data.EverCompleted;
            level.Info.CameraPitch = data.CameraPitch;
            level.Info.CameraYaw = data.CameraYaw;
            level.Info.CameraDistance = data.CameraDistance;
            level.Info.InEditor = options.InEditor;
            level.Info.InMenu = options.InMenu;

            level.Info.RandomSeed = data.RandomSeed;
            level.Random = new Random();

            // Populate the level
            var ids = data.TileIDs;
            var tiles = level.Tiles;
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int z = 0; z < depth; ++z)
                    {
                        var id = data.TileIDs[x, y, z];
                        var direction = data.TileDirections[x, y, z];
                        var tile = lookup.GetTileFromID(id);
                        if (tile != Dan200.Game.Level.Tiles.Air && tile != Dan200.Game.Level.Tiles.Extension)
                        {
                            var tileCoords = new TileCoordinates(x + tiles.MinX, y + tiles.MinY, z + tiles.MinZ);
                            if (tiles.GetTile(tileCoords) != Dan200.Game.Level.Tiles.Extension)
                            {
                                tiles.SetTile(tileCoords, tile, direction, false);
                            }
                        }
                    }
                }
            }

            return level;
        }

        private class EntityDistanceComparer : IComparer<Entity>
        {
            public Vector3 CameraPos;

            public EntityDistanceComparer()
            {
                CameraPos = Vector3.Zero;
            }

            public int Compare(Entity x, Entity y)
            {
                float xDistanceSq = x.Dead ? 0.0f : (x.Position - CameraPos).LengthSquared;
                float yDistanceSq = y.Dead ? 0.0f : (y.Position - CameraPos).LengthSquared;
                return yDistanceSq.CompareTo(xDistanceSq);
            }
        }

        private TileMap m_tileMap;
        private SkyInstance m_sky;
        private ParticleManager m_particles;

        private Matrix4 m_transform;
        private List<Entity> m_entities;
        private IEntityCollection m_entityCollection;

        private EntityDistanceComparer m_depthComparer;
        private List<Entity> m_depthSortedEntities;

        private LevelInfo m_info;

        private AmbientLight m_ambientLight;
        private DirectionalLight m_skyLight;
        private DirectionalLight m_skyLight2;
        private List<PointLight> m_pointLights;
        private ILightCollection m_lightCollection;

        private TimeMachine m_timeMachine;
        private TelepadDirectory m_telepadDirectory;
        private HintDirectory m_hintDirectory;

        private FlatEffectInstance m_flatOpaqueEffect;
        private FlatCutoutEffectInstance m_flatCutoutEffect;

        private LitEffectInstance m_litOpaqueEffect;
        private LitEffectInstance m_litCutoutEffect;
        private LitEffectInstance m_litTranslucentEffect;
        private ShadowEffectInstance m_shadowEffect;

        private GameAudio m_audio;

        public Matrix4 Transform
        {
            get
            {
                return m_transform;
            }
            set
            {
                m_transform = value;
            }
        }

        public bool Visible;

        public LevelInfo Info
        {
            get
            {
                return m_info;
            }
        }

        public bool InEditor
        {
            get
            {
                return m_info.InEditor;
            }
        }

        public bool InMenu
        {
            get
            {
                return m_info.InMenu;
            }
        }

        public int RandomSeed
        {
            get
            {
                return m_info.RandomSeed;
            }
        }

        ITileMap ILevel.Tiles
        {
            get
            {
                return m_tileMap;
            }
        }

        public TileMap Tiles
        {
            get
            {
                return m_tileMap;
            }
        }

        public TimeMachine TimeMachine
        {
            get
            {
                return m_timeMachine;
            }
        }

        public TelepadDirectory Telepads
        {
            get
            {
                return m_telepadDirectory;
            }
        }

        public HintDirectory Hints
        {
            get
            {
                return m_hintDirectory;
            }
        }

        public SkyInstance Sky
        {
            get
            {
                return m_sky;
            }
            set
            {
                m_sky = value;
            }
        }

        private class EntityCollection : IEntityCollection
        {
            private Level m_owner;

            public int Count
            {
                get
                {
                    int count = m_owner.m_entities.Count;
                    int livingCount = 0;
                    for (int i = 0; i < count; ++i)
                    {
                        var entity = m_owner.m_entities[i];
                        if (!entity.Dead)
                        {
                            ++livingCount;
                        }
                    }
                    return livingCount;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public EntityCollection(Level owner)
            {
                m_owner = owner;
            }

            public void Add(Entity entity)
            {
                if (!m_owner.m_entities.Contains(entity))
                {
                    m_owner.m_entities.Add(entity);
                    if (entity.NeedsRenderPass(RenderPass.Translucent))
                    {
                        m_owner.m_depthSortedEntities.Add(entity);
                    }
                    entity.Init(m_owner);
                }
            }

            public bool Remove(Entity entity)
            {
                if (m_owner.m_entities.Contains(entity) && !entity.Dead)
                {
                    entity.Shutdown();
                    return true;
                }
                return false;
            }

            public bool Contains(Entity entity)
            {
                return m_owner.m_entities.Contains(entity) && !entity.Dead;
            }

            public void Clear()
            {
                for (int i = 0; i < m_owner.m_entities.Count; ++i)
                {
                    var e = m_owner.m_entities[i];
                    e.Shutdown();
                }
                m_owner.m_entities.Clear();
            }

            public void CopyTo(Entity[] entities, int index)
            {
                int i = 0;
                foreach (Entity e in this)
                {
                    entities[index + i] = e;
                    ++i;
                }
            }

            public IEnumerator<Entity> GetEnumerator()
            {
                int count = m_owner.m_entities.Count;
                for (int i = 0; i < count; ++i)
                {
                    var entity = m_owner.m_entities[i];
                    if (!entity.Dead)
                    {
                        yield return entity;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return (System.Collections.IEnumerator)GetEnumerator();
            }
        }

        public IEntityCollection Entities
        {
            get
            {
                return m_entityCollection;
            }
        }

        public class LightCollection : ILightCollection
        {
            private Level m_owner;

            public AmbientLight AmbientLight
            {
                get
                {
                    return m_owner.m_ambientLight;
                }
            }

            public DirectionalLight SkyLight
            {
                get
                {
                    return m_owner.m_skyLight;
                }
            }

            public DirectionalLight SkyLight2
            {
                get
                {
                    return m_owner.m_skyLight2;
                }
            }

            public ICollection<PointLight> PointLights
            {
                get
                {
                    return m_owner.m_pointLights;
                }
            }

            public LightCollection(Level owner)
            {
                m_owner = owner;
            }
        }

        public ILightCollection Lights
        {
            get
            {
                return m_lightCollection;
            }
        }

        public GameAudio Audio
        {
            get
            {
                return m_audio;
            }
            set
            {
                m_audio = value;
            }
        }

        public Random Random
        {
            get;
            private set;
        }

        public ParticleManager Particles
        {
            get
            {
                return m_particles;
            }
        }

        public Level(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            m_transform = Matrix4.Identity;
            m_timeMachine = new TimeMachine();
            m_tileMap = new TileMap(this, minX, minY, minZ, maxX, maxY, maxZ);

            m_entities = new List<Entity>();
            m_entityCollection = new EntityCollection(this);
            m_info = new LevelInfo();

            m_depthComparer = new EntityDistanceComparer();
            m_depthSortedEntities = new List<Entity>();

            m_ambientLight = new AmbientLight(new Vector3(0.5f, 0.5f, 0.5f));
            m_ambientLight.Active = true;

            m_skyLight = new DirectionalLight(new Vector3(0.6f, -1.0f, -0.6f), new Vector3(0.5f, 0.5f, 0.5f));
            m_skyLight.Active = true;

            m_skyLight2 = new DirectionalLight(-Vector3.UnitY, Vector3.Zero);
            m_skyLight2.Active = false;

            m_pointLights = new List<PointLight>();
            m_lightCollection = new LightCollection(this);

            m_telepadDirectory = new TelepadDirectory();
            m_hintDirectory = new HintDirectory();

            Visible = true;
            Random = new Random();

            m_flatOpaqueEffect = new FlatEffectInstance();
            m_flatCutoutEffect = new FlatCutoutEffectInstance();
            m_litOpaqueEffect = new LitEffectInstance(RenderPass.Opaque);
            m_litCutoutEffect = new LitEffectInstance(RenderPass.Cutout);
            m_litTranslucentEffect = new LitEffectInstance(RenderPass.Translucent);
            m_shadowEffect = new ShadowEffectInstance();

            m_particles = new ParticleManager(this);
        }

        public void Dispose()
        {
            for (int i = 0; i < m_entities.Count; ++i)
            {
                var entity = m_entities[i];
                if (!entity.Dead)
                {
                    entity.Shutdown();
                }
            }
            m_tileMap.Dispose();
            m_particles.Dispose();
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new StreamWriter(path))
            {
                Save(stream);
            }
        }

        public void Save(TextWriter output)
        {
            // Compress
            m_tileMap.Compress();

            // Save out header
            var kvp = new KeyValuePairs();
            kvp.Comment = "Level data";
            kvp.Set("id", Info.ID);
            kvp.Set("title", Info.Title);
            kvp.Set("music", Info.MusicPath);
            kvp.Set("sky", Info.SkyPath);
            kvp.Set("script", Info.ScriptPath);
            kvp.Set("item", Info.ItemPath);
            kvp.Set("intro", Info.IntroPath);
            kvp.Set("outro", Info.OutroPath);
            kvp.Set("item_count", Info.TotalPlacements);
            kvp.Set("ever_completed", Info.EverCompleted);
            kvp.Set("camera.pitch", Info.CameraPitch);
            kvp.Set("camera.yaw", Info.CameraYaw);
            kvp.Set("camera.distance", Info.CameraDistance);
            kvp.Set("random.seed", Info.RandomSeed);

            // Save out tiles
            kvp.Set("tiles.width", Tiles.Width);
            kvp.Set("tiles.height", Tiles.Height);
            kvp.Set("tiles.depth", Tiles.Depth);
            kvp.Set("tiles.x_origin", Tiles.MinX);
            kvp.Set("tiles.y_origin", Tiles.MinY);
            kvp.Set("tiles.z_origin", Tiles.MinZ);

            var lookup = new TileLookup();
            int lastID = -1;
            var tiles = new StringBuilder();
            var robotCount = 0;
            for (int x = Tiles.MinX; x < Tiles.MaxX; ++x)
            {
                for (int y = Tiles.MinY; y < Tiles.MaxY; ++y)
                {
                    for (int z = Tiles.MinZ; z < Tiles.MaxZ; ++z)
                    {
                        var coords = new TileCoordinates(x, y, z);
                        var tile = Tiles.GetTile(coords);
                        var direction = tile.GetDirection(this, coords);
                        if (tile == Dan200.Game.Level.Tiles.Extension)
                        {
                            tile = Dan200.Game.Level.Tiles.Air;
                            direction = FlatDirection.North;
                        }
                        int id = lookup.GetIDForTile(tile);
                        if (id < 0)
                        {
                            id = lookup.AddTile(tile);
                            lastID = id;
                        }
                        tiles.Append(Base64.ToString(id, 2));
                        tiles.Append(Base64.ToString((int)direction, 1));

                        var behaviour = tile.GetBehaviour(this, coords);
                        if (behaviour is SpawnTileBehaviour && ((SpawnTileBehaviour)behaviour).Required)
                        {
                            robotCount++;
                        }
                    }
                }
            }
            kvp.Set("tiles.data", tiles.ToString());
            kvp.Set("robot_count", robotCount);

            // Save out lookup
            var lookupString = new StringBuilder();
            for (int i = 0; i <= lastID; ++i)
            {
                var tile = lookup.GetTileFromID(i);
                lookupString.Append(tile.Path);
                if (i < lastID)
                {
                    lookupString.Append(',');
                }
            }
            kvp.Set("tiles.lookup", lookupString.ToString());

            // Save out file
            kvp.Save(output);
        }

        public void Update(float dt)
        {
            // Update time
            m_timeMachine.Update(dt);

            int steps = 0;
            while (steps == 0 || m_timeMachine.Step())
            {
                // Update tiles and entities
                m_info.Update(m_timeMachine.Time);
                m_tileMap.Update();
                m_particles.Update();
                for (int i = 0; i < m_entities.Count; ++i)
                {
                    var e = m_entities[i];
                    if (!e.Dead)
                    {
                        e.Update();
                    }
                    else
                    {
                        m_entities.RemoveAt(i);
                        m_depthSortedEntities.Remove(e);
                        --i;
                    }
                }
                ++steps;
            }
        }

        public bool RaycastTiles(Ray ray, out TileCoordinates o_hitCoords, out Direction o_hitSide, out float o_hitDistance, bool solidOpaqueSidesOnly = false)
        {
            return Tiles.Raycast(ray.ToLocal(Transform), out o_hitCoords, out o_hitSide, out o_hitDistance, solidOpaqueSidesOnly);
        }

        public bool RaycastEntities(Ray ray, out Entity o_entity, out Direction o_hitSide, out float o_hitDistance)
        {
            Ray localRay = ray.ToLocal(Transform);
            Entity bestResult = null;
            var bestSide = default(Direction);
            var bestDistance = default(float);
            for (int i = 0; i < m_entities.Count; ++i)
            {
                var entity = m_entities[i];
                float distance;
                Direction side;
                if (!entity.Dead && entity.Raycast(localRay, out side, out distance))
                {
                    if (bestResult == null || distance < bestDistance)
                    {
                        bestResult = entity;
                        bestSide = side;
                        bestDistance = distance;
                    }
                }
            }
            if (bestResult != null)
            {
                o_entity = bestResult;
                o_hitSide = bestSide;
                o_hitDistance = bestDistance;
                return true;
            }
            o_entity = default(Entity);
            o_hitSide = default(Direction);
            o_hitDistance = default(float);
            return false;
        }

        public void DepthSortEntities(Vector3 cameraPosition)
        {
            // Sort the entities
            m_depthComparer.CameraPos = cameraPosition;
            m_depthSortedEntities.Sort(m_depthComparer);
        }

        public void Draw(ModelEffectInstance effect, RenderPass pass)
        {
            // Draw sky
            if (m_sky != null)
            {
                m_sky.DrawForeground(effect, pass);
            }

            // Draw tilemap
            m_tileMap.RebuildIfNeeded();
            m_tileMap.Draw(effect, pass);

            // Draw entities
            if (pass == RenderPass.Translucent)
            {
                for (int i = 0; i < m_depthSortedEntities.Count; ++i)
                {
                    var e = m_depthSortedEntities[i];
                    if (!e.Dead)
                    {
                        e.Draw(effect, pass);
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_entities.Count; ++i)
                {
                    var e = m_entities[i];
                    if (!e.Dead && e.NeedsRenderPass(pass))
                    {
                        e.Draw(effect, pass);
                    }
                }
            }
        }

        public void DrawShadows(ShadowEffectInstance effect)
        {
            if (m_sky != null)
            {
                m_sky.DrawForegroundShadows(effect);
            }
            m_tileMap.RebuildIfNeeded();
            m_tileMap.DrawShadows(effect);
            for (int i = 0; i < m_entities.Count; ++i)
            {
                var e = m_entities[i];
                if (!e.Dead && e.NeedsRenderPass(RenderPass.Opaque))
                {
                    e.DrawShadows(effect);
                }
            }
        }

        public void Draw(Camera camera, bool drawShadows = false)
        {
            if (Visible)
            {
                // Sort entities
                var cameraTransInv = camera.Transform;
                MathUtils.FastInvert(ref cameraTransInv);

                var cameraPosition = Vector3.TransformPosition(Vector3.Zero, cameraTransInv);
                DepthSortEntities(cameraPosition);

                // Setup effects:
                // Opaque
                m_litOpaqueEffect.WorldMatrix = Transform;
                m_litOpaqueEffect.ModelMatrix = Matrix4.Identity;
                m_litOpaqueEffect.ViewMatrix = camera.Transform;
                m_litOpaqueEffect.ProjectionMatrix = camera.CreateProjectionMatrix();
                m_litOpaqueEffect.AmbientLight = m_ambientLight;

                // Cutout
                m_litCutoutEffect.WorldMatrix = m_litOpaqueEffect.WorldMatrix;
                m_litCutoutEffect.ModelMatrix = m_litOpaqueEffect.ModelMatrix;
                m_litCutoutEffect.ViewMatrix = m_litOpaqueEffect.ViewMatrix;
                m_litCutoutEffect.ProjectionMatrix = m_litOpaqueEffect.ProjectionMatrix;
                m_litCutoutEffect.AmbientLight = m_ambientLight;

                // Translucent
                m_litTranslucentEffect.WorldMatrix = m_litOpaqueEffect.WorldMatrix;
                m_litTranslucentEffect.ModelMatrix = m_litOpaqueEffect.ModelMatrix;
                m_litTranslucentEffect.ViewMatrix = m_litOpaqueEffect.ViewMatrix;
                m_litTranslucentEffect.ProjectionMatrix = m_litOpaqueEffect.ProjectionMatrix;
                m_litTranslucentEffect.AmbientLight = m_ambientLight;

                if (drawShadows)
                {
                    // Flat Opaque
                    m_flatOpaqueEffect.WorldMatrix = m_litOpaqueEffect.WorldMatrix;
                    m_flatOpaqueEffect.ModelMatrix = m_litOpaqueEffect.ModelMatrix;
                    m_flatOpaqueEffect.ViewMatrix = m_litOpaqueEffect.ViewMatrix;
                    m_flatOpaqueEffect.ProjectionMatrix = m_litOpaqueEffect.ProjectionMatrix;

                    // Flat Cutout
                    m_flatCutoutEffect.WorldMatrix = m_litOpaqueEffect.WorldMatrix;
                    m_flatCutoutEffect.ModelMatrix = m_litOpaqueEffect.ModelMatrix;
                    m_flatCutoutEffect.ViewMatrix = m_litOpaqueEffect.ViewMatrix;
                    m_flatCutoutEffect.ProjectionMatrix = m_litOpaqueEffect.ProjectionMatrix;

                    // Shadow
                    m_shadowEffect.WorldMatrix = m_litOpaqueEffect.WorldMatrix;
                    m_shadowEffect.ModelMatrix = m_litOpaqueEffect.ModelMatrix;
                    m_shadowEffect.ViewMatrix = m_litOpaqueEffect.ViewMatrix;
                    m_shadowEffect.ProjectionMatrix = m_litOpaqueEffect.ProjectionMatrix;
                    m_shadowEffect.Light = m_skyLight;
                }

                if (drawShadows)
                {
                    // Fill the depth buffer with opaque geometry
                    GL.ColorMask(false, false, false, false);
                    GL.DepthMask(true);
                    GL.StencilMask(0x00);
                    Draw(m_flatOpaqueEffect, RenderPass.Opaque);
                    Draw(m_flatCutoutEffect, RenderPass.Cutout);

                    // Enable stencil test
                    GL.Enable(EnableCap.StencilTest);

                    // Enable stencil writes
                    GL.ColorMask(false, false, false, false);
                    GL.DepthMask(false);
                    GL.StencilMask(0xff);

                    // Clear the stencil buffer
                    GL.StencilFunc(StencilFunction.Always, 0, 0xff);
                    GL.Clear(ClearBufferMask.StencilBufferBit);

                    // Draw the shadow volumes
#if false
                    // Z-PASS (doesn't work inside shadows)
                    // Increment front faces
                    GL.CullFace( CullFaceMode.Back );
                    GL.StencilOp( StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap );
                    DrawShadows( m_shadowEffect );

                    // Decrement back faces
                    GL.CullFace( CullFaceMode.Front );
                    GL.StencilOp( StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap );
                    DrawShadows( m_shadowEffect );
#else
                    // Z-FAIL (works inside shadows)
                    // Increment back face fails
                    GL.CullFace(CullFaceMode.Front);
                    GL.StencilOp(StencilOp.Keep, StencilOp.IncrWrap, StencilOp.Keep);
                    DrawShadows(m_shadowEffect);

                    // Decrement front face fails
                    GL.CullFace(CullFaceMode.Back);
                    GL.StencilOp(StencilOp.Keep, StencilOp.DecrWrap, StencilOp.Keep);
                    DrawShadows(m_shadowEffect);
#endif

                    // Enable colour writes
                    GL.CullFace(CullFaceMode.Back);
                    GL.DepthMask(true);
                    GL.ColorMask(true, true, true, true);
                    GL.StencilMask(0x00);

                    // Draw the unlit opaque geometry
                    GL.StencilFunc(StencilFunction.Notequal, 0, 0xff);
                    m_litOpaqueEffect.Light = null;
                    m_litOpaqueEffect.Light2 = m_skyLight2;
                    Draw(m_litOpaqueEffect, RenderPass.Opaque);

                    m_litCutoutEffect.Light = null;
                    m_litCutoutEffect.Light2 = m_skyLight2;
                    Draw(m_litCutoutEffect, RenderPass.Cutout);

                    // Draw the lit opaque geometry
                    GL.StencilFunc(StencilFunction.Equal, 0, 0xff);
                    m_litOpaqueEffect.Light = m_skyLight;
                    m_litOpaqueEffect.Light2 = m_skyLight2;
                    Draw(m_litOpaqueEffect, RenderPass.Opaque);

                    m_litCutoutEffect.Light = m_skyLight;
                    m_litCutoutEffect.Light2 = m_skyLight2;
                    Draw(m_litCutoutEffect, RenderPass.Cutout);

                    // Disable stencil test
                    GL.Disable(EnableCap.StencilTest);

                    // Draw the transparent geometry (always lit)
                    m_litTranslucentEffect.Light = m_skyLight;
                    m_litTranslucentEffect.Light2 = m_skyLight2;
                    Draw(m_litTranslucentEffect, RenderPass.Translucent);
                }
                else
                {
                    // Draw the whole level lit
                    m_litOpaqueEffect.Light = m_skyLight;
                    m_litOpaqueEffect.Light2 = m_skyLight2;
                    Draw(m_litOpaqueEffect, RenderPass.Opaque);

                    m_litCutoutEffect.Light = m_skyLight;
                    m_litCutoutEffect.Light2 = m_skyLight2;
                    Draw(m_litCutoutEffect, RenderPass.Cutout);

                    m_litTranslucentEffect.Light = m_skyLight;
                    m_litTranslucentEffect.Light2 = m_skyLight2;
                    Draw(m_litTranslucentEffect, RenderPass.Translucent);
                }

                // Draw the particles
                m_particles.Draw(camera);
            }
        }
    }
}
