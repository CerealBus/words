using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordCollection
{
	/// <remarks>
	/// Some of this code is seriously flawed and disgusting.
	/// </remarks>
	public class WordTree : IWordCollection
	{
		class Node
		{
			public char value;
			public bool IsWord; // NOTE: IsWord does not imply IsLeaf!
			readonly Node[] _children = new Node[26];

			public Node this[char index]
			{
				get
				{
					//if (index < 'A' || 'Z' < index)
					//	throw new ArgumentOutOfRangeException("index", index, "Index must be between A and Z");
					return _children[index - 'A'];
				}

				set
				{
					//if (index < 'A' || 'Z' < index)
					//	throw new ArgumentOutOfRangeException("index", index, "Index must be between A and Z");
					_children[index - 'A'] = value;
				}
			}
		}

		private Node _root = new Node();

		public WordTree()
		{
			Count = 0;
		}

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public IEnumerator<string> GetEnumerator()
		{
			var word = new StringBuilder();
			var stack = new Stack<Tuple<Node, char>>();
			var node = _root;
			var c = 'A';

			while (true)
			{
				if (c > 'Z')
				{
					if (stack.Count == 0)
						break;
					if (node.IsWord)
						yield return word.ToString();
					word.Length -= 1;
					var frame = stack.Pop();
					node = frame.Item1;
					c = frame.Item2;
				}
				else
				{
					var child = node[c];
					if (child != null)
					{
						word.Append(child.value);
						stack.Push(Tuple.Create(node, c));
						node = child;
						c = '@'; // @ == A - 1
					}
				}

				c = (char)(c + 1);
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<string>

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <param name="word">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public void Add(string word)
		{
			if (word == null)
				throw new ArgumentNullException("word");
			else if (word.Length == 0)
				throw new ArgumentOutOfRangeException("word", "cannot be empty");

			var current = _root;

			var chars = new Queue<char>(word.ToUpper());
			while (chars.Count > 0)
			{
				var c = chars.Dequeue();
				var next = current[c];
				if (next == null)
				{
					next = new Node { value = c };
					current[c] = next;
				}
				current = next;
			}

			if (!current.IsWord)
			{
				current.IsWord = true;
				Count += 1;
			}
		}

		/// <summary>
		/// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
		public void Clear()
		{
			_root = new Node();
			Count = 0;
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="word"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
		/// </returns>
		/// <param name="word">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		public bool Contains(string word)
		{
			if (word == null)
				throw new ArgumentNullException("word");

			var current = _root;
			foreach (var c in word.ToUpper())
			{
				current = current[c];
				if (current == null)
					return false;
			}
			return current.IsWord;
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
		public void CopyTo(string[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			else if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException("arrayIndex", "index must be non-negative");

			long free = array.Length - arrayIndex;

			foreach (var word in this)
			{
				if (free <= 0)
					throw new ArgumentException("There is not enough space in the array after the offset for the copy", "array");

				array[arrayIndex] = word;
				arrayIndex += 1;
				free -= 1;
			}
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// true if <paramref name="word"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="word"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="word">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public bool Remove(string word)
		{
			if (word == null)
				throw new ArgumentNullException("word");
			else if (word.Length == 0)
				throw new ArgumentOutOfRangeException("word", "cannot be empty");

			var current = _root;
			foreach (var c in word.ToUpper())
			{
				current = current[c];
				if (current == null)
					return false;
			}

			// TODO: Remove node if it was a word with no children.
			if (current.IsWord)
			{
				Count -= 1;
				current.IsWord = false;
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		public int Count { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly
		{
			// TODO: Allow setting an initialized WordCollection to ReadOnly.
			get { return false; }
		}

		#endregion

		#region Implementation of IWordCollection

		/// <summary>
		/// Adds the words specified to the collection.
		/// </summary>
		/// <param name="words">The words to add. Cannot be null, but may be empty.</param>
		public void AddRange(IEnumerable<string> words)
		{
			// TODO: This is very not optimal.
			//       Should be able to either remember leaf nodes,
			//       or work on a sorted list so we don't have to
			//       traverse the tree on every add.
			foreach (var word in words)
			{
				Add(word);
			}
		}

		/// <summary>
		/// Searches the collection for words that can be made from the specified characters.
		/// </summary>
		/// <param name="chars">The characters available to construct words.</param>
		/// <returns>The known words that can be made from <paramref name="chars"/>.</returns>
		public IEnumerable<string> Search(IEnumerable<char> chars)
		{
			return Search("", chars.Select(Char.ToUpper).ToList(), _root);
		}

		/// <summary>
		/// Searches the collection for words that can be made from the specified characters.
		/// </summary>
		/// <param name="chars">The characters available to construct words.</param>
		/// <returns>The known words that can be made from <paramref name="chars"/>.</returns>
		public IEnumerable<string> Search(string chars)
		{
			return Search("", chars.ToUpper().ToList(), _root);
		}

		/// <summary>
		/// Searches the tree rooted at <paramref name="root"/> for words
		/// starting with <paramref name="prefix"/>.
		/// </summary>
		/// <param name="prefix">The word stem to look for.</param>
		/// <param name="chars">The remaining chars to search the tree for.</param>
		/// <param name="root">The tree to search.</param>
		/// <returns>Words matching <paramref name="prefix"/> and <paramref name="chars"/>.</returns>
		/// <remarks>
		/// The tree navigated up to this point, the <paramref name="root"/>,
		/// is expected to "spell" <paramref name="prefix"/>.
		/// 
		/// Also, this recursive implementation is undesirable b/c it requires
		/// iterating over IEnumerable results of recursive calls.
		/// </remarks>
		private IEnumerable<string> Search(string prefix, ICollection<char> chars, Node root)
		{
			if (root == null)
				yield break;

			// NOTE: This doesn't handle duplicates in chars.
			//       The word CRATE will be reported twice for search chars
			//       "CAERET"
			foreach (var c in chars)
			{
				// Assumes there's only at most one wildcard, and that it comes
				// at the end of chars.
				if (c == '*')
				{
					foreach (var word in WildSearch(prefix, root))
						yield return word;
					// WildSearch will have included this node if it is
					// a word node, so end this iterator block now
					// to avoid duplicating it with this method's final yield.
					yield break;
				}

				var nextPrefix = prefix + c;
				var unsearched = new List<char>(chars);
				unsearched.Remove(c);
				foreach (var word in Search(nextPrefix, unsearched, root[c]))
					yield return word;
			}

			if (root.IsWord)
				yield return prefix;
		}

		private IEnumerable<string> WildSearch(string prefix, Node root)
		{
			if (root == null)
				yield break;
			// Don't do wildcard searches on the tree root,
			// because doing so would just dump the whole tree.
			else if (root == _root)
				yield break;

			for (char c = 'A'; c <= 'Z'; ++c)
				foreach (var word in WildSearch(prefix + c, root[c]))
					yield return word;

			if (root.IsWord)
				yield return prefix;
		}

		#endregion
	}
}
