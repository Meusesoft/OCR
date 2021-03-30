using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections;

namespace OCR
{

    
   // [XmlRoot("WLE")]
    public class WordListEntry
    {
        [XmlAttribute("V")]
        public Char Character = (Char)0x00;

        [XmlArray("C")]
        [XmlArrayItem("I")]
        public List<int> Children = new List<int>(0);

        /// <summary>
        /// This function adds an ID of a child and sorts the
        /// list afterwards
        /// </summary>
        /// <param name="ChildID"></param>
        public void AddChild(int ChildID)
        {
            Children.Add(ChildID);

            Children.Sort();
        }
    }

    public class WordListWorkEntry : WordListEntry
    {
        [XmlAttribute("D")]
        public int Depth;

        [XmlAttribute("ID")]
        public int ID;

        [XmlAttribute("newID")]
        public int newID = -1;

        [XmlAttribute("Parent")]
        public int Parent;
    }

    public class SuggestionEntry
    {
        public int EditDistance;
        public String Suggestion;
    }

    public class Replacement
    {
        public Replacement(String pOriginal, String pReplacement)
        {
            Original = pOriginal;
            ReplacementString = pReplacement;
        }
        
        [XmlAttribute("O")]
        public String Original;

        [XmlAttribute("R")]
        public String ReplacementString;
    }
    
    public class CharacterReplacements
    {
        public CharacterReplacements()
        {
            Replacements = new List<Replacement>(0);

            Replacements.Add(new Replacement("iii", "m"));
            Replacements.Add(new Replacement("iii", "ni"));
            Replacements.Add(new Replacement("iii", "in"));
            Replacements.Add(new Replacement("in", "m"));
            Replacements.Add(new Replacement("ni", "m"));
            Replacements.Add(new Replacement("8", "e"));
            Replacements.Add(new Replacement("e", "8"));
            Replacements.Add(new Replacement("5", "s"));
            Replacements.Add(new Replacement("0", "o"));
            Replacements.Add(new Replacement("l", "i"));
        }

        public List<Replacement> Replacements;
    }
    
    
    public class WordList
    {
        enum ASCII {SOT = 0x02, EOT = 0x03};
        
        public WordList()
        {
            WLEs = new List<WordListEntry>(0);
            m_WLEs = new List<WordListWorkEntry>(0);
            m_CharacterReplacements = new CharacterReplacements();
        }


        /// <summary>
        /// This functions checks if this wordlist contains the given word.
        /// </summary>
        /// <param name="Word"></param>
        /// <returns></returns>
        public bool Contains(String Word)
        {
            String WordFound;
            
            if (WLEs.Count <= 1) return false;

            WordFound = Contains(WLEs[0], Word.ToLower(), "", false);

            return (WordFound == Word);
        }

        /// <summary>
        /// This functions checks if this wordlist contains the given word 
        /// and returns the word in the wordlist.
        /// </summary>
        /// <param name="Word"></param>
        /// <returns></returns>
        public String Contains(String Word, Boolean CaseSensitive)
        {
            if (WLEs.Count <= 1) return "";

            return Contains(WLEs[0], Word, "", CaseSensitive);
        }

        /// <summary>
        /// This function checks if the first Character in the word matches one of the 
        /// siblings. And if so it continues to search for the next Character in the word in 
        /// the children of this matching sibling.
        /// </summary>
        /// <param name="WLE"></param>
        /// <param name="Word"></param>
        /// <returns></returns>
        private String Contains(WordListEntry WLE, String Word, String WordListWord, Boolean CaseSensitive)
        {
            bool Result;
            Char SearchCharacter;
            WordListEntry childWLE;

            Result = false;
            SearchCharacter = (Word.Length > 0 ? Word[0] : (Char)ASCII.EOT);
            if (!CaseSensitive && SearchCharacter != (Char)ASCII.EOT) SearchCharacter = System.Char.ToLower(SearchCharacter);

            //Iterate through the children to find the matching Character.
            int index = WLE.Children.Count;

            do
            {
                index--;
                childWLE = WLEs[WLE.Children[index]];

                Char CompareChar = CaseSensitive ? childWLE.Character : System.Char.ToLower(childWLE.Character);

                if ( CompareChar == SearchCharacter)
                {
                    Result = true;
                    WLE = childWLE;
                    if (SearchCharacter != (Char)ASCII.EOT) WordListWord += WLE.Character;
                }
            }
            while (index > 0 && !Result);

            //Continue to search for the rest of the Characters in the word (if exist)
            if (Result && Word.Length > 0 && WLE.Children.Count > 0)
            {
                WordListWord = Contains(WLE, Word.Substring(1), WordListWord, CaseSensitive);
            }
            if (!Result)
            {
                WordListWord = "";
            }

            return WordListWord;
        }

