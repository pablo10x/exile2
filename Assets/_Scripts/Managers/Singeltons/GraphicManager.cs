using Newtonsoft.Json;
using UnityEngine;

namespace _Scripts.Managers.Singeltons
{
    public enum AntiAliasingLevel
    {
        None   = 0,
        TwoX   = 2,
        FourX  = 4,
        EightX = 8,
    }

    public enum OverallQualityLevel
    {
        VeryLow = 0,
        Low     = 1,
        Medium  = 2,
        High    = 3,
    }

    public enum ShadowQualityLevel
    {
        NoShadows = 0,
        VeryLow   = 1,
        Low       = 2,
        Medium    = 3,
        High      = 4,
    }

    public class GraphicManager : Singleton<GraphicManager>
    {
        private ShadowQualityLevel   currentshadowLevel;
        private AntiAliasingLevel    currentAntiAliasing;
        private OverallQualityLevel  currentOverallQuality;
        private AnisotropicFiltering currentAnisotropic;

        public void SetShadowQuality(ShadowQualityLevel level)
        {
            currentshadowLevel = level;
            switch (level)
            {
                case ShadowQualityLevel.NoShadows:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;

                case ShadowQualityLevel.VeryLow:
                    QualitySettings.shadows          = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowDistance   = 10;
                    break;
                case ShadowQualityLevel.Low:

                    QualitySettings.shadows          = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowDistance   = 20;

                    break;

                case ShadowQualityLevel.Medium:
                    QualitySettings.shadows          = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowDistance   = 40;


                    break;
                case ShadowQualityLevel.High:
                    QualitySettings.shadows          = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance   = 60;
                    break;
            }
        }

        public ShadowQualityLevel GetShadowQuality()
        {
            return currentshadowLevel;
        }

        public void SetAntiAliasing(AntiAliasingLevel level)
        {
            currentAntiAliasing     = level;
            QualitySettings.antiAliasing = (int)level;
        }

        public int GetAntiAliasing()
        {
            return QualitySettings.antiAliasing;
        }

        #region Texture Quality

        public void SetTextureQuality(int quality)
        {
            QualitySettings.globalTextureMipmapLimit = quality;
        }

        public int GetTextureQuality()
        {
            return QualitySettings.globalTextureMipmapLimit;
        }

        #endregion


        #region Load bias

        public void SetLODBias(float bias)
        {
            QualitySettings.lodBias = bias;
        }

        public float GetLODBias()
        {
            return QualitySettings.lodBias;
        }

        #endregion

        #region Anisotropic

        public void SetAnisotropicFiltering(AnisotropicFiltering filtering)
        {
            currentAnisotropic          = filtering;
            QualitySettings.anisotropicFiltering = filtering;
        }

        public AnisotropicFiltering GetAnisotropicFiltering()
        {
            return QualitySettings.anisotropicFiltering;
        }

        #endregion

        public void SetOverallQualityLevel(OverallQualityLevel level)
        {
            currentOverallQuality = level;
            QualitySettings.SetQualityLevel((int)level);
            
        }

        public OverallQualityLevel GetOverallQualityLevel()
        {
            return (OverallQualityLevel)QualitySettings.GetQualityLevel();
        }


        //loading 

        public void LoadSettings()
        {
          
        }

       
    }
    
   

    internal struct GraphicManagerSave
    {
        [JsonProperty] public ShadowQualityLevel   shadowQualityLevel;
        [JsonProperty] public AntiAliasingLevel    currentAntiAliasingLevel;
        [JsonProperty] public OverallQualityLevel  currentOverallQualityLevel;
        [JsonProperty] public AnisotropicFiltering currentAnisotropicFiltering;
    }
}