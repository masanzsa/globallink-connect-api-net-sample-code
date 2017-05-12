using GlobalLink.Connect;
using GlobalLink.Connect.Config;
using GlobalLink.Connect.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SampleUsage
{
    class Program
    {
        private const string SEND = "-send";
        private const string RETRIEVE = "-retrieve";

        private const string SOURCE_ROOT_FOLDER = "..\\..\\resources\\";


        private const string CONFIG_URL = "url";
        private const string CONFIG_USERNAME = "username";
        private const string CONFIG_PASSWORD = "password";
        private const string CONFIG_USERAGENT = "userAgent";
        private const string CONFIG_PROJECT = "project";
        private const string CONFIG_FILE_FORMAT = "fileFormatProfile";

        private const string CONFIG_SOURCE_LANGUAGE = "sourceLanguage";
        private const string CONFIG_TARGET_LANGUAGES = "targetLanguages";
        private const char CONFIG_SEPARATOR = ',';

        private const string DEFAULT_USER_AGENT = "glcl.sample.code";


        static void Main(string[] args)
        {



            string url = ConfigurationManager.AppSettings[CONFIG_URL];

            string[] submissionTickets = doSend();
            Console.Write("Attempting download in ");
            for (int a = 30; a >= 0; a--)
            {
                Console.CursorLeft = 23;
                Console.Write("{0} ", a);    // Add space to make sure to override previous contents
                System.Threading.Thread.Sleep(1000);
            }

            doRetrieve(submissionTickets);
            Console.WriteLine("Done, press any key to close");
            Console.ReadLine();




        }

        private static ProjectDirectorConfig getPDConfig()
        {
            ProjectDirectorConfig config = new ProjectDirectorConfig();
            string url = ConfigurationManager.AppSettings[CONFIG_URL];
            if (String.IsNullOrEmpty(url))
            {
                throw new Exception("Configuration option '" + CONFIG_URL + "' is not set");
            }
            else
            {
                config.url = url;
            }

            string username = ConfigurationManager.AppSettings[CONFIG_USERNAME];
            if (String.IsNullOrEmpty(username))
            {
                throw new Exception("Configuration option '" + CONFIG_USERNAME + "' is not set");
            }
            else
            {
                config.username = username;
            }

            string password = ConfigurationManager.AppSettings[CONFIG_PASSWORD];
            if (String.IsNullOrEmpty(password))
            {
                throw new Exception("Configuration option '" + CONFIG_PASSWORD + "' is not set");
            }
            else
            {
                config.password = password;
            }

            string userAgent = ConfigurationManager.AppSettings[CONFIG_USERAGENT];
            if (String.IsNullOrEmpty(userAgent))
            {
                System.Console.WriteLine(CONFIG_USERAGENT + " is not set. Using default '" + DEFAULT_USER_AGENT + "'.");
                userAgent = DEFAULT_USER_AGENT;
            }
            config.userAgent = userAgent;
            return config;
        }

        private static string[] doSend()
        {
            System.Console.WriteLine("Starting send");
            GLExchange pdClient = new GLExchange(getPDConfig());

            string sourceLanguage = ConfigurationManager.AppSettings[CONFIG_SOURCE_LANGUAGE];
            if (string.IsNullOrEmpty(sourceLanguage))
            {
                throw new Exception("Configuration option '" + CONFIG_SOURCE_LANGUAGE + "' is not set");
            }
            string targetLanguagesStr = ConfigurationManager.AppSettings[CONFIG_TARGET_LANGUAGES];
            if (string.IsNullOrEmpty(targetLanguagesStr))
            {
                throw new Exception("Configuration option '" + CONFIG_TARGET_LANGUAGES + "' is not set");
            }
            string fileFormat = ConfigurationManager.AppSettings[CONFIG_FILE_FORMAT];
            if (string.IsNullOrEmpty(fileFormat))
            {
                throw new Exception("Configuration option '" + CONFIG_FILE_FORMAT + "' is not set");
            }
            HashSet<String> targetLanguages = new HashSet<string>();
            if (targetLanguagesStr.IndexOf(CONFIG_SEPARATOR) > 0)
            {
                string[] langsArray = targetLanguagesStr.Split(new char[] { CONFIG_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
                if (langsArray.Length > 0)
                {
                    foreach (string lang in langsArray)
                    {
                        if (lang.Trim().Length > 1)
                        {
                            targetLanguages.Add(lang.Trim());
                        }
                    }
                }
            }
            else
            {
                if (targetLanguagesStr.Trim().Length > 1)
                {
                    targetLanguages.Add(targetLanguagesStr.Trim());
                }
            }
            if (targetLanguages.Count <= 0)
            {
                throw new Exception("Not able to find target languages");
            }

            string shortcode = ConfigurationManager.AppSettings[CONFIG_PROJECT];
            if (string.IsNullOrEmpty(shortcode))
            {
                throw new Exception("Configuration option '" + CONFIG_PROJECT + "' is not set");
            }
            Project project = pdClient.getProject(shortcode);

            DateTime dt = DateTime.Now;
            DateTime dueDate = dt.AddDays(5); // get this from the UI, i am hard coding to 5 days from now
            Submission submission = new Submission();

            submission.name = "GLC_Sample_Code_" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
            submission.project = project;
            submission.dueDate = dueDate;

            pdClient.initSubmission(submission);

            string folder = SOURCE_ROOT_FOLDER + sourceLanguage;
            string[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(folder);
            }
            catch (Exception ex)
            {
                throw new Exception("Directory '" + folder + "' not found." + ex.Message);
            }

            string report = "";
            foreach (string filePath in filePaths)
            {
                // read file into memory stream
                MemoryStream m = new MemoryStream();
                FileStream fileStream = File.OpenRead(filePath);
                m.SetLength(fileStream.Length);
                fileStream.Read(m.GetBuffer(), 0, (int)fileStream.Length);
                string filename = filePath.Substring(filePath.LastIndexOf("\\") + 1);

                Document document = new Document();
                document.fileformat = fileFormat;
                document.name = filename;
                document.sourceLanguage = sourceLanguage;
                document.targetLanguages = targetLanguages.ToArray<String>();
                document.setDataFromMemoryStream(m);

                string ticket = null;
                try
                {
                    ticket = pdClient.uploadTranslatable(document);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    m.Flush();
                    m.Close();
                    fileStream.Close();
                }
                if (ticket != null)
                {
                    report += filePath + " -> " + ticket + "\n";

                }
            }

            System.Console.WriteLine(report);

            string[] submissionTickets = pdClient.startSubmission();
            System.Console.WriteLine("Started Sub : " + submission.name + " [" + submissionTickets[0] + "]");

            return submissionTickets;
        }

        private static void doRetrieve(string[] submissionTickets)
        {
            GLExchange pdClient = new GLExchange(getPDConfig());
            string shortcode = ConfigurationManager.AppSettings[CONFIG_PROJECT];
            if (string.IsNullOrEmpty(shortcode))
            {
                throw new Exception("Configuration option '" + CONFIG_PROJECT + "' is not set");
            }
            string report = "";


            foreach (string submissionTicket in submissionTickets)
            {
                Target[] completedTargets = pdClient.getCompletedTargets(submissionTicket, 999);

                for (int i = 0; i < completedTargets.Length; i++)
                {
                    try
                    {
                        System.Console.WriteLine("\n\nAttempting to retrieve target\nname=" + completedTargets[i].documentName + "\ntargetLocale =" + completedTargets[i].targetLocale + "\ndocumentTicket=" + completedTargets[i].documentTicket);
                        string targetTicket = completedTargets[i].ticket;
                        MemoryStream translatedText = pdClient.downloadCompletedTarget(targetTicket);
                        saveFile(completedTargets[i], translatedText);
                        // Do the processing that you need with the translated XML.
                        System.Console.WriteLine("Download success\n");
                        // On successful processing, send confirmation
                        pdClient.sendDownloadConfirmation(completedTargets[i].ticket);
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Problem processing " + completedTargets[i].documentName);
                        System.Console.WriteLine(e);
                    }

                }

                // Get Cancelled
                Target[] cancelledtargets = pdClient.getCancelledTargetsBySubmissions(new[] { submissionTicket }, 999);
            }


            // Re-Delivery Check .. gives you all targets from all submissions 
            Target[] targets = pdClient.getCompletedTargets(999);
            for (int i = 0; i < targets.Length; i++)
            {

                System.Console.WriteLine("\nRedelivery to retrieve target\nname=" + targets[i].documentName + "\ntargetLocale =" + targets[i].targetLocale);

            }
            System.Console.WriteLine(report);


        }
        private static void saveFile(Target target, MemoryStream ms)
        {
            string folder = SOURCE_ROOT_FOLDER + target.targetLocale;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string fileName = folder + "\\" + target.documentName;
            using (FileStream file = new FileStream(fileName, FileMode.Create, System.IO.FileAccess.Write))
            {
                ms.WriteTo(file);
                file.Close();
                ms.Close();
            }
        }
    }
}
