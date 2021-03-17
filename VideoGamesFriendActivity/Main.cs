// By ItsClonkAndre
// Version 1.0

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Un4seen.Bass;
using VGFA.Helper;
using GTA;

namespace VideoGamesFriendActivity {

    public enum Menu
    {
        MainMenu = 0,
        Playing
    }

    public class Main : Script {

        #region Variables
        private PrivateFontCollection pfc;
        private GTA.Font generalFont, _8BitFont_48, _8BitFont_24, _8BitFont_16, _8BitFont_14;

        private IPluginBase game;
        private PluginManager manager;
        private List<SavehouseTV> Savehouses;
        private Ped playingPed;
        private bool isVideoConsoleActive;
        private bool isWithFriend;
        private bool tempBlock;
        private bool tempSpeach;
        private int selectedIndex;
        private string DataFolder = Game.InstallFolder + "\\scripts\\VideoGamesFriendActivity";

        // Settings
        protected internal static int soundVolume;

        // Sizes
        private Size AfariTextSize;

        // State
        private Menu CurrentMenu = Menu.MainMenu;
        private SavehouseTV CurrentSavehouse;

        // Fading
        private static bool showFadingscreen;
        private static bool fadeIn, fadeOut;
        private static int fadingSpeed;
        private static int fadingAlphaInt;
        #endregion

        #region Methods
        private void AGame_ExitGameCalled(object sender, EventArgs e)
        {
            if (game != null) {
                game.Unload();
            }
            CurrentMenu = Menu.MainMenu;
        }
        protected internal static void FadeScreenIn(int _fadingSpeed = 1)
        {
            fadingAlphaInt = 255;
            showFadingscreen = true;
            fadingSpeed = _fadingSpeed;
            fadeOut = false;
            fadeIn = true;
        }
        protected internal static void FadeScreenOut(int _fadingSpeed = 1)
        {
            fadingAlphaInt = 0;
            showFadingscreen = true;
            fadingSpeed = _fadingSpeed;
            fadeIn = false;
            fadeOut = true;
        }
        #endregion

        public Main()
        {
            // Setup Bass.dll
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            // Font
            pfc = new PrivateFontCollection();

            #region Font
            pfc.AddFontFile(DataFolder + "\\bit5x3.ttf");
            generalFont = new GTA.Font("Arial", 26f, FontScaling.Pixel);
            _8BitFont_48 = new GTA.Font(pfc.Families[0].Name, 48f, FontScaling.Pixel);
            _8BitFont_48.Effect = FontEffect.None;
            _8BitFont_24 = new GTA.Font(pfc.Families[0].Name, 24f, FontScaling.Pixel);
            _8BitFont_24.Effect = FontEffect.None;
            _8BitFont_16 = new GTA.Font(pfc.Families[0].Name, 16f, FontScaling.Pixel);
            _8BitFont_16.Effect = FontEffect.None;
            _8BitFont_14 = new GTA.Font(pfc.Families[0].Name, 14f, FontScaling.Pixel);
            _8BitFont_14.Effect = FontEffect.None;
            #endregion

            // Load Settings
            soundVolume = Settings.GetValueInteger("General", "Volume", 20);
            if (soundVolume > 100) {
                soundVolume = 100;
            }
            else if (soundVolume < 0) {
                soundVolume = 0;
            }

            // Size
            AfariTextSize = TextRenderer.MeasureText("AFARI VCS", _8BitFont_48.WindowsFont);
            
            // Savehouses
            Savehouses = new List<SavehouseTV>();
            Savehouses.Add(new SavehouseTV("Hove Beach", new Vector3(895.54f, -495.78f, 19.41f), new Vector3(893.75f, -493.29f, 19.41f)));
            Savehouses.Add(new SavehouseTV("South Bohan", new Vector3(597.86f, 1406.36f, 17.48f), new Vector3(601.14f, 1408.95f, 17.48f)));
            Savehouses.Add(new SavehouseTV("Middle Park East", new Vector3(105.12f, 854.90f, 45.08f), new Vector3(101.07f, 850.12f, 45.08f)));
            Savehouses.Add(new SavehouseTV("Northwood", new Vector3(-427.47f, 1468.76f, 38.97f), new Vector3(-422.82f, 1463.27f, 38.97f)));
            Savehouses.Add(new SavehouseTV("Alderny City", new Vector3(-963.67f, 890.73f, 19.01f), new Vector3(-966.68f, 890.01f, 19.01f)));

            // Other
            manager = new PluginManager();
            manager.LoadPlugins(DataFolder + "\\Games\\");
            AGame.ExitGameCalled += AGame_ExitGameCalled;

            this.Interval = 10;
            this.Tick += Main_Tick;
            this.PerFrameDrawing += Main_PerFrameDrawing;
            this.KeyDown += Main_KeyDown;
            this.KeyUp += Main_KeyUp;
        }

