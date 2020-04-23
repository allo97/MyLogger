using System;
using System.Collections.Generic;

namespace MyLogger
{

    public class LogType
    {
        public static readonly LogType FILE = new LogType("File", new FileLogger());
        public static readonly LogType DATABASE = new LogType("Database", new DatabaseLogger());
        public static readonly LogType CONSOLE = new LogType("Console", new ConsoleLogger());

        public string Name { get; }
        public Logger Logger { get; }

        // private constructor to forbid creating object from outside
        private LogType(string name, Logger logger)
        {
            Name = name;
            Logger = logger;
        }
    }

    public class MessageType
    {
        public static readonly MessageType INFORMATION = new MessageType("Information", ConsoleColor.White);
        public static readonly MessageType WARNING = new MessageType("Warning", ConsoleColor.Yellow);
        public static readonly MessageType ERROR = new MessageType("Error", ConsoleColor.Red);

        public string Name { get; set; }
        public ConsoleColor ConsoleColor { get; set; }

        // private constructor to forbid creating object from outside
        private MessageType(string name, ConsoleColor consoleColor)
        {
            Name = name;
            ConsoleColor = consoleColor;
        }
    }

    public abstract class Logger
    {
        protected readonly object synchronizeLock = new object();
        public abstract void LogMessage(string message, MessageType messageType);

    }

    public class FileLogger : Logger
    {
        public override void LogMessage(string message, MessageType messageType)
        {
            // Save log to file
            lock (synchronizeLock)
            {
                Console.WriteLine("Saving message to the file:\n" + messageType.Name + "\n" + message);
                string path = "LogFile" + DateTime.Now.ToShortDateString() + ".txt";
                //string path = System.Configuration.ConfigurationManager.AppSettings["LogFileDirectory"] + "LogFile" + DateTime.Now.ToShortDateString() + ".txt";

                // this method checks already if the file exists, if not it creates new file
                System.IO.File.AppendAllText(path, "\n" + DateTime.Now.ToShortDateString() + "\n" + messageType.Name + "\n" + message);
            }
        }
    }

    public class DatabaseLogger : Logger
    {

        public override void LogMessage(string message, MessageType messageType)
        {
            // save log to database

            lock (synchronizeLock)
            {
                Console.WriteLine("Saving message to the database:\n" + messageType.Name + "\n" + message);


                //System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.AppSettings["ConnectionString"]);
                //connection.Open();

                //System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("Insert into Log Values('" + messageType.Name + "', " + message + ")");
                //command.ExecuteNonQuery();
            }
        }
    }

    public class ConsoleLogger : Logger
    {
        public override void LogMessage(string message, MessageType messageType)
        {
            // print log to console

            lock (synchronizeLock)
            {
                Console.ForegroundColor = messageType.ConsoleColor;
                Console.WriteLine(DateTime.Now.ToShortDateString() + " " + message);
                Console.ResetColor();
            }
        }
    }

    // now I want to initialize this loggers, when I have loggerTypes
    public class JobLogger
    {
        // Created HashSet to remove duplicates
        private HashSet<LogType> _loggerTypes { get; }
        private HashSet<MessageType> _messageTypes { get; }

        // params keyword doesn't work here because I have to pass two different arrays 
        public JobLogger(LogType[] loggerTypes, MessageType[] messageTypes)
        {
            _loggerTypes = new HashSet<LogType>(loggerTypes);
            _messageTypes = new HashSet<MessageType>(messageTypes);
        }

        // Message should not be both types for example information and warning
        public void LogMessage(string message, MessageType messageType)
        {
            // it's better to not use exception here, just simple error condition
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("Exception occured: Message is Null or Empty!");
                return;
            }

            message.Trim();

            // Should I return any message if message type is bad for logger? Maybe just return to the console this info
            if (_messageTypes.Contains(messageType))
            {
                foreach (LogType logType in _loggerTypes)
                {
                    logType.Logger.LogMessage(message, messageType);
                }
            }
            else
            {
                Console.WriteLine("Exception occured: MessageType: " + messageType.Name + " is inappropriate for this logger! Message won't be logged!");
            }

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            JobLogger errorlogger = new JobLogger(new LogType[] { LogType.FILE, LogType.DATABASE, LogType.CONSOLE },
                                                new MessageType[] { MessageType.ERROR });

            JobLogger infologger = new JobLogger(new LogType[] { LogType.FILE, LogType.DATABASE, LogType.CONSOLE },
                                                new MessageType[] { MessageType.INFORMATION });

            JobLogger warninglogger = new JobLogger(new LogType[] { LogType.FILE, LogType.DATABASE, LogType.CONSOLE },
                                                new MessageType[] { MessageType.WARNING });

            JobLogger everyLogger = new JobLogger(new LogType[] { LogType.DATABASE, LogType.CONSOLE },
                                                new MessageType[] { MessageType.ERROR, MessageType.INFORMATION, MessageType.WARNING });

            JobLogger every2Logger = new JobLogger(new LogType[] { },
                                               new MessageType[] { MessageType.ERROR });

            JobLogger badLogger = new JobLogger(new LogType[] { LogType.FILE, LogType.DATABASE, LogType.CONSOLE },
                                                new MessageType[] { MessageType.INFORMATION, MessageType.WARNING });


            // check for null or empty
            string message = "";

            errorlogger.LogMessage(message, MessageType.ERROR);

            message = null;

            // successfull logs
            errorlogger.LogMessage(message, MessageType.ERROR);

            errorlogger.LogMessage("To jest moja wiadomość, ktora jest typu ERROR", MessageType.ERROR);

            infologger.LogMessage("To jest moja wiadomość, ktora jest typu INFORMATION", MessageType.INFORMATION);

            warninglogger.LogMessage("To jest moja wiadomość, ktora jest typu WARNING", MessageType.WARNING);

            everyLogger.LogMessage("To jest moja wiadomość, ktora jest typu warning ale dla loggera ktory moze wypisywac do DATABASE i CONSOLE", MessageType.WARNING);

            // it won't save message to log
            badLogger.LogMessage("To jest moja wiadomość, ktora jest typu ERROR ale dla loggera ktory nie moze wypisywac typu ERROR wiec nie wypisze tej wiadomosci", MessageType.ERROR);

            every2Logger.LogMessage("powinien byc blad", MessageType.ERROR);


        }
    }

}
