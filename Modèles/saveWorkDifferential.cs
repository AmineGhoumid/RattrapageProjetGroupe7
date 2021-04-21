using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// the role of this class is to implement the differential backup system.
/// i,e. a basic full backup and then the next backups only save the files different from the first one, thus saving space compared to the full backup system.
/// </summary>
namespace easysave
{
    public class saveWorkDifferential : saveWork
    {
        public saveWorkDifferential(string appellation_, string srcDir_, string destDir_)
        {
            appellation = appellation_; //Name of the save
            sourceDir = srcDir_; //Source directory path
            destDir = destDir_; //Destination directory path
            mode = 1; // Differential save (1)
            statusPercentage = 0; //Percentage of completion of the backup.
            tempsEcoule = -1; //Elapsed time 
            cryptTime = 0;
            fileBuffer = new string[5000]; // For now 5000 non prio files max to save, will be changed later
            fileDestBuffer = new string[5000];
        }

        /// <summary>
        /// Returns the path of the previous save
        /// </summary>
        /// <param name="dest">the target directory</param>
        /// <param name="appelation">the name of the saveWork</param>
        /// <returns>The prevous save</returns>
        public string getCompaDir(string dest, string appelation)
        {
            try
            {
                foreach (string path in Directory.GetDirectories(dest))
                {
                    if (path.Contains("last") && path.Contains(appelation))
                    {
                        while (isPaused) { }
                        Directory.Move(path, path.Replace("-last", ""));
                        return path.Replace("-last", "");
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error, compa directory not created ! Creating it ...", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            return dest;
        }

        public override async void save()
        {
            this.stop = false; // Ensure that this check variable is false
            // Check if job software is running
            Process[] pname = Process.GetProcessesByName(App.jobSoftwareName);
            if (pname.Length != 0) {
                ((Button)progressDisplay).IsEnabled = true;
                ((Button)pauseDisplay).IsEnabled = false;
                ((Button)pauseDisplay).Content = "Pause";
                ((Button)stopDisplay).IsEnabled = false;
                MessageBox.Show("Error, the job software has been detected, aborting this save (" + appellation + ").", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            this.horodatage = DateTime.Now.ToString();
            
                await Task.Run(() =>
                {
                    Thread saveThread = new Thread(new ThreadStart(saveDiff));
                    saveThread.Start();
                    saveThread.Join();
                });

        }


        public void saveDiff()
        {
            string dest = destDir;
            string src = sourceDir;

            //Check if there is last tag in folder
            string compa_dir = getCompaDir(dest, appellation);

            string trueDir = dest + appellation + date() + "-last\\";

            Stopwatch timer = new Stopwatch();
            timer.Start();

            //Get all files from the source folder
            string[] eachFile = Directory.Exists(src) ? allFromDir(src) : new string[0];
            if (eachFile == null || eachFile.Length == 0)
            {
                MessageBox.Show("Dir not found");
                return; //Directory not found error code
            }

            string[] compa = Directory.Exists(compa_dir) && compa_dir != dest ? allFromDir(compa_dir) : new string[0];
            if (compa == null || compa.Length == 0)
            {
                compa_dir = trueDir;
                createDir(compa_dir);
                createAllDir(src, compa_dir + @"\");
                copyFiles(src, compa_dir + @"\");
            }

            //Remove the files that aren't anymore in the savework from compa

            // Then recreate last directory
            createDir(trueDir);
            createAllDir(src, trueDir);

            string output = ""; // Used to summarize everything in one messagebox
            output += "--- Detected modified file : \n";
            bool difference = false;

            // get usefull information about the transfer
            int howManyFileTotal = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length;
            int lookedFile = 0;


            // Now we really do the compare process with log support
            Task.Run(() =>
            {
                for (int i = 0; i < eachFile.Length; i++)
                {
                    if (stop) { Thread.CurrentThread.Abort(); }
                    while (isPaused) { }
                    if (toCopy(eachFile[i], dest, appellation, lookedFile, howManyFileTotal))
                    {
                            Stopwatch encryption_timer = new Stopwatch();
                            encryption_timer.Start();
                            bool hasEncryption = false;
                            if (eachFile[i].Contains(App.cryptExt) == false) // If no encryption
                            {
                                File.Copy(eachFile[i], trueDir + scrapPath(eachFile[i], src));   
                            }
                            else // If encryption
                            {
                                hasEncryption = true;
                                var proc = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cryptosoft\\cryptosoft.exe",
                                        Arguments = "0 " + App.cryptKey + " " + "\"" + eachFile[i] + "\" " + "\"" + trueDir + scrapPath(eachFile[i], src) + "\" " + "0",
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
                                catch { MessageBox.Show("Error, cryptosoft binaries are missing ! (file " + eachFile[i] + ")", "", MessageBoxButton.OK, MessageBoxImage.Error); }
                                finally { encryption_timer.Stop(); }
                            }
                            if (hasEncryption) { cryptTime = TimeSpan.FromMilliseconds(encryption_timer.ElapsedMilliseconds).TotalMilliseconds; }
                            output += scrapPath(eachFile[i], src) + "\n";
                            string horodatage = DateTime.Now.ToString();
                            string[] columnname = { "horodatage", "appellation", "state", "number_of_file_to_save", "remains_file", "progress", "source", "target" };
                            string[] data = { horodatage, appellation, "active", howManyFileTotal.ToString(), (howManyFileTotal - lookedFile).ToString(), statusPercentage.ToString(), sourceDir, destDir };
                            log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml", appellation, 8, columnname, data);
                            difference = true; // We are able to know that there at least one change, allows us to create the differential save

                    }
                    lookedFile++;
                }
                // Creation of XML file in order to save the logs of some informations
                timer.Stop(); // Stop the timer
                if (!difference) { output += "No difference ! \n"; }
                tempsEcoule = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds).TotalMilliseconds; //Elasped time
                string[] columname = { "appellation", "source_directory", "destination_file", "mode", "Time_taken_ms", "Timestamp", "encryption_time" };
                string[] dataforcolumn = { appellation, sourceDir, destDir, mode.ToString(), tempsEcoule.ToString(), this.horodatage, cryptTime.ToString() };
                log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\saved_work_history.xml", appellation + "_diffsave", 7, columname, dataforcolumn);

                string horodatage_end = DateTime.Now.ToString();
                string[] columnname_end = { "horodatage", "appellation", "state", "mode", "number_of_file_to_save", "remains_file", "progress", "source", "target" };
                string[] data_end = { horodatage_end, appellation, "done", this.mode.ToString(), howManyFileTotal.ToString(), "0", "100", sourceDir, destDir };
                log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml", appellation, 9, columnname_end, data_end);

                ((Button)progressDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,new Action(delegate ()
                 {
                     ((Button)progressDisplay).IsEnabled = true;
                     ((Button)progressDisplay).Content = "Execute";
                 }));

                ((Button)stopDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ((Button)stopDisplay).IsEnabled = false;
                }));

                ((Button)pauseDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ((Button)pauseDisplay).IsEnabled = false;
                    ((Button)pauseDisplay).Content = "Pause";
                }));

                MessageBox.Show("Save " + appellation + " executed ! - Report : \n" + output, "", MessageBoxButton.OK, MessageBoxImage.Information);
                //reset
                this.statusPercentage = 0;
                this.fileBufferID = 0;
            });
        }

        private static DateTime RoundToSecond(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day,
                                dt.Hour, dt.Minute, dt.Second);
        }


        private bool toCopy(string file, string dest, string appelation, int savedFile, int howManyFileTotal)
        {
            this.statusPercentage = (float)((float)savedFile / (float)howManyFileTotal)*100.0f;
            ((Button)progressDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                   new Action(delegate ()
                                   {
                                       ((Button)progressDisplay).Content = statusPercentage + " %";
                                   }));

            bool wasSeen = false;
            string[] allVersions = anteriorVersion(dest, appelation);
            DateTime newer = DateTime.MinValue;

            foreach (string vers in allVersions)
            {
                if (vers.Contains(Path.GetFileName(file)))
                {
                    wasSeen = true;
                    if (RoundToSecond(newer) < RoundToSecond(File.GetLastWriteTime(vers)))
                    {
                        newer = File.GetLastWriteTime(vers);
                    }
                }
            }

            if ((wasSeen && File.GetLastWriteTime(file) > newer) || !wasSeen)
            {
                return true;
            }

            return false;
        }

        private string[] anteriorVersion(string dest, string appelation)
        {
            List<string> allFiles = new List<string>();
            foreach (string path in allFromDir(dest))
            {
                if (path.Contains(appelation))
                {
                    allFiles.Add(path);
                }
            }
            return allFiles.ToArray();
        }

        /// <summary>
        /// Returns all files in dir and it's subdir
        /// </summary>
        /// <param name="dir">The path you want to scan</param>
        /// <returns>string array[] containing all files in dir and it's subdir</returns>
        private string[] allFromDir(string dir)
        {
            return Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Read the file and associte the line and it's index into a dataStruct and return dataStruct array[]
        /// </summary>
        /// <param name="src">string The file you wanna scan</param>
        /// <returns></returns>
        private dataStruct[] readfile(string src)
        {
            int i = 0;
            string line;
            List<dataStruct> list = new List<dataStruct>();
            StreamReader file = new StreamReader(src);

            while ((line = file.ReadLine()) != null)
            {
                list.Add(new dataStruct(i, line, ""));
                i++;
            }
            file.Close();
            return list.ToArray();
        }
        /// <summary>
        /// Removes the useless part of the path and returns the actual cleansed path
        /// </summary>
        /// <param name="str">Path to cleanse</param>
        /// <param name="toScrap">Part to remove from the path</param>
        /// <returns>Cleansed from toscrap path path</returns>
        private string scrapPath(string str, string toScrap)
        {
            int n = toScrap.Length;
            str = str.Remove(0, n);
            return str;
        }

   

        /// <summary>
        /// Create the specified Dir
        /// </summary>
        /// <param name="dir">Dir to create</param>
        /// <returns>Boolean False if the folder already exists</returns>
        private bool createDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create all the subdirs from the specified dir into the specified dir
        /// </summary>
        /// <param name="SourcePath">The path to reacreate</param>
        /// <param name="DestinationPath">The path where to reacreate the sourcepath</param>
        private void createAllDir(string SourcePath, string DestinationPath)
        {
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",SearchOption.AllDirectories))
            {
                while (isPaused) { }
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));
            }
        }

