using AliceWritersNotepad.Models.Alice;

namespace AliceWritersNotepad.Static
{
    public static class Phrases
    {
        public static readonly SimpleResponse Help = new SimpleResponse(
            @"""Блокнот писателя""",
            new[] {"Навык в предложном падеже", "Выход"}
        );

        public static readonly SimpleResponse Exit = new SimpleResponse("Закрываю блокнот.");

        public static readonly SimpleResponse UnknownCommand = new SimpleResponse(
            @"Назовите любое русское слово и опишите форму, в которую его поставить.",
            new[] {"Летать в прошедшем времени", "Помощь", "Выход"}
        );
        
        public static SimpleResponse UnknownWord(string word) => new SimpleResponse(
            @$"К сожалению, я пока не знаю слово ""{word.ToLower()}"". Попробуйте другое.",
            new[] {"Красивый женского рода", "Помощь", "Выход"}
        );

        public static readonly SimpleResponse UnknownForm = new SimpleResponse(
            @"Не нашла ни одной подходящей формы, попробуйте сформулировать иначе.",
            new[] {"Зеркало во множественном числе", "Помощь", "Выход"}
        );
        
        public static readonly SimpleResponse NonExactForms = new SimpleResponse(
            @"В точности такой формы не нашлось, вот ближайшее: \n\n"
        );

        public static readonly SimpleResponse StandardButtons = new SimpleResponse(
            "",
            new[] {"Помощь", "Выход"}
        );
    }
}