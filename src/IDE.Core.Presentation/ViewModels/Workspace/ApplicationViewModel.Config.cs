﻿namespace IDE.Core.ViewModels
{
    using Core.Settings;
    using Core.Utilities;
    using IDE.Core.Interfaces;
    using IDE.Core.MRU;
    using System;
    using System.IO;
    using System.Linq;

    public partial class ApplicationViewModel
    {
        /// <summary>
        /// Save application settings when the application is being closed down
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                _appCoreModel.CreateAppDataFolder();

                //we want to ensure we have the options saved on close for now; consider removing this call in the future
                _settingsManager.SaveOptions(_appCoreModel.DirFileAppSettingsData);

                //save to profile settings from recent files model
                var profile = (Profile)_settingsManager.SessionData;
                var mruModel = (RecentFilesModel)_recentFiles;

                profile.MruList = mruModel.MruList.Select(m => new MruItem { IsPinned = m.IsPinned, FilePath = m.PathFileName }).ToList();

                _settingsManager.SaveProfileData(_appCoreModel.DirFileAppSessionData);
            }
            catch (Exception exp)
            {
                MessageDialog.Show(exp.Message);
            }
        }

        public void SaveXmlLayout(string xmlLayout)
        {
            if (SolutionManager.IsSolutionOpen())
            {
                var slnFolder = Path.GetDirectoryName(SolutionManager.SolutionFilePath);
                if (!Directory.Exists(slnFolder))
                    return;

                var ideFolder = Path.Combine(slnFolder, AppHelpers.SolutionConfigFolderName);

                //ensure folder exists
                Directory.CreateDirectory(ideFolder);

                var layoutFilePath = Path.Combine(ideFolder, AppHelpers.LayoutFileName);

                File.WriteAllText(layoutFilePath, xmlLayout);
            }
        }

        private void LoadXmlLayout()
        {
            if (SolutionManager.IsSolutionOpen())
            {
                var slnFolder = Path.GetDirectoryName(SolutionManager.SolutionFilePath);
                if (!Directory.Exists(slnFolder))
                    return;

                var ideFolder = Path.Combine(slnFolder, AppHelpers.SolutionConfigFolderName);

                var layoutFilePath = Path.Combine(ideFolder, AppHelpers.LayoutFileName);

                if (!File.Exists(layoutFilePath))
                    return;

                var xmlLayout = File.ReadAllText(layoutFilePath);

                OnLoadLayoutRequested(xmlLayout);
            }
        }

        private void OnLoadLayoutRequested(string xmlLayout)
        {
            LoadLayoutRequested?.Invoke(this, xmlLayout);
        }

        /// <summary>
        /// Load configuration from persistence on startup of application
        /// </summary>
        public void LoadConfig()
        {
            // Re/Load program options and user profile session data to control global behaviour of program
            _settingsManager.LoadOptions(_appCoreModel.DirFileAppSettingsData);
            _settingsManager.LoadProfileData(_appCoreModel.DirFileAppSessionData);

            _themesManager.SetSelectedTheme(SettingsManager.DefaultTheme);
            ResetTheme();
        }

        /// <summary>
        /// Save session data on closing
        /// </summary>
        public void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (CanCloseAndSaved() == true)      // Close all open files and check whether application is ready to close
                {
                    OnRequestClose();                          // (other than exception and error handling)

                    e.Cancel = false;
                }
                else
                    e.Cancel = ShutDownInProgress_Cancel = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Execute closing function and persist session data to be reloaded on next restart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnClosed()
        {
            try
            {
                // Save/initialize program options that determine global program behaviour
                SaveConfig();

                DisposeResources();
            }
            catch (Exception exp)
            {
                MessageDialog.Show(exp.Message);
            }
        }

        /// <summary>
        /// Disposes all reserved resources when the application is in its last phase of shuttng down.
        /// </summary>
        private void DisposeResources()
        {
            try
            {
                foreach (var item in Files)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}
