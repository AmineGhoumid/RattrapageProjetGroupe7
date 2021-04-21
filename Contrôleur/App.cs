using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace easysave
{
    // The controler (MVC), main entry point of the software
    public class App
    {
        // Config
        public static string version = "3.0"; // App version
        public static string cryptExt = "ezcrypt";
        public static string cryptKey = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        public static string jobSoftwareName = "Calculator";
        public static string language = "EN";
        public static string[] prioFile = { "prio1", "prio2" };
        public static float limitTransfer = 5000.0f;

        // This section handles priority
        public static Object lockPrioFile = new Object();
        public static int poolPrioFile = 0;

        // This section handles limit transfer
        public static Object lockLimitTransfer = new Object();

        saveWorkHolder holder; // Contains all our saved works (MVC model)
        Window mainWindow; // Our window
        mainWindow contentWindow; // Our window content

        public App() // Constructor, start the menu with its constructor
        {
            loadConfig();

            holder = saveWorkHolder.getInstance(); // Singleton, must be initialized before menu, because this App is send to the menu and the menu will use the holder.
            contentWindow = new mainWindow(holder);
            mainWindow = new Window
            {
                Title = "EasySave v" + version,
                Content = contentWindow
            };
            mainWindow.Height = 570;
            mainWindow.Width = 720;
            mainWindow.MaxHeight = 570;
            mainWindow.MaxWidth = 720;
            mainWindow.MinHeight = 570;
            mainWindow.MinWidth = 720;
            mainWindow.ShowDialog();
        }

        private void loadConfig()
        {
            // Now we try to retrieve settings data (aes key, file encryption extension and job software
            if (File.Exists((AppDomain.CurrentDomain.BaseDirectory + "\\config.xml")))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
                    XmlNodeList nodes = doc.DocumentElement.SelectNodes("/root");

                    foreach (XmlNode node in nodes)
                    {
                        foreach (XmlNode settings in node)
                        {
                            foreach (XmlNode setting in settings)
                            {
                                if (setting.Name == "key") { App.cryptKey = setting.InnerText; }
                                else if (setting.Name == "crypting_extension") { App.cryptExt = setting.InnerText; }
                                else if (setting.Name == "job_software") { App.jobSoftwareName = setting.InnerText; }
                                else if (setting.Name == "language") { App.language = setting.InnerText; }
                                else if (setting.Name == "limit_transfer") { App.limitTransfer = Int32.Parse(setting.InnerText); }
                                else if (setting.Name == "prio_file_ext") { App.prioFile = setting.InnerText.Split(';'); }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing
                }
            }
            // Check if other instance of easysave is running
            Process[] pname = Process.GetProcessesByName("easysave");
            if (pname.Length != 1)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("An other instance of easysave is already running !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Une autre instance d'easysave est déjà en execution !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Application.Current.Shutdown();
                return;
            }
        }


    }
}
