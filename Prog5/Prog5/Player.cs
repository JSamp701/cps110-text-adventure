using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Prog5
{
    using Sentence = System.Collections.Generic.List<Phrase>;//used to alias List<Phrase> as Sentence, for convenience sake

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
    class InvalidReturnException : Exception
    {

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
        public Dictionary<string, bool> switches;
        public string lastInput;
        public string lastTarget;
        public bool done;
    }

    class Player
    {
        const string AUTHOR_INFO = "CpS 110 Program 5: Text Adventure Interpreter, by Joel Sampson (jsamp701)";
        const int SLEEP_MILLISECONDS = 10;
        static void Main(string[] args)
        {
            Console.WriteLine(AUTHOR_INFO + "\n");
            if (args.Length > 0)
            {
                try {
                    Script script = parseFile(args[0]);
                    interpretFile(script);
                } catch(InvalidSentenceException e)
                {
                    Console.WriteLine("ERROR PARSING FILE: SENTENCE WAS INVALID. SENTENCE: " + e.sentence);
                } catch(InvalidCommandException e)
                {
                    Console.WriteLine("ERROR INTERPRETING FILE: COMMAND WAS INVALID. COMMAND: " + e.command);
                } catch(InvalidRoomException e)
                {
                    Console.WriteLine("ERROR INTERPRETING FILE: ROOM WAS INVALID. ROOM: " + e.room);
                } catch(InvalidReturnException e)
                {
                    Console.WriteLine("ERROR INTERPRETING FILE: RETURN WAS INVALID.  INTERPRETER TRIED TO RETURN FROM NO VISIT.");
                }
            }
            else
            {
                Console.WriteLine("usage: prog5.exe GAME_SCRIPT_FILE\n");
            }
        }

        //reads in the file and populates the script structure with it
        static Script parseFile(string fname)
        {
            Script script = new Script();
            script.sentences = new List<Sentence>();
            script.roomToSentenceNumber = new Dictionary<string, int>();
            script.currentPositions = new List<Coord>();
            script.done = false;
            script.lastInput = "";
            script.switches = new Dictionary<string, bool>();
            using (StreamReader reader = new StreamReader(fname))
            {
                string line;
                int lineno; //used for storing rooms
                for (line = reader.ReadLine(), lineno = 0; line != null; line = reader.ReadLine(), ++lineno)
                {
                    if (line == "") //ignore empty lines
                    {
                        lineno = lineno - 1;
                        continue;
                    }
                    if (line[0] == '>') //auto-replace the ">" "with println "
                    {
                        line = line.Replace(' ', '+');
                        line = "println " + line.Substring(1) + "+++++"; //so that you can '>' an empty line
                    }
                    string[] linearray = line.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if(linearray.Length % 2 == 0) //make sure there are only valid sentences with valid phrases
                    {
                        //proceed as normal, the sentence is well formed
                        Sentence s = new Sentence();
                        for(int i = 0; i < linearray.Length; i = i + 2) //add all the phrases to the sentence
                        {
                            Phrase p = new Phrase();
                            p.command = linearray[i];
                            p.info = linearray[i + 1].Replace('+', ' ');
                            s.Add(p);
                            if(p.command != "room") { ; }
                            else script.roomToSentenceNumber.Add(p.info, lineno); //add rooms to the rooms dictionary
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

        //interprets the sentences of file
        static void interpretFile(Script script)
        {
            while (!script.done)
            {
                //get where we are in the file and shove the current phrase through the switch
                Coord current = script.currentPositions[script.currentPositions.Count - 1]; 
                Sentence currSentence = script.sentences[current.sentenceIndex];
                Phrase currPhrase = currSentence[current.phraseIndex];
                switch(currPhrase.command)
                {
                    case ("room"):
                        doRoom(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("print"):
                        doPrint(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("println"):
                        doPrintln(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("goto"):
                        doGoto(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("end"):
                        doEnd(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("prompt"):
                        doPrompt(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("on"):
                        doOn(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("set"):
                        doSet(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("clear"):
                        doClear(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("if"):
                        doIf(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("unless"):
                        doUnless(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("visit"):
                        doVisit(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("return"):
                        doReturn(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("wait"):
                        doWait(modifyInfo(currPhrase.info, script), script);
                        break;
                    case ("chance"):
                        doChance(modifyInfo(currPhrase.info, script), script);
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

        //substitutes "$" for the name of the last room entered
        static string modifyInfo(string info, Script script)
        {
            return info.Replace("$", script.lastTarget);
        }

        //helper function for printing things like a teletype machine would
        static void printTeletypey(string info)
        {
            foreach(char c in info)
            {
                Console.Write(c);
                Thread.Sleep(SLEEP_MILLISECONDS);
            }
        }

        //basically, just advances the indexes
        static void doRoom(string info, Script script)
        {
            advanceIndex(script, true);
        }

        //shoves info into the teletypey printer and advances the index
        static void doPrint(string info, Script script)
        {
            //Console.Write(info);
            printTeletypey(info);
            advanceIndex(script, true);
        }

        //shoves info (along with a newline character) into the teletyper printer and advances the index
        static void doPrintln(string info, Script script)
        {
            //Console.WriteLine(info);
            printTeletypey(info + "\n");
            advanceIndex(script, true);
        }

        //updates last target and then updates the most recent indices
        static void doGoto(string info, Script script)
        {
            if (script.roomToSentenceNumber.ContainsKey(info))
            {
                Coord currentCoord = script.currentPositions[script.currentPositions.Count - 1];
                currentCoord.phraseIndex = 0;
                currentCoord.sentenceIndex = script.roomToSentenceNumber[info];
                script.lastTarget = info;
            }
            else
            {
                InvalidRoomException e = new InvalidRoomException();
                e.room = info;
                throw e;
            }
        }

        //sets a flag stating the game is done
        static void doEnd(string info, Script script)
        {
            script.done = true;
        }

        //prints out a prompt, and then stores the input and advances the index
        static void doPrompt(string info, Script script)
        {
            Console.Write(info + " ");
            script.lastInput = Console.ReadLine();
            advanceIndex(script, true);
        }

        //advances the index based on whether info is found in the last input.  
        // NOTES-FOR-MR.-JUECKSTOCK: Should this be case sensitive?
        static void doOn(string info, Script script)
        {
            advanceIndex(script, script.lastInput.IndexOf(info) != -1);
        }

        //sets a value in the rooms dictionary and advances the index
        static void doSet(string info, Script script)
        {
            script.switches[info] = true;
            advanceIndex(script, true);
        }

        //clears a value in the rooms dictionary and advances the index
        static void doClear(string info, Script script)
        {
            script.switches[info] = false;
            advanceIndex(script, true);
        }

        //advances the index based on a value (or lack thereof) in the rooms dictionary
        static void doIf(string info, Script script)
        {
            if (script.switches.ContainsKey(info))
                advanceIndex(script, script.switches[info]);
            else
            {
                script.switches[info] = false;
                advanceIndex(script, false);
            }
        }

        //advances the index based on the inverse of a value (or lack thereof) in the rooms dictionary
        static void doUnless(string info, Script script)
        {
            if (script.switches.ContainsKey(info))
                advanceIndex(script, !script.switches[info]);
            else
            {
                script.switches[info] = false;
                advanceIndex(script, true);
            }
        }

        //creates a new index and moves to it
        static void doVisit(string info, Script script)
        {
            if (script.roomToSentenceNumber.ContainsKey(info))
            {
                /*
                Coord currentCoord = script.currentPositions[script.currentPositions.Count - 1];
                currentCoord.phraseIndex = 0;
                currentCoord.sentenceIndex = script.roomToSentenceNumber[info];
                */
                Coord newCoord = new Coord();
                newCoord.phraseIndex = 0;
                newCoord.sentenceIndex = script.roomToSentenceNumber[info];
                script.currentPositions.Add(newCoord);
            }
            else
            {
                InvalidRoomException e = new InvalidRoomException();
                e.room = info;
                throw e;
            }
        }

        //removes an index and returns to the one beneath it.  
        // NOTES-FOR-MR.-JUECKSTOCK: If there are no other indices, something has gone wrong.  Should this be mentioned?
        static void doReturn(string info, Script script)
        {
            script.currentPositions.RemoveAt(script.currentPositions.Count - 1);
            if (script.currentPositions.Count != 0)
                advanceIndex(script, false);
            else
            {
                InvalidReturnException e = new InvalidReturnException();
                throw e;
                //script.done = true;
            }
        }

        //waits a specified number of seconds, and then advances the index
        static void doWait(string info, Script script)
        {
            int seconds = Convert.ToInt32(info);
            Thread.Sleep(seconds * 1000);
            advanceIndex(script, true);
        }

        //generates a random number and compares it to the provided number in info.  
        //If the random number is greater, then the sentence continues execution.  
        //Otherwise, the next sentence will execute
        static void doChance(string info, Script script)
        {
            int percent = Convert.ToInt32(info);
            Random rng = new Random();
            int chance = rng.Next(101);
            advanceIndex(script, chance >= percent);
        }

        //controls the logic for indexing through sentences and phrases.
        //if continueSentence is false, it will attempt to skip the rest of the current sentence and move on to the next sentence
        //if continueSentence is true, it will attempt to resume processing the current sentence
        // NOTES-FOR-MR.-JUECKSTOCK: Is there a defined case for when there are no more lines to execute?  I just assumed and said that that case
        // specified end of game.
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
                    currentCoord.phraseIndex = 0;
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
