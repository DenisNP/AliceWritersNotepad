namespace AliceWritersNotepad.Models
{
    public class SessionState
    {
        public string LastWord { get; set; } = "";
        public string LastForm { get; set; } = "";
        public int LastFormAccent { get; set; } = -1;

        public void Clear()
        {
            LastWord = "";
            LastForm = "";
            LastFormAccent = -1;
        }
    }
}