using System;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Monitoramento;

internal class TaskManager
{
    //--Variaveis--\\
    public static int Pingmin = 999, Pingmax, Cpuporcent, Ramporcent;
    public static float DiscPorcent;

    public static void MainFunc()
    {
        try
        {
            TaskManager taskmanager = new TaskManager();
            taskmanager.RunTasks();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

        }
    }

    private void RunTasks()
    {
        try
        {
            Task dadosRede = Task.Run(() =>
            {
                try
                {
                    Ping pingSender = new Ping();
                    string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);
                    int timeout = 4000;
                    PingOptions options = new PingOptions(114, true);

                    PingReply reply = pingSender.Send(Program.PingIp, timeout, buffer, options);

                    if (reply != null && (int)reply.RoundtripTime < Pingmin)
                    {
                        Pingmin = (int)reply.RoundtripTime;
                    }

                    if (reply != null && reply.RoundtripTime > Pingmax)
                    {
                        Pingmax = (int)reply.RoundtripTime;
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                }
            });

            Task desempenho = Task.Run(() =>
            {
                ManagementObjectSearcher s1 = new ManagementObjectSearcher("root\\CIMV2", "SELECT LoadPercentage FROM Win32_Processor");
                foreach (var p in s1.Get())
                {
                    Cpuporcent = Convert.ToInt32(p["LoadPercentage"]);
                }

                PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                Ramporcent = (int)ramCounter.NextValue();

                PerformanceCounter diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                DiscPorcent = diskCounter.NextValue();

            });
            Task.WaitAll(dadosRede, desempenho);
            Program.UpStats();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

        }
    }
}
