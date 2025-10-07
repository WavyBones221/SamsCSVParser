using System.Text.RegularExpressions;

namespace SamsCSVParser.Object
{
    public class IColumn : Attribute
    {
        private static readonly string defaultRegex = "^.*$";
        public string Name { get; private set; }
        public Regex ValidationRegex { get; private set; }

        public IColumn(string name) : this(name, null) { }
        public IColumn(string name, string? validationRegex)
        {
            this.Name = name;
            this.ValidationRegex = new Regex(validationRegex ?? defaultRegex, RegexOptions.ExplicitCapture);
        }
    }
}
