using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Controls;

namespace ModernMinas.Launcher.ValidationRules
{
    public class PositiveIntegerRule : ValidationRule
    {
        public PositiveIntegerRule()
        {
            Min = int.MinValue;
            Max = int.MaxValue;
        }

        public int Min { get; set; }

        public int Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int parameter = 0;

            try
            {
                if (((string)value).Length > 0)
                {
                    parameter = int.Parse((String)value);
                }
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or "
                                             + e.Message);
            }

            if ((parameter < this.Min) || (parameter > this.Max))
            {
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + this.Min + " - " + this.Max + ".");
            }
            return new ValidationResult(true, null);
        }
    }
}
