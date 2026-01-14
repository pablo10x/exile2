namespace FewClicksDev.Core.Versioning
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Changelog : ScriptableObject
    {
        [SerializeField] private PackageInformation packageInformation = null;
        [SerializeField] private List<VersionDescription> versions = new List<VersionDescription>();

        public PackageInformation PackageInfo => packageInformation;
        public List<VersionDescription> Versions => versions;
    }
}