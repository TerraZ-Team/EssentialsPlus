namespace EssentialsPlus.Extensions
{
    public static class StringExtensions
    {
        public static string GetIndex(int number)
        {
            int absNumber = number < 0 ? -number : number;
            int lastTwo = absNumber % 100;
            int last = absNumber % 10;

            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return number + "th";
            }

            return last switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th",
            };
        }
    }
}
