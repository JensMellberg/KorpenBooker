namespace KorpenBooker
{
    public class Storage
    {
        private const string FileName = "Storage.txt";

        public static Storage GetFromFile()
        {
            if (!File.Exists(FilePath()))
            {
                return new Storage();
            }

            var text = File.ReadAllText(FilePath());
            var tokens = text.Split('|');

            return new Storage
            {
                SessionCookie = !string.IsNullOrEmpty(tokens[0]) ? tokens[0] : null,
                UserId = !string.IsNullOrEmpty(tokens[1]) ? tokens[1] : null
            };
        }

        public string? SessionCookie { get; set; }

        public string? UserId { get; set; }

        public void SaveToFile()
        {
            File.WriteAllText(FilePath(), (SessionCookie ?? "") + "|" + (UserId ?? ""));
        }

        private static string FilePath() => Path.Combine(AppContext.BaseDirectory, FileName);
    }
}
