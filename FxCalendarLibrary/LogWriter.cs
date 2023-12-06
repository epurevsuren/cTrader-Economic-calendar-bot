using System;
using System.IO;
using System.Reflection;


public class LogWriter
{
    private string logPath = string.Empty;
    private string fName = string.Empty;
    private TextWriter txtWriter;

    public LogWriter(string fileName)
    {
        try
        {
            fName = fileName;
            logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            StreamWriter w = File.AppendText(logPath + "\\log\\" + fName + ".txt");
            txtWriter = w;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }       
    }

    public void WriteLine(string logMessage)
    {
        try
        {
            Console.WriteLine(logMessage);
            txtWriter.WriteLine(logMessage);
            txtWriter.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void Write(string logMessage)
    {
        try
        {
            Console.Write(logMessage);
            txtWriter.Write(logMessage);
            txtWriter.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void ErrorLog(string logMessage)
    {
        try
        {
            txtWriter.Write("\r\nLog Entry : ");
            txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            txtWriter.WriteLine("  :");
            txtWriter.WriteLine("  :{0}", logMessage);
            txtWriter.WriteLine("-------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}