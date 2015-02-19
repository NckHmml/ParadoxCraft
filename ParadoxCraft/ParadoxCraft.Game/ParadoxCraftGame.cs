using ParadoxCraft.Terrain;
using ParadoxCraft.Blocks;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Paradox.DataModel;
using ParadoxCraft.Blocks.Chunks;
using ParadoxCraft.Helpers;

namespace ParadoxCraft
{
    /// <summary>
    /// Main game instance
    /// </summary>
    public class ParadoxCraftGame : Game
    {
        #region Variables
        /// <summary>
        /// Bool to switch wireframe mode
        /// </summary>
        private bool isWireframe = false;

        /// <summary>
        /// Bool to switch to deferred mode
        /// </summary>
        private bool isDeferred = false;

        /// <summary>
        /// Player camera entity
        /// </summary>
        private Entity Camera { get; set; }

        /// <summary>
        /// Sun light and global light container
        /// </summary>
        private List<Entity> DirectionalLights { get; set; }

        /// <summary>
        /// Terrain entity
        /// </summary>
        private GraphicalTerrain Terrain { get; set; }

        /// <summary>
        /// Player movement helper
        /// </summary>
        private Movement PlayerMovement { get; set; }

        /// <summary>
        /// Chunk handling factory
        /// </summary>
        public ChunkFactory Factory { get; private set; }

        /// <summary>
        /// The player its cursor
        /// </summary>
        public PrimitiveRender Cursor { get; set; }
        #endregion

        #region Initialization
        /// <summary>
        /// Creates a new instance of <see cref="ParadoxCraftGame"/>
        /// </summary>
        public ParadoxCraftGame()
        {
            // Target 11.0 profile by default
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };

            DirectionalLights = new List<Entity>();
            Factory = new ChunkFactory();
        }

        /// <summary>
        /// Loads the game content
        /// </summary>
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            IsMouseVisible = false;

            // Initialize the cursor
            float res = (float)Window.ClientBounds.Width / Window.ClientBounds.Height;
            Cursor = new PrimitiveRender
            {
                DrawEffect = new SimpleEffect(GraphicsDevice) { Color = Color.White },
                Primitives = new[] {
                    GeometricPrimitive.Plane.New(GraphicsDevice, .06f / res, .01f),
                    GeometricPrimitive.Plane.New(GraphicsDevice, .01f / res, .06f)
                }
            };

            // Create pipeline
            if (isDeferred)
                RenderPipelineLightingFactory.CreateDefaultDeferred(this, "ParadoxCraftEffectMain", "ParadoxCraftPrepassEffect", Color.DarkBlue, false, false);
            else
                RenderPipelineLightingFactory.CreateDefaultForward(this, "ParadoxCraftEffectForward", Color.DarkBlue, false, false);
            RenderSystem.Pipeline.Renderers.Add(new DelegateRenderer(Services) { Render = RenderCursor });
           
            // Wireframe mode
            if (isWireframe)
                GraphicsDevice.Parameters.Set(Effect.RasterizerStateKey, RasterizerState.New(GraphicsDevice, new RasterizerStateDescription(CullMode.None) { FillMode = FillMode.Wireframe }));

            // Lights
            DirectionalLights.Add(CreateDirectLight(Vector3.Zero, new Color3(0), .5f));
            DirectionalLights.Add(CreateDirectLight(new Vector3(-1, -1, -1), new Color3(1, 1, 1), .2f));
            DirectionalLights.Add(CreateDirectLight(new Vector3(1, 1, 1), new Color3(1, 1, 1), .2f));
            Entities.Add(DirectionalLights.ToArray());

            // Entities
            LoadTerrain();
            LoadCamera();

