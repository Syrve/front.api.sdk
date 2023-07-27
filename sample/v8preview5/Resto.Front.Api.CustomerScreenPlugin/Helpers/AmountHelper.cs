namespace Resto.Front.Api.CustomerScreen.Helpers
{
    internal static class AmountHelper
    {
        public static string CalculateModifierAmountString(decimal modifierAmount, int defaultAmount, bool hideIfDefaultAmount, bool isPaid, bool isAmountIndependentOfParentAmount)
        {
            // Setting the display method for the number of group dish modifiers.
            var showDeltaAmount = PluginContext.Operations.GetHostRestaurant().DisplayRelativeNumberOfModifiers;

           // If the option "Quantity does not depend on the quantity of the dish" is enabled, then we always write "+N".
            if (isAmountIndependentOfParentAmount)
                return $"+{modifierAmount}";

            // If the modifier is paid or we show the absolute number of modifiers, then we write "×N".
            const string charX = "\u00D7";
            var multiplyAmountString = $"{charX}{modifierAmount}";

            if (isPaid || !showDeltaAmount)
                return multiplyAmountString;

            // Show the relative number of modifiers.
            var deltaAmount = modifierAmount - defaultAmount;

            switch (deltaAmount)
            {
                case 1:
                    return "+";
                case -1:
                    return "-";
                case 0 when hideIfDefaultAmount:
                    return string.Empty;
                case 0:
                    return multiplyAmountString;
                default:
                    return $"{deltaAmount:+#;-#;0}";
            }
        }
    }
}
