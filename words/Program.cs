using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WordCollection;

namespace words
{
	static class Program
	{
		/// <summmary>
		/// System proc exit code for bad arguments.
		/// </summary>
		/// <seealso href="https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-#ERROR_BAD_ARGUMENTS">
		/// ERROR_BAD_ARGUMENTS
		/// </seealso>
		private const int ERROR_BAD_ARGUMENTS = 0xA0;

		static void Main(string[] args)
		{
			// Switch to a parsing lib if args get any more complex.
			if (args.Length > 1)
			{
				Console.WriteLine(
					$"""
					This little app only takes, at most, one argument: a dictionary name.
					Available dictionaries are:
					  {GetAvailableDictionaries().Aggregate((list, next) => $"{list}\n  {next}")}
					If not specified, the aspell dictionary will be used by default.
					Input is received on standard input, and redirection
					works just fine.
					Here's the usage:
					"""
				);
				WriteUsage();
				Environment.ExitCode = ERROR_BAD_ARGUMENTS;
				return;
			}

			// Check that the specified dictionary is valid.
			string dictArg = (args.Length == 0 ? "aspell" : args[0]).ToLowerInvariant();
			if (!GetAvailableDictionaries().Contains(dictArg))
			{
				Console.WriteLine(
					$"""
					The specified dictionnary ("{dictArg}") is invalid.
					Available dictionaries are:
					  {GetAvailableDictionaries().Aggregate((list, next) => $"{list}\n  {next}")}
					"""
				);
				Environment.ExitCode = ERROR_BAD_ARGUMENTS;
				return;
			}
			
			IWordCollection words = new WordTree();

			using (var wordReader = new StreamReader(GetWordStream(dictArg)))
			while (!wordReader.EndOfStream)
				words.Add(wordReader.ReadLine()!);

			var inputExpression = new Regex(@"
				^
				(?:{(?:
					(?<equality>=(?<exact>[0-9]+))
					|(?<inequality>\>(?<lower>[0-9]+))
					|(?<inequality>\<(?<upper>[0-9]+))
					|(?<inequality>\>(?<lower>[0-9]+)\<(?<upper>[0-9]+))
				)})?
				(?<letters>[a-zA-Z]+\*?)
				$",
				RegexOptions.IgnorePatternWhitespace
			);
			
			while (true)
			{
				if (!Console.IsInputRedirected)
				{
					Console.Write("> ");
				}
				string? chars = Console.ReadLine();

				if (chars == null) // end of stream, or user did a ^Z
					break;

				// Strip whitespace. Allows some notational leeway, as
				// well as making redirected standard in easier.
				chars = Regex.Replace(chars, @"\s+", "");
				Match match; // assigned as side effect in a conditional below

			    if (chars.Length == 1 && chars[0] == '\x04') // ^D, AKA end of transmission
					break;
				else if ((chars.Length == 1 && chars[0] == '?'))
					WriteUsage();
				else if (!(match = inputExpression.Match(chars)).Success)
				{
					WriteColor(ConsoleColor.Red, "Invalid input: expected an optional quantifier followed by letters");
					Console.WriteLine();
				}
				else
				{
					int low = 3, high = 8; // default limits
					// todo: extract to function
					// the RE shields us from having to check
					// for weird quantifier combinations
					Group group;
					if ((group = match.Groups["exact"]).Success)
						low = high = int.Parse(group.Value);
					if ((group = match.Groups["lower"]).Success)
						low = int.Parse(group.Value);
					if ((group = match.Groups["upper"]).Success)
						high = int.Parse(group.Value);

					chars = match.Groups["letters"].Value;
					var list = words.Search(chars)
						.Where(s => low <= s.Length && s.Length <= high)
						.OrderBy(s => s)
						.Distinct()
						.ToList();
					for (var i = low; i <= high; ++i)
						foreach (var word in list.Where(s => s.Length == i))
							Console.WriteLine(word);
				}
			}
		}