        private void Main_Tick(object sender, EventArgs e)
        {
            Ped[] allPeds = World.GetPeds(Player.Character.Position, 10f);
            for (int i = 0; i < allPeds.Length; i++) {
                Ped ped = allPeds[i];
                if (Exists(ped)) {
                    if (ped.Model.ToString() == "0x89395FC9" || ped.Model.ToString() == "0x98E29920" || ped.Model.ToString() == "0xDB354C19" || ped.Model.ToString() == "0x58A1E271" || ped.Model.ToString() == "0x64C74D3B") { // Roman, Brucie, Dwayne, Little Jacob, Patrick McReary
                        playingPed = ped;
                        isWithFriend = ped.isRequiredForMission;
                    }
                    else {
                        playingPed = null;
                    }
                }
            }

            // Check if player is in one of the available savehouse tv spot
            for (int i = 0; i < Savehouses.Count; i++) {
                SavehouseTV savehouse = Savehouses[i];
                if (savehouse.CheckIfPlayerIsInArea(true)) {
                    CurrentSavehouse = savehouse;
                    break;
                }
                else {
                    CurrentSavehouse = null;
                }
            }

            // Plugin
            switch (CurrentMenu) {
                case Menu.Playing:
                    manager.InvokeTickMethodOnAllPlugins();
                    break;
            }
        }

        private void Main_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            e.Graphics.Scaling = FontScaling.Pixel;

            if (CurrentSavehouse != null && isWithFriend && !tempBlock) {
                Size keyHintSize = TextRenderer.MeasureText("Press 'E' to play some video games.", generalFont.WindowsFont);
                e.Graphics.DrawText("Press 'E' to play some video games.", new RectangleF(70f, 70f, keyHintSize.Width, keyHintSize.Height + 2), TextAlignment.Left, Color.White, generalFont);
            }
            else if (CurrentSavehouse != null && !isWithFriend && !tempBlock) {
                Size keyHintSize = TextRenderer.MeasureText("Press 'G' to play some video games.", generalFont.WindowsFont);
                e.Graphics.DrawText("Press 'G' to play some video games.", new RectangleF(70f, 70f, keyHintSize.Width, keyHintSize.Height + 2), TextAlignment.Left, Color.White, generalFont);
            }

            #region Fading
            if (showFadingscreen) {
                if (fadeIn) {
                    if (!(fadingAlphaInt <= 0)) {
                        fadingAlphaInt -= fadingSpeed;
                    }
                }
                if (fadeOut) {
                    if (!(fadingAlphaInt >= 255))  {
                        fadingAlphaInt += fadingSpeed;
                    }
                }

                if (fadingAlphaInt < 0) {
                    e.Graphics.DrawRectangle(Game.Resolution.Width / 2, Game.Resolution.Height / 2, Game.Resolution.Width, Game.Resolution.Height, Color.FromArgb(0, 0, 0, 0));
                }
                else if (fadingAlphaInt > 255) {
                    e.Graphics.DrawRectangle(Game.Resolution.Width / 2, Game.Resolution.Height / 2, Game.Resolution.Width, Game.Resolution.Height, Color.FromArgb(255, 0, 0, 0));
                }
                else {
                    e.Graphics.DrawRectangle(Game.Resolution.Width / 2, Game.Resolution.Height / 2, Game.Resolution.Width, Game.Resolution.Height, Color.FromArgb(fadingAlphaInt, 0, 0, 0));
                }
            }
            #endregion

            if (isVideoConsoleActive) {
                if (fadingAlphaInt >= 255) {

                    switch (CurrentMenu) {
                        case Menu.MainMenu:
                            // Logo
                            e.Graphics.DrawRectangle(new RectangleF(70f, 50f, 30f, 56f), Color.FromArgb(255, 158, 110));
                            e.Graphics.DrawRectangle(new RectangleF(100f, 50f, 30f, 56f), Color.FromArgb(254, 209, 108));
                            e.Graphics.DrawRectangle(new RectangleF(130f, 50f, 30f, 56f), Color.FromArgb(253, 243, 146));
                            e.Graphics.DrawText("AFARI VCS", new RectangleF(175f, 58f, AfariTextSize.Width, AfariTextSize.Height + 2), TextAlignment.Left, Color.White, _8BitFont_48);

                            // Copyright
                            Size soundsFromText = TextRenderer.MeasureText("Copyright (c) 1977 Afari, Inc.", _8BitFont_14.WindowsFont);
                            e.Graphics.DrawText("Copyright (c) 1977 Afari, Inc.", new RectangleF(70f, Game.Resolution.Height - 50, soundsFromText.Width + 70, soundsFromText.Height + 2), TextAlignment.Left, Color.White, _8BitFont_14);

                            // List games
                            e.Graphics.DrawText(string.Format("All games ({0})", manager.plugins.Count.ToString()), new RectangleF(70f, 180f, 250f, 20f), TextAlignment.Left, Color.White, _8BitFont_24);
                            e.Graphics.DrawText("Navigate with UP, DOWN and ENTER to start the selected game.", new RectangleF(70f, 205f, 700f, 20f), TextAlignment.Left, Color.White, _8BitFont_16);

                            if (manager.plugins.Count != 0) {
                                for (int i = 0; i < manager.plugins.Count; i++) {
                                    if (selectedIndex == i) {
                                        e.Graphics.DrawText(manager.plugins[i].ToString().Split('.')[0], new RectangleF(70f, 255f + i * 25f, 700f, 20f), TextAlignment.Left, Color.FromArgb(255, 158, 110), _8BitFont_16);
                                    }
                                    else {
                                        e.Graphics.DrawText(manager.plugins[i].ToString().Split('.')[0], new RectangleF(70f, 255f + i * 25f, 700f, 20f), TextAlignment.Left, Color.White, _8BitFont_16);
                                    }
                                }
                            }
                            else {
                                e.Graphics.DrawText("NO GAMES INSTALLED", new RectangleF(70f, 255f, 700f, 20f), TextAlignment.Left, Color.White, _8BitFont_16);

                                if (!tempSpeach) {
                                    if (playingPed != null) playingPed.SayAmbientSpeech("SHIT");
                                    tempSpeach = true;
                                }
                            }

                            break;
                        case Menu.Playing:
                            manager.InvokeDrawMethodOnAllPlugins(sender, e);
                            break;
                    }

                }
            }
        }

