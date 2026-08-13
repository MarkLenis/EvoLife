using System.Globalization;

namespace EvoLife.UI
{
    /// <summary>
    /// Safe ratio formatting for empty populations. Never divides by zero.
    /// </summary>
    public static class RatioFormatter
    {
        public const string NotAvailable = "n/a";

        public static string PredatorPrey(int predators, int herbivores)
        {
            if (herbivores <= 0)
            {
                return NotAvailable;
            }

            return ((float)predators / herbivores).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