		static void WriteUsage()
		{
			Console.Write("Enter a string of ");
			WriteBold("letters");
			Console.WriteLine(" to see what words they can make.");
			Console.WriteLine("You can also:");
			Console.Write("  put a ");
			WriteBold("quantifier");
			Console.WriteLine(" first to limit word lengths.");
			Console.Write("  put a ");
			WriteBold("wildcard");
			Console.Write(" on the end to match ");
			WriteBold("anything");
			Console.WriteLine(".");
			Console.WriteLine("Examples:");
			Console.Write("    ");
			WriteBold("{=m}");
			Console.WriteLine("letters  : words of length m");
			Console.Write("    ");
			WriteBold("{>m}");
			Console.WriteLine("letters  : words of at least length m");
			Console.Write("    ");
			WriteBold("{<m}");
			Console.WriteLine("letters  : words of at most length m");
			Console.Write("  ");
			WriteBold("{>m<n}");
			Console.WriteLine("letters  : words with length netween m and n");
			Console.Write("        letters");
			WriteBold("*");
			Console.WriteLine(" : words that start with any of the provided letters");
			Console.Write("If no ");
			WriteBold("quantifier");
			Console.WriteLine(" is given, then word length will be be between 3 and 8.");
			Console.Write("A ");
			WriteBold("wildcard");
			Console.WriteLine(" will dump every word starting with one of the given letters,");
			Console.WriteLine("so you may want to filter results through a regular expression.");
			Console.WriteLine();
			Console.WriteLine("Enter ? to see this help.");
			Console.WriteLine("Enter ^C, ^D, or ^Z to quit.");
		}

		static IEnumerable<string> GetAvailableDictionaries() {
			var asm = typeof(Program).Assembly;
			var dict_prefix = $"{typeof(Program).Assembly.GetName().Name}.dictionaries.";

			return
				from resourceName in asm.GetManifestResourceNames()
				where resourceName.StartsWith(dict_prefix)
				select resourceName.Substring(dict_prefix.Length);
		}

		/// <summary>
		/// Retrieves a dictioonary's words as a stream.
		/// </summary>
		/// <excepion cref="System.ArgumentException">
		/// if the specified dictionary can't be found
		/// </exception>
		static Stream GetWordStream(string dictName)
		{
			var t = typeof(Program);
			Stream stream;
			try
			{
				stream = t.Assembly.GetManifestResourceStream(t, $"dictionaries.{dictName}")!;
				if (stream == null)
					throw new ArgumentException($"Dictionary \"{dictName}\" could not be loaded.");
			}
			catch (Exception e)
			{
				throw new ArgumentException($"Dictionary \"{dictName}\" could not be loaded.", e);
			}

			Console.Write("Using the ");
			WriteBold(dictName);
			Console.WriteLine(" dictionary");

			return stream;
		}

		/// <summary>
		/// Writes something to Console.Out with a different intensity.
		/// </summary>
		/// <param name="value">The string to emphasize.</param>
		/// <remarks>
		/// Inverts output colors if the bold text is going have the
		/// same foreground and background.
		/// </remarks>
		static void WriteBold(string value)
		{
			var bg = Console.BackgroundColor;

			// flip the high bit of the color nibble
			// to change its intensity
			var bold = (ConsoleColor)(((int)Console.ForegroundColor) ^ 0x08);

			// Flip the background color when its  the same as the bold color.
			if (bold == bg)
				Console.BackgroundColor = (ConsoleColor)(((int)~bg) & 0x0f);

			WriteColor(bold, value);

			Console.BackgroundColor = bg;
		}

		/// <summary>
		/// Writes something to Console.Out with a specific foreground color.
		/// </summary>
		/// <param name="color">The color to write with.</param>
		/// <param name="value">The string to write.</param>
		static void WriteColor(ConsoleColor color, string value)
		{
			var fg = Console.ForegroundColor;

			Console.ForegroundColor = color;
			Console.Write(value);

			Console.ForegroundColor = fg;
		}
	}
}
