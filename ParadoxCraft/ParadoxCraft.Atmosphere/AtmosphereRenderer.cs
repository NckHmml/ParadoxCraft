using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Graphics;
using System;

namespace ParadoxCraft
{
    public class AtmosphereRenderer : Renderer
    {
        private BlendState blendState;
        private AtmosphereData data;
        private Effect effect;
        private LightComponent sunLight;
        private ParameterCollection parameters = new ParameterCollection();

        public AtmosphereRenderer(IServiceRegistry services, AtmosphereData data, LightComponent sunLight)
            : base(services)
        {
            this.data = data;
            this.sunLight = sunLight;
        }

        public override void Load()
        {
            var desc = new BlendStateDescription(Blend.One, Blend.SourceAlpha);
            desc.RenderTargets[0].AlphaSourceBlend = Blend.Zero;
            desc.RenderTargets[0].AlphaDestinationBlend = Blend.One;
            blendState = BlendState.New(GraphicsDevice, desc);

            effect = EffectSystem.LoadEffect("AtmosphereLighting");
            Pass.StartPass += Render;
        }

        public override void Unload()
        {
            Pass.StartPass -= Render;
        }

        public void Render(RenderContext context)
        {
            var eyePosition = context.GraphicsDevice.Parameters.Get(TransformationKeys.Eye);
            var view = context.CurrentPass.Parameters.Get(TransformationKeys.View);
            var sunDirection = Vector3.Normalize(-sunLight.LightDirection);
            var altitude = Math.Max(eyePosition.Y, 10.0f) + data.Settings.GroundHeight;

            GraphicsDevice.SetBlendState(blendState);
            GraphicsDevice.SetRenderTarget(GraphicsDevice.BackBuffer);

            parameters.Set(AtmosphereLightingKeys.CenterVS, Vector3.TransformNormal(Vector3.UnitY * -altitude, view));
            parameters.Set(AtmosphereLightingKeys.SunDirectionVS, Vector3.TransformNormal(sunDirection, view));

            // TODO: Handle state correctly
            effect.Apply(context.CurrentPass.Parameters, data.Parameters, parameters);

            GraphicsDevice.Draw(PrimitiveType.TriangleList, 3);
        }
    }
}
