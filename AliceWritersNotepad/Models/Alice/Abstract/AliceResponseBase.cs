using System.Collections.Generic;

namespace AliceWritersNotepad.Models.Alice.Abstract
{
    public class Button
    {
        public string Title { get; set; }
        public Dictionary<string, string> Payload { get; set; }
        public string Url { get; set; }
        public bool Hide { get; set; } = true;

        public Button() { }

        public Button(string b)
        {
            Title = b;
        }
    }

    public class Response
    {
        public string Text { get; set; }
        public string Tts { get; set; }
        public List<Button> Buttons { get; set; }
        public bool EndSession { get; set; }
    }

    public class AliceResponseBase<TUserState, TSessionState>
    {
        public AliceEmpty StartAccountLinking { get; set; }
        public Response Response { get; set; } = new Response();
        public Session Session { get; set; }
        public string Version { get; set; }

        public TUserState UserStateUpdate { get; set; }
        public TSessionState SessionState { get; set; }

        public AliceResponseBase(
            AliceRequestBase<TUserState, TSessionState> request,
            TSessionState sessionState = default,
            TUserState userState = default
        )
        {
            Session = request.Session;
            Version = request.Version;
            SessionState = sessionState ?? request.State.Session;
            UserStateUpdate = userState ?? request.State.User;
        }
        
        public AliceResponseBase<TUserState, TSessionState> ToAuthorizationResponse()
        {
            Response = null;
            StartAccountLinking = new AliceEmpty();
            return this;
        }

        public AliceResponseBase<TUserState, TSessionState> ToPong()
        {
            Response = new Response
            {
                Text = "pong"
            };
            return this;
        }
    }
}