        private void Main_KeyDown(object sender, GTA.KeyEventArgs e)
        {
            // Plugin
            switch (CurrentMenu) {
                case Menu.Playing:
                    manager.InvokeKeyDownMethodOnAllPlugins(sender, e);
                    break;
            }

            if (e.Key == Keys.E) {
                if (CurrentSavehouse != null && isWithFriend && !tempBlock) {
                    tempBlock = true;
                    Game.LocalPlayer.CanControlCharacter = false;
                    Game.LocalPlayer.Character.SayAmbientSpeech("VGAME_START");
                    if (playingPed != null) Game.LocalPlayer.Character.Task.TurnTo(playingPed);
                    Wait(2000);
                    if (playingPed != null) playingPed.SayAmbientSpeech("VGAME_START");
                    Wait(2500);
                    FadeScreenOut(2);
                    isVideoConsoleActive = true;
                    tempSpeach = false;
                }
            }
            else if (e.Key == Keys.G) {
                if (CurrentSavehouse != null && !isWithFriend && !tempBlock) {
                    tempBlock = true;
                    Game.LocalPlayer.CanControlCharacter = false;
                    FadeScreenOut(2);
                    isVideoConsoleActive = true;
                    tempSpeach = false;
                }
            }
            else if (e.Key == Keys.Back) {
                if (isVideoConsoleActive) {
                    if (CurrentMenu == Menu.MainMenu) {
                        if (isWithFriend) {
                            FadeScreenIn(2);
                            isVideoConsoleActive = false;
                            Game.LocalPlayer.CanControlCharacter = true;
                            Wait(1500);
                            if (playingPed != null) playingPed.SayAmbientSpeech("VGAME_PLAYER_PLAYS_WELL");
                            Wait(2000);
                            Game.LocalPlayer.Character.SayAmbientSpeech("THANKS");
                            Wait(1000);
                            tempBlock = false;
                        }
                        else {
                            FadeScreenIn(2);
                            isVideoConsoleActive = false;
                            Game.LocalPlayer.CanControlCharacter = true;
                            Wait(1000);
                            tempBlock = false;
                        }
                    }
                }
            }
            else if (e.Key == Keys.Up) {
                if (CurrentMenu == Menu.MainMenu) {
                    if (selectedIndex == 0) {
                        selectedIndex = (manager.plugins.Count - 1);
                    }
                    else {
                        selectedIndex--;
                    }
                }
            }
            else if (e.Key == Keys.Down) {
                if (CurrentMenu == Menu.MainMenu) {
                    if (selectedIndex == (manager.plugins.Count - 1)) {
                        selectedIndex = 0;
                    }
                    else {
                        selectedIndex++;
                    }
                }
            }
            else if (e.Key == Keys.Enter) {
                try {
                    if (CurrentMenu == Menu.MainMenu) {
                        if (isVideoConsoleActive) {
                            if (manager.plugins.Count != 0) {
                                game = manager.GetPlugin(manager.plugins[selectedIndex].ToString());
                                if (game != null) {
                                    CurrentMenu = Menu.Playing;
                                    game.Load();
                                    game.Start(playingPed);
                                }
                            }
                        }
                    }
                }
                catch (Exception) {

                }
            }
        }

        private void Main_KeyUp(object sender, GTA.KeyEventArgs e)
        {
            // Plugin
            switch (CurrentMenu) {
                case Menu.Playing:
                    manager.InvokeKeyUpMethodOnAllPlugins(sender, e);
                    break;
            }
        }

    }

    public class SavehouseTV
    {
        public string Name { get; private set; }
        public Vector3 Position1 { get; private set; }
        public Vector3 Position2 { get; private set; }
        public SavehouseTV(string name, Vector3 pos1, Vector3 pos2)
        {
            Name = name;
            Position1 = pos1;
            Position2 = pos2;
        }
        public bool CheckIfPlayerIsInArea(bool ignoreHeight = false)
        {
            return Game.LocalPlayer.Character.isInArea(Position1, Position2, ignoreHeight);
        }
    }

}
