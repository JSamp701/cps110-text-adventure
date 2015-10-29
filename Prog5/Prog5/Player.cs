using System;
using System.IO;
using System.Collections.Generic;

namespace Prog5
{
    using Sentence = System.Collections.Generic.List<Phrase>;

    class InvalidSentenceException : Exception
    {
        public string sentence;
    }
    class InvalidCommandException : Exception
    {
        public string command;
    }
    class InvalidRoomException : Exception
    {
        public string room;
    }

    class Phrase
    {
        public string command;
        public string info;
    }

    class Coord
    {
        public int sentenceIndex;
        public int phraseIndex;
    }

    class Script
    {
        public List<Sentence> sentences;
        public Dictionary<string, int> roomToSentenceNumber;
        public List<Coord> currentPositions;
        public string lastInput;
        public bool done;
    }

    class Player
    {
        const string AUTHOR_INFO = "CpS 110 Program 5: Text Adventure Interpreter, by Joel Sampson (jsamp701)";
        static void Main(string[] args)
        {
            Console.WriteLine(AUTHOR_INFO + "\n");
            if (args.Length > 0)
            {
                Script script = parseFile(args[0]);
                interpretFile(script);
            }
            else
            {
                Console.WriteLine("usage: prog5.exe GAME_SCRIPT_FILE\n");
            }
        }

        static Script parseFile(string fname)
        {
            Script script = new Script();
            script.sentences = new List<Sentence>();
            script.roomToSentenceNumber = new Dictionary<string, int>();
            script.currentPositions = new List<Coord>();
            script.done = false;
            script.lastInput = "";
            using (StreamReader reader = new StreamReader(fname))
            {
                string line;
                int lineno;
                for(line = reader.ReadLine(), lineno = 0; line != null; line = reader.ReadLine(), ++lineno)
                {
                    if (line == "")
                    {
                        lineno = lineno - 1;
                        continue;
                    }
                    if (line[0] == '>')
                    {
                        line = line.Replace(' ', '+');
                        line = "println " + line.Substring(1);
                    }
                    string[] linearray = line.Split(' ');
                    if(linearray.Length % 2 == 0)
                    {
                        //proceed as normal, the sentence is well formed
                        Sentence s = new Sentence();
                        for(int i = 0; i < linearray.Length; i = i + 2)
                        {
                            Phrase p = new Phrase();
                            p.command = linearray[i];
                            p.info = linearray[i + 1].Replace('+', ' ');
                            s.Add(p);
                            if(p.command != "room") { ; }
                            else script.roomToSentenceNumber.Add(p.info, lineno);
                        }
                        script.sentences.Add(s);
                    }
                    else
                    { 
                        //oops, the sentence has an odd number of words.  Die gracefully...
                        InvalidSentenceException e = new InvalidSentenceException();
                        e.sentence = line;
                        throw e;
                    }
                }
            }
            Coord c = new Coord();
            c.sentenceIndex = 0;
            c.phraseIndex = 0;
            script.currentPositions.Add(c);
            return script;
        }

        static void interpretFile(Script script)
        {
            while (!script.done)
            {
                Coord current = script.currentPositions[script.currentPositions.Count - 1];
                Sentence currSentence = script.sentences[current.sentenceIndex];
                Phrase currPhrase = currSentence[current.phraseIndex];
                switch(currPhrase.command)
                {
                    case ("room"):
                        doRoom(currPhrase.info, script);
                        break;
                    case ("print"):
                        doPrint(currPhrase.info, script);
                        break;
                    case ("println"):
                        doPrintln(currPhrase.info, script);
                        break;
                    case ("goto"):
                        doGoto(currPhrase.info, script);
                        break;
                    case ("end"):
                        doEnd(currPhrase.info, script);
                        break;
                    case ("prompt"):
                        doPrompt(currPhrase.info, script);
                        break;
                    case ("on"):
                        doOn(currPhrase.info, script);
                        break;
                    case ("set"):
                        doSet(currPhrase.info, script);
                        break;
                    case ("clear"):
                        doClear(currPhrase.info, script);
                        break;
                    case ("if"):
                        doIf(currPhrase.info, script);
                        break;
                    case ("unless"):
                        doUnless(currPhrase.info, script);
                        break;
                    case ("visit"):
                        doVisit(currPhrase.info, script);
                        break;
                    case ("return"):
                        doReturn(currPhrase.info, script);
                        break;
                    default:
                        InvalidCommandException e = new InvalidCommandException();
                        e.command = currPhrase.command;
                        throw e;
                }
            }
            return;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /*
        Helper functions for interpreting the script.  Each of them receives the information it needs to process the instruction.
        Each modify the indices in script
        */
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        static void doRoom(string info, Script script)
        {
            advanceIndex(script, true);
        }

        static void doPrint(string info, Script script)
        {
            Console.Write(info);
            advanceIndex(script, true);
        }

        static void doPrintln(string info, Script script)
        {
            Console.WriteLine(info);
            advanceIndex(script, true);
        }

        static void doGoto(string info, Script script)
        {
            if (script.roomToSentenceNumber.ContainsKey(info))
            {
                Coord currentCoord = script.currentPositions[script.currentPositions.Count - 1];
                currentCoord.phraseIndex = 0;
                currentCoord.sentenceIndex = script.roomToSentenceNumber[info];
            }
            else
            {
                InvalidRoomException e = new InvalidRoomException();
                e.room = info;
                throw e;
            }
        }

        static void doEnd(string info, Script script)
        {
            script.done = true;
        }

        static void doPrompt(string info, Script script)
        {
            throw new NotImplementedException();
            Console.Write(info);
            script.lastInput = Console.ReadLine();
        }

        static void doOn(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void doSet(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void doClear(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void doIf(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void doUnless(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void doVisit(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void doReturn(string info, Script script)
        {
            throw new NotImplementedException();
        }

        static void advanceIndex(Script script, bool continueSentence)
        {
            Coord currentCoord = script.currentPositions[script.currentPositions.Count - 1];
            Sentence currentSentence = script.sentences[currentCoord.sentenceIndex];
            if (currentCoord.phraseIndex == currentSentence.Count - 1 || !continueSentence)
            {
                //advance to the next sentence
                if(script.sentences.Count - 1 > currentCoord.sentenceIndex + 1) // there exists another sentence
                {
                    //advance sentence counter
                    currentCoord.sentenceIndex = currentCoord.sentenceIndex + 1;
                }
                else //that was the last sentence
                {
                    script.done = true;
                }
            }
            else currentCoord.phraseIndex = currentCoord.phraseIndex + 1; //advance the phrase counter
        }
    }
}
