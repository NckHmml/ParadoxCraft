using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;

namespace ParadoxCraft
{
    public class AtmosphereBuilder : ComponentBase
    {
        private BlendState blendState;
        private Effect computeTransmittance, computeSingleIrradiance, computeMultipleIrradiance;
        private Effect computeSingleInscatter, computeMultipleInscatter, computeOutscatter, copySingleInscatter, copyMultipleInscatter;
        private Effect copySlice;
        private Texture deltaE, deltaSM, deltaSR, deltaJ;
        private ParameterCollection parameters = new ParameterCollection();

        public AtmosphereData Data { get; set; }

        public AtmosphereBuilder(GraphicsDevice device, EffectSystem effectSystem)
        {
            blendState = BlendState.New(device, new BlendStateDescription(Blend.One, Blend.One)).DisposeBy(this);

            Data = new AtmosphereData(device, new AtmosphereSettings());

            // TODO: Use max precision temporary textures
            /*
                        var intermediateTransmittanceDesc = Data.Transmittance.Description;
                        intermediateTransmittanceDesc.Format = PixelFormat.R32G32B32A32_Float;
                        var intermediateIrradianceDesc = Data.Irradiance.Description;
                        intermediateIrradianceDesc.Format = PixelFormat.R32G32B32A32_Float;
                        var intermediateInscatterDesc = Data.Inscatter.Description;
                        intermediateInscatterDesc.Format = PixelFormat.R32G32B32A32_Float;

                        var intermediateTransmittance = Texture2D.New(device, intermediateTransmittanceDesc).DisposeBy(this);
                        var intermediateIrradiance = Texture3D.New(device, intermediateIrradianceDesc).DisposeBy(this);
                        var intermediateInscatter = Texture3D.New(device, intermediateInscatterDesc).DisposeBy(this);
             */

            deltaE = Texture.New(device, Data.Irradiance.Description).DisposeBy(this);
            deltaSM = Texture.New(device, Data.Inscatter.Description).DisposeBy(this);
            deltaSR = Texture.New(device, Data.Inscatter.Description).DisposeBy(this);
            deltaJ = Texture.New(device, Data.Inscatter.Description).DisposeBy(this);

            computeTransmittance = effectSystem.LoadEffect("ComputeTransmittance");
            computeSingleIrradiance = effectSystem.LoadEffect("SingleIrradiance");
            copySingleInscatter = effectSystem.LoadEffect("CopySingleInscatter");
            computeSingleInscatter = effectSystem.LoadEffect("SingleInscatter");
            computeOutscatter = effectSystem.LoadEffect("Outscatter");
            computeMultipleIrradiance = effectSystem.LoadEffect("MultipleIrradiance");
            computeMultipleInscatter = effectSystem.LoadEffect("MultipleInscatter");
            copyMultipleInscatter = effectSystem.LoadEffect("CopyMultipleInscatter");
            copySlice = effectSystem.LoadEffect("CopySlice");

            parameters.Set(AtmospherePrecomputationKeys.DeltaSR, deltaSR);
            parameters.Set(AtmospherePrecomputationKeys.DeltaSM, deltaSM);
            parameters.Set(AtmospherePrecomputationKeys.DeltaE, deltaE);
            parameters.Set(AtmospherePrecomputationKeys.DeltaJ, deltaJ);
        }

        public static AtmosphereData Generate(GraphicsDevice device, EffectSystem effectSystem)
        {
            if (VirtualFileSystem.ApplicationCache.FileExists("atmosphere"))
            {
                using (var stream = VirtualFileSystem.ApplicationCache.OpenStream("atmosphere", VirtualFileMode.Open, VirtualFileAccess.Read))
                {
                    return AtmosphereData.Load(device, stream);
                }
            }
            else
            {
                using (var builder = new AtmosphereBuilder(device, effectSystem))
                {
                    builder.Generate(device);

                    using (var stream = VirtualFileSystem.ApplicationCache.OpenStream("atmosphere", VirtualFileMode.Create, VirtualFileAccess.Write))
                    {
                        builder.Data.Save(stream);
                    }

                    return builder.Data;
                }
            }
        }

