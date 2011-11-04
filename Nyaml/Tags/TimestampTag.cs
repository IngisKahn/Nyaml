namespace Nyaml.Tags
{
    using System;
    using System.Text.RegularExpressions;

    public class Timestamp : Scalar<DateTime>
    {
        internal Timestamp()
        {
            this.Name = "tag:yaml.org,2002:timestamp";
        }

        static readonly Regex dateExpression = new Regex(@"^((?<y>\d{4})-(?<m>\d\d)-(?<d>\d\d)|(?<y>\d{4})-(?<m>\d\d?)-(?<d>\d\d?)([tT]|[ \t]+)(?<h>\d\d?):(?<n>\d\d):(?<s>\d\d)(?<f>\.\d*)?([ \t]*(Z|(?<zh>[-+]\d\d?)(:(?<zm>\d\d))?))?)$", RegexOptions.Compiled);

        public override Func<string, string> CanonicalFormatter
        {
            get
            {
                return v => ToString(Parse(v));
            }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && dateExpression.IsMatch(node.Content);
        }

        private static DateTime Parse(string time)
        {
            var match = dateExpression.Match(time);
            var year = int.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var month = int.Parse(match.Groups["m"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var day = int.Parse(match.Groups["d"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var hour = int.Parse("0" + match.Groups["h"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var min = int.Parse("0" + match.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var sec = int.Parse("0" + match.Groups["s"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var fracString = "0" + match.Groups["f"].Value;
            if (fracString == "0.")
                fracString = "0";
            var frac = double.Parse(fracString, System.Globalization.CultureInfo.InvariantCulture);
            var zoneHour = int.Parse("0" + match.Groups["zh"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var zoneMin = int.Parse("0" + match.Groups["zm"].Value, System.Globalization.CultureInfo.InvariantCulture);

            return new DateTime(year, month, day, hour - zoneHour, min - zoneMin, sec, (int)(frac * 1000), DateTimeKind.Utc);
        }

        private static string ToString(DateTime time)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                           "{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}{6}Z",
                           time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second,
                           time.Millisecond != 0 ? (time.Millisecond / 1000f).ToString("F", System.Globalization.CultureInfo.InvariantCulture).Substring(1) : "");
        }

        protected override DateTime Construct(Nodes.Base node)
        {
            return Parse(((Nodes.Scalar)node).Content);
        }

        public override Nodes.Base Represent(DateTime value) { return new Nodes.Scalar<DateTime> { ScalarTag = this, Content = ToString(value) }; }
    }
}
