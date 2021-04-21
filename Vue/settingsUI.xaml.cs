using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace easysave
{
    /// <summary>
    /// Logique d'interaction pour settingsUI.xaml
    /// </summary>
    public partial class settingsUI : UserControl
    {
        public settingsUI()
        {
            InitializeComponent();
            crypt_ext_textbox.Text = App.cryptExt;
            crypt_key_textbox.Text = App.cryptKey;
            job_name_textbox.Text = App.jobSoftwareName;
            languagetextbox.Text = App.language;
            filesize_textbox.Text = App.limitTransfer.ToString();
            foreach (string file in App.prioFile) { fileextprio_textbox.Text += file + ";"; }
            fileextprio_textbox.Text = fileextprio_textbox.Text.Substring(0, (fileextprio_textbox.Text.Length - 1));
            if (App.language != "EN") { translate(); }
        }

        private void translate()
        {
            this.masterlabel.Content = "Paramètres EasySave";
            this.cryptingextensionlabel.Content = "Extension chiffrement :";
            this.keylabel.Content = "Clé chiffrement XOR : ";
            this.joblabel.Content = "Processus métier à surveiller : ";
            this.cryptextinfo.Content = "XOR sera utilisé sur tous les fichiers portant cette extension";
            this.keyinfolabel.Content = "Cette clé sera utilisé avec XOR";
            this.jobinfolabel.Content = "Si ce processus est détecté, annulation de la sauvegarde courante";
            savebutton.Content = "Sauver";
            ext_label.Content = "Les fichiers avec cette extension seront priorisés";
            priofile_label.Content = "Extension fichiers prioritaires :";
            label_size_limit.Content = "Limite taille fichier (ko):";
            filesize_label.Content = "Les fichiers dépassants cette limite ne pourront pas être sauvegardés en même temps";
        }

        private void savebuttonclick(object sender, MouseButtonEventArgs e)
        {
            if (crypt_key_textbox.Text.Length < 64)
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error ! The XOR key must be at least 64 character long !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Erreur ! La clé XOR doit mesurer au moins 64 caractères", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            try
            {
                Int32.Parse(filesize_textbox.Text);
            }
            catch
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error ! Wrong input for limit file input !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Erreur ! L'entrée est invalide pour la limite de fichier !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            if (languagetextbox.Text != "FR" && languagetextbox.Text != "EN")
            {
                if (App.language == "EN")
                {
                    MessageBox.Show("Error ! This language is not supported !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Erreur ! Ce langage n'est pas supporté !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            App.cryptExt = crypt_ext_textbox.Text;
            App.cryptKey = crypt_key_textbox.Text;
            App.jobSoftwareName = job_name_textbox.Text;
            App.language = languagetextbox.Text;
            App.prioFile = fileextprio_textbox.Text.Split(';');
            App.limitTransfer = Int32.Parse(filesize_textbox.Text);

            string[] columnNames = { "key", "job_software", "crypting_extension", "language", "prio_file_ext", "limit_transfer" };
            string[] data = { App.cryptKey, App.jobSoftwareName, App.cryptExt, App.language, fileextprio_textbox.Text, App.limitTransfer.ToString() };
            log.replaceXmlSession(AppDomain.CurrentDomain.BaseDirectory + "\\config.xml", "config", 6, columnNames, data);

            if (App.language == "EN")
            {
                MessageBox.Show("The new configuration is saved ! You may need to restart EasySave for them to apply", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Configuration sauvegardé, vous devrez peut-être redemarrer pour qu'elles prennent effet.", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



    }
}
