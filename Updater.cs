using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Monitoramento;

public class Updater
{
    private const string HttpUpUrl = "http://seusite/monitora/";

    private const string UpFile = "exe";
    private const string UpFileName = "MonitorametoSetup.exe";

    public static async void upCheck()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(new Uri(HttpUpUrl + "version"));

                var versionString = await response.Content.ReadAsStringAsync();
                int version = int.Parse(versionString.Trim());

                if (version > GetCurrentVersion())
                {
                    await DownloadUP();

                    Process.Start(Path.Combine(Environment.SpecialFolder.Downloads + "\\" + UpFileName));
                }
            }
        }
        catch (Exception)
        {
            Console.Write("Nao foi possivel verificar atualizacoes");
        }
    }

    private static int GetCurrentVersion()
    {
        return 2;
    }

    private static async Task DownloadUP()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(new Uri(HttpUpUrl + UpFileName));

                using (FileStream file = new FileStream(Path.Combine(Environment.SpecialFolder.Downloads + "\\" + UpFileName), FileMode.Create))
                {
                    await response.Content.CopyToAsync(file);
                }
            }
        }
        catch (Exception)
        {
            Console.Write("Nao foi possivel baixar a atualizacao");
        }
    }

}
