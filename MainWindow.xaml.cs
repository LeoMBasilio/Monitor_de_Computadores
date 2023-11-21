using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MonitoraYou
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Task firewall = Task.Run(() =>
            // {
            //   Monitora.Firewall();
            // });
            // Task.WaitAll(firewall);

            MessageBox.Show(@"Favor verifique se o cabo de rede esta conectado, caso esteja conectado ignore essa menssagem");

        }

        private void MatriculaTxb_TextChanged(object sender, TextChangedEventArgs e)
        {
            Program.TxMatricula = MatriculaTxb.Text;
        }

        private void MatriculaTxb_GotFocus(object sender, RoutedEventArgs e)
        {
            MatriculaTxb.Text = string.Empty;
        }

        private void MatriculaTxb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MatriculaTxb.Text))
            {
                MatriculaTxb.Text = "Exemplo:101010";
            }
        }

        private void EnterBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Program.Nega = 0;



                if (String.IsNullOrEmpty(MatriculaTxb.Text) && string.IsNullOrEmpty(SenhaPb.Password))
                {
                    MessageBox.Show(@"Prencha todos os campos");
                    return;
                }

                bool resultLdap = Program.CnLdap();

                if (resultLdap)
                {

                    Task.Run(() =>
                    {
                        MessageBox.Show(@"Seja Bem vindo(a)!");
                    });
                    titulo.Visibility = Visibility.Hidden;
                    Monitorando.Visibility = Visibility.Visible;
                    WindowState = WindowState.Minimized;
                    try
                    {
                        Task.Run(() => Program.AbreApp());

                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        throw;
                    }
                }
                else
                {
                    MessageBox.Show(@"Confirme sua matricula e tente novamente");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Termoslb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start("http://seusite/monitora/termos");//link para termos de uso
            }
            catch (Exception)
            {
                MessageBox.Show(@"Link não encontrado, contate o suporte");
                throw;
            }
        }

        private void SenhaPb_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Program.TxSenha = SenhaPb.Password;
        }

        private void EnterBtn_ContextMenuClosing()
        {

        }
    }

}
