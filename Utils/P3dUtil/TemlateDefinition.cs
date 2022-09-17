namespace P3dUtil
{
    public class TemlateDefinition
    {
        public string TemplateFile { get; set; }
        public string TextureBaseDirectory { get; set; }
        public string TexturePattern { get; set; }
        public string TextureNameFilter { get; set; }
        public string InitialTexture { get; set; }
        public string TextureBaseGamePath { get; set; }
        public bool? Backup { get; set; }
        public string Mode { get; set; }
    }
}