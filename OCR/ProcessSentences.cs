using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCR;

namespace OCR
{
    class ProcessSentences : WorkThread
    {
        public PageImage Image;
        private WordList m_Wordlist;

        public void Execute()
        {
            WorkPackage WorkPackage;
            int[] Assignment;

            Console.WriteLine("Execute " + this.GetType().ToString());

            //Read the wordlist from disc
            WordList tempWordList = new WordList();
            m_Wordlist = tempWordList.LoadXML("d:\\ocr\\wordlist.xml");
            
            //create a workpackage per thread.
            for (int i = 0; i < ThreadCount; i++)
            {
                WorkPackage = new WorkPackage();
                WorkPackage.Method = ExecuteActivity;
                WorkPackage.Image = Image;

                Assignment = new int[2];
                Assignment[0] = i; //start index
                Assignment[1] = ThreadCount; //step size through list

                WorkPackage.Assignment = (object)Assignment;
                WorkPackage.Parameter = (object)m_Wordlist;
                RunPackage(WorkPackage);
            }

            WaitForWorkDone(this.GetType().ToString());
        }

        
        /// <summary>
        /// This function process the workpackage for processing of the sentences
        /// </summary>
        /// <param name="Parameter"></param>
        public static void ExecuteActivity(object Parameter)
        {
            //Initialize the workpackage
            WorkPackage WorkPackage = (WorkPackage)Parameter;
            int[] Assignment = (int[])WorkPackage.Assignment;

            //Run the package
            for (int index = Assignment[0];
                 index < WorkPackage.Image.Sentences.Count;
                 index += Assignment[1])
            {                
                ProcessSentence(WorkPackage.Image.Sentences[index], (WordList)WorkPackage.Parameter);
            }

            SignalWorkDone();
        }

        /// <summary>
        /// This function processes a sentence; build the content and check/repair imperfections
        /// </summary>
        /// <param name="Sentence"></param>
        private static void ProcessSentence(Sentence Sentence, WordList WordList)
        {
            int Start = 0;
            int End = 0;
            Sentence.Content = "";

            foreach (PageComponent Component in Sentence.Components)
            {
                switch (Component.Type)
                {
                    case ePageComponentType.eSpace:
                        {
                            if (End != Start)
                            {
                                Sentence.Content += ProcessWord(Sentence, Start, End, WordList);
                                Start = End;
                            }
                            End++;
                            Sentence.Content += " ";
                            break;
                        }

                    default:
                        {
                            End++;
                            break;
                        }
                    }
                }

            if (Start!=End)
            {
                Sentence.Content += ProcessWord(Sentence, Start, End, WordList);
            }
        }

        private static String ProcessWord(Sentence Sentence, int Start, int End, WordList WordList)
        {
            String Result;
            String Content;
            List<SuggestionEntry> Suggestions;

            Result = "";

            for (int index = Start; index < End; index++)
            {
                Content = Sentence.Components[index].Content;

                if (Content == "connected") Content = "??";
                if (Content == "garbage") Content = "?";

                Result += Content;
            }

            if (DebugTrace.DebugTrace.ApplyWordList)
            {
                //check if there is a match in the wordlist
                if (!WordList.Contains(Result))
                {
                    System.Console.WriteLine(Result);

                    Suggestions = WordList.Suggestions(Result);

                    if (Suggestions.Count > 0)
                    {
                        Result = Suggestions.First().Suggestion;
                    }
                }
            }

            return Result;
        }
    }
}
