using System;
using System.IO;
using Apteco.PullMarketing.Console.Models;
using Newtonsoft.Json;

namespace Apteco.PullMarketing.Console
{
  class Program
  {
    #region private methods
    private static void Main(string[] args)
    {
      try
      {
        if (args.Length == 0)
        {
          OutputHelpMessage();
          return;
        }

        switch (args[0].ToLower())
        {
          case "upload":
            if (args.Length < 3)
              OutputHelpMessage();
            else
              UploadFile(args[1], args[2]);

            break;

          case "example-upload-spec":
            if (args.Length < 2)
            {
              OutputHelpMessage();
            }
            else
            {
              string spec = CreateExampleUploadSpecFile(args[1]);
              if (spec == null)
                OutputHelpMessage();
              else
                System.Console.WriteLine(spec);
            }
            break;


          default:
            OutputHelpMessage();
            break;
        }
      }
      catch (Exception e)
      {
        System.Console.Error.WriteLine(e);
        throw;
      }
    }

    private static void UploadFile(string filename, string uploadSpecFilename)
    {
      using (FileStream fs = new FileStream(uploadSpecFilename, FileMode.Open, FileAccess.Read))
      {
        using (StreamReader sr = new StreamReader(fs))
        {
          string uploadSpecString = sr.ReadToEnd();
          UploadSpecification uploadSpec = JsonConvert.DeserializeObject<UploadSpecification>(uploadSpecString);

          string results = new Uploader().Upload(filename, uploadSpec);
          System.Console.WriteLine(results);
        }
      }
    }

    private static string CreateExampleUploadSpecFile(string dataStoreType)
    {
      if (dataStoreType == null)
        return null;

      UploadSpecification uploadSpec;
      switch (dataStoreType.ToLower())
      {
        case "dynamodb":
          uploadSpec = UploadSpecification.CreateExampleDynamoDbUploadSpecFile();
          break;

        case "mongodb":
          uploadSpec = UploadSpecification.CreateExampleMongoDbUploadSpecFile();
          break;

        default:
          return null;
      }

      return JsonConvert.SerializeObject(uploadSpec, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
    }

    private static void OutputHelpMessage()
    {
      System.Console.WriteLine("Usage:  dotnet Apteco.PullMarketing.Console.dll <command> [parameters]");
      System.Console.WriteLine();
      System.Console.WriteLine("The available commands are:");
      System.Console.WriteLine();
      System.Console.WriteLine("upload <file to upload> <control file>");
      System.Console.WriteLine("  Upload the given file using information specified in the given control file.");
      System.Console.WriteLine("");
      System.Console.WriteLine("example-upload-spec <datastore type> <control file>");
      System.Console.WriteLine("  Generate an example upload specification file for the given data store");
      System.Console.WriteLine("  (currently supported data stores are \"DynamoDb\" and \"MongoDb\")");
      System.Console.WriteLine("");
      System.Console.WriteLine("help");
      System.Console.WriteLine("  Output this message");
    }
    #endregion
  }
}