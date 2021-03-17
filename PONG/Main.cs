// Version 0.1

using GTA;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VGFA.Helper;
using PONG.Elements;

namespace PONG {
    [Export(typeof(IPluginBase))]
    public class Main : IPluginBase {

        private readonly string DataDir = Game.InstallFolder + "\\scripts\\VideoGamesFriendActivity\\Games\\PONG_DATA";

        #region Variables and Enums
        // Elements
        private RectangleF Bounds;
        private Element Player;
        private Element Player2;
        private Element Ball;

        // Font
        private PrivateFontCollection pfc;
        private GTA.Font _8BitFont_48;

        // Audio
        private VGFA.Helper.API.Audio audio;
        private int wallSound;
        private int paddleSound;
        private int scoreSound;

        // Controls
        private bool keyUpPressed;
        private bool keyDownPressed;
        private bool canGoUp = true;
        private bool canGoDown = true;

        // Other
        private GTA.Timer multiplikatorTimer;
        private Random rnd;
        private Ped challenger;

        private bool gameStarted;
        private bool speakTemp;

        private float ballSpeed = 4;
        private float ballSpeedMultiplier = 1f;

        private float playerMoveSpeed = 5f;
        private float cpuMoveSpeed = 5.8f;
        private int scorePlayer;
        private int scoreCPU;

        // States
        private BallDirectionLeftRight ballDirectionLeftRight = BallDirectionLeftRight.None;
        private BallDirectionTopDown ballDirectionTopDown = BallDirectionTopDown.None;

        // Enums
        private enum BallDirectionLeftRight
        {
            None = 0,
            Left,
            Right
        }
        private enum BallDirectionTopDown
        {
            None = 0,
            Top,
            Down
        }
        #endregion

        #region Plugin Methods
        public bool Load()
        {
            multiplikatorTimer = new GTA.Timer(5000, false);
            multiplikatorTimer.Tick += MultiplikatorTimer_Tick;
            rnd = new Random(Game.GameTime);
            pfc = new PrivateFontCollection();
            pfc.AddFontFile(Game.InstallFolder + "\\scripts\\VideoGamesFriendActivity\\bit5x3.ttf");
            _8BitFont_48 = new GTA.Font(pfc.Families[0].Name, 48f, FontScaling.Pixel);
            audio = new VGFA.Helper.API.Audio();
            wallSound = audio.CreateFile(DataDir + "\\WALL.mp3", false, true);
            paddleSound = audio.CreateFile(DataDir + "\\PADDLE.mp3", false, true);
            scoreSound = audio.CreateFile(DataDir + "\\SCORE.mp3", false, true);
            return true;
        }
        public bool Unload()
        {
            // Free
            multiplikatorTimer.Stop();
            multiplikatorTimer.Tick -= MultiplikatorTimer_Tick;
            multiplikatorTimer = null;
            audio.FreeStream(wallSound);
            audio.FreeStream(paddleSound);
            audio.FreeStream(scoreSound);
            rnd = null;
            audio = null;
            Bounds = RectangleF.Empty;
            Player = null;
            Player2 = null;
            Ball = null;
            gameStarted = false;
            speakTemp = false;
            ballDirectionLeftRight = BallDirectionLeftRight.None;
            ballDirectionTopDown = BallDirectionTopDown.None;
            scorePlayer = 0;
            scoreCPU = 0;
            playerMoveSpeed = 5f;
            cpuMoveSpeed = 5.8f;
            ballSpeed = 5;
            ballSpeedMultiplier = 0.5f;
            return true;
        }
        public bool Start(Ped _challenger)
        {
            challenger = _challenger;
            Bounds = new RectangleF(0, 0, Game.Resolution.Width, Game.Resolution.Height);
            Player = new Element(new RectangleF(40f, (Bounds.Height / 2f) - 50f, 15f, 100f), Color.White);
            Player2 = new Element(new RectangleF(Bounds.Width - 40f, (Bounds.Height / 2f) - 50f, 15f, 100f), Color.White);
            Ball = new Element(new RectangleF((Bounds.Width / 2f) - 8f, (Bounds.Height / 2f) - 8f, 16f, 16f), Color.White);
            return true;
        }
        #endregion

