using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ETWControler.UI
{
    class PortnumberValidator : ValidationRule
    {
        public int Min
        {
            get;
            set;
        }

        public int Max
        {
            get;
            set;
        }

        public PortnumberValidator()
        {
            Min = 500;
        }


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int port = 0;

            try
            {
                if (((string)value).Length > 0)
                    port = Int32.Parse((String)value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((port < Min) || (port > Max))
            {
                return new ValidationResult(false,
                  "Please enter a port in the range: " + Min + " - " + Max + ".");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}
