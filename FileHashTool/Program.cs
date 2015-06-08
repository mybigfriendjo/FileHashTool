using FileHashTool.hash;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHashTool {
	class Program {
		private static bool generateFlag = false;
		private static bool compareFlag = false;
		private static bool verboseFlag = false;
		private static bool outputToFileFlag = false;

		private static string sourceHash = null;
		private static string targetFolder = null;
		private static string outputFilePath = null;

		private static StringBuilder hashContent = null;
		private static int generateCounter = 0;

		static MurMurHash3 murmur = new MurMurHash3();

		static void Main(string[] args) {

			// ------- DELETE FOR PROD
			args = new string[] { "/g", "C:\\tools", "c:\\temp\\hash.txt" };
			// ------- END DELETE FOR PROD

			if (args == null || args.Length == 0) {
				writeError("Keine Argumente gefunden.");
				writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
				return;
			}
			for (int i = 0; i < args.Length; i++) {
				string cleanArgument = args[i].Trim().ToLower().Replace("/", "").Replace("-", "");

				switch (cleanArgument) {
					case "help":
					case "?":
						printHelp();
						return;
					case "v":
						verboseFlag = true;
						break;
					case "g":
						generateFlag = true;
						if (i == args.Length - 2) {
							writeError("'g' benoetigt zwei Pfadangaben.");
							writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
							return;
						}
						i++;
						targetFolder = args[i];
						i++;
						sourceHash = args[i];
						break;
					case "f":
						outputToFileFlag = true;
						if (i == args.Length - 1) {
							writeError("'f' benoetigt eine Pfadangabe.");
							writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
							return;
						}
						i++;
						outputFilePath = args[i];
						break;
					case "c":
						compareFlag = true;
						if (i == args.Length - 2) {
							writeError("'c' benoetigt 2 Pfadangaben.");
							writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
							return;
						}
						i++;
						sourceHash = args[i];
						i++;
						targetFolder = args[i];
						break;
					default:
						writeError("Unbekanntes Argument");
						writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
						return;
				}
			}

			if (generateFlag && compareFlag) {
				writeError("'c' und 'g' koennen nicht gleichzeitig ausgefuehrt werden");
				return;
			}
			if (generateFlag) {
				generateHash(targetFolder);
			}
			if (compareFlag) {
				compareHashWithPath();
			}
		}

		private static void compareHashWithPath() {
			throw new NotImplementedException();
		}

		private static void generateHash(string path) {
			if (!File.Exists(path) && !Directory.Exists(path)) {
				writeError("Pfad '" + path + "'wurde nicht gefunden!");
				return;
			}

			FileAttributes attr = File.GetAttributes(path);
			string[] filenames;
			int substringPos = 0;
			if (attr.HasFlag(FileAttributes.Directory)) {
				filenames = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
				substringPos = path.Length;
			} else {
				filenames = new string[] { path };
			}


			if (Directory.Exists(sourceHash)) {
				writeError("Der zweite Pfad von 'g' muss ein gueltiger Dateiname sein (kein Ordner)");
				return;
			}

			string dirToCreate = Path.GetDirectoryName(sourceHash);
			if (!Directory.Exists(dirToCreate)) {
				Directory.CreateDirectory(dirToCreate);
			}
			File.WriteAllText(sourceHash, ";" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + Environment.NewLine, Encoding.UTF8);

			hashContent = new StringBuilder();
			foreach (string fileToHash in filenames) {
				byte[] fileContent = File.ReadAllBytes(fileToHash);
				hashContent.AppendLine(fileToHash.Substring(substringPos+1) + "|" + fileContent.Length + "|" + Convert.ToBase64String(murmur.ComputeHash(fileContent)));
				generateCounter++;
				if (generateCounter % 100 == 0) {
					Console.WriteLine("Verarbeite " + generateCounter + " von " + filenames.Length + " Files");
				}
				if (generateCounter % 500 == 0) {
					File.AppendAllText(sourceHash, hashContent.ToString(), Encoding.UTF8);
					hashContent.Clear();
				}
			}
			File.AppendAllText(sourceHash, hashContent.ToString(), Encoding.UTF8);
			Console.WriteLine("Fertig mit der Verarbeitung von " + filenames.Length + " Files");
			Console.WriteLine("Hash-File wurde erstellt unter '" + sourceHash + "'");
		}

		private static void printHelp() {
			string appName = Process.GetCurrentProcess().ProcessName;

			ConsoleColor current = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;

			Console.WriteLine(appName + ".exe [/v] [/f <path>] (/g <path1> <path2>|/c <path1> <path2>)");
			Console.WriteLine("    v    - Verbose, aktiviert Ausgabe aller Files statt nur Fehler");
			Console.WriteLine("    f    - leitet die Ausgabe in das angegebene File um");
			Console.WriteLine("           (File wird ueberschrieben!)");
			Console.WriteLine("    g    - generiert ein Hash-File für die angegebene Datei.");
			Console.WriteLine("           Ist der Pfad ein Ordner wird von jeder Datei");
			Console.WriteLine("           (inkl. Unterordner) ein Hash erstellt");
			Console.WriteLine("           <path2> = Pfad wo die Hash-Datei erstellt wird.");
			Console.WriteLine("    c    - Validiert ein File oder einen Ordner anhand eines");
			Console.WriteLine("           vorher generiertem Hash-Files");
			Console.WriteLine("           <path1> = das Hash-File");
			Console.WriteLine("           <path2> der/das zu validierende Ordner/File");

			Console.ForegroundColor = current;
		}

		private static void writeError(string errorText) {
			ConsoleColor current = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(errorText);
			Console.ForegroundColor = current;
		}
	}
}
