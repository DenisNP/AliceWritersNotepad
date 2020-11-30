using System.Linq;
using System.Text.RegularExpressions;
using AliceWritersNotepad.Models.Alice.Abstract;

namespace AliceWritersNotepad.Models.Alice
{
    public class SimpleResponse
    {
        private static readonly Regex Pattern = new Regex("\\[[a-z\\+]{1,10}\\|?[^\\[\\]]*\\]");
        
        private string _text;
        private string[] _buttons;
        
        public SimpleResponse(string text, string[] buttons = null)
        {
            _text = text;
            _buttons = buttons;
        }

        public SimpleResponse Append(string s)
        {
            _text += s;
            return this;
        }

        public SimpleResponse AddButtons(params string[] buttons)
        {
            _buttons = _buttons == null ? buttons : _buttons.Concat(buttons).ToArray();
            return this;
        }

        public SimpleResponse RemoveButtons(params string[] buttons)
        {
            _buttons = _buttons?.Except(buttons).ToArray();
            return this;
        }

        public AliceResponse Generate(AliceRequest request)
        {
            var (text, tts) = GetTextTtsPair();
            return new AliceResponse(request)
            {
                Response =
                {
                    Text = text,
                    Tts = tts,
                    Buttons = _buttons?.Select(b => new Button(b)).ToList()
                }
            };
        }

        public (string text, string tts) GetTextTtsPair()
        {
            var t = _text;
            var text = "";
            var tts = "";
            
            var match = Pattern.Match(t);
            
            // check if no found
            if (!match.Success)
            {
                return (_text, _text);
            }
            
            // if found
            while (match.Success)
            {
                // remove beginning of string
                if (match.Index > 0)
                {
                    var before = t.Substring(0, match.Index);
                    text += before;
                    tts += before;
                }

                // remove match from string
                t = t.Substring(match.Index + match.Length);
                
                // process match
                var pars = match.Value.Substring(1, match.Value.Length - 2).Split("|");
                var type = pars[0];

                switch (type)
                {
                    case "+": // accent
                        tts += "+";
                        break;
                    case "p": // pause
                        var delay = pars.Length > 1 ? int.Parse(pars[1]) : 0;
                        if (delay > 0)
                        {
                            tts += $" sil <[{delay}]> ";
                        }
                        else
                        {
                            tts += " - ";
                        }
                        break;
                    case "audio": // audio resource
                        tts += $"<speaker audio=\"{pars[1]}\">";
                        break;
                    case "screen": // only on screen
                        text += pars[1];
                        break;
                    case "voice": // only in voice
                        tts += pars[1];
                        break;
                }
                
                // find pattern again
                match = Pattern.Match(t);
            }

            text += t;
            tts += t;

            return (text, tts);
        }

        public static SimpleResponse operator +(SimpleResponse a, SimpleResponse b)
        {
            var sumText = a._text + b._text;
            
            string[] buttons = null;
            if (a._buttons != null && b._buttons != null)
                buttons = a._buttons.Concat(b._buttons).ToArray();
            else if (a._buttons != null)
                buttons = a._buttons.ToArray();
            else if (b._buttons != null) 
                buttons = b._buttons.ToArray();
            
            return new SimpleResponse(sumText, buttons);
        }
    }
}