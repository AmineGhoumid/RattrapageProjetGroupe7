using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace easysave
{
    partial class mainWindow : UserControl
    {
        private saveWorkHolder holder;
        public mainWindow(saveWorkHolder holder_)
        {
            InitializeComponent();

            holder = holder_;

            loadPreviousSave();

            if (App.language != "EN") // Only EN and FR is supported
            {
                translateInterface();
            }

            // On démarre le serveur une fois que tout est configuré et s'il n'y a pas d'erreur
            server easySaveServer = new server(80, "0.0.0.0", holder);
            easySaveServer.beginStart(); // Méthode non bloquante

        }
        private void loadPreviousSave()
        {
            // Now we try to recover data from last session (realtime file)
            //First detect if file is here
            if (File.Exists((AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml")))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml");
                    XmlNodeList nodes = doc.DocumentElement.SelectNodes("/root");

                    foreach (XmlNode node in nodes)
                    {
                        foreach (XmlNode specificsave in node) // For each master save beacon
                        {
                            string appellation = "";
                            string mode = "";
                            string src = "";
                            string dst = "";
                            foreach (XmlNode save in specificsave) // Each beacon in each save
                            {
                                if (save.Name == "appellation") { appellation = save.InnerText; }
                                else if (save.Name == "mode") { mode = save.InnerText; }
                                else if (save.Name == "source") { src = save.InnerText; }
                                else if (save.Name == "target") { dst = save.InnerText; }
                            }

                            // Now recreate the save internally
                            Button executeButton = new Button();
                            Button pauseButton = new Button();
                            Button stopButton = new Button();

                            if (holder.addSaveWork(appellation, src, dst, mode, executeButton, pauseButton, stopButton, false) == 0) // Add to holder, then verify and if its ok print it
                            {
                                ListBox saveWorkContainer = new ListBox();

                                Label lbl_appelation = new Label();
                                lbl_appelation.Content = appellation;
                                Label lbl_src = new Label();
                                lbl_src.Content = "Source: " + src;
                                Label lbl_dest = new Label();
                                lbl_dest.Content = "Dest: " + dst;
                                Label lbl_type = new Label();
                                lbl_type.Content = "Type: " + mode;
                                executeButton.Content = "Execute";
                                int id = holder.getNbOfWork();


                                pauseButton.Content = "Pause";
                                pauseButton.IsEnabled = false;
                                pauseButton.Click += (sender2, e2) => pauseClick(sender2, e2, id, pauseButton, stopButton);
                                executeButton.Click += (sender2, e2) => executeClick(sender2, e2, id, executeButton, pauseButton, stopButton);
                                pauseButton.Tag = "pause";

                                stopButton.Content = "Stop";
                                stopButton.IsEnabled = false;
                                stopButton.Click += (sender2, e2) => stopButtonClick(sender2, e2, id, executeButton, pauseButton, stopButton);

                                //Add content to listbox
                                saveWorkContainer.Items.Add(lbl_appelation);
                                saveWorkContainer.Items.Add(lbl_src);
                                saveWorkContainer.Items.Add(lbl_dest);
                                saveWorkContainer.Items.Add(lbl_type);
                                saveWorkContainer.Items.Add(executeButton);
                                saveWorkContainer.Items.Add(pauseButton);
                                saveWorkContainer.Items.Add(stopButton);
                                // Add this listbox to global listbo
                                registeredSavePanel.Items.Add(saveWorkContainer);
                            }
                        }

                    }
                }
                catch
                {
                    if (App.language == "EN")
                    {
                        MessageBox.Show("Error ! Inconstistent XML realtime log, this file has been automatically deleted.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Erreur ! Fichier de config xml inconsistant, il a été supprimé automatiquement..", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\save_work_realtime_log.xml");
                }
            }
        }

        private void savebuttonclick(object sender, MouseButtonEventArgs e)
        {
            string type_save; // 0 (full) or 1 (diff)
            if ((type_full_cbox.IsChecked == false && type_diff_cbox.IsChecked == false) || (type_full_cbox.IsChecked == true && type_diff_cbox.IsChecked == true))
            {
                if (App.language == "EN") { MessageBox.Show("Error, you must select either full or differential !", "", MessageBoxButton.OK, MessageBoxImage.Error); }
                else { MessageBox.Show("Erreur, vous devez selectionner différentielle ou complète !", "", MessageBoxButton.OK, MessageBoxImage.Error); }
                return;
            }
            else
            {
                if (type_diff_cbox.IsChecked == true) { type_save = "1"; }
                else { type_save = "0"; }
            }
            Button executeButton = new Button();
            Button pauseButton = new Button();
            Button stopButton = new Button();

            if (holder.addSaveWork(appellation.Text, src.Text, dest.Text, type_save, executeButton, pauseButton, stopButton) == 0) // Add to holder, then verify and if its ok print it
            {
                ListBox saveWorkContainer = new ListBox();

                Label lbl_appelation = new Label();
                lbl_appelation.Content = appellation.Text;
                Label lbl_src = new Label();
                lbl_src.Content = "Source: " + src.Text;
                Label lbl_dest = new Label();
                lbl_dest.Content = "Dest: " + dest.Text;
                Label lbl_type = new Label();
                lbl_type.Content = "Type: " + type_save;

                executeButton.Content = "Execute";
                pauseButton.Content = "Pause";
                int id = holder.getNbOfWork();

                pauseButton.Content = "Pause";
                pauseButton.IsEnabled = false;
                pauseButton.Click += (sender2, e2) => pauseClick(sender2, e2, id, pauseButton, stopButton);
                executeButton.Click += (sender2, e2) => executeClick(sender2, e2, id, executeButton, pauseButton, stopButton);
                pauseButton.Tag = "pause";

                stopButton.Content = "Stop";
                stopButton.IsEnabled = false;
                stopButton.Click += (sender2, e2) => stopButtonClick(sender2, e2, id, executeButton, pauseButton, stopButton);

                //Add content to listbox
                saveWorkContainer.Items.Add(lbl_appelation);
                saveWorkContainer.Items.Add(lbl_src);
                saveWorkContainer.Items.Add(lbl_dest);
                saveWorkContainer.Items.Add(lbl_type);
                saveWorkContainer.Items.Add(executeButton);
                saveWorkContainer.Items.Add(pauseButton);
                saveWorkContainer.Items.Add(stopButton);
                // Add this listbox to global listbo
                registeredSavePanel.Items.Add(saveWorkContainer);
            }
        }

        private void pauseClick(object sender, EventArgs e, int id, Button buttonExecRef, Button stopButton)
        {
            if (buttonExecRef.Tag.ToString() == "pause")
            {
                holder.getSaveWork(id - 1).isPaused = true;
                buttonExecRef.Tag = "play";
                if (App.language == "EN")
                {
                    buttonExecRef.Content = "Resume";
                }
                else
                {
                    buttonExecRef.Content = "Reprendre";
                }
                stopButton.IsEnabled = false;
            }
            else if (buttonExecRef.Tag.ToString() == "play")
            {
                holder.getSaveWork(id - 1).isPaused = false;
                buttonExecRef.Tag = "pause";
                buttonExecRef.Content = "Pause";
                stopButton.IsEnabled = true;
            }

        }
        private void stopButtonClick(object sender, EventArgs e, int id, Button buttonExecRef, Button pauseButton, Button stopButton)
        {
            holder.getSaveWork(id - 1).stop = true;
            holder.getSaveWork(id - 1).tempsEcoule = -1;
            holder.getSaveWork(id - 1).cryptTime = 0;
            holder.getSaveWork(id - 1).nbOfNonPrioFiles = 0;
            holder.getSaveWork(id - 1).fileBufferID = 0;
            buttonExecRef.IsEnabled = true;
            buttonExecRef.Content = "Execute";
            pauseButton.IsEnabled = false; ;
            stopButton.IsEnabled = false;
            pauseButton.Tag = "pause";
        }
        private void executeClick(object sender, EventArgs e, int id, Button buttonExecRef, Button pauseButton, Button stopButton)
        {
            if (id <= 0)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error, no save work to execute !(id=" + id + ")");
                }
                else
                {
                    MessageBox.Show("Erreur, pas de sauvegarde correspondante !(id=" + id + ")");
                }

                return;
            }
            if (id > 0)
            {
                stopButton.IsEnabled = true;
                pauseButton.IsEnabled = true;
                buttonExecRef.IsEnabled = false;
                holder.executeSaveWork(id - 1);
            }
        }
        private void summaryclick(object sender, MouseButtonEventArgs e)
        {
            holder.summary();
        }

        private void removelastsave(object sender, MouseButtonEventArgs e)
        {
            if (holder.getNbOfWork() > 0)
            {
                int lastItem = registeredSavePanel.Items.Count;
                registeredSavePanel.Items.RemoveAt(lastItem - 1);
            }
            holder.deleteLastWork();
        }

        private void textboxhint(object sender, MouseButtonEventArgs e)
        {
            ((TextBox)sender).Text = "";
        }

        private void executeall(object sender, MouseButtonEventArgs e)
        {
            if (holder.getNbOfWork() <= 0)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error, no save work to execute !");
                }
                else
                {
                    MessageBox.Show("Erreur, pas de sauvegarde a executer !");
                }

                return;
            }
            for (int i = 0; i < holder.getNbOfWork(); i++)
            {
                if (holder.getSaveWork(i).statusPercentage == 0)
                {
                    ((Button)holder.getSaveWork(i).stopDisplay).IsEnabled = true;
                    ((Button)holder.getSaveWork(i).pauseDisplay).IsEnabled = true;
                    ((Button)holder.getSaveWork(i).progressDisplay).IsEnabled = false;
                    holder.executeSaveWork(i);
                }
            }
        }

        private void settingsbuttonclick(object sender, MouseButtonEventArgs e)
        {
            Window settingsWindow = new Window();
            settingsWindow.Width = 613;
            settingsWindow.Height = 450;
            settingsWindow.ResizeMode = ResizeMode.NoResize;

            settingsUI settings_ui = new settingsUI();
            settingsWindow.Content = settings_ui;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            settingsWindow.ShowDialog();
        }

        private void translateInterface()
        {
            this.settingsButton.Content = "Paramètres";
            this.savebutton.Content = "Enregistrer";
            this.saveworknamelabel.Content = "Nom: ";
            this.saveworktypelabel.Content = "Type: ";
            this.sourceDirectoryLabel.Content = "Répertoire source: ";
            this.destinationlabel.Content = "Destination: ";
            this.registeredlabel.Content = "Enregistré(s): ";
            this.remove.Content = "Supprimer dernier";
            this.executelabel.Content = "Executer tout";
            this.appellation.Text = "Le nom de la sauvegarde";
            this.dest.Text = "Format UNC";
            this.type_diff_cbox.Content = "Différentielle";
            this.type_full_cbox.Content = "Complète";
            this.execute_specific.Content = "Executer n° spécifique";
            this.pause_all_but.Content = "Tout mettre en pause";
            this.resume_all_but.Content = "Tout reprendre";
            this.stop_all_but.Content = "Tout stopper";
        }

        private void exec_specific_clicked(object sender, MouseButtonEventArgs e)
        {
            Window mywindow = new Window();
            mywindow.Width = 740;
            mywindow.Height = 190;
            mywindow.ResizeMode = ResizeMode.NoResize;

            execSpecificSaveControl settings_ui = new execSpecificSaveControl(holder);
            mywindow.Content = settings_ui;
            mywindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mywindow.ShowDialog();

        }

        private void pauseall_but_clicked(object sender, MouseButtonEventArgs e)
        {
            if (holder.getNbOfWork() <= 0)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error, no save work to pause !");
                }
                else
                {
                    MessageBox.Show("Erreur, pas de sauvegarde a mettre en pause !");
                }

                return;
            }

            for (int i = 0; i < holder.getNbOfWork(); i++)
            {
                if (holder.getSaveWork(i).isPaused == false && holder.getSaveWork(i).statusPercentage != 0)
                {
                    holder.getSaveWork(i).isPaused = true;
                    ((Button)holder.getSaveWork(i).progressDisplay).IsEnabled = false;
                    ((Button)holder.getSaveWork(i).pauseDisplay).Tag = "play";
                    ((Button)holder.getSaveWork(i).pauseDisplay).IsEnabled = true;
                    ((Button)holder.getSaveWork(i).stopDisplay).IsEnabled = false;
                    if (App.language == "EN")
                    {
                        ((Button)holder.getSaveWork(i).pauseDisplay).Content = "Resume";
                    }
                    else
                    {
                        ((Button)holder.getSaveWork(i).pauseDisplay).Content = "Reprendre";
                    }
                }
            }
        }

        private void resume_all_but_clicked(object sender, MouseButtonEventArgs e)
        {
            if (holder.getNbOfWork() <= 0)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error, no save work to resume !");
                }
                else
                {
                    MessageBox.Show("Erreur, pas de sauvegarde a redémarrer !");
                }

                return;
            }

            for (int i = 0; i < holder.getNbOfWork(); i++)
            {
                if (holder.getSaveWork(i).isPaused == true)
                {
                    holder.getSaveWork(i).isPaused = false;
                    ((Button)holder.getSaveWork(i).progressDisplay).IsEnabled = false;
                    ((Button)holder.getSaveWork(i).pauseDisplay).Tag = "pause";
                    ((Button)holder.getSaveWork(i).pauseDisplay).IsEnabled = true;
                    ((Button)holder.getSaveWork(i).pauseDisplay).Content = "Pause";
                    ((Button)holder.getSaveWork(i).stopDisplay).IsEnabled = true;
                }
            }
        }

        private void stop_all_clicked(object sender, MouseButtonEventArgs e)
        {
            if (holder.getNbOfWork() <= 0)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error, no save work to stop !");
                }
                else
                {
                    MessageBox.Show("Erreur, pas de sauvegarde à stopper !");
                }

                return;
            }

            for (int i = 0; i < holder.getNbOfWork(); i++)
            {
                if (holder.getSaveWork(i).isPaused == false && holder.getSaveWork(i).stop == false && holder.getSaveWork(i).statusPercentage != 0)
                {
                    holder.getSaveWork(i).stop = true;
                    holder.getSaveWork(i).statusPercentage = 0;
                    holder.getSaveWork(i).tempsEcoule = -1;
                    holder.getSaveWork(i).cryptTime = 0;
                    ((Button)holder.getSaveWork(i).progressDisplay).IsEnabled = true;
                    ((Button)holder.getSaveWork(i).progressDisplay).Content = "Execute";
                    ((Button)holder.getSaveWork(i).pauseDisplay).IsEnabled = false; ;
                    ((Button)holder.getSaveWork(i).stopDisplay).IsEnabled = false;
                    ((Button)holder.getSaveWork(i).pauseDisplay).Tag = "pause";
                }
            }
            this.InvalidateVisual(); // Sometimes there is a bug where the display is not entirely updated, so we use invalidateVisual here
        }

        private void full_cbox_checked(object sender, RoutedEventArgs e)
        {
            type_diff_cbox.IsChecked = false;
        }

        private void diff_cbox_checked(object sender, RoutedEventArgs e)
        {
            type_full_cbox.IsChecked = false;
        }
    }
}
