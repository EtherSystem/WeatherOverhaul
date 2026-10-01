using System.Reflection;
using WeatherOverhaul.Weather;

namespace WeatherOverhaul.UI
{
    internal static class WeatherMapStageIconLibrary
    {
        private const string ResourcePrefix = "WeatherOverhaul.Assets.WeatherIcons.";
        private const string PlaceholderFileName = "Placeholder.png";

        private static readonly Dictionary<WeatherStageId, string> s_IconFiles = new()
        {
            [WeatherStageId.Clear] = "Clear.png",
            [WeatherStageId.PartlyCloudy] = "PartlyCloudy.png",
            [WeatherStageId.Cloudy] = "Cloudy.png",
            [WeatherStageId.LightSnow] = "LightSnow.png",
            [WeatherStageId.HeavySnow] = "HeavySnow.png",
            [WeatherStageId.Blizzard] = "Blizzard.png",
            [WeatherStageId.LightFog] = "LightFog.png",
            [WeatherStageId.DenseFog] = "DenseFog.png",
            [WeatherStageId.ClearAurora] = "ClearAurora.png",
            [WeatherStageId.ToxicFog] = "ToxicFog.png",
            [WeatherStageId.LowOvercast] = "LowOvercast.png",
            [WeatherStageId.HeavyOvercast] = "HeavyOvercast.png",
            [WeatherStageId.VeryHeavySnow] = "VeryHeavySnow.png",
            [WeatherStageId.Whiteout] = "Whiteout.png",
            [WeatherStageId.WindyLightSnow] = "WindyLightSnow.png",
            [WeatherStageId.ViolentBlizzard] = "ViolentBlizzard.png",
            [WeatherStageId.VeryDenseFog] = "VeryDenseFog.png",
            [WeatherStageId.FreezingFog] = "FreezingFog.png",
            [WeatherStageId.Ashfall] = "Ashfall.png",
            [WeatherStageId.GlimmerFog] = "GlimmerFog.png",
            [WeatherStageId.CloudyAurora] = "CloudyAurora.png",
            [WeatherStageId.SnowyAurora] = "SnowyAurora.png",
            [WeatherStageId.FoggyAurora] = "FoggyAurora.png",
            [WeatherStageId.ClearBloodMoon] = "ClearBloodMoon.png",
            [WeatherStageId.SnowBloodMoon] = "SnowBloodMoon.png",
            [WeatherStageId.Undefined] = "Unknown.png"
        };

        private static readonly Dictionary<string, Texture2D> s_LoadedTextures = new(StringComparer.Ordinal);
        private static Texture2D? s_GeneratedFallback;

        internal static Texture2D GetIcon(WeatherStageId stageId)
        {
            string fileName = s_IconFiles.TryGetValue(stageId, out string? mappedFile) ? mappedFile : "Unknown.png";
            Texture2D? texture = LoadEmbeddedTexture(fileName);
            if (texture != null) return texture;

            texture = LoadEmbeddedTexture(PlaceholderFileName);
            return texture ?? GetGeneratedFallback();
        }

        private static Texture2D? LoadEmbeddedTexture(string fileName)
        {
            if (s_LoadedTextures.TryGetValue(fileName, out Texture2D? cached)) return cached;

            string resourceName = ResourcePrefix + fileName;
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            byte[] bytes = new byte[stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read <= 0) break;
                offset += read;
            }

            if (offset != bytes.Length) return null;

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
            {
                name = "WO_MapWeatherIcon_" + Path.GetFileNameWithoutExtension(fileName),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            s_LoadedTextures[fileName] = texture;
            return texture;
        }

        private static Texture2D GetGeneratedFallback()
        {
            if (s_GeneratedFallback != null) return s_GeneratedFallback;

            const int size = 32;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "WO_MapWeatherIcon_GeneratedPlaceholder",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color transparent = new(0f, 0f, 0f, 0f);
            Color visible = new(0.88f, 0.86f, 0.78f, 0.95f);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

            for (int y = 4; y < size - 4; y++)
            {
                for (int x = 4; x < size - 4; x++)
                {
                    bool border = x <= 5 || x >= size - 6 || y <= 5 || y >= size - 6;
                    bool slash = Math.Abs(x - y) <= 1 || Math.Abs((size - 1 - x) - y) <= 1;
                    if (border || slash) pixels[y * size + x] = visible;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            s_GeneratedFallback = texture;
            return texture;
        }
    }
}
