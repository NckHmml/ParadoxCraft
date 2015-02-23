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
using SiliconStudio.Paradox.Effects.ShadowMaps;
using SiliconStudio.Paradox.Effects.Renderers;
using SiliconStudio.Paradox.Effects.Processors;

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
        /// Player camera entity
        /// </summary>
        private Entity Camera { get; set; }

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

        /// <summary>
        /// Component for the sunlight
        /// </summary>
        private LightComponent Sunlight { get; set; }

        /// <summary>
        /// Component#1 for the background lighting
        /// </summary>
        private LightComponent EnviromentLight1 { get; set; }

        /// <summary>
        /// Component#2 for the background lighting
        /// </summary>
        private LightComponent EnviromentLight2 { get; set; }

        /// <summary>
        /// Atmosphere handler for the sunlight
        /// </summary>
        private AtmosphereData Atmosphere { get; set; }
        #endregion

        #region Initialization
        /// <summary>
        /// Creates a new instance of <see cref="ParadoxCraftGame"/>
        /// </summary>
        public ParadoxCraftGame()
        {
            // Target 11.0 profile by default
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            Factory = new ChunkFactory();
        }

        /// <summary>
        /// Loads the game content
        /// </summary>
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Hides the mouse
            IsMouseVisible = false;

            // Set fog values
            GraphicsDevice.Parameters.Set(FogEffectKeys.fogNearPlaneZ, 100f);
            GraphicsDevice.Parameters.Set(FogEffectKeys.fogFarPlaneZ, 160f);

            // Create the Atmosphere lighting
            Atmosphere = AtmosphereBuilder.Generate(GraphicsDevice, EffectSystem);
            CreateSunLight();

            // Lights
            EnviromentLight1 = CreateDirectLight(new Vector3(-1, -1, -1), new Color3(1, 1, 1), .3f);
            EnviromentLight2 = CreateDirectLight(new Vector3(1, 1, 1), new Color3(1, 1, 1), .3f);

            // Create the pipeline
            CreatePipeline();
            CreateCursor();

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
        /// Creates the rendering pipeline
        /// </summary>
        private void CreatePipeline()
        {
            var renderers = RenderSystem.Pipeline.Renderers;
            var width = GraphicsDevice.BackBuffer.Width;
            var height = GraphicsDevice.BackBuffer.Height;
            var viewport = new Viewport(0, 0, width, height);
            var clearColor = Color.Black;
            var effectName = "ParadoxCraftEffectMain";
            var fowardEffectName = "ParadoxCraftEffectForward";
            var prepassEffectName = "ParadoxCraftPrepassEffect";

            // Adds a light processor that will track all the entities that have a light component.
            // This will also handle the shadows (allocation, activation etc.).
            var lightProcessor = Entities.GetProcessor<LightShadowProcessor>();
            if (lightProcessor == null)
                Entities.Processors.Add(new DynamicLightShadowProcessor(GraphicsDevice, false));

            // Camera 
            renderers.Add(new CameraSetter(Services));

            // Create G-buffer pass
            var gbufferPipeline = new RenderPipeline("GBuffer");

            // Renders the G-buffer for opaque geometry.
            gbufferPipeline.Renderers.Add(new ModelRenderer(Services, effectName + ".ParadoxGBufferShaderPass").AddOpaqueFilter());
            var gbufferProcessor = new GBufferRenderProcessor(Services, gbufferPipeline, GraphicsDevice.DepthStencilBuffer, false);

            // Add sthe G-buffer pass to the pipeline.
            renderers.Add(gbufferProcessor);

            // Performs the light prepass on opaque geometry.
            // Adds this pass to the pipeline.
            var lightDeferredProcessor = new LightingPrepassRenderer(Services, prepassEffectName, GraphicsDevice.DepthStencilBuffer, gbufferProcessor.GBufferTexture);
            renderers.Add(lightDeferredProcessor);

            renderers.Add(new RenderTargetSetter(Services)
            {
                ClearColor = clearColor,
                EnableClearDepth = false,
                RenderTarget = GraphicsDevice.BackBuffer,
                DepthStencil = GraphicsDevice.DepthStencilBuffer,
                Viewport = viewport
            });

            renderers.Add(new RenderStateSetter(Services) { DepthStencilState = GraphicsDevice.DepthStencilStates.Default, RasterizerState = GraphicsDevice.RasterizerStates.CullBack });

            // Renders all the meshes with the correct lighting.
            renderers.Add(new ModelRenderer(Services, fowardEffectName).AddLightForwardSupport());

            // Blend atmoshpere inscatter on top
            renderers.Add(new AtmosphereRenderer(Services, Atmosphere, Sunlight));

            // Wireframe mode
            if (isWireframe)
                GraphicsDevice.Parameters.Set(Effect.RasterizerStateKey, RasterizerState.New(GraphicsDevice, new RasterizerStateDescription(CullMode.None) { FillMode = FillMode.Wireframe }));

            GraphicsDevice.Parameters.Set(RenderingParameters.UseDeferred, true);
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

        private void CreateCursor()
        {
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

            // Cursor render
            RenderSystem.Pipeline.Renderers.Add(new DelegateRenderer(Services) { Render = RenderCursor });
        }

        /// <summary>
        /// Renders the cursor
        /// </summary>
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

                    Factory.CheckLoad(playerX + x, -1, playerZ + z);
                    Factory.CheckLoad(playerX - x, -1, playerZ + z);
                    Factory.CheckLoad(playerX + x, -1, playerZ - z);
                    Factory.CheckLoad(playerX - x, -1, playerZ - z);

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

        /// <summary>
        /// Script for the "Day/Night Cycle" 
        /// </summary>
        private async Task LightCycleScript()
        {
            float
                sunlightIntensity = Sunlight.Intensity,
                lightIntensity = EnviromentLight1.Intensity,
                sunZenith = MathUtil.RevolutionsToRadians(0.2f),
                intensityPercentage = 1f,
                realAngle;
            double time;
            while (IsRunning)
            {
                await Task.Delay(500);
                time = UpdateTime.Total.TotalSeconds;
                float curAngle = (float)time / (Constants.DayNightCycle / 360f);
                float angle = curAngle / Constants.Degrees90 / 90;
                angle %= Constants.Degrees90 * 4;

                if (angle > 0 && angle < Constants.Degrees90 * 2)
                {
                    //Day

                    realAngle = angle * (180 / (float)Math.PI);
                    if (realAngle < 20)
                        intensityPercentage = realAngle * .05f;
                    if (realAngle > 160)
                        intensityPercentage = (180 - realAngle) * .05f;

                    Sunlight.Intensity = sunlightIntensity + (.1f * intensityPercentage);
                    EnviromentLight1.Intensity = lightIntensity + (.1f * intensityPercentage);
                    EnviromentLight2.Intensity = lightIntensity + (.1f * intensityPercentage);

                    float height = (float)Math.Sin(angle);
                    float width = (float)Math.Cos(angle);
                    Sunlight.Color = new Color3(1f, .4f + (.6f * height), .2f + height * .8f);
                }

                sunZenith = MathUtil.Mod2PI(angle - Constants.Degrees90);
                Sunlight.LightDirection = new Vector3((float)Math.Sin(sunZenith), -(float)Math.Cos(sunZenith), 0);
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
        private LightComponent CreateDirectLight(Vector3 direction, Color3 color, float intensity)
        {
            var directLightEntity = new Entity();
            LightComponent directLightComponent = new LightComponent
            {
                Type = LightType.Directional,
                Color = color,
                Deferred = false,
                Enabled = true,
                Intensity = intensity,
                LightDirection = direction
            };
            directLightEntity.Add(directLightComponent);
            Entities.Add(directLightEntity);
            return directLightComponent;
        }

        private void CreateSunLight()
        {
            // create the lights
            var directLightEntity = new Entity();
            Sunlight = new LightComponent
            {
                Type = LightType.Directional,
                Color = new Color3(1, 1, 1),
                Deferred = false,
                Enabled = true,
                Intensity = .2f,
                LightDirection = new Vector3(0)
            };
            directLightEntity.Add(Sunlight);
            Entities.Add(directLightEntity);
        }
        #endregion
    }
}
