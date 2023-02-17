using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordCollection
{
	/// <summary>
	/// A collection of words that can be searched by available letters.
	/// Think "I have these letters, what words can I make?"
	/// </summary>
	public interface IWordCollection : ICollection<string>
	{
		/// <summary>
		/// Adds the words specified to the collection.
		/// </summary>
		/// <param name="words">The words to add. Cannot be null, but may be empty.</param>
		void AddRange(IEnumerable<string> words);

		/// <summary>
		/// Searches the collection for words that can be made from the specified characters.
		/// </summary>
		/// <param name="chars">The characters available to construct words.</param>
		/// <returns>The known words that can be made from <paramref name="chars"/>.</returns>
		IEnumerable<string> Search(IEnumerable<char> chars);

		/// <summary>
		/// Searches the collection for words that can be made from the specified characters.
		/// </summary>
		/// <param name="chars">The characters available to construct words.</param>
		/// <returns>The known words that can be made from <paramref name="chars"/>.</returns>
		IEnumerable<string> Search(string chars);
	}
}
