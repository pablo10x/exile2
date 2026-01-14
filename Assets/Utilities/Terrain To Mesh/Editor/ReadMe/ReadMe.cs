// Terrain To Mesh <https://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using System;
using System.IO;

using UnityEngine;
using UnityEditor;


namespace AmazingAssets.TerrainToMesh.Editor
{
    //[CreateAssetMenuAttribute(fileName = "ReadMe", menuName = "Amazing Assets/Terrain To Mesh/ReadMe")]
    public class ReadMe : ScriptableObject
    {
        public enum URLType { OpenPage, MailTo }


        public Texture2D logo;
        public Section[] sections;

        [Serializable]
        public class Section
        {
            public string heading, text, linkText, url;
            public URLType urlType;

            public Section()
            {

            }

            public Section(string heading, string text, string linkText, string url, URLType urlType = URLType.OpenPage)
            {
                this.heading = heading;
                this.text = text;
                this.linkText = linkText;
                this.url = url;
                this.urlType = urlType;
            }
        }
    }
}