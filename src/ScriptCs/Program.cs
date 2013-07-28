﻿using System;
using System.IO;
using Autofac;
using PowerArgs;
using ScriptCs.Command;
using System.Linq;
using ScriptCs.Contracts;
using ScriptCs.Engine.Roslyn;

namespace ScriptCs
{
    internal static class Program
    {
        private static int Main(string[] args) 
        {
            string[] scriptArgs;
            ScriptCsArgs.SplitScriptArgs(ref args, out scriptArgs);
 
            var commandArgs = ParseArguments(args);
            var configurator = new LoggerConfigurator(commandArgs.LogLevel);
            var console = new ScriptConsole();
            configurator.Configure(console);
            var logger = configurator.GetLogger();
 
            var scriptServicesBuilder = new ScriptServicesBuilder(console, logger)   .
                Debug(commandArgs.Debug).
                LogLevel(commandArgs.LogLevel).
                ScriptName(commandArgs.ScriptName).
                Repl(commandArgs.Repl);
            
            scriptServicesBuilder.LoadModules(Path.GetExtension(commandArgs.ScriptName), commandArgs.Modules);
            var scriptServiceRoot = scriptServicesBuilder.Build();

            var commandFactory = new CommandFactory(scriptServiceRoot);
            var command = commandFactory.CreateCommand(commandArgs, scriptArgs);

            var result = command.Execute();

            return result == CommandResult.Success ? 0 : -1;
        }

        private static ScriptCsArgs ParseArguments(string[] args)
        {
            const string UnexpectedArgumentMessage = "Unexpected Argument: ";

            //no args initialized REPL
            if (args.Length <= 0) return new ScriptCsArgs { Repl = true, LogLevel = LogLevel.Info};

            try
            {
                var scriptcsArgs = Args.Parse<ScriptCsArgs>(args);
                
                //if there is only 1 arg and it is a loglevel, it's also REPL
                if (args.Length == 2 && args.Any(x => x.ToLowerInvariant() == "-log"))
                {
                    scriptcsArgs.Repl = true;
                }

                return scriptcsArgs;
            }
            catch (ArgException ex)
            {
                if (ex.Message.StartsWith(UnexpectedArgumentMessage))
                {
                    var token = ex.Message.Substring(UnexpectedArgumentMessage.Length);
                    Console.WriteLine("Parameter \"{0}\" is not supported!", token);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return new ScriptCsArgs();
        }
    }
}