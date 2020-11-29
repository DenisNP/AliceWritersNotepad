using AliceWritersNotepad.Models.Alice.Abstract;

namespace AliceWritersNotepad.Models.Alice
{
    public class AliceResponse : AliceResponseBase<UserState, SessionState>
    {
        public AliceResponse(
            AliceRequest request,
            SessionState sessionState = default,
            UserState userState = default
        ) : base(request, sessionState, userState) { }
    }
}