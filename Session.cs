namespace MeroDokan
{
    public static class Session
    {
        public static int UserId { get; set; } = -1;
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;

        public static bool IsLoggedIn => UserId != -1;

        public static void Clear()
        {
            UserId = -1;
            Username = string.Empty;
            FullName = string.Empty;
            Role = string.Empty;
        }
    }
}
