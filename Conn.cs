namespace Monitoramento;

internal class Conn
{
    static private string servidor = "ip";
    static private string bancoDados = "bd";
    static private string usuario = "user";
    static private string senha = "senha";

    static private string servidorTi = "ip";
    static private string bancoDadosTi = "bd";
    static private string usuarioTi = "user";
    static private string senhaTi = "senha";

    static private string servidorTipc = "ip";
    static private string bancoDadosTipc = "bd";
    static private string usuarioTipc = "user";
    static private string senhaTipc = "senha";

    public static string StrConn = "server=" + servidor + "; User Id=" + usuario + "; database=" + bancoDados + "; password=" + senha + ";sslMode=none";
    public static string StrConnTi = "server=" + servidorTi + "; User Id=" + usuarioTi + "; database=" + bancoDadosTi + "; password=" + senhaTi + ";sslMode=Preferred";
    public static string StrConnTipc = "server=" + servidorTipc + "; User Id=" + usuarioTipc + "; database=" + bancoDadosTipc + "; password=" + senhaTipc + ";sslMode=Preferred";
}
