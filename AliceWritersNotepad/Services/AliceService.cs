using System;
using System.Collections.Generic;
using System.Linq;
using AliceWritersNotepad.Models.Alice;
using AliceWritersNotepad.Static;
using Nestor;
using Nestor.Models;

namespace AliceWritersNotepad.Services
{
    public class AliceService
    {
        private NestorMorph _nMorph;
        
        public void Load()
        {
            _nMorph = new NestorMorph();
        }
        
        public AliceResponse HandleRequest(AliceRequest request)
        {
            // start
            if (request.IsEnter())
            {
                var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var resp = request.State.User.LastEnter < currentTime - 30 * 24 * 3600000L
                    ? Phrases.FirstRun.Generate(request)
                    : Phrases.Hi.Generate(request);

                resp.UserStateUpdate.LastEnter = currentTime;
                return resp;
            }
            
            // help command
            if (request.HasIntent(Intents.YandexHelp1) || request.HasIntent(Intents.YandexHelp2))
            {
                return Phrases.Help.Generate(request);
            }
            
            // exit
            if (request.HasIntent(Intents.Exit))
            {
                var exit = Phrases.Exit.Generate(request);
                exit.Response.EndSession = true;
                return exit;
            }
            
            // by letters
            if (request.HasIntent(Intents.ByLetters))
            {
                var w = request.GetSlot(Intents.ByLetters, Slots.Word);
                var accent = -1;
                
                if (w.IsNullOrEmpty() && !request.State.Session.LastForm.IsNullOrEmpty())
                {
                    w = request.State.Session.LastForm;
                    accent = request.State.Session.LastFormAccent;
                } 
                else if (!w.IsNullOrEmpty() && _nMorph.WordExists(w))
                {
                    request.State.Session.Clear();
                    request.State.Session.LastWord = w;
                }
                
                return ReadByLetters(w, accent).Generate(request);
            }
            
            // has word
            var filler = new []{"слово", "слова", "слов", "словом"};
            // workaround
            var startsFromFiller = request.Request.Nlu.Tokens.Count == 2 
                                   && filler.Contains(request.Request.Nlu.Tokens[0]);
            
            var hasWord = request.HasSlot(Intents.Main, Slots.Word) 
                            || request.Request.Nlu.Tokens.Count == 1
                            || startsFromFiller;

            var changeForm = Slots.GrammemeSlots.Any(s => request.HasSlot(Intents.Main, s));

            // no word / unknown command
            if (
                !hasWord
                && (
                    request.State.Session.LastWord.IsNullOrEmpty()
                    || (!changeForm && !request.HasIntent(Intents.ByLetters))
                )
            )
            {
                return Phrases.UnknownCommand.Generate(request);
            }
            
            // word command
            if (hasWord)
            {
                request.State.Session.LastWord = request.HasSlot(Intents.Main, Slots.Word)
                    ? request.GetSlot(Intents.Main, Slots.Word)
                    : startsFromFiller 
                        ? request.Request.Nlu.Tokens[1] 
                        : request.Request.Nlu.Tokens.First();
            }

            // word not exists
            if (!_nMorph.WordExists(request.State.Session.LastWord))
            {
                var resp = Phrases.UnknownWord(request.State.Session.LastWord).Generate(request);
                request.State.Session.Clear();
                return resp;
            }

            // word exists, find it
            var words = _nMorph.WordInfo(request.State.Session.LastWord);
            
            // get all slots
            var pos = ParseEnum<Pos>(request, Slots.Pos);
            var number = ParseEnum<Number>(request, Slots.Number);
            var gender = ParseEnum<Gender>(request, Slots.Gender);
            var @case = ParseEnum<Case>(request, Slots.Case);
            var tense = ParseEnum<Tense>(request, Slots.Tense);
            var person = ParseEnum<Person>(request, Slots.Person);
            
            // filter by pos if possible
            if (pos != Pos.None && words.Any(w => w.Tag.Pos == pos))
            {
                words = words.Where(w => w.Tag.Pos == pos).ToArray();
            }

            // find forms
            var exactForms = new List<(Word, WordForm)>();
            var nonExactForms = new List<(Word, WordForm)>();
            foreach (var w in words)
            {
                
                if (!changeForm)
                {
                    // get form from input word
                    var formsFound = w.ExactForms(request.State.Session.LastWord);
                    exactForms.AddRange(formsFound.Select(f => (w, f)));
                }
                else
                {
                    // get form from input data
                    var form = w.ClosestForm(gender, @case, number, tense, person, true);
                    if (form != null)
                    {
                        exactForms.Add((w, form));
                    }
                    else
                    {
                        form = w.ClosestForm(gender, @case, number, tense, person, false);
                        nonExactForms.Add((w, form));
                    }
                }
            }

            // no forms found
            if (exactForms.Count == 0 && nonExactForms.Count == 0)
            {
                return Phrases.UnknownForm.Generate(request);
            }

            // read all found forms
            var response = Phrases.StandardButtons;
            var formsToRead = exactForms;
            if (exactForms.Count == 0)
            {
                formsToRead = nonExactForms;
                response += Phrases.NonExactForms;
            }

            request.State.Session.LastForm = formsToRead.First().Item2.Word;
            request.State.Session.LastFormAccent = formsToRead.First().Item2.GetAccentIndex();
            
            for (var i = 0; i < formsToRead.Count; i++)
            {
                var (w, f) = formsToRead[i];
                response += ReadSingleForm(w, f, i == formsToRead.Count - 1);
            }

            return response.Generate(request);
        }