            // Scripts
            Script.Add(MovementScript);
            Script.Add(RenderChunksScript);
            Script.Add(BuildTerrain);
            Script.Add(LightCycleScript);
        }

        /// <summary>
        /// Creates the entity for the player camera
        /// </summary>
        private void LoadCamera()
        {
            // Initialize player (camera)
            var campos = new Vector3(0, 0, 0);
            Camera = new Entity(campos)
            {
                new CameraComponent(null, .1f, 18 * 2 * 16) {
                    VerticalFieldOfView = 1.5f,
                    TargetUp = Vector3.UnitY
                },
            };
            PlayerMovement = new Movement(Camera);
            RenderSystem.Pipeline.SetCamera(Camera.Get<CameraComponent>());
            Entities.Add(Camera);
        }

        /// <summary>
        /// Creates the entity for the terrain
        /// </summary>
        private void LoadTerrain()
        {
            Terrain = new GraphicalTerrain(GraphicsDevice, Asset.Load<Material>("Materials/Terrain"));
            Factory.Terrain = Terrain;
            Entities.Add(Terrain);
        }

        private void RenderCursor(RenderContext renderContext)
        {
            Cursor.DrawEffect.Apply();
            foreach (var primitive in Cursor.Primitives)
                primitive.Draw();
        }
        #endregion

        #region Scripts
        /// <summary>
        /// Script for tracking key inputs to move the player
        /// </summary>
        private async Task MovementScript()
        {
            float dragX = 0f,
                  dragY = 0f;
            double time = UpdateTime.Total.TotalSeconds;
            Input.ResetMousePosition();
            while (IsRunning)
            {
                await Script.NextFrame();
                
                //Else the player slows down on a lower framerate
                var diff = UpdateTime.Total.TotalSeconds - time;
                time = UpdateTime.Total.TotalSeconds;

                if (Input.IsKeyDown(Keys.W))
                    PlayerMovement.Add(Direction.Foward);

                if (Input.IsKeyDown(Keys.S))
                    PlayerMovement.Add(Direction.Back);

                if (Input.IsKeyDown(Keys.A))
                    PlayerMovement.Add(Direction.Left);

                if (Input.IsKeyDown(Keys.D))
                    PlayerMovement.Add(Direction.Right);

                if (Input.IsKeyDown(Keys.Q))
                    PlayerMovement.Add(Direction.Up);

                if (Input.IsKeyDown(Keys.E))
                    PlayerMovement.Add(Direction.Down);

                PlayerMovement.Move(diff);

                //Move the 'camera'
                dragX *= .01f;
                dragY *= .01f;

                dragX += (Input.MousePosition.X - .5f) * .3f;
                dragY += (Input.MousePosition.Y - .5f) * .3f;

                if (Input.ResetMousePosition())
                    PlayerMovement.YawPitch(dragX, dragY);                
            }
        }

        /// <summary>
        /// Script for checking chunks that should be rendered
        /// </summary>
        private async Task RenderChunksScript()
        {
            while (IsRunning)
            {                
                double playerX = PlayerMovement.X;
                double playerZ = PlayerMovement.Z;
                //playerX += StartPosition.X;
                //playerZ += StartPosition.Z;
                playerX /= Constants.ChunkSize;
                playerZ /= Constants.ChunkSize;

                // TODO: handle y
                for (int i = 0; i < Constants.DrawRadius * Constants.DrawRadius; i++)
                {
                    int x = i % Constants.DrawRadius;
                    int z = (i - x) / Constants.DrawRadius;

                    if ((x * x) + (z * z) > (Constants.DrawRadius * Constants.DrawRadius)) continue;

                    Factory.CheckLoad(playerX + x, 0, playerZ + z);
                    Factory.CheckLoad(playerX - x, 0, playerZ + z);
                    Factory.CheckLoad(playerX + x, 0, playerZ - z);
                    Factory.CheckLoad(playerX - x, 0, playerZ - z);

                    Factory.CheckLoad(playerX + x, 1, playerZ + z);
                    Factory.CheckLoad(playerX - x, 1, playerZ + z);
                    Factory.CheckLoad(playerX + x, 1, playerZ - z);
                    Factory.CheckLoad(playerX - x, 1, playerZ - z);

                    Factory.CheckLoad(playerX + x, 2, playerZ + z);
                    Factory.CheckLoad(playerX - x, 2, playerZ + z);
                    Factory.CheckLoad(playerX + x, 2, playerZ - z);
                    Factory.CheckLoad(playerX - x, 2, playerZ - z);

                    Factory.CheckLoad(playerX + x, 3, playerZ + z);
                    Factory.CheckLoad(playerX - x, 3, playerZ + z);
                    Factory.CheckLoad(playerX + x, 3, playerZ - z);
                    Factory.CheckLoad(playerX - x, 3, playerZ - z);

                    if (i % Constants.DrawRadius == 0)
                        await Task.Delay(100);
                }
            }
        }

        /// <summary>
        /// Checks and re-builds the terrain
        /// </summary>
        private async Task BuildTerrain()
        {
            while (IsRunning)
            {
                double playerX = PlayerMovement.X;
                double playerZ = PlayerMovement.Z;
                //playerX += StartPosition.X;
                //playerZ += StartPosition.Z;
                playerX /= Constants.ChunkSize;
                playerZ /= Constants.ChunkSize;

                // TODO: handle y
                Factory.PurgeDistancedChunks(playerX, 0, playerZ, Constants.DrawRadius + 1);

                Terrain.Build();
                await Task.Delay(100);
            }
        }
        float realAngle;
        /// <summary>
        /// Script for the "Day/Night Cycle" 
        /// </summary>
        private async Task LightCycleScript()
        {
            LightComponent 
                light1 = DirectionalLights[1].Get<LightComponent>(),
                light2 = DirectionalLights[2].Get<LightComponent>(),
                sunlight = DirectionalLights[0].Get<LightComponent>();
            float
                sunlightIntensity = sunlight.Intensity,
                lightIntensity = light1.Intensity,
                intensityPercentage = 1f;
            double time;
            while (IsRunning)
            {
                await Task.Delay(100);
                time = UpdateTime.Total.TotalSeconds;
                float curAngle = (float)time / (Constants.DayNightCycle / 360f);
                float angle = curAngle / Constants.Degrees90 / 90;
                angle %= Constants.Degrees90 * 4;

                if (angle < 0 || angle > Constants.Degrees90 * 2)
                {
                    //Night
                    sunlight.Intensity = 0;
                }
                else
                {
                    realAngle = angle * (180 / (float)Math.PI);
                    if (realAngle < 20)
                        intensityPercentage = realAngle * .05f;
                    if (realAngle > 160)
                        intensityPercentage = (180 - realAngle) * .05f;

                    //Day
                    sunlight.Intensity = sunlightIntensity * intensityPercentage;
                    light1.Intensity = lightIntensity + (.1f * intensityPercentage);
                    light2.Intensity = lightIntensity + (.1f * intensityPercentage);
                    float height = (float)Math.Sin(angle);
                    float width = (float)Math.Cos(angle);

                    sunlight.LightDirection = new Vector3(-width, -height, .8f);
                    if (height < 0)
                        height *= -1;
                    sunlight.Color = new Color3(1f, .4f + (.6f * height), .2f + height * .8f);
                }
            }
        }
        #endregion

        #region Lights
        /// <summary>
        /// Creates a light entity based on the deferred lightning example
        /// </summary>
        /// <param name="direction">Light direction</param>
        /// <param name="color">Light color</param>
        /// <param name="intensity">Light intensity</param>
        /// <returns>The light entity</returns>
        private Entity CreateDirectLight(Vector3 direction, Color3 color, float intensity)
        {
            return new Entity()
            {
                new LightComponent
                {
                    Type = LightType.Directional,
                    Color = color,
                    Deferred = isDeferred,
                    Enabled = true,
                    Intensity = intensity,
                    LightDirection = direction,
                    Layers = RenderLayers.RenderLayerAll,
                    ShadowMap = false,
                    ShadowFarDistance = 3000,
                    ShadowNearDistance = 1,
                    ShadowMapMaxSize = 512,
                    ShadowMapMinSize = 256,
                    ShadowMapCascadeCount = 4,
                    ShadowMapFilterType = ShadowMapFilterType.Variance,
                    BleedingFactor = 0,
                    MinVariance = 0
                }
            };
        } 
        #endregion
    }
}
