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
		private static int notFoundCounter = 0;
		private static int differentCounter = 0;

		static MurMurHash3 murmur = new MurMurHash3();

		static void Main(string[] args) {
			args = new string[] { "c", "c:\\temp\\hash.txt", "c:\\tools" };

			if (args == null || args.Length == 0) {
				writeError("Keine Argumente gefunden.");
				writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
				return;
			}
			for (int i = 0; i < args.Length; i++) {
				string cleanArgument = args[i].Trim().ToLower().Replace("/", "").Replace("-", "");

				switch (cleanArgument) {
					case "help": // show help
					case "?":
						printHelp();
						return;
					case "v": // verbose output
						verboseFlag = true;
						break;
					case "g": // generate
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

						if (!pathExists(targetFolder)) {
							writeError("Pfad '" + targetFolder + "' wurde nicht gefunden!");
							return;
						}

						break;
					case "f": // output file
						outputToFileFlag = true;
						if (i == args.Length - 1) {
							writeError("'f' benoetigt eine Pfadangabe.");
							writeError("Aufruf mit /? oder /help um vorhandene Optionen zu zeigen");
							return;
						}
						i++;
						outputFilePath = args[i];

						if (isDirectory(outputFilePath)) {
							writeError("Pfad '" + outputFilePath + "'ist ein existierender Ordner!");
							return;
						}
						string dirPath = Path.GetDirectoryName(outputFilePath);
						if (!Directory.Exists(dirPath)) {
							Directory.CreateDirectory(dirPath);
						}
						File.WriteAllText(outputFilePath, "", Encoding.UTF8);
						break;
					case "c": // compare
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

						if (!File.Exists(sourceHash)) {
							writeError("Pfad '" + sourceHash + "' existiert nicht oder ist kein File!");
							return;
						}
						if (isDirectory(sourceHash)) {
							writeError("Pfad '" + sourceHash + "' ist ein Ordner!");
							return;
						}
						if (!pathExists(targetFolder)) {
							writeError("Pfad '" + targetFolder + "' wurde nicht gefunden!");
							return;
						}

						break;
					default: // unknown argument
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
				generateHash();
			}
			if (compareFlag) {
				compareHashWithPath();
			}
		}

		private static void compareHashWithPath() {
			string[] hashLines = File.ReadAllLines(sourceHash, Encoding.UTF8);
			List<string> cleanLines = cleanupLines(hashLines);

			if (cleanLines.Count == 0) {
				writeError("Keine Eintraege im HashFile");
				return;
			}
			if (cleanLines.Count == 1) {
				write("Nur ein Eintrag im Hash-File. Pfad wird als Filepfad interpretiert");
				if (!File.Exists(targetFolder)) {
					writeError("Angegebenes File existiert nicht");
					return;
				}
				string[] fileInfo = cleanLines[0].Split(new string[] { "|" }, StringSplitOptions.None);
				compare(fileInfo[2], fileInfo[1], targetFolder);
				write("Vergleichen von '" + targetFolder + "' abgeschlossen");
			} else {
				if (!Directory.Exists(targetFolder)) {
					writeError("Angegebener Ordnerpfad existiert nicht");
					return;
				}

				if (targetFolder.EndsWith("\\") || targetFolder.EndsWith("/")) {
					targetFolder = targetFolder.Substring(0, targetFolder.Length - 1);
				}

				// DELETE
				File.WriteAllText("C:\\temp\\bytesOf1.txt", "", Encoding.UTF8);
				// DELETE
				foreach (string line in cleanLines) {
					string[] fileInfo = line.Split(new string[] { "|" }, StringSplitOptions.None);
					compare(fileInfo[2], fileInfo[1], targetFolder + "\\" + fileInfo[0]);
				}
				write("Vergleichen von " + cleanLines.Count + " Dateien abgeschlossen");
				write(notFoundCounter + " Dateien nicht gefunden");
				write(differentCounter + " Dateien unterschiedlich");
			}
		}

		private static void compare(string hash, string size, string filename) {
			if (!File.Exists(filename)) {
				writeCompareError("'" + filename + "' existiert nicht.");
				notFoundCounter++;
				return;
			}
			byte[] contentBytes = File.ReadAllBytes(filename);

			// --- DELETE
			foreach (byte part in contentBytes) {
				File.AppendAllText("C:\\temp\\bytesOf1.txt", part + ",", Encoding.UTF8);
			}
			File.AppendAllText("C:\\temp\\bytesOf1.txt", Environment.NewLine, Encoding.UTF8);
			// --- DELETE

			if (Int32.Parse(size) != contentBytes.Length) {
				writeCompareError("'" + filename + "' hat eine andere Dateigroesse.");
				differentCounter++;
				return;
			}
			string calculatedHash = getHash(contentBytes);
			if (!hash.Equals(calculatedHash)) {
				writeCompareError("'" + filename + "' hat einen anderen Hash. ('" + hash + "' <> '" + calculatedHash + "')");
				differentCounter++;
				return;
			}
			if (verboseFlag) {
				writeCompareInfo("'" + filename + "' hat gleichen Hash. ('" + hash + "' <> '" + calculatedHash + "')");
			}
		}

		private static List<string> cleanupLines(string[] hashLines) {
			List<string> temp = new List<string>();
			foreach (string line in hashLines) {
				if (line.Trim().Length == 0) {
					continue;
				}
				if (line.Trim().StartsWith(";")) {
					continue;
				}
				temp.Add(line);
			}
			return temp;
		}

		private static bool isDirectory(string path) {
			if (!pathExists(path)) {
				return false;
			}
			FileAttributes attr = File.GetAttributes(path);
			if (attr.HasFlag(FileAttributes.Directory)) {
				return true;
			}
			return false;
		}

		private static bool pathExists(string path) {
			return (Directory.Exists(path) || File.Exists(path));
		}

		private static void generateHash() {
			string[] filenames;
			int substringPos = 0;
			if (isDirectory(targetFolder)) {
				filenames = Directory.GetFiles(targetFolder, "*", SearchOption.AllDirectories);
				substringPos = targetFolder.Length;
			} else {
				filenames = new string[] { targetFolder };
			}

			if (isDirectory(sourceHash)) {
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
				hashContent.AppendLine(fileToHash.Substring(substringPos + 1) + "|" + fileContent.Length + "|" + getHash(fileContent));
				generateCounter++;
				if (generateCounter % 100 == 0) {
					write("Verarbeite " + generateCounter + " von " + filenames.Length + " Files");
				}
				if (generateCounter % 500 == 0) {
					File.AppendAllText(sourceHash, hashContent.ToString(), Encoding.UTF8);
					hashContent.Clear();
				}
			}
			File.AppendAllText(sourceHash, hashContent.ToString(), Encoding.UTF8);
			write("Fertig mit der Verarbeitung von " + filenames.Length + " Files");
			write("Hash-File wurde erstellt unter '" + sourceHash + "'");
		}

		private static string getHash(byte[] fileContent) {
			return Convert.ToBase64String(murmur.ComputeHash(fileContent));
		}


		private static void printHelp() {
			string appName = Process.GetCurrentProcess().ProcessName;

			StringBuilder helpText = new StringBuilder();

			helpText.AppendLine(appName + ".exe [/v] [/f <path>] (/g <path1> <path2>|/c <path1> <path2>)");
			helpText.AppendLine("    v    - Verbose, aktiviert Ausgabe aller Files statt nur Fehler");
			helpText.AppendLine("    f    - leitet die Ausgabe in das angegebene File um");
			helpText.AppendLine("           (File wird ueberschrieben!)");
			helpText.AppendLine("    g    - generiert ein Hash-File für die angegebene Datei.");
			helpText.AppendLine("           Ist der Pfad ein Ordner wird von jeder Datei");
			helpText.AppendLine("           (inkl. Unterordner) ein Hash erstellt");
			helpText.AppendLine("           <path2> = Pfad wo die Hash-Datei erstellt wird.");
			helpText.AppendLine("    c    - Validiert ein File oder einen Ordner anhand eines");
			helpText.AppendLine("           vorher generiertem Hash-Files");
			helpText.AppendLine("           <path1> = das Hash-File");
			helpText.AppendLine("           <path2> der/das zu validierende Ordner/File");

			setConsoleColor(ConsoleColor.Yellow);
			textOutput(helpText.ToString());
			unsetConsoleColor();
		}

		private static ConsoleColor currentConsoleColor = Console.ForegroundColor;

		private static void setConsoleColor(ConsoleColor color) {
			ConsoleColor currentConsoleColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
		}
		private static void unsetConsoleColor() {
			Console.ForegroundColor = currentConsoleColor;
		}

		private static void write(string text) {
			setConsoleColor(ConsoleColor.White);
			textOutput(text);
			unsetConsoleColor();
		}

		private static void writeCompareError(string text) {
			setConsoleColor(ConsoleColor.Red);
			textOutput("[COMPERR] " + text);
			unsetConsoleColor();
		}

		private static void writeCompareInfo(string text) {
			setConsoleColor(ConsoleColor.Green);
			textOutput("[COMPINF] " + text);
			unsetConsoleColor();
		}

		private static void writeError(string text) {
			setConsoleColor(ConsoleColor.Red);
			textOutput("[ERROR] " + text);
			unsetConsoleColor();
		}

		private static void textOutput(string text) {
			if (outputToFileFlag == false) {
				Console.WriteLine(text);
			} else {
				File.AppendAllText(outputFilePath, text + Environment.NewLine, Encoding.UTF8);
			}
		}

	}
}
