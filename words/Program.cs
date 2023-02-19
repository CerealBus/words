using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WordCollection;

namespace words
{
	static class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 0)
			{
				Console.Write    ("Yeah, no. Sorry, but this little app");
				Console.WriteLine("doesn't accept arguments.");;
				Console.Write    ("It'll work just fine if you redirect");
				Console.WriteLine("standard input to it though.");
				Console.WriteLine("Here's the usag:");
				WriteUsage();
				Environment.ExitCode = 0xA0; // ERROR_BAD_ARGUMENTS
				return;
			}
			
			IWordCollection words = new WordTree();

			using (var wordReader = new StreamReader(GetWordStream()))
			while (!wordReader.EndOfStream)
				words.Add(wordReader.ReadLine());

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
					for (var i = 3; i <= 8; ++i)
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
			Console.Write("You can also put a ");
			WriteBold("quantifier");
			Console.WriteLine(" first to limit word lengths.");
			Console.WriteLine("Examples:");
			Console.Write("    ");
			WriteBold("{=m}");
			Console.WriteLine("letters : words of length m");
			Console.Write("    ");
			WriteBold("{>m}");
			Console.WriteLine("letters : words of at least length m");
			Console.Write("    ");
			WriteBold("{<m}");
			Console.WriteLine("letters : words of at most length m");
			Console.Write("  ");
			WriteBold("{>m<n}");
			Console.WriteLine("letters : words with length netween m and n");
			Console.Write("If no ");
			WriteBold("quantifier");
			Console.WriteLine(" is given, then word length will be be between 3 and 8.");
			Console.WriteLine();
			Console.WriteLine("Enter ? to see this help.");
			Console.WriteLine("Enter ^C, ^D, or ^Z to quit.");
		}

		/// <summary>
		/// Retrieves the the list of known words as a stream.
		/// </summary>
		static Stream GetWordStream()
		{
			var t = typeof(Program);
			//return t.Assembly.GetManifestResourceStream(t, "dictionary");
			// TODO: runtime dictionary selection
			// TODO: remove extra DefineConstant from .csproj
#if ASPELL
			#warning using aspell dictionary
			var resource = "dictionary";
			Console.Write("Using the ");
			WriteBold("aspell");
			Console.WriteLine(" dictionary");
#elif SCROGGLE
			#warning using scroggle dictionary
			var resource = "scroggle";
			Console.Write("Using the ");
			WriteBold("scroggle");
			Console.WriteLine(" dictionary");
#else
			#error No dictionary selected. Define either ASPELL or SCROGGLE.
			// defined so VS code would stop bitching about this
			// still complains about the #error though
			// ¯\_(ツ)_/¯
			var resource = "";
#endif
			return t.Assembly.GetManifestResourceStream(t, resource);
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
