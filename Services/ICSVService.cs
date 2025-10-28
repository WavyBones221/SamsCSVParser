using SamsCSVParser.Object;
using System.ComponentModel;
using System.Text;

namespace SamsCSVParser.Services
{
    public interface ICSVService
    {
        [Description("This Method splits the file into rows, default number or columns to process is 2 but this can be changed, requires a type T and attribute of type T as Attr," +
                    " this method will delete columns not specified in the attribute Attr's Name variable, also Attr is required to have a variable called Name")]
        public abstract ICollection<string[]>? SplitFile<T, Attr>(string filePath, Encoding encoder, string varDelimiter, int numberOfColumnsToProcess = 2) where Attr : IColumn where T : Attr, new();
        [Description($"Takes A Collection of Arrays of Strings split and will attempt to turn that into a collection of your Object T, this Object must implement IColumn correctly")]
        public abstract ICollection<T>? ParseCSVInfo<T, Attr>(ICollection<string[]> split) where Attr : IColumn where T : Attr, new();
        [Description("for columnNames, please include delimiters for example if your delimiter was [\",\"] please format as \"column1\",\"column2\" , if you do not supply a totalNumberOfColumns, it will go off the String columnNames ")]
        public abstract void PrependColumnNames(string file, string columnNames, string varDelimiter, int? totalNumberOfColumns = null);
        [Description("for columnNames, please include delimiters for example if your delimiter was [\",\"] please format as \"column1\",\"column2\" , if you do not supply a totalNumberOfColumns, it will go off the String columnNames ")]
        public abstract void AppendColumnNames(string file, string columnNames, string varDelimiter, int? totalNumberOfColumns = null);
        [Description("will work for most cases, could do with a rework, in otherwords make a backup file before running this, I am warning you!!")]
        public abstract void RemoveColumnByIndex(string filePath, int index, string delimiter);
    }
}