        private SimpleResponse ReadByLetters(string w, int accent)
        {
            var response = $@"Читаю слово [screen|{(accent == -1 ? w.ToUpper() : w.ToUpper().Insert(accent + 1, " ́"))}]" +
                           $"[voice|{(accent == -1 ? w : w.Insert(accent, "+"))}] по буквам:\n\n";
            
            var letters = w.Select(l => $"[screen|{l.ToString().ToUpper()}][voice|{TtsOfLetter(l.ToString())}]");

            return new SimpleResponse(
                $"{response}{string.Join("-[p|200]", letters)}",
                new[] {"Помощь", "Выход"}
            );
        }

        private SimpleResponse ReadSingleForm(Word w, WordForm f, bool isLast)
        {
            var wordForm = f.Word;
            var accent = f.GetAccentIndex();

            var grammemes = new List<string>();
            
            // pos
            grammemes.Add(f.Tag.Pos switch
            {
                Pos.None => "",
                Pos.Noun => "существительное",
                Pos.Verb => "глагол",
                Pos.Adjective => "прилагательное",
                Pos.Adverb => "наречие",
                Pos.Numeral => "числительное",
                Pos.Participle => "причастие",
                Pos.Transgressive => "междометие",
                Pos.Pronoun => "местоимение",
                Pos.Preposition => "предлог",
                Pos.Conjunction => "союз",
                Pos.Particle => "частица",
                Pos.Interjection => "деепричастие",
                Pos.Predicative => "предикатив",
                _ => ""
            });
            
            // others
            grammemes.Add(f.Tag.Case switch
            {
                Case.None => f.Tag.Pos == Pos.Noun ? "несклоняемое" : "",
                Case.Nominative => "в именительном падеже",
                Case.Genitive => "в родительном падеже",
                Case.Dative => "в дательном падеже",
                Case.Accusative => "в винительном падеже",
                Case.Instrumental => "в творительном падеже",
                Case.Prepositional => "в предложном падеже",
                Case.Locative => "в местном падеже",
                Case.Partitive => "в частичном падеже",
                Case.Vocative => "в звательном падеже",
                _ => ""
            });
            
            grammemes.Add(f.Tag.Gender switch
            {
                Gender.None => "",
                Gender.Masculine => "мужского рода",
                Gender.Feminine => "женского рода",
                Gender.Neuter => "среднего рода",
                Gender.Common => "общего рода",
                _ => ""
            });
            
            grammemes.Add(f.Tag.Number switch
            {
                Number.None => "",
                Number.Singular => "единственного числа",
                Number.Plural => "множественного числа",
                _ => ""
            });
            
            grammemes.Add(f.Tag.Person switch
            {
                Person.None => "",
                Person.First => "первого лица",
                Person.Second => "второго лица",
                Person.Third => "третьего лица",
                _ => ""
            });
            
            grammemes.Add(f.Tag.Tense switch
            {
                Tense.None => "",
                Tense.Past => "прошедшего времени",
                Tense.Present => "настоящего времени",
                Tense.Future => "будущего времени",
                Tense.Infinitive => "инфинитив",
                _ => ""
            });
            
            // combine
            grammemes.RemoveAll(g => g.IsNullOrEmpty());
            if (grammemes.Count == 0) grammemes.Add("данных о форме слова нет");

            var text = $"[screen|{(accent == -1 ? wordForm.ToUpper() : wordForm.ToUpper().Insert(accent + 1, " ́"))}]" +
                              $"[voice|{(accent == -1 ? wordForm : wordForm.Insert(accent, "+"))}]";
            
            // add lemma if current form is not
            if (w.Lemma.Word != f.Word)
            {
                accent = w.Lemma.GetAccentIndex();
                text += $"[screen| ({(accent == -1 ? w.Lemma.Word.ToUpper() : w.Lemma.Word.ToUpper().Insert(accent + 1, " ́"))})]" +
                        $"[p|150][voice| {(accent == -1 ? w.Lemma.Word : w.Lemma.Word.Insert(accent, "+"))}]";
            }
            
            // add grammemes
            text += $": {grammemes.Join(", ")}.";
            
            if (!isLast) text += "\n\n";
            return new SimpleResponse(text);
        }

        private T ParseEnum<T>(AliceRequest request, string slot) where T : struct
        {
            var hasEnum = Enum.TryParse<T>(request.GetSlot(Intents.Main, slot), out var result);
            return hasEnum ? result : default;
        }
        
        private static string TtsOfLetter(string letter)
        {
            switch (letter.ToUpper())
            {
                case "Б": case "В": case "Г": case "Д": case "Ж": case "З": case "П": case "Т": case "Ц": case "Ч":
                    return letter.ToLower() + "э";
                case "К": case "Х": case "Ш": case "Щ": case "А":
                    return letter.ToLower() + "а";
                case "Л": case "М": case "Н": case "Р": case "С": case "Ф":
                    return "э" + letter.ToLower();
                case "И":
                    return "ии";
                case "Й":
                    return "и краткое";
                case "Ь":
                    return "мягкий знак";
                case "Ъ":
                    return "твёрдый знак";
                default:
                    return letter.ToLower();
            }
        }
    }
}