using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using AxWMPLib;
using Resto.Front.Api.CustomerScreen.Settings;
using WMPLib;
using System.Windows.Forms;

namespace Resto.Front.Api.CustomerScreen.View.Controls
{
    /// <summary>
    /// Interaction logic for CustomerMediaControl.xaml
    /// </summary>
    public partial class CustomerMediaControl
    {
        public CustomerMediaControl()
        {
            InitializeComponent();
        }

        public void StartPlayer()
        {
            UpdateFilesAndPlayNext();
        }

        public void RefreshPlayer()
        {
            // TODO: implement a method that "refresh" the mediacontrol so that large files can be played
            // but it's better to eliminate the reason why the video in such files does not work (the progress itself and the sound go)
        }



        private readonly List<string> files = new List<string>();
        private readonly List<string> unsupportedFiles = new List<string>();
        AxWindowsMediaPlayer mediaPlayer;
        private bool firstSearchLogged;

        #region WindowsMediaPlayer

        private void UpdateFilesAndPlayNext()
        {
            var newFiles = new List<string>();
            try
            {
                var path = Path.GetFullPath(CustomerScreenConfig.Instance.PathToPlaylistFolder);
                var extensions = CustomerScreenConfig.Instance.SupportedExtensions.ToArray();
                //Log once the list of files we are looking for.
                if (!firstSearchLogged)
                {
                    PluginContext.Log.InfoFormat("Path to video folder:'{0}'\n Extensions: '{1}'", path, extensions.Length > 0 ? string.Join("', '", extensions) : "all");
                    firstSearchLogged = true;
                }
                if (Directory.Exists(path))
                    newFiles.AddRange(extensions.Length == 0
                        ? Directory.GetFiles(path).Except(unsupportedFiles)
                        : extensions.SelectMany(f => Directory.GetFiles(path, f, SearchOption.TopDirectoryOnly)).Except(unsupportedFiles));
                else
                    PluginContext.Log.WarnFormat("Directory {0} doesn't exist.", path);
            }
            catch (Exception e)
            {
                PluginContext.Log.Error(string.Format("Can't load files from {0}.", CustomerScreenConfig.Instance.PathToPlaylistFolder), e);
            }

            if (newFiles.Count == 0)
            {
                wmpHost.Visibility = Visibility.Collapsed;
                FilesNotFoundErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (mediaPlayer == null)
            {
                mediaPlayer = new AxWindowsMediaPlayer();
                mediaPlayer.PlayStateChange += mediaPlayer_PlayStateChange;
                mediaPlayer.ErrorEvent += mediaPlayer_ErrorEvent;
                wmpHost.Child = mediaPlayer;
                mediaPlayer.settings.setMode("Loop", true);
                mediaPlayer.settings.enableErrorDialogs = true;
                mediaPlayer.settings.autoStart = true;
                mediaPlayer.settings.mute = true;
                mediaPlayer.settings.volume = 0;
                mediaPlayer.enableContextMenu = false;
                mediaPlayer.stretchToFit = true;
                mediaPlayer.uiMode = "none";
            }
            try
            {
                if (files.SequenceEqual(newFiles))
                    return;
                files.Clear();
                files.AddRange(newFiles);
                PluginContext.Log.InfoFormat("New files list: \n'{0}'", string.Join("',\n'", files.ToArray()));
                mediaPlayer.currentPlaylist.clear();
                files.ForEach(f => mediaPlayer.currentPlaylist.appendItem(mediaPlayer.newMedia(f)));
            }
            catch (AxHost.InvalidActiveXStateException ex)
            {
                PluginContext.Log.ErrorFormat("Error {0}", ex.Message);
                wmpHost.Visibility = Visibility.Collapsed;
                FilesNotFoundErrorMessage.Visibility = Visibility.Visible;
            }
        }

        void mediaPlayer_PlayStateChange(object sender, _WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (mediaPlayer.playState == WMPPlayState.wmppsMediaEnded)
            {
                UpdateFilesAndPlayNext();
            }
            if (mediaPlayer.playState == WMPPlayState.wmppsReady)
            {
                mediaPlayer.Ctlcontrols.play();
            }
        }

        void mediaPlayer_ErrorEvent(object sender, EventArgs e)
        {
            MediaErrorHandler();
        }

        void MediaErrorHandler()
        {
            //If we failed to play the video, try to play the next one
            unsupportedFiles.Add(mediaPlayer.currentMedia.sourceURL);
            PluginContext.Log.WarnFormat("File {0} is unsupported. System won't try to play this file until Syrve POS be restarted", mediaPlayer.currentMedia.sourceURL);
            UpdateFilesAndPlayNext();
        }
        #endregion
    }
}
