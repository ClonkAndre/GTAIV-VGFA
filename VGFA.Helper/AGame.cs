using System;

namespace VGFA.Helper {
    public class AGame {

        /// <summary>
        /// This is only used for the main mod.
        /// <para>Please do not use this.</para>
        /// </summary>
        public static event EventHandler ExitGameCalled;

        public static void ExitGame()
        {
            ExitGameCalled?.Invoke(null, EventArgs.Empty);
        }

    }
}
