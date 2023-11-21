using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Monitoramento;

public class Program
{
    //--Constantes--\\
    public const string Versao = "2.0.0.1", DomainName = "dominio do dc", DomainNameSec = "dominio do dc secundario";
    const int DelayTime = 360000;
    private const string PathOut = @"C:\Program Files (x86)\Monitoramento\out.txt";

    //--Variaveis--\\
    static int ramal;

    //--VariaveisPublicas--\\
    public static string TxMatricula,
        TxSenha,
        TxEstado,
        Cpu,
        IpHost,
        Nhost = "",
        MaxLogins,
        Segmento,
        Hostname,
        Super,
        Coord,
        arqTraecrt;

    public static double Mem;
    public static int Conexao, Nega = 1, I = 1;

    /*
        Descrição:
        ----------
    
        Esta função que faz o upload dos dados de desempenho do computador para o banco de dados
     
    */
    public static void UpStats()
    {
        try
        {
            Task up = Task.Run(() =>
            {
                using (var cn = new MySqlConnection(Conn.StrConnTi))
                {
                    DateTime date = DateTime.Today; // Puxa a data do computador em formato string
                    string hora = DateTimeOffset.Now.ToString("hh:mm:ss"); //Pega o horario da maquina

                    cn.Open();
                    MySqlCommand my = new MySqlCommand(
                        "INSERT INTO tb_monitora (mat_you, Pingmax, Pingmin, PercentCPU, PercentRAM, PercentDisco, Conexao, HorarioCerto, DataHora) " +
                        "VALUES (@matricula, @pingmax, @pingmin, @cpuporcent, @ramporcent, @discporcent, @conexao, @horariocerto, @datahora);",
                        cn);
                    my.Parameters.AddWithValue("@matricula", TxMatricula);
                    my.Parameters.AddWithValue("@pingmax", TaskManager.Pingmax);
                    my.Parameters.AddWithValue("@pingmin", TaskManager.Pingmin);
                    my.Parameters.AddWithValue("@cpuporcent", TaskManager.Cpuporcent);
                    my.Parameters.AddWithValue("@ramporcent", TaskManager.Ramporcent);
                    my.Parameters.AddWithValue("@discporcent", TaskManager.DiscPorcent);
                    my.Parameters.AddWithValue("@conexao", VerificacaoRede());
                    my.Parameters.AddWithValue("@horariocerto", VerifyHora());
                    my.Parameters.AddWithValue("@datahora", date.ToString("yyyy-MM-dd") + " " + hora);
                    my.ExecuteNonQuery();
                    cn.Close();
                }

                Thread.Sleep(DelayTime);
            });
            Task.WaitAll(up);
            Task.Run(() => { TaskManager.MainFunc(); });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

        }
    }

    /*
        Descrição:
        ----------
        Esta função adiciona uma regra no Firewall do Windows para permitir a conexão do programa com o banco de dados
    */
    public static void Firewall()
    {
        // Execute o comando netsh para adicionar uma regra no Firewall do Windows
        string command = "netsh advfirewall firewall add rule name=\"Monitoramento\" dir=in action=allow program=\"C:\\Program Files (x86)\\Monitoramento\\Monitoramento.exe\" enable=yes";

        ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
        processInfo.CreateNoWindow = true;
        processInfo.UseShellExecute = true;

        Process.Start(processInfo);

    }

    /*
        Descrição:
        ----------
        Esta  função verifica a quantidade de memoria ram disponivel no computador
    */
    private static double MemRam()
    {
        ManagementObjectSearcher
            s1 = new ManagementObjectSearcher(
                "SELECT * FROM Win32_ComputerSystem"); //indica onde vai buscar as informaçoes da memoria

        foreach (var o in s1.Get())
        {
            var mo = (ManagementObject)o;
            Mem = float.Parse(mo["TotalPhysicalMemory"].ToString()) / 1024 / 1024 / 1024; //pega a memoria total do computador
            return Math.Round(Mem);
        }

        return Mem;
    }

