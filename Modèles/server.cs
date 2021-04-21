using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace easysave
{
    class server
    {
        private int listeningPort;
        private string listeningAddress;
        private Socket serverSocket;
        public saveWorkHolder holder;
        public server(int listeningPort_, string listeningAddress_, saveWorkHolder holder_)
        {
            listeningAddress = listeningAddress_;
            listeningPort = listeningPort_;
            holder = holder_;
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void beginStart()
        {
            Task.Run(() => {
                startListening();
            });
        }

        private void startListening()
        {

            serverSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            serverSocket.Listen(1);

            while (true)
            {

                Socket clientSocket = serverSocket.Accept();


                MessageBox.Show("Client is connected from " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString(), "", MessageBoxButton.OK, MessageBoxImage.Information);
                clientSocket.Send(Encoding.ASCII.GetBytes("hello")); // say hello

                bool exit = false;

                while (!exit)
                {
                    byte[] data = new byte[30];
                    int nb = 0;
                    try
                    {
                        nb = clientSocket.Receive(data);
                    }
                    catch
                    {
                        exit = true;
                    }

                    if (nb > 0)
                    {
                        string received_string = Encoding.UTF8.GetString(data, 0, data.Length);
                        if (received_string.Contains("START"))
                        {
                            received_string = received_string.Replace("START", "");
                            int id_save = -1;
                            try { id_save = Int32.Parse(received_string); } catch { }

                            ((Button)holder.getSaveWork(id_save - 1).pauseDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                             new Action(delegate ()
                             {
                                 ((Button)holder.getSaveWork(id_save - 1).progressDisplay).IsEnabled = false;
                                 ((Button)holder.getSaveWork(id_save - 1).pauseDisplay).IsEnabled = true;
                                 ((Button)holder.getSaveWork(id_save - 1).stopDisplay).IsEnabled = true;
                                 holder.executeSaveWork(id_save - 1);
                             }));

                        }
                        else if (received_string.Contains("STOP"))
                        {
                            received_string = received_string.Replace("STOP", "");
                            int id_save = -1;
                            try { id_save = Int32.Parse(received_string); } catch { }
                            if (id_save > 0 && id_save <= holder.getNbOfWork())
                            {
                                stop(id_save);
                            }
                        }
                        else if (received_string.Contains("PAUSE"))
                        {
                            received_string = received_string.Replace("PAUSE", "");
                            int id_save = -1;
                            try { id_save = Int32.Parse(received_string); } catch { }
                            if (id_save > 0 && id_save <= holder.getNbOfWork())
                            {
                                pause(id_save);
                            }
                        }
                        else if (received_string.Contains("RESUME"))
                        {
                            received_string = received_string.Replace("RESUME", "");
                            int id_save = -1;
                            try { id_save = Int32.Parse(received_string); } catch { }
                            if (id_save > 0 && id_save <= holder.getNbOfWork())
                            {
                                this.resume(id_save);
                            }
                        }
                        else if (received_string.Contains("EXIT"))
                        {
                            clientSocket.Shutdown(SocketShutdown.Both);
                            clientSocket.Close();
                            exit = true;
                        }
                    }
                }
            }
            
            if(App.language == "EN")
            {
                MessageBox.Show("The server has terminated !", "",MessageBoxButton.OK,MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Le serveur s'est terminé !", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void stop(int id)
        {
            holder.getSaveWork(id - 1).stop = true;
            holder.getSaveWork(id - 1).tempsEcoule = -1;
            holder.getSaveWork(id - 1).cryptTime = 0;
            holder.getSaveWork(id - 1).nbOfNonPrioFiles = 0;
            holder.getSaveWork(id - 1).fileBufferID = 0;

            ((Button)holder.getSaveWork(id - 1).progressDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
             new Action(delegate ()
             {
                 ((Button)holder.getSaveWork(id - 1).progressDisplay).IsEnabled = true;
                 ((Button)holder.getSaveWork(id - 1).progressDisplay).Content = "Execute";
             }));

            ((Button)holder.getSaveWork(id - 1).pauseDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
             new Action(delegate ()
             {
                 ((Button)holder.getSaveWork(id - 1).pauseDisplay).IsEnabled = false;
                 ((Button)holder.getSaveWork(id - 1).pauseDisplay).Tag = "pause";
             }));

            ((Button)holder.getSaveWork(id - 1).stopDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
             new Action(delegate ()
             {
                 ((Button)holder.getSaveWork(id - 1).stopDisplay).IsEnabled = false;
             }));

        }

        private void pause(int id)
        {
            holder.getSaveWork(id - 1).isPaused = true;
        }

        private void resume(int id)
        {
            ((Button)holder.getSaveWork(id - 1).progressDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
             new Action(delegate ()
             {
                 holder.getSaveWork(id - 1).isPaused = false;

                 ((Button)holder.getSaveWork(id - 1).progressDisplay).Tag = "pause";
                 if (App.language == "EN")
                 {
                     ((Button)holder.getSaveWork(id - 1).progressDisplay).Content = "Resume";
                 }
                 else
                 {
                     ((Button)holder.getSaveWork(id - 1).progressDisplay).Content = "Reprendre";
                 }
             }));

            ((Button)holder.getSaveWork(id - 1).stopDisplay).Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
             new Action(delegate ()
             {
                 ((Button)holder.getSaveWork(id - 1).stopDisplay).IsEnabled = true;
             }));
        }
    }
}
