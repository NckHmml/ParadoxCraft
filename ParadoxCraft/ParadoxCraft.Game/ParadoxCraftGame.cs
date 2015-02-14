using ParadoxCraft.Terrain;
using ParadoxCraft.Block;

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

namespace ParadoxCraft
{
    public class ParadoxCraftGame : Game
    {
        private bool isWireframe = false;

        private Entity Camera { get; set; }

        private GraphicalTerrain Terrain { get; set; }

        private Movement PlayerMovement { get; set; }


        #region Initialization
        public ParadoxCraftGame()
        {
            // Target 11.0 profile by default
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
        }
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Create pipeline
            RenderPipelineLightingFactory.CreateDefaultForward(this, "ParadoxCraftEffectMain", Color.DarkBlue, false, false);

            // Wireframe mode
            if (isWireframe)
                GraphicsDevice.Parameters.Set(Effect.RasterizerStateKey, RasterizerState.New(GraphicsDevice, new RasterizerStateDescription(CullMode.None) { FillMode = FillMode.Wireframe }));

            // Lights
            Entities.Add(CreateDirectLight(new Vector3(-1, 1, 1), new Color3(1, 1, 1), 0.25f));
            Entities.Add(CreateDirectLight(new Vector3(1, -1, -1), new Color3(1, 1, 1), 0.25f));

            // Entities
            LoadTerrain();
            LoadCamera();

            // Scripts
            Script.Add(MovementScript);
        }

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

        private void LoadTerrain()
        {
            Terrain = new GraphicalTerrain(GraphicsDevice, Asset.Load<Material>("Materials/Terrain"));
            Terrain.Blocks.Add(new GraphicalBlock(Vector3.Zero, BlockSides.All));
            Terrain.Build();
            Entities.Add(Terrain);
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
                dragX = 0.95f * dragX;
                dragY = 0.95f * dragY;
                if (Input.PointerEvents.Count > 0)
                {
                    dragX = Input.PointerEvents.Sum(x => x.DeltaPosition.X);
                    dragY = Input.PointerEvents.Sum(x => x.DeltaPosition.Y);
                    PlayerMovement.YawPitch(dragX, dragY);
                }
            }
        } 
        #endregion

        #region Lights
        private static Entity CreateDirectLight(Vector3 direction, Color3 color, float intensity)
        {
            return new Entity()
            {
                new LightComponent
                {
                    Type = LightType.Directional,
                    Color = color,
                    Deferred = false,
                    Enabled = true,
                    Intensity = intensity,
                    LightDirection = direction,
                    Layers = RenderLayers.RenderLayerAll,
                    ShadowMap = false,
                    ShadowFarDistance = 3000,
                    ShadowNearDistance = 10,
                    ShadowMapMaxSize = 1024,
                    ShadowMapMinSize = 512,
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