    /*
        Descrição:
        ----------
        Esta função verifica qual modelo do processador    
    */
    private static string CpuModel()
    {
        ManagementObjectSearcher
            s2 = new ManagementObjectSearcher("root\\CIMV2",
                "SELECT * FROM Win32_Processor"); //indica onde vai buscar as informaçoes da processador

        foreach (var o in s2.Get())
        {
            var mo = (ManagementObject)o;
            Cpu = mo["Name"].ToString(); //pega o modelo do processador
        }

        return Cpu;
    }

    /*
        Descrição:
        ----------
        Esta função verifica se o usuario esta logando dentro do horario permitido em sua escala e informa no banco de dados
     */
    private static int VerifyHora()
    {
        DateTime time = DateTime.Now;

        using (var cn = new MySqlConnection(Conn.StrConnTi))
        {
            cn.Open();
            MySqlCommand my =
                new MySqlCommand("select `novo horario` from tb_base_ip where" + TxMatricula + ";", cn);
            MySqlDataReader hora = my.ExecuteReader();
            hora.Read();
            var aux = hora.GetString(0);
            var entrada = DateTime.Parse(aux);
            if (DateTime.Compare(time, entrada) < 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    /*
        Descrição:
        ----------
        Esta função verifica os adaptadores de rede do computador e retorna o qual esta sendo utilizado
        0 - Wifi e Ethernet
        1 - Wifi
    */
    private static int VerificacaoRede()
    {
        try
        {
            string nameConect;
            ProcessStartInfo processInfo;
            Process process;
            processInfo = new ProcessStartInfo("powershell.exe",
                "Get-NetAdapter -Physical | Select-Object Name,InterfaceDescription,Status | Where-Object Status -eq \'Up\'");
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            process = Process.Start(processInfo);
            process?.WaitForExit();

            nameConect = process?.StandardOutput.ReadToEnd();

            if (nameConect != null && (nameConect.Contains("Wi-Fi")) && (nameConect.Contains("Up")) &&
                (nameConect.Contains("Ethernet")))
            {
                Conexao = 0;
            }
            else if (nameConect != null && (nameConect.Contains("Wi-Fi")) && (nameConect.Contains("Up")))
            {
                Conexao = 1;
            }
            else
            {
                Conexao = 0;
            }

            return Conexao;
        }
        catch (Exception)
        {
            return Conexao;
        }
    }

    /*
        Descrição:
        ----------
        Esta funcação verifica se o usuario esta presente na escala para home office

        Retorno:
        ----------
        IpHost - Ip do computador que de se conectar
        segmento - Segmento do usuario para registor de entrada e configuração de ramal
        Super - Supervisor do usuario para registor de entrada e relatorios
        Coord - Coordenador do usuario para registor de entrada e relatorios
     */
    private static bool VerificaLogin()
    {
        bool result = false;
        try
        {
            using (var cn = new MySqlConnection(Conn.StrConnTi))
            {
                try
                {
                    cn.Open(); //abre a conexao
                    MySqlCommand cmd =
                        new MySqlCommand("SELECT * FROM tb_base_ip WHERE `matr you` = @matricula;", cn);
                    cmd.Parameters.AddWithValue("@matricula", TxMatricula);
                    MySqlDataReader dados = cmd.ExecuteReader();
                    result = dados.HasRows; //retorna true se tiver a matricula no banco
                    dados.Read();
                    if (!dados.IsDBNull(10))
                    {
                        IpHost = dados.GetString(10); //pega o ip que esta vinculando ao nome da pessoas
                        Segmento = dados.GetString(5); //pega o segmento do cr
                        Super = dados.GetString(6);
                        Coord = dados.GetString(7);
                    }

                    cn.Close();
                }
                catch (MySqlException)
                {
                    Console.Write(@"Confirme sua matricula e tente novamente");
                    cn.Close();
                }
            }
        }
        catch (Exception)
        {
            return result;
        }

        return result;
    }

    /*
        Descrição:
        ----------
        Esta função verfica em uma api esterna os dados de ip e provedor de internet do usuario
    */
    private static string MeuIp()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://ipinfo.io/json");
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        Stream receiveStream = response.GetResponseStream();
        StreamReader readStream =
            new StreamReader(receiveStream ?? throw new InvalidOperationException(), Encoding.UTF8);
        string ipClient = readStream.ReadToEnd();
        response.Close();
        readStream.Close();

        String[] ipclient = ipClient.Split(':', '"');
        return ipclient[34];
    }

    /*
        Descrição:
        ----------
        Esta função utiliza do IP que foi pego na função VerificaLogin() para que 
        possa ser automatizado automatizado a conexão do usuario com o computador remoto
    */
    private static void Rdp()
    {
        try
        {
            if (IpHost != null)
            {
                //ramal();

                ProcessStartInfo processInfo;
                Process process;
                processInfo = new ProcessStartInfo("powershell.exe", "mstsc /v:" + IpHost);
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                process = Process.Start(processInfo);
                process?.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
    }

    /*
        Descrição:
        ----------
        Esta função faz o upload dos dados do usuario para o banco de dados
    */
    public static void Upload()
    {
        DateTime date = DateTime.Today; // Puxa a data do computador em formato string
        string hora = DateTimeOffset.Now.ToString("hh:mm:ss"); //Pega o horario da maquina
        try
        {
            StreamReader arq = new StreamReader(PathOut);

            while (!arq.EndOfStream)
            {
                arqTraecrt = arq.ReadToEnd();
            }
        }
        catch (Exception e)
        {
        }

        try
        {
            using (var cn = new MySqlConnection(Conn.StrConnTi))
            {
                // UpEntrada();

                cn.Open();
                //faz uma verificaçao do ip do cr no banco
                if (TxEstado == "")
                {
                    if (IpHost == "")
                    {
                        MessageBox.Show(@"Seu IP nao Foi encontrado");
                        MySqlCommand cmd = new MySqlCommand(
                            "INSERT INTO tb_monitorados(mat_you,processador,qt_memoria,DataHora_Login,Provedor,Versao,Negar,Supervisor,Coordenador, Rota) VALUES(@matricula, @processador, @memoria, @data, @provedor, @versao, @nega, @super, @coord, @rota) ON DUPLICATE KEY UPDATE processador=@processador2, qt_memoria=@memoria2, Provedor=@provedor2, versao=@versao2, Negar=@nega2 , Rota=@rota",
                            cn);
                        cmd.Parameters.AddWithValue("@matricula", TxMatricula);
                        cmd.Parameters.AddWithValue("@processador", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria", MemRam());
                        cmd.Parameters.AddWithValue("@data", date.ToString("yyyy-MM-dd") + " " + hora);
                        cmd.Parameters.AddWithValue("@provedor", MeuIp());
                        cmd.Parameters.AddWithValue("@versao", Versao);
                        cmd.Parameters.AddWithValue("@nega", Nega);
                        cmd.Parameters.AddWithValue("@super", Super);
                        cmd.Parameters.AddWithValue("@coord", Coord);
                        cmd.Parameters.AddWithValue("@processador2", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria2", MemRam());
                        cmd.Parameters.AddWithValue("@provedor2", MeuIp());
                        cmd.Parameters.AddWithValue("@versao2", Versao);
                        cmd.Parameters.AddWithValue("@nega2", Nega);
                        cmd.Parameters.AddWithValue("@rota", arqTraecrt);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        MySqlCommand cmd = new MySqlCommand(
                            "INSERT INTO tb_monitorados(mat_you,processador,qt_memoria,DataHora_Login,Provedor,Versao,Negar,Supervisor,Coordenador, Rota) VALUES(@matricula, @processador, @memoria, @data, @provedor, @versao, @nega, @super, @coord, @rota) ON DUPLICATE KEY UPDATE processador=@processador2, qt_memoria=@memoria2, Provedor=@provedor2, versao=@versao2, Negar=@nega2, Rota=@rota",
                            cn);
                        cmd.Parameters.AddWithValue("@matricula", TxMatricula);
                        cmd.Parameters.AddWithValue("@processador", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria", MemRam());
                        cmd.Parameters.AddWithValue("@data", date.ToString("yyyy-MM-dd") + " " + hora);
                        cmd.Parameters.AddWithValue("@provedor", MeuIp());
                        cmd.Parameters.AddWithValue("@versao", Versao);
                        cmd.Parameters.AddWithValue("@nega", Nega);
                        cmd.Parameters.AddWithValue("@super", Super);
                        cmd.Parameters.AddWithValue("@coord", Coord);
                        cmd.Parameters.AddWithValue("@processador2", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria2", MemRam());
                        cmd.Parameters.AddWithValue("@provedor2", MeuIp());
                        cmd.Parameters.AddWithValue("@versao2", Versao);
                        cmd.Parameters.AddWithValue("@nega2", Nega);
                        cmd.Parameters.AddWithValue("@rota", arqTraecrt);
                        cmd.ExecuteNonQuery();
                        Rdp();
                    }

                    cn.Close();
                }
                else
                {
                    if (IpHost == "")
                    {
                        MySqlCommand cmd = new MySqlCommand(
                            "INSERT INTO tb_monitorados(mat_you,uf,processador,qt_memoria,DataHora_Login,Provedor,Versao,Negar,Supervisor,Coordenador, Rota) VALUES(@matricula, @estado, @processador, @memoria, @data, @provedor, @versao, @nega, @super, @coord, @rota) ON DUPLICATE KEY UPDATE processador=@processador2, qt_memoria=@memoria2, Provedor=@provedor2, versao=@versao2, Negar=@nega2, Rota=@rota",
                            cn);
                        cmd.Parameters.AddWithValue("@matricula", TxMatricula);
                        cmd.Parameters.AddWithValue("@estado", TxEstado);
                        cmd.Parameters.AddWithValue("@processador", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria", MemRam());
                        cmd.Parameters.AddWithValue("@data", date.ToString("yyyy-MM-dd") + " " + hora);
                        cmd.Parameters.AddWithValue("@provedor", MeuIp());
                        cmd.Parameters.AddWithValue("@versao", Versao);
                        cmd.Parameters.AddWithValue("@nega", Nega);
                        cmd.Parameters.AddWithValue("@super", Super);
                        cmd.Parameters.AddWithValue("@coord", Coord);
                        cmd.Parameters.AddWithValue("@processador2", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria2", MemRam());
                        cmd.Parameters.AddWithValue("@provedor2", MeuIp());
                        cmd.Parameters.AddWithValue("@versao2", Versao);
                        cmd.Parameters.AddWithValue("@nega2", Nega);
                        cmd.Parameters.AddWithValue("@rota", arqTraecrt);
                        MessageBox.Show(@"Seu IP nao Foi encontrado");
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        MySqlCommand cmd = new MySqlCommand(
                            "INSERT INTO tb_monitorados(mat_you,uf,processador,qt_memoria,DataHora_Login,Provedor,Versao,Negar,Supervisor,Coordenador, Rota) VALUES(@matricula, @estado, @processador, @memoria, @data, @provedor, @versao, @nega, @super, @coord, @rota) ON DUPLICATE KEY UPDATE processador=@processador2, qt_memoria=@memoria2, Provedor=@provedor2, versao=@versao2, Negar=@nega2, Rota=@rota",
                            cn);
                        cmd.Parameters.AddWithValue("@matricula", TxMatricula);
                        cmd.Parameters.AddWithValue("@estado", TxEstado);
                        cmd.Parameters.AddWithValue("@processador", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria", MemRam());
                        cmd.Parameters.AddWithValue("@data", date.ToString("yyyy-MM-dd") + " " + hora);
                        cmd.Parameters.AddWithValue("@provedor", MeuIp());
                        cmd.Parameters.AddWithValue("@versao", Versao);
                        cmd.Parameters.AddWithValue("@nega", Nega);
                        cmd.Parameters.AddWithValue("@super", Super);
                        cmd.Parameters.AddWithValue("@coord", Coord);
                        cmd.Parameters.AddWithValue("@processador2", CpuModel());
                        cmd.Parameters.AddWithValue("@memoria2", MemRam());
                        cmd.Parameters.AddWithValue("@provedor2", MeuIp());
                        cmd.Parameters.AddWithValue("@versao2", Versao);
                        cmd.Parameters.AddWithValue("@nega2", Nega);
                        cmd.Parameters.AddWithValue("@rota", arqTraecrt);
                        cmd.ExecuteNonQuery();
                        Rdp();
                    }

                    cn.Close();
                }
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e + @"Upload");
            using (var cn = new MySqlConnection(Conn.StrConnTi))
            {
                cn.Close();
            }
        }
    }

    /*
        Descrição:
        ----------
        Esta função utiliza do segimento obtido na função VerificaLogin() para que
        possa ser configurado as ferramentas encessarias para o usuario do ambiene de voz
     */
    private static void Ramal()
    {
        try
        {
            ProcessStartInfo processInfo;
            Process process;
            processInfo = new ProcessStartInfo("powershell.exe",
                "Get-WmiObject -Class Win32_Product | where-Object { $_.name -match \"Cisco IP Communicator\"}");
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            process = Process.Start(processInfo);
            process?.WaitForExit();

            string cisc = process?.StandardOutput.ReadToEnd();

            if (GetUserOu() == "PortOut")
            {
                Task evaTask = Task.Run(() => { InstallEva(); });
                Task.WaitAll(evaTask);

                Process.Start("C:\\Program Files (x86)\\EVAMIND\\Worker Systray\\eva.worker_systray.exe");
            }

            using (var cn = new MySqlConnection(Conn.StrConnTipc))
            {
                cn.Open();
                MySqlCommand cmd =
                    new MySqlCommand("select ramalCisk from youtility.autoMap where ip='" + IpHost + "';", cn);
                MySqlDataReader my = cmd.ExecuteReader();
                while (my.Read())
                {
                    ramal = my.GetInt32(0);
                }
                // MessageBox.Show(_ramal.ToString());
            }

            if ((GetUserOu() == "Inbound"))
            {
                if (cisc != "Cisco IP Communicator")
                {
                    InstallCisc();
                }

                while (cisc != "Cisco IP Communicator")
                {
                    Thread.Sleep(10000);
                }

                RegistryKey rK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                rK?.SetValue("HostName", "YOU_RJ_" + ramal);
                rK?.Close();
                RegistryKey reK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                reK?.SetValue("TftpServer1", 595503114, RegistryValueKind.DWord);
                reK?.Close();
                RegistryKey rgK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                rgK?.SetValue("TftpServer2", 1048473610, RegistryValueKind.DWord);
                rgK?.Close();

                Process.Start("C:\\Program Files (x86)\\Cisco Systems\\Cisco IP Communicator\\communicatork9.exe");
            }
            else if (GetUserOu() == "Live")
            {
                if (cisc != "Cisco IP Communicator")
                {
                    InstallCisc();
                }

                while (cisc != "Cisco IP Communicator")
                {
                    Thread.Sleep(10000);
                }

                RegistryKey rK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                rK?.SetValue("HostName", "YOF_VR_" + ramal);
                rK?.Close();
                RegistryKey reK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                reK?.SetValue("TftpServer1", 595503114, RegistryValueKind.DWord);
                reK?.Close();
                RegistryKey rgK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                rgK?.SetValue("TftpServer2", 1048473610, RegistryValueKind.DWord);
                rgK?.Close();

                Process.Start("C:\\Program Files (x86)\\Cisco Systems\\Cisco IP Communicator\\communicatork9.exe");
            }
            else if (GetUserOu() == "BO")
            {
                if (cisc != "Cisco IP Communicator")
                {
                    InstallCisc();
                }

                while (cisc != "Cisco IP Communicator")
                {
                    Thread.Sleep(10000);
                }

                RegistryKey rK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                rK?.SetValue("HostName", "YOU_RJ_" + ramal);
                rK?.Close();
                RegistryKey reK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                reK?.SetValue("TftpServer1", 595503114, RegistryValueKind.DWord);
                reK?.Close();
                RegistryKey rgK =
                    Registry.LocalMachine.CreateSubKey("SOFTWARE\\WOW6432Node\\Cisco Systems, Inc.\\Communicator");
                rgK?.SetValue("TftpServer2", 1048473610, RegistryValueKind.DWord);
                rgK?.Close();

                Process.Start("C:\\Program Files (x86)\\Cisco Systems\\Cisco IP Communicator\\communicatork9.exe");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex + @"ramal");
        }
    }


    /*
        Descrição:
        _________
        Esta função utiliza da conexão com o gerenciador de dominio para 
        que possa fazer a autenticação do usuario
     */
    public static bool CnLdap()
    {
        bool isAuth = false;
        try
        {
            string path = DomainName;
            DirectoryEntry entry = new DirectoryEntry(path, TxMatricula, TxSenha);
            object nativeObject = entry.NativeObject;
            var dc = entry.Name;
            // MessageBox.Show(dc);
            if (nativeObject == null)
            {
                return false;
            }
            else if (VerificaLogin())
            {
                // MessageBox.Show(@"1");
                isAuth = true;
            }
        }
        catch (DirectoryServicesCOMException)
        {
            return false;
        }
        catch (Exception)
        {
            try
            {
                string path = DomainNameSec;
                DirectoryEntry entry = new DirectoryEntry(path, TxMatricula, TxSenha);
                object nativeObject = entry.NativeObject;
                var dc = entry.Name;
                // MessageBox.Show(dc);
                if (nativeObject == null)
                {
                    return false;
                }
                else if (VerificaLogin())
                {
                    // MessageBox.Show(@"2");
                    isAuth = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e + @"CnLdap");

            }
            return isAuth;
        }

        return isAuth;
    }

    /*
        Descrição:
        ----------
        Esta função utiliza da conexão com o gerenciador de dominio para
        coletar a OU do usuario para auciliar na configuração do computador

        função nao é mais utiliada
     
     */
    private static string GetUserOu()
    {
        string distinguishedName = null, domainsede = "domino da sede", domainfilial = "dominio da filial";

        try
        {
            // Cria uma instância da classe DirectoryEntry para se conectar ao domínio especificado
            using (DirectoryEntry entry = new DirectoryEntry("LDAP://" + domainfilial, "jr3", ""))
            {
                using (DirectorySearcher searcher = new DirectorySearcher(entry))
                {
                    // Define os critérios de pesquisa pelo nome de usuário
                    searcher.Filter = "(sAMAccountName=" + TxMatricula + ")";

                    // Define quais atributos devem ser retornados na pesquisa
                    searcher.PropertiesToLoad.Add("distinguishedName");

                    // Executa a pesquisa
                    SearchResult result = searcher.FindOne();

                    // Verifica se o usuário foi encontrado
                    if (result != null)
                    {
                        // Obtém o atributo "distinguishedName" do resultado
                        distinguishedName = result.Properties["distinguishedName"][0].ToString();
                    }
                    else
                    {
                        Console.WriteLine("Usuário não encontrado.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Cria uma instância da classe DirectoryEntry para se conectar ao domínio especificado
            using (DirectoryEntry entry = new DirectoryEntry("LDAP://" + domainsede, TxMatricula, TxSenha))
            {
                using (DirectorySearcher searcher = new DirectorySearcher(entry))
                {
                    // Define os critérios de pesquisa pelo nome de usuário
                    searcher.Filter = "(sAMAccountName=" + TxMatricula + ")";

                    // Define quais atributos devem ser retornados na pesquisa
                    searcher.PropertiesToLoad.Add("distinguishedName");

                    // Executa a pesquisa
                    SearchResult result = searcher.FindOne();

                    // Verifica se o usuário foi encontrado
                    if (result != null)
                    {
                        // Obtém o atributo "distinguishedName" do resultado
                        distinguishedName = result.Properties["distinguishedName"][0].ToString();
                    }
                    else
                    {
                        Console.WriteLine("Usuário não encontrado.");
                    }
                }
            }
        }

        return distinguishedName;
    }

    /*
        Descrição:
        ----------
        Esta função Faz a instalação do EVA caso o usuario nao tenha instalado
        e seja exigido pelo seu segmento
     */
    private static void InstallEva()
    {
        ProcessStartInfo processInfo;
        Process process;
        processInfo = new ProcessStartInfo("powershell.exe",
            "Get-WmiObject -Class Win32_Product | where-Object { $_.name -match \"Worker Stray\"}");
        processInfo.CreateNoWindow = true;
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardOutput = true;
        process = Process.Start(processInfo);
        process?.WaitForExit();

        string eva = process?.StandardOutput.ReadToEnd();

        if (eva != null && !eva.Contains("Worker Stray"))
        {
            processInfo = new ProcessStartInfo("powershell.exe",
                "*\\MonitoraYou\\eva.worker_webfront_installer.msi /quiet");
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            process = Process.Start(processInfo);
            process?.WaitForExit();
        }
    }

    /*
        Descrição:
        ----------
        Esta função Faz a instalação do Cisco IP Communicator caso o usuario nao tenha instalado
        e seja exigido pelo seu segmento
     */
    private static void InstallCisc()
    {
        var code = @"C:\Program Files (x86)\Monitoramento\Complet\CiscoIPCommunicatorSetup8.6.6.0.msi";
        Process.Start(code);
    }

    /*
        Descrição:
        ----------
        Esta função abre os programas necessarios para que o usuario possa trabalhar
        alem de disparar as outras fuções para que o programa funcione corretamente
     */
    public static void AbreApp()
    {
        //Ramal();
        Upload();
        TaskManager.MainFunc();
    }

    /*
        Descrição:
        ----------
        Esta Função verifica se o FortiClient esta instalado no computador
        caso nao esteja ele faz o download e a instalação

        função nao é mais utiliada pois e o programa de vpn inpede que seja instalado e configurado remotamente
     */
    public static void FortClient()
    {
        try
        {
            using (PowerShell powerShell = PowerShell.Create())
            {
                // Define o script do PowerShell para verificar a presença do FortiClient
                var script =
                    @"Get-WmiObject -Class Win32_Product | where-Object { $_.name -match 'FortiClient VPN'}";
                powerShell.AddScript(script);

                // Executa o script e aguarda a sua conclusão
                Collection<PSObject> results = powerShell.Invoke();
                powerShell.Streams.Error.Clear();

                // Obtém o resultado da execução do script
                string vpn = results.Count > 0 ? results[0].ToString() : "";

                if (vpn.Contains("FortiClient VPN"))
                {
                    var codigo =
                        @"'C:\ProgramData\Microsoft\Windows\Start Menu\Programs\FortiClient VPN\FortiClient VPN.lnk'";
                    ProcessStartInfo processInfo;
                    Process process;
                    processInfo =
                        new ProcessStartInfo("powershell.exe", "Start-Process -FilePath (" + codigo + ")");
                    processInfo.CreateNoWindow = true;
                    processInfo.UseShellExecute = false;
                    processInfo.RedirectStandardOutput = true;
                    process = Process.Start(processInfo);
                    process?.WaitForExit();
                }
                else
                {
                    var codigo = @"'C:\Program Files (x86)\MonitoraYou\Complet\FortiClientVPN-7.0.exe'";
                    ProcessStartInfo processInfo;
                    Process process;
                    processInfo =
                        new ProcessStartInfo("powershell.exe", "Start-Process -FilePath (" + codigo + ")");
                    processInfo.CreateNoWindow = true;
                    processInfo.UseShellExecute = false;
                    processInfo.RedirectStandardOutput = true;
                    process = Process.Start(processInfo);
                    process?.WaitForExit();
                }
            }

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface adapter = adapters.FirstOrDefault(a =>
                a.Description.Contains("Fortinet SSL VPN Virtual Ethernet Adapter"));

            while (adapter == null || adapter.OperationalStatus != OperationalStatus.Up)
            {
                Thread.Sleep(10000);
                adapters = NetworkInterface.GetAllNetworkInterfaces();
                adapter = adapters.FirstOrDefault(a =>
                    a.Description.Contains("Fortinet SSL VPN Virtual Ethernet Adapter"));
            }
        }
        catch (Exception e)
        {
            Console.Write(e.ToString());
        }
    }
}
