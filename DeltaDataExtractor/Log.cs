using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaDataExtractor
{
    public static class Log
    {
        public static void WriteHeader(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            DoWritePayload(msg);
        }

        public static void WriteSuccess(string topic, string msg)
        {
            DoLog(topic, msg, "S", ConsoleColor.Green);
        }

        public static void WriteError(string topic, string msg)
        {
            DoLog(topic, msg, "E", ConsoleColor.Red);
        }

        public static void WriteWarning(string topic, string msg)
        {
            DoLog(topic, msg, "W", ConsoleColor.Yellow);
        }

        public static void WriteInfo(string topic, string msg)
        {
            DoLog(topic, msg, "I", ConsoleColor.White);
        }

        private static void DoLog(string topic, string msg, string type, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            string payload = $"{type}: [{topic}] ({DateTime.Now.ToLongTimeString()}) {msg}";
            DoWritePayload(payload);
        }

        private static void DoWritePayload(string payload)
        {
            //Write to screen
            Console.WriteLine(payload);

            //Write to file
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload + "\n");
            Program.log.Write(payloadBytes, 0, payloadBytes.Length);
        }
    }
}
