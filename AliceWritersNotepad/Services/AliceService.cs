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
                return Phrases.Hi.Generate(request);
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

            // no word / unknown command
            if (
                !request.HasSlot(Intents.Main, Slots.Word)
                && (
                    request.State.Session.LastWord.IsNullOrEmpty()
                    ||
                    (
                        Slots.GrammemeSlots.All(s => !request.HasSlot(Intents.Main, s))
                        && !request.HasIntent(Intents.ByLetters)
                    )
                )
            )
            {
                return Phrases.UnknownCommand.Generate(request);
            }
            
            // word command
            if (request.HasSlot(Intents.Main, Slots.Word))
            {
                request.State.Session.LastWord = request.GetSlot(Intents.Main, Slots.Word);
            }

            // word not exists
            if (!_nMorph.WordExists(request.State.Session.LastWord))
            {
                request.State.Session.Clear();
                return Phrases.UnknownWord(request.State.Session.LastWord).Generate(request);
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
            var exactForms = new List<WordForm>();
            var nonExactForms = new List<WordForm>();
            foreach (var w in words)
            {
                var form = w.ClosestForm(gender, @case, number, tense, person, true);
                if (form != null)
                {
                    exactForms.Add(form);
                }
                else
                {
                    form = w.ClosestForm(gender, @case, number, tense, person, false);
                    nonExactForms.Add(form);
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

            request.State.Session.LastForm = formsToRead.First().Word;
            request.State.Session.LastFormAccent = formsToRead.First().GetAccentIndex();
            
            for (var i = 0; i < formsToRead.Count; i++)
            {
                var f = formsToRead[i];
                response += ReadSingleForm(f, i == formsToRead.Count - 1);
            }

            return response.Generate(request);
        }

        private SimpleResponse ReadByLetters(string w, int accent)
        {
            var response = $@"Читаю слово [screen|{w.ToUpper()}][voice|{(accent == -1 ? w : w.Insert(accent, "+"))}] по буквам:\n\n";
            var letters = w.Select(l => $"[screen|{l.ToString().ToUpper()}][voice|{TtsOfLetter(l.ToString())}]");

            return new SimpleResponse(
                $"{response}{string.Join("-[p|200]", letters)}",
                new[] {"Помощь", "Выход"}
            );
        }

        private SimpleResponse ReadSingleForm(WordForm f, bool isLast)
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
                Pos.Participle => "частица",
                Pos.Transgressive => "причастие",
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
                Case.None => "",
                Case.Nominative => "именительного падежа",
                Case.Genitive => "родительного падежа",
                Case.Dative => "дательного падежа",
                Case.Accusative => "винительного падежа",
                Case.Instrumental => "творительного падежа",
                Case.Prepositional => "предложного падежа",
                Case.Locative => "местного падежа",
                Case.Partitive => "частичного падежа",
                Case.Vocative => "звательного падежа",
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

            var text = $"[screen|{wordForm.ToUpper()}]" +
                       $"[voice|{(accent == -1 ? wordForm : wordForm.Insert(accent, "+"))}]: " +
                       $"{grammemes.Join(", ")}.";
            
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