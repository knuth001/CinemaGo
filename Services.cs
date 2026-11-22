using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace CinemaGo;

public sealed class FileManager
{
    private readonly string _dir = Path.Combine(AppContext.BaseDirectory, "Data");
    public FileManager() => Directory.CreateDirectory(_dir);
    public string PathFor(string file) => Path.Combine(_dir,file);
    public List<string> Read(string file) => File.Exists(PathFor(file)) ? File.ReadAllLines(PathFor(file)).Where(x=>!string.IsNullOrWhiteSpace(x)).ToList() : new();
    public void Write(string file, IEnumerable<string> lines) => File.WriteAllLines(PathFor(file),lines);
}
public sealed class Logger
{
    private readonly string _path;
    public Logger(string directory) { Directory.CreateDirectory(directory); _path=Path.Combine(directory,"CinemaGo.log"); }
    public void Log(string message) { try { File.AppendAllText(_path,$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}"); } catch { } }
}
public static class PasswordHelper
{
    public static string Hash(string password)
    {
        byte[] salt=RandomNumberGenerator.GetBytes(16);
        byte[] hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,120000,HashAlgorithmName.SHA256,32);
        return Convert.ToBase64String(salt)+"."+Convert.ToBase64String(hash);
    }
    public static bool Verify(string password,string stored)
    {
        try {
            var parts=stored.Split('.');
            var salt=Convert.FromBase64String(parts[0]); var expected=Convert.FromBase64String(parts[1]);
            var actual=Rfc2898DeriveBytes.Pbkdf2(password,salt,120000,HashAlgorithmName.SHA256,32);
            return CryptographicOperations.FixedTimeEquals(actual,expected);
        } catch { return false; }
    }
}
public sealed class EmailService
{
    private readonly Logger _log;
    public EmailService(Logger log)=>_log=log;
    public void SendTicket(string to,string subject,string body)
    {
        string? host=Environment.GetEnvironmentVariable("CINEMAGO_SMTP_HOST");
        string? user=Environment.GetEnvironmentVariable("CINEMAGO_SMTP_USER");
        string? pass=Environment.GetEnvironmentVariable("CINEMAGO_SMTP_PASSWORD");
        string port=Environment.GetEnvironmentVariable("CINEMAGO_SMTP_PORT") ?? "587";
        if(string.IsNullOrWhiteSpace(host)||string.IsNullOrWhiteSpace(user)||string.IsNullOrWhiteSpace(pass))
        { _log.Log("SMTP not configured; email skipped for "+to); return; }
        try {
            using var client=new SmtpClient(host,int.Parse(port)){EnableSsl=true,Credentials=new NetworkCredential(user,pass)};
            using var message=new MailMessage(user,to,subject,body);
            client.Send(message);
            _log.Log("Email sent to "+to);
        } catch(Exception ex) { _log.Log("SMTP error: "+ex.Message); }
    }
}
