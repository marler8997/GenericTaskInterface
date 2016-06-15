using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Gti
{
    class ErrorMessageException : Exception
    {
        public ErrorMessageException(String message)
            : base(message)
        {
        }
    }
    class GtiGui
    {
        static List<String> ParseCommandLine(String[] args)
        {
            List<String> nonOptionArgs = new List<String>(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                String arg = args[i];

                if (arg.Length >= 1 && arg[0] == '-')
                {
                    throw new ErrorMessageException(String.Format("Unknown command-line option '{0}'", arg));
                }
                else
                {
                    nonOptionArgs.Add(arg);
                }
            }
            return nonOptionArgs;
        }

        static void Usage()
        {
            Console.WriteLine("GtiGui.exe [options] <gti-file>");
        }
        [STAThread]
        static Int32 Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Usage();
                    return 0;
                }

                List<String> nonOptionArgs = ParseCommandLine(args);
                if (nonOptionArgs.Count <= 0)
                {
                    Console.WriteLine("Error: not enough command line arguments");
                    return 1;
                }
                String gtiFilename = nonOptionArgs[0];

                GtiXml gtiXml;
                {
                    var serializer = GtiXml.CreateSerializer();
                    using (XmlReader xmlReader = XmlReader.Create(new FileStream(gtiFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        gtiXml = (GtiXml)serializer.Deserialize(xmlReader, GtiXml.SerializerEvents);
                    }
                }
                gtiXml.FinalizeDeserialization();



                GtiForm.Start(gtiFilename, gtiXml);
                return 0;
            }
            catch(ErrorMessageException e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
        }
    }
}
