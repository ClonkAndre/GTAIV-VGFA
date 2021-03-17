namespace VGFA.Helper {
    public interface IPluginBase {

        bool Load();
        bool Unload();
        void Draw(object sender, GTA.GraphicsEventArgs e);
        void Tick();
        void KeyDown(object sender, GTA.KeyEventArgs e);
        void KeyUp(object sender, GTA.KeyEventArgs e);
        bool Start(GTA.Ped _challenger);

    }
}