        #region Methods / Functions
        private void Reset(int addScorePlayer1, int addScorePlayer2, bool playSound)
        {
            if (addScorePlayer1 != 0) scorePlayer += addScorePlayer1;
            if (addScorePlayer2 != 0) scoreCPU += addScorePlayer2;

            if (playSound) audio.ChangeStreamPlayMode(VGFA.Helper.API.AudioPlayMode.Play, scoreSound);

            if (addScorePlayer1 != 0) { // Player
                switch (rnd.Next(1, 10)) {
                    case 5:
                        Game.LocalPlayer.Character.SayAmbientSpeech("VGAME_PLAYER_PLAYS_WELL");
                        if (challenger != null) {
                            Game.WaitInCurrentScript(2000);
                            challenger.SayAmbientSpeech("GENERIC_DEJECTED");
                        }
                        break;
                    case 2:
                        Game.LocalPlayer.Character.SayAmbientSpeech("VGAME_PLAYER_PLAYS_WELL");
                        if (challenger != null) {
                            Game.WaitInCurrentScript(2000);
                            challenger.SayAmbientSpeech("VGAME_PLAYER_PLAYS_WELL");
                        }
                        break;
                }
            }
            else if (addScorePlayer2 != 0) { // CPU / Player 2
                switch (rnd.Next(1, 10)) {
                    case 3:
                        if (challenger != null) challenger.SayAmbientSpeech("VGAME_HAPPY");
                        Game.WaitInCurrentScript(1000);
                        break;
                    case 5:
                        if (challenger != null) challenger.SayAmbientSpeech("VGAME_PLAYER_PLAYS_POORLY");
                        Game.WaitInCurrentScript(1000);
                        break;
                }
            }

            Game.WaitInCurrentScript(1000);

            multiplikatorTimer.Start();
            ballSpeedMultiplier = 0.5f;

            Player.eRectangle = new RectangleF(40f, (Bounds.Height / 2f) - 50f, 15f, 100f);
            Ball.eRectangle = new RectangleF((Bounds.Width / 2f) - 8f, (Bounds.Height / 2f) - 8f, 16f, 16f);
            Player2.eRectangle = new RectangleF(Bounds.Width - 40f, (Bounds.Height / 2f) - 50f, 15f, 100f);

            ballDirectionLeftRight = GetRandomLeftRight();
            ballDirectionTopDown = GetRandomTopDown();

            speakTemp = false;
        }
        private BallDirectionTopDown GetRandomTopDown()
        {
            int rndNumber = rnd.Next(0, 60);
            if (Between(rndNumber, 0, 20)) {
                return BallDirectionTopDown.Down;
            }
            else if (Between(rndNumber, 20, 40)) {
                return BallDirectionTopDown.Top;
            }
            return BallDirectionTopDown.None;
        }
        private BallDirectionLeftRight GetRandomLeftRight()
        {
            int rndNumber = rnd.Next(0, 60);
            if (Between(rndNumber, 0, 40)) {
                return BallDirectionLeftRight.Left;
            }
            else if (Between(rndNumber, 20, 40)) {
                return BallDirectionLeftRight.Right;
            }
            return BallDirectionLeftRight.Right;
        }
        private bool Between(int num, int lower, int upper, bool inclusive = false)
        {
            return inclusive
                ? lower <= num && num <= upper
                : lower < num && num < upper;
        }
        #endregion

        #region Timer
        private void MultiplikatorTimer_Tick(object sender, EventArgs e)
        {
            if (gameStarted) {
                ballSpeedMultiplier += 0.5f;
            }
        }
        #endregion