        public void Generate(GraphicsDevice device)
        {
            Draw(device, computeTransmittance, Data.Transmittance);

            Draw(device, computeSingleIrradiance, deltaE);

            DrawSlices(device, computeSingleInscatter, deltaSR, deltaSM);

            device.Clear(Data.Irradiance, Color.Black);

            DrawSlices(device, copySingleInscatter, Data.Inscatter);

            for (int order = 2; order <= 4; order++)
            {
                parameters.Set(AtmospherePrecomputationKeys.IsFirst, order == 2);

                DrawSlices(device, computeOutscatter, deltaJ);
                Draw(device, computeMultipleIrradiance, deltaE);
                DrawSlices(device, computeMultipleInscatter, deltaSR);

                device.SetBlendState(blendState);
                {
                    device.SetRenderTarget(Data.Irradiance);
                    device.DrawTexture(deltaE, device.SamplerStates.PointClamp);
                    DrawSlices(device, copyMultipleInscatter, Data.Inscatter);
                }
                device.SetBlendState(device.BlendStates.Default);
            }
        }

        private void Draw(GraphicsDevice device, Effect effect, Texture renderTarget)
        {
            device.SetRenderTarget(renderTarget);
            effect.Apply(Data.Parameters, parameters);
            device.Draw(PrimitiveType.TriangleList, 3);
        }

        private void DrawSlices(GraphicsDevice device, Effect effect, params Texture[] renderTargets)
        {
            device.SetRenderTargets(renderTargets);
            parameters.Set(VolumeShaderBaseKeys.SliceCount, (uint)Data.Settings.AltitudeResolution);
            effect.Apply(Data.Parameters, parameters);
            device.DrawInstanced(PrimitiveType.TriangleList, 3, Data.Settings.AltitudeResolution);
        }

        private void Save(GraphicsDevice device, Texture texture, string url)
        {
            var desc = texture.Description;
            desc.Format = PixelFormat.R8G8B8A8_UNorm;
            desc.Flags |= TextureFlags.RenderTarget;

            using (var renderTarget = Texture.New(device, desc))
            {
                //device.Clear(texTarget, Color.Black);
                device.SetRenderTarget(renderTarget);
                device.DrawTexture(texture, device.SamplerStates.PointClamp);
                Save(renderTarget, url);
            }
        }

        private void Save(Texture texture, string url)
        {
            VirtualFileSystem.ApplicationBinary.CreateDirectory("Precomputed");
            using (var stream = VirtualFileSystem.ApplicationBinary.OpenStream("Precomputed/" + url, VirtualFileMode.Create, VirtualFileAccess.Write))
                texture.Save(stream, ImageFileType.Png);
        }

        private void SaveInscatter(GraphicsDevice device, Texture inscatter, string name)
        {
            var parameters = new ParameterCollection();

            using (var compactInscatter = Texture.New2D(device, Data.Settings.SunZenithResolution * Data.Settings.ViewSunResolution, Data.Settings.ViewZenithResolution * Data.Settings.AltitudeResolution, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource))
            {
                device.SetRenderTarget(compactInscatter);
                device.Clear(compactInscatter, Color.Black);

                for (int i = 0; i < Data.Settings.AltitudeResolution; i++)
                {
                    device.SetViewport(new Viewport(0, i * Data.Settings.ViewZenithResolution, Data.Settings.SunZenithResolution * Data.Settings.ViewSunResolution, Data.Settings.ViewZenithResolution));
                    parameters.Set(CopySliceKeys.Source, inscatter);
                    parameters.Set(CopySliceKeys.Slice, (i + 0.5f) / Data.Settings.AltitudeResolution);

                    copySlice.Apply(parameters);
                    device.Draw(PrimitiveType.TriangleList, 3);
                }

                Save(compactInscatter, "Compact" + name + ".png");
            }
        }

    }
}
