using AliceWritersNotepad.Models.Alice;

namespace AliceWritersNotepad.Static
{
    public static class Phrases
    {
        public static readonly SimpleResponse FirstRun = new SimpleResponse(
            @"Открываю «Блокнот Писателя». Назовите любое русское слово, и я расскажу его морфологию. " +
                "Вы также можете поставить заданное слово в любую форму по падежу, роду, числу, а для глаголов — " + 
                "по времени и лицу.\n\nЕщё я умею произносить слово по буквам.\n\nИтак, какое будет слово?",
            new[] { "Слово стали", "Помощь", "Выход" }
        );
        
        public static readonly SimpleResponse Hi = new SimpleResponse(
            @"Открываю «Блокнот Писателя». О каком слове вам рассказать?",
            new[] { "Слово стали", "Помощь", "Выход" }
        );
        
        public static readonly SimpleResponse Help = new SimpleResponse(
            @"Я — «Блокнот Писателя» — могу рассказать вам морфологию любого русского слова либо " +
                "поставить слово в заданную форму по падежу, роду, числу, а для глаголов — " + 
                "по времени и лицу.\n\nЕщё я умею произносить слово по буквам.\n\nИтак, какое будет слово?",
            new[] {"Навык в предложном падеже", "Выход"}
        );

        public static readonly SimpleResponse Exit = new SimpleResponse("Закрываю блокнот.");

        public static readonly SimpleResponse UnknownCommand = new SimpleResponse(
            "Назовите любое русское слово и опишите форму, в которую его поставить.",
            new[] {"Летать в прошедшем времени", "Помощь", "Выход"}
        );
        
        public static SimpleResponse UnknownWord(string word) => new SimpleResponse(
            @$"К сожалению, я пока не знаю слово «{word.ToLower()}». Попробуйте другое.",
            new[] {"Красивый женского рода", "Помощь", "Выход"}
        );

        public static readonly SimpleResponse UnknownForm = new SimpleResponse(
            "Не нашла ни одной подходящей формы, попробуйте сформулировать иначе.",
            new[] {"Зеркало во множественном числе", "Помощь", "Выход"}
        );
        
        public static readonly SimpleResponse NonExactForms = new SimpleResponse(
            "В точности такой формы не нашлось, вот ближайшее: \n\n"
        );

        public static readonly SimpleResponse StandardButtons = new SimpleResponse(
            "",
            new[] {"По буквам", "Помощь", "Выход"}
        );
    }
}