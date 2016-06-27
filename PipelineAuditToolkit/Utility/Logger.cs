using System;

namespace PipelineAuditToolkit.Utility
{
    internal class Logger : ILogger
    {
        private readonly bool _printDebug;
        private readonly bool _printErrors;

        public Logger(bool printDebug, bool printErrors)
        {
            _printDebug = printDebug;
            _printErrors = printErrors;
        }

        public void WriteDebug(string message)
        {
            if (_printDebug)
            {
                Console.WriteLine($"Debug: {message}"); 
            }
        }

        public void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteError(string message)
        {
            if (_printErrors)
            {
                Console.WriteLine($"ERROR: {message}"); 
            }
        }
    }
}