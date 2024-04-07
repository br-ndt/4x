using System;

public static class Randomizer
{
    public static string RandomString(string source = "", int length = 4)
    {
        Random res = new();

        // String of alphabets  
        string str = source != "" ? source : "abcdefghijklmnopqrstuvwxyz";

        // Initializing the empty string 
        string ran = "";

        for (int i = 0; i < length; i++)
        {

            // Selecting a index randomly 
            int x = res.Next(26);

            // Appending the character at the  
            // index to the random string. 
            ran += str[x];
        }

        return ran;
    }
}