using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace easysave
{
    // This class is used to contains data about a single full save work, must be registered in a save work holder.
    public class saveWorkFull : saveWork
    {
        // The constructor is used to initialize basic mother-class variables.
        public saveWorkFull(string appellation_, string srcDir_, string destDir_)
        {
            appellation = appellation_;
            sourceDir = srcDir_;
            destDir = destDir_;
            statusPercentage = 0;
            tempsEcoule = -1;
            mode = 0;
            cryptTime = 0;
            fileBuffer = new string[5000]; // For now 5000 non prio files max to save, will be changed later
            fileDestBuffer = new string[5000];
        }

        // The save function, specific to the save type
        public override async void save()
        {
            this.stop = false; // Ensure that this check variable si false
            // Check if job software is running
            Process[] pname = Process.GetProcessesByName(App.jobSoftwareName);
            if (pname.Length != 0)
            {
                ((Button)progressDisplay).IsEnabled = true;
                ((Button)pauseDisplay).IsEnabled = false;
                ((Button)pauseDisplay).Content = "Pause";
                ((Button)stopDisplay).IsEnabled = false;
                MessageBox.Show("Error, the job software has been detected, aborting this save (" + appellation + ").", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await Task.Run(() =>
            {
                Thread saveThread = new Thread(new ThreadStart(saveFull));
                saveThread.Start();
                saveThread.Join();
            });
            ((Button)progressDisplay).IsEnabled = true;
            ((Button)progressDisplay).Content = "Execute";
            ((Button)pauseDisplay).IsEnabled = false;
            ((Button)pauseDisplay).Content = "Pause";
            ((Button)stopDisplay).IsEnabled = false;
            // reset
            this.statusPercentage = 0;
            this.fileBufferID = 0;
            MessageBox.Show(appellation + " saved !", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void saveFull()
        {
            this.horodatage = DateTime.Now.ToString();
            Stopwatch timer = new Stopwatch();
            timer.Start(); // Used to get the elipsed time save
            bool moving = true;
            int how_many_file_copy = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length;
            int file_copied = 0;

            if (Directory.Exists(destDir) == false) { Directory.CreateDirectory(destDir); }

            Task.Run(() =>
            {
                //First add how many file that are prioritary to the global pool
                int howmany = 0;
                foreach (string extension in App.prioFile)
                {
                    string[] files = Directory.GetFiles(sourceDir, "*." + extension, SearchOption.AllDirectories);
                    howmany += files.Length;
                }
                lock (App.lockPrioFile)
                {
                    App.poolPrioFile += howmany;
                }
                nbOfNonPrioFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length - howmany;
                // Secondly detect all files in this folder that must be encrypted and encrypt it
                string[] files_to_encrypt = Directory.GetFiles(sourceDir, "*." + App.cryptExt, SearchOption.AllDirectories);
                Stopwatch encryption_timer = new Stopwatch();
                encryption_timer.Start();
                bool hasEncryption = false;
                foreach (string encfile in files_to_encrypt)
                {
                    hasEncryption = true;
                    // Start cryptosoft
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cryptosoft\\cryptosoft.exe",
                            Arguments = "0 " + App.cryptKey + " " + "\"" + encfile + "\" " + "\"" + encfile + "\" " + "0",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    try
                    {
                        proc.Start();
                        while (!proc.HasExited) { } // Wait till the process has exited to ensure that the file will be encrypted BEFORE saved
                    }
                    catch { MessageBox.Show("Error, cryptosoft binaries are missing ! (file " + encfile + ")", "", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                encryption_timer.Stop();
                if (hasEncryption) { cryptTime = TimeSpan.FromMilliseconds(encryption_timer.ElapsedMilliseconds).TotalMilliseconds; }

                MoveDirectory(sourceDir, destDir); // This function will save all prio files and add non prio to buffer
                SaveNonPrioFiles(); // This function will save only non prio file when there is no other prio file to save somewhere

                moving = false;

                // Then decrypt all encrypted files in the source folder
                string[] files_to_decrypt = Directory.GetFiles(sourceDir, "*." + App.cryptExt, SearchOption.AllDirectories);

                foreach (string encfile in files_to_decrypt)
                {
                    // Start cryptosoft
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cryptosoft\\cryptosoft.exe",
                            Arguments = "0 " + App.cryptKey + " " + "\"" + encfile + "\" " + "\"" + encfile + "\" " + "1",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    try
                    {
                        proc.Start();
                    }
                    catch { MessageBox.Show("Error, cryptosoft binaries are missing ! (file " + encfile + ")", "", MessageBoxButton.OK, MessageBoxImage.Error); cryptTime = -1; }
                }
            });

            int turn = 0; // Used to keep the log system from writing every millisecond in the file

            while (moving)
            {
                if (stop) { Thread.CurrentThread.Abort(); }
                while (isPaused) { }
                turn++;
                if (turn >= 15)
                {
                    file_copied = Directory.GetFiles(destDir, "*", SearchOption.AllDirectories).Length;
                    this.statusPercentage = (float)((float)file_copied / (float)how_many_file_copy) * 100.0f;
                    ((Button)progressDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                               new Action(delegate ()
                               {
                                   ((Button)progressDisplay).Content = statusPercentage + " %";
                               }));
                    // Log the new save work
                    string horodatage = DateTime.Now.ToString();
                    string[] columnname = { "horodatage", "appellation", "state", "mode", "number_of_file_to_save", "progress", "source", "target" };
                    string[] data = { horodatage, appellation, "active", this.mode.ToString(), Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length.ToString(), this.statusPercentage.ToString(), sourceDir, destDir };
                    log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml", appellation, 8, columnname, data);
                    turn = 0;
                }

            }

            // Stop the timer
            timer.Stop();
            this.tempsEcoule = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds).TotalMilliseconds;

            // Writing in the saved work daily log
            string[] columname = { "appellation", "source_directory", "destination_file", "mode", "Time_taken_ms", "Timestamp", "final_directory_size", "encryption_time" };
            string[] dataforcolumn = { this.appellation, this.sourceDir, this.destDir, this.mode.ToString(), this.tempsEcoule.ToString(), this.horodatage, new System.IO.DirectoryInfo(destDir).GetFiles("*", SearchOption.AllDirectories).Length.ToString(), cryptTime.ToString() };
            log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\saved_work_history.xml", this.appellation + "_fullsave", 8, columname, dataforcolumn);

            //Writing in the realtime log
            string horodatage_end = DateTime.Now.ToString();
            string[] columnname_end = { "horodatage", "appellation", "state", "mode", "number_of_file_to_save", "progress", "source", "target" };
            string[] data_end = { horodatage_end, appellation, "done", this.mode.ToString(), Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length.ToString(), "100", sourceDir, destDir };
            log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml", appellation, 8, columnname_end, data_end);
        }

        private void SaveNonPrioFiles()
        {
            bool end = false;
            int i = 0;
            while (!end)
            {
                if (stop) { Thread.CurrentThread.Abort(); }
                while (isPaused) { }
                if (App.poolPrioFile == 0)
                {
                    try // there may be no file to save
                    {
                        // Check if the file is larger than the limit
                        long length = new System.IO.FileInfo(fileBuffer[i]).Length;
                        if ((length) * 1000.0f <= App.limitTransfer) // If it is not just move it like all non prioritary file
                        {
                            File.Copy(fileBuffer[i], fileDestBuffer[i], true);
                        }
                        //Otherwise, wait till there is no other big file on other saves that are already saving

                        lock (App.lockLimitTransfer)
                        {
                            File.Copy(fileBuffer[i], fileDestBuffer[i], true);
                        }
                        i++;
                    }
                    catch { // if no file to save
                        end = true;
                    }
                }

                if (i == nbOfNonPrioFiles) { end = true; }

            }
        }

        private void MoveDirectory(string source, string target)
        {
            DirectoryInfo sourceInfo = Directory.CreateDirectory(source);
            DirectoryInfo targetInfo = Directory.CreateDirectory(target);

            if (sourceInfo.FullName == targetInfo.FullName)
            {
                throw new System.IO.IOException("Source and target directories are the same.");
            }

            Stack<DirectoryInfo> sourceDirectories = new Stack<DirectoryInfo>();
            sourceDirectories.Push(sourceInfo);

            Stack<DirectoryInfo> targetDirectories = new Stack<DirectoryInfo>();
            targetDirectories.Push(targetInfo);

            while (sourceDirectories.Count > 0)
            {
                if (stop) { Thread.CurrentThread.Abort(); }
                if (!isPaused)
                {
                    DirectoryInfo sourceDirectory = sourceDirectories.Pop();
                    DirectoryInfo targetDirectory = targetDirectories.Pop();

                    foreach (FileInfo file in sourceDirectory.GetFiles())
                    {
                        if (stop) { Thread.CurrentThread.Abort(); }
                        while (isPaused) { } // Block until it is not paused

                        // Check if file is prio
                        bool prio = false;
                        foreach (string ext in App.prioFile)
                        {
                            if (file.Name.Contains(ext)) { prio = true; }
                        }

                        if (prio)
                        {
                            // Check if the file is larger than the limit
                            if ((file.Length) * 1000.0f <= App.limitTransfer) // If it is not just move it like all prioritary file
                            {
                                file.CopyTo(Path.Combine(targetDirectory.FullName, file.Name), overwrite: true);
                                lock (App.lockPrioFile)
                                {
                                    App.poolPrioFile--;
                                }
                            }
                            else // Wait till there is no other save saving a big file to save our big file
                            {

                                lock (App.lockLimitTransfer)
                                {
                                    file.CopyTo(Path.Combine(targetDirectory.FullName, file.Name), overwrite: true);
                                    lock (App.lockPrioFile)
                                    {
                                        App.poolPrioFile--;
                                    }
                                }

                            }
                        }
                        else // If file non prio then add it to buffer to save it later
                        {
                            fileBuffer[fileBufferID] = file.FullName;
                            fileDestBuffer[fileBufferID] = Path.Combine(targetDirectory.FullName, file.Name);
                            fileBufferID++;
                        }
                    }

                    foreach (DirectoryInfo subDirectory in sourceDirectory.GetDirectories())
                    {
                        if (stop) { Thread.CurrentThread.Abort(); }
                        while (isPaused) { }
                        sourceDirectories.Push(subDirectory);
                        targetDirectories.Push(targetDirectory.CreateSubdirectory(subDirectory.Name));

                    }
                }
            }

        }

    }

}