        /// <summary>
        /// This function searches in the word list for suggestions / variations on the given word.
        /// The result is order by distance to the given word.
        /// </summary>
        /// <param name="Word"></param>
        /// <returns></returns>
        public List<SuggestionEntry> Suggestions(String Word)
        {
            List<SuggestionEntry> Result = new List<SuggestionEntry>(0);

            SearchSuggestion(Result, WLEs.First(), Word, "", false, 0, 2, true);

            //int index = Result.Count;

            //while (index > 0)
            //{
            //    index--;

            //    if (DamerauLevenshteinDistance(Result[index].ToLower(), Word.ToLower()) > 2)
            //    {
            //        Result.RemoveAt(index);
            //    }
            //}

            return Result;
        }

        private bool SearchSuggestion(List<SuggestionEntry> Suggestions, WordListEntry WLE, String Word, String WordListWord, Boolean CaseSensitive, int Alterations, int MaxAlterations, Boolean CheckReplacements)
        {
            Char SearchCharacter;
            WordListEntry childWLE;

            //Do not continue if we used all our alterations
            if (Alterations>MaxAlterations) return false;

            //Do not continue if this element is the End-Of-Text mark. We found a possible suggestion
            if (WLE.Character == (Char)ASCII.EOT)
            {
                Alterations += Word.Length;
                
                if (Alterations <= MaxAlterations) SearchSuggestionAdd(Suggestions, Alterations, WordListWord.Substring(0, WordListWord.Length - 1));
            }

            //Do not continue if this element doesn't have children -> nothing to compare against.
            if (WLE.Children.Count == 0) return false;

            //Use the editdistance of the first suggestion as the maxalterations value
            if (Suggestions.Count > 0)
            {
                MaxAlterations = Suggestions.First().EditDistance;
            }

            try
            {
                String ReplacementWord;
                
                //Check if the parameter Word starts with one of the replacements. If so, start
                //another comparison branche.
                if (CheckReplacements)
                {
                    foreach (Replacement Replacement in m_CharacterReplacements.Replacements)
                    {
                        if (SearchSuggestionCompareReplacement(Word, Replacement.Original))
                        {
                            ReplacementWord = Replacement.ReplacementString + Word.Substring(Replacement.Original.Length);

                            SearchSuggestion(Suggestions, WLE, ReplacementWord, WordListWord, CaseSensitive, Alterations, MaxAlterations, false);
                        }
                    }
                }
                
                SearchCharacter = (Word.Length > 0 ? Word[0] : (Char)ASCII.EOT);
                if (!CaseSensitive && SearchCharacter != (Char)ASCII.EOT) SearchCharacter = System.Char.ToLower(SearchCharacter);

                //Iterate through the children
                int index = WLE.Children.Count;

                do
                {
                    index--;
                    childWLE = WLEs[WLE.Children[index]];

                    Char CompareChar = CaseSensitive ? childWLE.Character : System.Char.ToLower(childWLE.Character);

                    SearchSuggestionCompareAndExplore(CompareChar, SearchCharacter, Suggestions, childWLE, Word, WordListWord, CaseSensitive, Alterations, MaxAlterations);
                }
                while (index > 0);

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("{0} Exception caught." + e.StackTrace.ToString());
            }

            return true;
        }
        