        public void Tick()
        {
            // Player 1
            RectangleF playerRect = RectangleF.Intersect(Bounds, Player.eRectangle);
            if (playerRect.Y <= 0) {
                canGoUp = false;
                keyUpPressed = false;
            }
            else if (playerRect.Bottom >= Game.Resolution.Height) {
                canGoDown = false;
                keyDownPressed = false;
            }
            else {
                canGoUp = true;
                canGoDown = true;
            }

            // Movement
            if (gameStarted) {
                if (keyUpPressed) {
                    Player.eRectangle.Y -= playerMoveSpeed;
                }
                else if (keyDownPressed) {
                    Player.eRectangle.Y += playerMoveSpeed;
                }
            }

            // Player 2 (CPU)
            RectangleF player2Rect = RectangleF.Intersect(Bounds, Player2.eRectangle);
            if (!(player2Rect.Y <= 0) || !(player2Rect.Bottom >= Game.Resolution.Height) ) {
                if (Ball.eRectangle.Y > Player2.eRectangle.Y) {
                    Player2.eRectangle.Y += cpuMoveSpeed;
                }
                else {
                    Player2.eRectangle.Y -= cpuMoveSpeed;
                }
            }

            // Ball
            RectangleF ballRect = RectangleF.Intersect(Bounds, Ball.eRectangle);
            if (ballRect.X <= 0) { // Left
                Reset(0, 1, true);
            }
            else if (ballRect.Right >= Game.Resolution.Width) { // Right
                Reset(1, 0, true);
            }
            else if (ballRect.Y <= 0) { // Wall top
                audio.ChangeStreamPlayMode(VGFA.Helper.API.AudioPlayMode.Play, wallSound);
                ballDirectionTopDown = BallDirectionTopDown.Down;
            }
            else if (ballRect.Bottom >= Game.Resolution.Height) { // Wall bottom
                audio.ChangeStreamPlayMode(VGFA.Helper.API.AudioPlayMode.Play, wallSound);
                ballDirectionTopDown = BallDirectionTopDown.Top;
            }
            else if (Player.eRectangle.IntersectsWith(Ball.eRectangle)) { // Paddle left
                audio.ChangeStreamPlayMode(VGFA.Helper.API.AudioPlayMode.Play, paddleSound);
                ballDirectionTopDown = GetRandomTopDown();
                ballDirectionLeftRight = BallDirectionLeftRight.Right;
            }
            else if (Player2.eRectangle.IntersectsWith(Ball.eRectangle)) { // Paddle right
                audio.ChangeStreamPlayMode(VGFA.Helper.API.AudioPlayMode.Play, paddleSound);
                ballDirectionTopDown = GetRandomTopDown();
                ballDirectionLeftRight = BallDirectionLeftRight.Left;
            }

            // Movement
            switch (ballDirectionLeftRight) {
                case BallDirectionLeftRight.Left:
                    Ball.eRectangle.X -= ballSpeed * ballSpeedMultiplier;
                    break;
                case BallDirectionLeftRight.Right:
                    Ball.eRectangle.X += ballSpeed * ballSpeedMultiplier;
                    break;
            }
            switch (ballDirectionTopDown) {
                case BallDirectionTopDown.Top:
                    Ball.eRectangle.Y -= ballSpeed * ballSpeedMultiplier;
                    break;
                case BallDirectionTopDown.Down:
                    Ball.eRectangle.Y += ballSpeed * ballSpeedMultiplier;
                    break;
            }

            // Random speech
            int rndSpeechNum = rnd.Next(1, 25);
            if (scoreCPU > scorePlayer) {
                switch (rndSpeechNum) {
                    case 11:
                        if (!speakTemp) {
                            if (challenger != null) challenger.SayAmbientSpeech("VGAME_HAPPY");
                            speakTemp = true;
                        }
                        break;
                }
            }
            else if (scoreCPU < scorePlayer) {
                switch (rndSpeechNum) {
                    case 20:
                        if (!speakTemp) {
                            if (challenger != null) challenger.SayAmbientSpeech("VGAME_UNHAPPY");
                            speakTemp = true;
                        }
                        break;
                }
            }
        }

        public void Draw(object sender, GraphicsEventArgs e)
        {
            // DEBUG
            //RectangleF rectangleF = RectangleF.Intersect(Bounds, Ball.eRectangle);
            //e.Graphics.DrawText(string.Format("X:{0} Y:{1} Width:{2} Height:{3} Left:{4} Right:{5} Top:{6} Bottom:{7}", rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height, rectangleF.Left, rectangleF.Right, rectangleF.Top, rectangleF.Bottom), 100f, 60f, Color.White);

            Size scorePlayerSize = TextRenderer.MeasureText(scorePlayer.ToString(), _8BitFont_48.WindowsFont);
            e.Graphics.DrawText(scorePlayer.ToString(), new RectangleF((Bounds.Width / 2f) - 150f, 150f, scorePlayerSize.Width + 50, scorePlayerSize.Height + 2), TextAlignment.Left, Color.White, _8BitFont_48);
            Size scoreCPUSize = TextRenderer.MeasureText(scoreCPU.ToString(), _8BitFont_48.WindowsFont);
            e.Graphics.DrawText(scoreCPU.ToString(), new RectangleF((Bounds.Width / 2f) + 150f, 150f, scoreCPUSize.Width + 50, scoreCPUSize.Height + 2), TextAlignment.Left, Color.White, _8BitFont_48);

            e.Graphics.DrawRectangle(Player.eRectangle, Player.eColor);
            if (gameStarted) e.Graphics.DrawRectangle(Ball.eRectangle, Ball.eColor);
            e.Graphics.DrawRectangle(Player2.eRectangle, Player2.eColor);

            if (!gameStarted) {
                Size startInfoTextSize = TextRenderer.MeasureText("Start by pressing SPACE", _8BitFont_48.WindowsFont);
                e.Graphics.DrawText("Start by pressing SPACE", new RectangleF((Bounds.Width / 2f) - (startInfoTextSize.Width - 200), Bounds.Height - 250f, startInfoTextSize.Width + 200, startInfoTextSize.Height + 2), TextAlignment.Left, Color.White, _8BitFont_48);
            }
        }

        public void KeyDown(object sender, GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.Back) {
                AGame.ExitGame();
            }
            else if (e.Key == Keys.Up) {
                if (canGoUp) keyUpPressed = true;
            }
            else if (e.Key == Keys.Down) {
                if (canGoDown) keyDownPressed = true;
            } 
            else if (e.Key == Keys.Space) {
                if (!gameStarted) {
                    gameStarted = true;
                    multiplikatorTimer.Start();
                    Reset(0, 0, false);
                }
            }
        }

        public void KeyUp(object sender, GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.Up) {
                keyUpPressed = false;
            }
            else if (e.Key == Keys.Down) {
                keyDownPressed = false;
            }
        }

    }
}
