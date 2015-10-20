namespace DirtBag.Modules {
    public interface IModuleSettings {
        bool Enabled { get; set; }
        int EveryXRuns { get; set; }
        PostType PostTypes { get; set; }

        void SetDefaultSettings();
    }
}