        /// <summary>
        /// This function adds a new suggestion to the suggestion list
        /// </summary>
        /// <param name="Suggestions"></param>
        /// <param name="EditDistance"></param>
        /// <param name="Word"></param>
        private void SearchSuggestionAdd(List<SuggestionEntry> Suggestions, int EditDistance, String Word)
        {
            SuggestionEntry newSuggestion;
            bool Added;
            int index;

            //check if the new editdistance is less than the current onein the list, if so clean the list
            if (Suggestions.Count > 0)
            {
                if (Suggestions.First().EditDistance > EditDistance) Suggestions.Clear();
            }


            //check if the Word isn't already present in the list
            index = Suggestions.Count;
            Added = true;

            while (index>0 && Added)
            {
                index--;
                if (Suggestions[index].Suggestion == Word)
                {
                    if (Suggestions[index].EditDistance > EditDistance)
                    {
                        Suggestions.RemoveAt(index);
                        Added = false;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            
            //add the new word to the list, order by distance
            Added = false;
            newSuggestion = new SuggestionEntry();
            newSuggestion.EditDistance = EditDistance;
            newSuggestion.Suggestion = Word;

            for (index = 0; index < Suggestions.Count && !Added; index++)
            {
                if (Suggestions[index].EditDistance > newSuggestion.EditDistance)
                {
                    Suggestions.Insert(index, newSuggestion);
                    Added = true;
                }
            }

            if (!Added)
            {
                Suggestions.Add(newSuggestion);
            }
        }


        /// <summary>
        /// This function compares the two characters and explores the children if
        /// there is a match
        /// </summary>
        /// <param name="CompareChar"></param>
        /// <param name="SearchCharacter"></param>
        /// <param name="Suggestions"></param>
        /// <param name="WLE"></param>
        /// <param name="Word"></param>
        /// <param name="WordListWord"></param>
        /// <param name="CaseSensitive"></param>
        /// <param name="Alterations"></param>
        /// <returns></returns>
        private void SearchSuggestionCompareAndExplore(Char CompareChar, Char SearchCharacter, 
                                                        List<SuggestionEntry> Suggestions, WordListEntry WLE, 
                                                        String Word, String WordListWord, 
                                                        Boolean CaseSensitive, int Alterations, int MaxAlterations)
        {

            String newWordListWord = WordListWord;

            
            if (CompareChar == SearchCharacter && CompareChar == (Char)ASCII.EOT && Alterations <= MaxAlterations)
            {
                //The end of the word is reached, add it as a suggestion
                SearchSuggestionAdd(Suggestions, Alterations, WordListWord);
            }

            if (Word.Length > 0)
            {
                //The characters match. Continue without a penalty to alterations
                if (CompareChar == SearchCharacter || SearchCharacter == (Char)'?')
                {
                    SearchSuggestion(Suggestions, WLE, Word.Substring(1), WordListWord + WLE.Character, CaseSensitive, Alterations, MaxAlterations, true);
                }
                else
                {
                    //Explorer the option 'replace character at current position in the word'     
                    SearchSuggestion(Suggestions, WLE, Word.Substring(1), WordListWord + WLE.Character, CaseSensitive, Alterations + 1, MaxAlterations, true);
                }
            }

            //Explorer the option 'add character at current position in the word'     
            SearchSuggestion(Suggestions, WLE, Word, WordListWord + WLE.Character, CaseSensitive, Alterations + 1, MaxAlterations, true);
        }

        /// <summary>
        /// This function checks if the Word starts with the replacement string.
        /// </summary>
        /// <param name="Word"></param>
        /// <param name="Replacement"></param>
        /// <returns></returns>
        private bool SearchSuggestionCompareReplacement(String Word, String Replacement)
        {
            bool Result;
            
            Result = true;

            if (Word.Length < Replacement.Length) return false;

            for (int index = 0; index < Replacement.Length && Result; index++)
            {
                if (Word[index] != (Char)'?' && System.Char.ToLower(Word[index]) != Replacement[index]) Result = false;
            }

            return Result;
        }

    /// <summary>
    /// This function calculates the approximation of the edit-distance between the two string
    /// See also: http://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance 
    /// </summary>
    /// <param name="string1"></param>
    /// <param name="string2"></param>
    /// <returns></returns>
    public int DamerauLevenshteinDistance(String string1, String string2)
    {
       // d is a table with lenStr1+1 rows and lenStr2+1 columns
       int[,] d = new int[string1.Length+1,string2.Length+1];
       
        // i and j are used to iterate over str1 and str2
       int i;
       int j;
       int cost;

       //for loop is inclusive, need table 1 row/column larger than string length.
       for (i = 0; i<=string1.Length; i++) d[i,0] = i;
       for (j = 0; j<=string2.Length; j++) d[0,j] = j;

       for (i = 1; i<string1.Length+1; i++)
       {
           for (j = 1; j<string2.Length+1; j++)
           {
               cost = (string1[i-1] == string2[j-1]) ? 0 : 1;
                
               d[i,j] = Math.Min(d[i-1, j] + 1, Math.Min(d[i,j-1]+1, d[i-1, j-1]+cost));
                
               if (i>1 && j>1 && string1[i-1] == string2[j-2] && string1[i-2] == string2[j-1])
               {
                   d[i,j] = Math.Min(d[i,j], d[i-2, j-2] + cost);
               }
           }
       }

       return d[string1.Length, string2.Length];
    }
        
        /// <summary>
        /// Builds the wordlist from a XML file.
        /// </summary>
        public WordList LoadXML(String Filename)
        {
            WordList newWordList;

            XmlSerializer s = new XmlSerializer(this.GetType());
            TextReader r = new StreamReader(Filename);
            newWordList = (WordList)s.Deserialize(r);
            r.Close();

            return newWordList;
        }

                /// <summary>
        /// Saves the XML file to the file system
        /// </summary>
        /// <param name="Filename"></param>
        public void SaveXML(String Filename)
        {
            StreamWriter w = new StreamWriter(Filename);
            XmlSerializer s = new XmlSerializer(this.GetType());
            s.Serialize(w, this);
            w.Close();
        }

        [XmlArray("WLEs")]
        [XmlArrayItem("WLE")]
        public List<WordListEntry> WLEs;

        private List<WordListWorkEntry> m_WLEs;
        private CharacterReplacements m_CharacterReplacements;

        /// <summary>
        /// This function builds the wordlist by processing the textbased file (word per row).
        /// </summary>
        /// <param name="Filename"></param>
        public void Build(string Filename)
        {
            StreamReader source;
            String word;
            WordListWorkEntry WLEroot;

            System.Diagnostics.Debug.WriteLine("Building WordList");
            System.Diagnostics.Debug.Indent();

            source = new StreamReader(Filename);

            //clear list
            m_WLEs.Clear();

            //create the top-node
            WLEroot = new WordListWorkEntry();
            WLEroot.Character = (Char)ASCII.SOT; //start of text
            WLEroot.Depth = 0;
            WLEroot.Parent = -1;
            WLEroot.ID = 0;
            m_WLEs.Add(WLEroot);

            //read the lines
            while ((word = source.ReadLine()) != null)
            {
                //check if the wordlist doens't already contains the given word
                if (!Contains(word))
                {
                    BuildAddWord(WLEroot, word, word, 0);
                }
            }

            source.Close();

            System.Diagnostics.Debug.WriteLine("Number of elements: " + m_WLEs.Count);
            System.Diagnostics.Debug.Unindent();

            //compress tree
            Compress();
        }

        /// <summary>
        /// This function add the word/sequence to the Wordlist
        /// </summary>
        /// <param name="WLE"></param>
        /// <param name="word"></param>
        private int BuildAddWord(WordListWorkEntry WLE, String word, String orgWord, int depth)
        {
            bool Continue;
            WordListWorkEntry newWLE;
            WordListWorkEntry childWLE;
            Char Character;
            int newDepth = 0;

            depth++;

            if (word.Length == 0)
            {
                Character = (char)ASCII.EOT;

            }
            else
            {
                Character = word[0];
            }
            
            Continue = true;
            int index = WLE.Children.Count;

            while (index > 0 && Continue)
            {
                index--;
                childWLE = m_WLEs[WLE.Children[index]];

                if (childWLE.Character == Character)
                {
                    Continue = false;

                    //Continue with the children of this item
                    if (Character != (Char)ASCII.EOT && word.Length > 0)
                    {
                        if (word.Length > 0) word = word.Substring(1);
                        newDepth = Math.Max(WLE.Depth, BuildAddWord(childWLE, word, orgWord, depth));
                    }
                    else
                    {
                        newDepth = 1;
                    }
                }
            }

            if (Continue)
            {
                //no corresponding item found, create one and continue
                newWLE = new WordListWorkEntry();
                newWLE.Character = Character;
                newWLE.Depth = 0;
                newWLE.ID = m_WLEs.Count;
                newWLE.Parent = WLE.Parent;
                m_WLEs.Add(newWLE);

                WLE.AddChild(newWLE.ID);

                //Continue with the next Character
                if (Character != (Char)ASCII.EOT)
                {
                    if (word.Length > 0) word = word.Substring(1);
                    newDepth = BuildAddWord(newWLE, word, orgWord, depth);
                }
                else
                {
                    newDepth = 1;
                }
            }

            WLE.Depth = Math.Max(WLE.Depth, newDepth);

            return WLE.Depth + 1;
        }

        /// <summary>
        /// This function combines 
        /// </summary>
        private void Compress()
        {
            //Boolean ElementsRemoved;
            //int index1, indexToSearchFor, indexCompareElement;
            //int NumberElementsRemovedTotal = 0;
            //int NumberElementsRemoved = 0;
            //int Depth = 0;
            List<int>[] WLEDepthArray;
            int index;

            System.Diagnostics.Debug.WriteLine("Start Compressing Wordlist");
            System.Diagnostics.Debug.Indent();

            System.Diagnostics.Debug.WriteLine("Number of elements " + m_WLEs.Count);

            //Fill the WLEDepthArray
            WLEDepthArray = new List<int>[m_WLEs[0].Depth + 1];

            for (int i = 0; i < WLEDepthArray.Count(); i++)
            {
                WLEDepthArray[i] = new List<int>(0);
            }

            foreach (WordListWorkEntry WLE in m_WLEs)
            {
                WLEDepthArray[WLE.Depth].Add(WLE.ID);
            }
            
                      
            //Check all elements per depth to see if they are identical
            WordListWorkEntry WLEToCompareWith;
            WordListWorkEntry WLEToCompareTo;
            
            for (int i = 0; i < WLEDepthArray.Count(); i++)
            {
                //compare elements with the same depth
                foreach (int WLEID in WLEDepthArray[i])
                {
                    WLEToCompareWith = m_WLEs[WLEID];
                    if (WLEToCompareWith.newID == -1)
                    {
                        foreach (int CompareWLEID in WLEDepthArray[i])
                        {
                            WLEToCompareTo = m_WLEs[CompareWLEID];

                            if (WLEToCompareWith.ID != WLEToCompareTo.ID &&
                                WLEToCompareTo.newID == -1 &&
                                CompareElements(WLEToCompareTo, WLEToCompareWith))
                            {
                                WLEToCompareTo.newID = WLEToCompareWith.ID;
                            }
                        }
                    }
                }

                //update the references to the new id's
                WordListWorkEntry WLENewReference;

                for (int j = i; j < WLEDepthArray.Count(); j++)
                {
                    foreach (int WLEID in WLEDepthArray[j])
                    {
                        WLENewReference = m_WLEs[WLEID];

                        index = WLENewReference.Children.Count;

                        while (index > 0)
                        {
                            index--;

                            if (m_WLEs[WLENewReference.Children[index]].newID != -1)
                            {
                                WLENewReference.Children[index] = m_WLEs[WLENewReference.Children[index]].newID;
                            }
                        }
                    }
                }
            }

            //reset all newIDs
            foreach (WordListWorkEntry item in m_WLEs)
            {
                item.newID = -1;
            }

            //rebuild the tree
            WLEs.Clear();
            CompressRebuildTree(m_WLEs[0]);            
            
            //do
            //{
            //    do
            //    {
            //        index1 = m_WLEs.Count - 1;
            //        ElementsRemoved = false;

            //        do
            //        {
            //            //if (m_WLEs[index1].Depth == Depth)
            //            {
            //                indexToSearchFor = index1;
            //                indexCompareElement = indexToSearchFor;

            //                while (indexCompareElement > 0)
            //                {
            //                    indexCompareElement--;

            //                   // if (m_WLEs[index2].Depth == Depth)
            //                    {
            //                        if (CompareElements(m_WLEs[indexToSearchFor], m_WLEs[indexCompareElement]))
            //                        {
            //                            RemoveElementAndRedirect(indexToSearchFor, indexCompareElement);
            //                            NumberElementsRemovedTotal++;
            //                            NumberElementsRemoved++;
            //                            ElementsRemoved = true;
            //                            indexToSearchFor = indexCompareElement;
            //                        }
            //                    }
            //                }
            //            }

            //            index1--;
            //            if (index1 >= m_WLEs.Count)
            //            {
            //                index1 = m_WLEs.Count - 1;
            //            }

            //        } while (index1 > 0);

                    //System.Diagnostics.Debug.WriteLine(NumberElementsRemoved.ToString());

            //    } while (ElementsRemoved);


            //    Depth++;

            //// System.Windows.MessageBox.Show(Depth.ToString());

            //} while (ElementsRemoved);

            System.Diagnostics.Debug.WriteLine("Number of elements after compression" + WLEs.Count);

            System.Diagnostics.Debug.Unindent();
            System.Diagnostics.Debug.WriteLine("End Compressing Wordlist");
        }

        private int CompressRebuildTree(WordListWorkEntry WLE)
        {
            WordListEntry newWLE;
            int newID;

            if (WLE.newID != -1) return WLE.newID;

            newWLE = new WordListEntry();
            newWLE.Character = WLE.Character;
            newID = WLEs.Count();
            WLE.newID = newID;

            WLEs.Add(newWLE);

            foreach (int childID in WLE.Children)
            {
                newWLE.AddChild(CompressRebuildTree(m_WLEs[childID]));

            }

            return newID;
        }




        /// <summary>
        /// This function returns true if both elements
        /// 1. contain the same character
        /// 2. have the same children
        /// 3. have the same siblings 
        /// </summary>
        /// <param name="Element1"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        private Boolean CompareElements(WordListWorkEntry WLE1, WordListWorkEntry WLE2)
        {
            Boolean Result = true;
            int index;

            //compare character
            Result = (WLE1.Character == WLE2.Character/* && WLE1.Depth == WLE2.Depth*/);
            if (!Result) return false;

            //compare children
            if (WLE1.Children.Count != WLE2.Children.Count) return false;

            index = WLE1.Children.Count;

            while (index > 0 && Result)
            {
                index--;
                if (WLE1.Children[index] != WLE2.Children[index]) Result = false;
            }

            return Result;
        }

    //    /// <summary>
    //    /// This function removes an element from the list en redirects all parents to the new element
    //    /// </summary>
    //    /// <param name="ElementToBeRemoved"></param>
    //    /// <param name="?"></param>
    //    private void RemoveElementAndRedirect(int ElementToBeRemoved, int ElementToPointTo)
    //    {
    //        int LastElement;
    //        WordListWorkEntry WLE;

    //        //Step 1: Walk through the list and update the links
    //        foreach (WordListWorkEntry item in m_WLEs)
    //        {
    //            if (item.FirstChild == ElementToBeRemoved) item.FirstChild = ElementToPointTo;
    //            if (item.NextSibling == ElementToBeRemoved) item.NextSibling = ElementToPointTo;
    //        }

    //        LastElement = m_WLEs.Count - 1;

    //        if (ElementToBeRemoved != LastElement)
    //        {
    //            //Step 2: Copy the last element to the current position
    //            WLE = m_WLEs[LastElement];
    //            m_WLEs[ElementToBeRemoved] = WLE;

    //            //Step 3: Walk through the list and update the links
    //            foreach (WordListWorkEntry item in m_WLEs)
    //            {
    //                if (item.FirstChild == LastElement) item.FirstChild = ElementToBeRemoved;
    //                if (item.NextSibling == LastElement) item.NextSibling = ElementToBeRemoved;
    //            }
    //        }

    //        m_WLEs.RemoveAt(LastElement);
    //    }
    }
}
