namespace FewClicksDev.Core.Versioning
{
    [System.Serializable]
    public struct Version
    {
        public static readonly Version Default = new Version(1, 0, 0);

        public int Major;
        public int Minor;
        public int Patch;

        public Version(int _major, int _minor, int _patch)
        {
            Major = _major;
            Minor = _minor;
            Patch = _patch;
        }

        public override string ToString()
        {
            return $"v{Major}.{Minor}.{Patch}";
        }
    }
}
