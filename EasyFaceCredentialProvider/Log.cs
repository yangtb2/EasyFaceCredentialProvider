using Windows.Win32;

namespace EasyFaceCredentialProvider;

public class Log
{
    static Log()
    {
#if DEBUG
        PInvoke.AllocConsole();
#endif
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Error(e.Exception);
        //e.SetObserved();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Error(e.ExceptionObject as Exception);
    }

    public static void Info(string msg)
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(msg);
#endif
    }

    public static void Error(Exception? ex)
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex?.ToString() + Environment.NewLine);
#endif
    }

    public static void Error(string msg, Exception? ex)
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.WriteLine(ex?.ToString() + Environment.NewLine);
#endif
    }
}