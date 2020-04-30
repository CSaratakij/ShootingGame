using System;

namespace MyGame.Network
{
    public class Global
    {
        public const string CLIENT_ID = "5e9ca315c1b0796906e0e7e2";

        public static string AccessToken = "";
        public static string Email = "";
        public static uint ExpiresIn = 0;
        public static string RefreshToken = "";
        public static DateTime IssuedAt = DateTime.UtcNow;
        public static string UserID = "";
        public static string DisplayName = "";
        public static uint Level;
        public static uint TotalExp;
        public static uint TotalPlay;
        public static uint TotalKill;
        public static uint MaxKil;

        new public static string ToString()
        {
            return $"AccessToken : {AccessToken}\n" + 
                   $"Email : {Email}\n" + 
                   $"ExpiresIn : {ExpiresIn}\n" + 
                   $"RefreshToken : {RefreshToken}\n" +
                   $"IssuedAt : {IssuedAt}\n" +
                   $"UserID : {UserID}\n" +
                   $"DisplayName : {DisplayName}\n";
        }
    }
}
