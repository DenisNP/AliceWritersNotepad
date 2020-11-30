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
        private readonly NestorMorph _nMorph;
        
        public AliceService()
        {
            _nMorph = new NestorMorph();
        }
        
        public AliceResponse HandleRequest(AliceRequest request)
        {
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
            
            // no word / unknown command
            if (
                !request.HasSlot(Intents.Main, Slots.Word)
                && (
                    request.State.Session.LastWord.IsNullOrEmpty()
                    || Slots.GrammemeSlots.All(s => !request.HasSlot(Intents.Main, s))
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
                request.State.Session.LastWord = "";
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
            var exactForms = new List<(Word, WordForm)>();
            var nonExactForms = new List<(Word, WordForm)>();
            foreach (var w in words)
            {
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
            
            for (var i = 0; i < formsToRead.Count; i++)
            {
                var (w, f) = formsToRead[i];
                response += ReadSingleForm(w, f, i == formsToRead.Count - 1);
            }

            return response.Generate(request);
        }

        private SimpleResponse ReadSingleForm(Word w, WordForm f, bool isLast)
        {
            throw new NotImplementedException();
        }

        private T ParseEnum<T>(AliceRequest request, string slot) where T : struct
        {
            var hasEnum = Enum.TryParse<T>(request.GetSlot(Intents.Main, slot), out var result);
            return hasEnum ? result : default;
        }
    }
}