        /// <summary>
        /// Copy all the files from the specified dir and subdirs to the specified destinationPath (Complementary of createAllDir method)
        /// </summary>
        /// <param name="SourcePath">The path to copy files from</param>
        /// <param name="DestinationPath">The path where will be copied the files</param>
        private void copyFiles(string SourcePath, string DestinationPath)
        {
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",SearchOption.AllDirectories))
            {
                if (stop) { Thread.CurrentThread.Abort(); }
                while (isPaused) { }
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
            }
               
        }

        /// <summary>
        /// Returns the dateTime in format hh-mm-ss_yy-MM-dd
        /// </summary>
        /// <returns>string dateTime in format hh-mm-ss_yy-MM-dd</returns>
        private string date()
        {
            return DateTime.Now.ToString("hh-mm-ss") + "_" + DateTime.UtcNow.ToString("yy-MM-dd");
            //return DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
        }
    }

    struct dataStruct
    {
        public int index { get; set; }
        public string oldItem { get; set; }
        public string newItem { get; set; }

        public dataStruct(int index, string oldItem, string newItem)
        {
            this.index = index;
            this.oldItem = oldItem;
            this.newItem = newItem;
        }
    }

    struct fileData
    {
        public string path { get; set; }
        public dataStruct[] modifs { get; set; }

        public fileData(string path, dataStruct[] modifs)
        {
            this.path = path;
            this.modifs = modifs;
        }

        public dataStruct nearestIndex()
        {
            int index = 500000000;
            dataStruct nearest = new dataStruct();
            foreach (dataStruct ds in modifs)
            {
                if (ds.index < index)
                {
                    index = ds.index;
                    nearest = ds;
                }
            }

            return nearest;
        }
    }
}
