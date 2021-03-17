using System;
using System.Threading.Tasks;
using Un4seen.Bass;
using GTA;

namespace VGFA.Helper.API {

    #region Public Enums
    public enum AudioPlayMode
    {
        Play,
        Pause,
        Stop,
        None
    }
    #endregion

    public class Audio {

        public bool isHandleCurrentlyFadingOut { get; private set; }
        private SettingsFile settings;
        private int volume;

        #region Constructor
        /// <summary>
        /// Initializes a new audio object
        /// </summary>
        public Audio()
        {
            settings = SettingsFile.Open(Game.InstallFolder + "\\scripts\\VideoGamesFriendActivity.ini");
            if (settings != null) {
                volume = settings.GetValueInteger("General", "Volume", 20);
                if (volume > 100) {
                    volume = 100;
                }
                else if (volume < 0) {
                    volume = 0;
                }
            }
            else {
                volume = 20;
            }
        }
        #endregion

        /// <summary>
        /// Fades the given stream out.
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <param name="after">The new play mode</param>
        /// <param name="fadingSpeed">The fading speed (in milliseconds)</param>
        public async void FadeStreamOut(int stream, AudioPlayMode after, int fadingSpeed = 1000)
        {
            if (!isHandleCurrentlyFadingOut)
            {
                isHandleCurrentlyFadingOut = true;

                float handleVolume = 0f;
                Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, 0f, fadingSpeed);

                while (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref handleVolume);

                    if (handleVolume <= 0f)
                    {
                        switch (after)
                        {
                            case AudioPlayMode.Stop:
                                Bass.BASS_ChannelStop(stream);
                                isHandleCurrentlyFadingOut = false;
                                break;
                            case AudioPlayMode.Pause:
                                Bass.BASS_ChannelPause(stream);
                                isHandleCurrentlyFadingOut = false;
                                break;
                        }
                    }

                    await Task.Delay(5);
                }
            }
        }
        /// <summary>
        /// Fades the given stream in.
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <param name="fadeToVolumeLevel">The volume the stream should fade to</param>
        /// <param name="fadingSpeed">The fading speed (in milliseconds)</param>
        public void FadeStreamIn(int stream, float fadeToVolumeLevel, int fadingSpeed)
        {
            Bass.BASS_ChannelPlay(stream, false);
            Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, fadeToVolumeLevel / 100.0f, fadingSpeed);
        }
        /// <summary>
        /// Changes the play mode of the given stream.
        /// <para>Warning: If you stop the stream, or the stream ends, the stream will be automatically cleared and is empty.</para>
        /// </summary>
        /// <param name="newState">The new state of the stream.</param>
        /// <param name="stream">The target stream.</param>
        /// <returns>If successful, true is returned, else false is returned. Use GetErrorCode() to get the error code.</returns>
        public bool ChangeStreamPlayMode(AudioPlayMode newState, int stream)
        {
            if (stream != 0)
            {
                switch (newState)
                {
                    case AudioPlayMode.Play:
                        return Bass.BASS_ChannelPlay(stream, false);
                    case AudioPlayMode.Pause:
                        return Bass.BASS_ChannelPause(stream);
                    case AudioPlayMode.Stop:
                        return Bass.BASS_ChannelStop(stream);
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Creates a sample stream from an MP3, MP2, MP1, OGG, WAV, AIFF file.
        /// <para>Info: You should save this stream into a variable.</para>
        /// <para>If you stop the stream, or the stream ends, the stream will be automatically cleared and is empty.</para>
        /// </summary>
        /// <param name="file">Filename for which a stream should be created.</param>
        /// <param name="createWithZeroDecibels">This will create the stream with 0 decibels.</param>
        /// <param name="dontDestroyOnStreamEnd">This will keep the created stream in memory. Use FreeStream() to free the stream manually.</param>
        /// <returns>If successful, the new stream's handle is returned, else 0 is returned. Use GetErrorCode() to get the error code.</returns>
        public int CreateFile(string file, bool createWithZeroDecibels, bool dontDestroyOnStreamEnd = false)
        {
            if (!string.IsNullOrWhiteSpace(file))
            {
                if (createWithZeroDecibels)
                {
                    if (dontDestroyOnStreamEnd)
                    {
                        int handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
                        SetStreamVolume(handle, 0f);
                        return handle;
                    }
                    else
                    {
                        int handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE);
                        SetStreamVolume(handle, 0f);
                        return handle;
                    }
                }
                else
                {
                    if (dontDestroyOnStreamEnd)
                    {
                        int handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
                        SetStreamVolume(handle, volume);
                        return handle;
                    }
                    else
                    {
                        int handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE);
                        SetStreamVolume(handle, volume);
                        return handle;
                    }
                }
            }
            else
            {
                return 2;
            }
        }
        /// <summary>
        /// Changes the volume of a stream.
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <param name="volume">The new volume of the stream. Range: 0 - 100</param>
        /// <returns>If successful, true is returned, else false is returned. Use GetErrorCode() to get the error code.</returns>
        public bool SetStreamVolume(int stream, float volume)
        {
            if (stream != 0)
            {
                return Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, volume / 100.0F);
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Gets the current play mode of the given stream.
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <returns>Returns 'Play' if the stream is playing, 'Pause' if the stream is paused, 'Stop' if the stream is stopped and 'None' if there is no stream given.</returns>
        public AudioPlayMode GetStreamPlayMode(int stream)
        {
            if (stream != 0)
            {
                switch (Bass.BASS_ChannelIsActive(stream))
                {
                    case BASSActive.BASS_ACTIVE_PLAYING:
                        return AudioPlayMode.Play;
                    case BASSActive.BASS_ACTIVE_PAUSED:
                        return AudioPlayMode.Pause;
                    case BASSActive.BASS_ACTIVE_STOPPED:
                        return AudioPlayMode.Stop;
                    default:
                        return AudioPlayMode.None;
                }
            }
            else
            {
                return AudioPlayMode.None;
            }
        }
        /// <summary>
        /// Frees a sample stream's resources, including any sync/DSP/FX it has.
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <returns>If successful, true is returned, else false is returned. Use GetErrorCode() to get the error code.</returns>
        public bool FreeStream(int stream)
        {
            if (stream != 0)
            {
                Bass.BASS_ChannelStop(stream);
                return Bass.BASS_StreamFree(stream);
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Retrieves the error code for the most recent BASS function call in the current thread. 
        /// </summary>
        /// <returns>If no error occured during the last BASS function call then 0 is returned, else one of the BASSError values is returned. See http://bass.radio42.com/help/html/78effdb0-70b5-1602-a234-b0847b4e6d6c.htm for all error codes.</returns>
        public int GetErrorCode()
        {
            return (int)Bass.BASS_ErrorGetCode();
        }

    }
}
