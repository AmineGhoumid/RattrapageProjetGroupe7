using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace easysave
{
    // This class is used to contain and handle all the active / inactive save works, should be initialize in the controller
    // Singleton design pattern
    public class saveWorkHolder
    {
        private saveWork[] holder; // The array that contains all of our save works
        private int nbOfWork; //The actual number of work
        private static saveWorkHolder swHolder = null;

        private saveWorkHolder() // Initialize all variables here, implementation of singleton design pattern
        {
            holder = new saveWork[5000];
            nbOfWork = 0;
        }
        public static saveWorkHolder getInstance() // Get the only possible instance
        {
            if(swHolder == null) { swHolder = new saveWorkHolder(); return swHolder; }
            return null;
        }
        public void deleteLastWork() // Delete the last saved save work
        {
            if (nbOfWork > 0)
            {
                string appellation = holder[nbOfWork - 1].appellation;
                MessageBox.Show(appellation + " deleted !", "", MessageBoxButton.OK, MessageBoxImage.Information);
                holder[nbOfWork-1] = null;
                nbOfWork--;
                log.deleteXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml", appellation);
            }
            else
            {
                if(App.language == "EN")
                {
                    MessageBox.Show("Error, no save work to delete !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Erreur, pas de sauvegarde a supprimer !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
        }
        public int addSaveWork(string appellation,string source, string dest, string mode, Object progressObject, Object pauseDisplay, Object stopDisplay,bool shouldLog=true) // Add a save work, interactive function (with the view Console... )
        {
            appellation = appellation.Replace(" ", "_");
            // Check if already registered save work has the same name
            foreach (saveWork sw in holder) { if (sw != null) { if (sw.appellation == appellation) { if (App.language == "EN") { MessageBox.Show("Error, save work already registered w/ the same name !", "", MessageBoxButton.OK, MessageBoxImage.Error); } else { MessageBox.Show("Erreur, un même nom de sauvegarde existe déjà !", "", MessageBoxButton.OK, MessageBoxImage.Error); } return -1; } } }
            if (!Directory.Exists(source)) { if (App.language == "EN") { MessageBox.Show("Error, the source directory does not exist !", "", MessageBoxButton.OK, MessageBoxImage.Error); } else { MessageBox.Show("Ereur, le répertoire source n'existe pas !", "", MessageBoxButton.OK, MessageBoxImage.Error); } return -1; }

            // Now create the save in the holder

            if (mode == "0") // Create new Full save work
            {
                if (source[source.Length - 1] != '\\' || dest[dest.Length - 1] != '\\') { MessageBox.Show("Error, source or target path is not UNC !", "", MessageBoxButton.OK, MessageBoxImage.Error); return -1; }
                holder[nbOfWork] = new saveWorkFull(appellation, source, dest);
                holder[nbOfWork].progressDisplay = progressObject;
                holder[nbOfWork].pauseDisplay = pauseDisplay;
                holder[nbOfWork].stopDisplay = stopDisplay;
                nbOfWork++;
            }
            else if (mode == "1")
            {
                if(source[source.Length-1] != '\\' || dest[dest.Length-1] != '\\') { MessageBox.Show("Error, source or target path is not UNC !", "", MessageBoxButton.OK, MessageBoxImage.Error); return -1; }
                holder[nbOfWork] = new saveWorkDifferential(appellation, source, dest);
                holder[nbOfWork].progressDisplay = progressObject;
                holder[nbOfWork].pauseDisplay = pauseDisplay;
                holder[nbOfWork].stopDisplay = stopDisplay;
                nbOfWork++;
            }
            else { if (App.language == "EN") { MessageBox.Show("Error, wrong mode input !", "", MessageBoxButton.OK, MessageBoxImage.Error); } else { MessageBox.Show("Erreur, le mode choisie (0 ou 1) est invalide !", "", MessageBoxButton.OK, MessageBoxImage.Error); } return -1; } // Error, wrong save type entered by the user
            // Log the new save work
            if (shouldLog)
            {
                string horodatage = DateTime.Now.ToString();
                string[] columnname = { "horodatage", "appellation", "state", "mode", "source", "target" };
                string[] data = { horodatage, appellation, "inactive", mode, source, dest };
                log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml", appellation, 6, columnname, data);
            }
            return 0;
        }
        public void summary() // Show a quick summary of the already saved save works
        {
            string output = "";
            output += "------ Save work summary :\n";
            if(nbOfWork == 0) { output += "------ nothing \n"; }
            else 
            {
                int i = 0;
                foreach (saveWork sw in holder)
                {
                    if (i < nbOfWork)
                    {
                        output += "------ " + sw.appellation + " :\n";
                        output += "---------- srcDir: " + sw.sourceDir + "\n";
                        output += "---------- destDir: " + sw.destDir + "\n";
                        output += "---------- mode: " + sw.mode + "\n";
                        i++;
                    }
                }
            }
            MessageBox.Show(output);
        }
        public int getNbOfWork() { return nbOfWork; } // Return the actual number of work registered
        public saveWork getSaveWork(int id) // Recovering save works
        {
            return holder[id];
        }
        public void executeSaveWork(int id) // Execute a registered save work, sequential or just one specified by its ID by the user
        {
            try
            {
                holder[id].save();
            }
            catch
            {
                MessageBox.Show("Error during save id " + id + " (from 0), the input or target directory may have been deleted or some process is still using it. Please retry later.","",MessageBoxButton.OK,MessageBoxImage.Error);
                //MessageBox.Show(ex);
            }
        }
    }
}
