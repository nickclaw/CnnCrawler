using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

namespace AccessPoint
{
    public class TrieNode
    {
        private Dictionary<char, TrieNode> children;
        private char? value;
        private bool endOfWord = false;

        /**
         * Constructs the TrieNode that stores a character or null
         */
        public TrieNode(char? character)
        {   
            value = character;
            children = new Dictionary<char, TrieNode>();
        }

        /**
         * Recursively builds branches as needed to finish the word 
         */
        public void build(string word)
        {
            if (word.Length > 0)
            {
                char firstChar = word[0];
                if (!children.ContainsKey(firstChar))
                {
                    children.Add(firstChar, new TrieNode(firstChar));
                }
                children[firstChar].build(word.Substring(1));
            }
            else
            {
                endOfWord = true;
            }
        }

        public List<string> search(string word,int max) {
            TrieNode node = this.find(word);
            if (node != null)
            {
                return node.getWords(word, new List<string>(), max, true);
            }
            else
            {
                return new List<string>();
            }
        }

        /**
         * Recursively searches the tree to find the last node of a given input
         * Returns null if nothing
         */
        private TrieNode find(string word)
        {
            if (word.Length > 0)
            {
                char firstChar = word[0];
                if (children.ContainsKey(firstChar))
                {
                    return children[firstChar].find(word.Substring(1));
                } else {
                    return null;
                }
            }
            return this;
        }

        /**
         * Return the 10 first words of a given node 
         */
        private List<string> getWords(string word, List<string> list,int max, bool first)
        {
            if (!first)
            {
                word = word + value;
            }

            if (this.isWord())
            {
                list.Add(word.Replace('_', ' '));
            }

            foreach (KeyValuePair<char, TrieNode> node in children)
            {
                if (list.Count >= max)
                {
                    return list;
                }
                list = node.Value.getWords(word, list, max, false);
            }
            return list;
        }

        public bool isWord()
        {
            return endOfWord;
        }
    }
}