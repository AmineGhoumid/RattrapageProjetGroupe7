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
    /// Logique d'interaction pour execSpecificSaveControl.xaml
    /// </summary>
    public partial class execSpecificSaveControl : UserControl
    {
        private saveWorkHolder holder;
        public execSpecificSaveControl(saveWorkHolder holder_)
        {
            holder = holder_;
            InitializeComponent();
            if(App.language != "EN")
            {
                translateWindow();
            }
        }

        public void translateWindow()
        {
            mainlabel.Content = "Executer une sauvegarde ou une rangée de sauvegarde spécifique";
            savelabel.Content = "Sauvegardes(s):";
            savebut.Content = "Sauver";
        }

        private void savebut_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(textbox.Text.Contains("-")) // Cas de la rangée
            {
                // split apart in twopiece by "-" character

                string total = textbox.Text;
                string[] parts = total.Split('-');
                if(parts.Length != 2)
                {
                    if (App.language == "EN")
                    {
                        MessageBox.Show("Error, the input is invalid !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Erreur, l'entrée n'est pas valide !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                int first = -1;
                int last = -1;

                try
                {
                    first = Int32.Parse(parts[0]);
                    last = Int32.Parse(parts[1]);
                }
                catch
                {
                    if (App.language == "EN")
                    {
                        MessageBox.Show("Error, the input is invalid !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Erreur, l'entrée n'est pas valide !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                if(first >= last)
                {
                    if (App.language == "EN")
                    {
                        MessageBox.Show("Error, the input is invalid ! First must be inferior to last", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Erreur, l'entrée n'est pas valide ! Le premier id doit être inférieur au second!", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                for(int i =first-1;i<last;i++)
                {
                    holder.executeSaveWork(i);
                }


            }
            else // Cas d'une simple sauvegarde
            {
                int saveid = -1;
                try { saveid = Int32.Parse(textbox.Text.ToString()); }
                catch
                {
                    if(App.language == "EN")
                    {
                        MessageBox.Show("Error, the input is invalid !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Erreur, l'entrée n'est pas valide !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                if(saveid >= 1 && saveid <= holder.getNbOfWork())
                {
                    holder.executeSaveWork(saveid - 1);
                }
                else
                {
                    if(App.language == "EN")
                    {
                        MessageBox.Show("Error, this save does not exist !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Erreur, la sauvegarde spécifique n'est pas existante !", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
