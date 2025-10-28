using SamsCSVParser.Object;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SamsCSVParser.Services
{
    internal class CSVService : ICSVService
    {
        [Description($"Takes A Collection of Arrays of Strings split and will attempt to turn that into a collection of your Object T, this Object must implement IColumn correctly")]
        public ICollection<T>? ParseCSVInfo<T, Attr>(ICollection<string[]> split) where Attr : IColumn where T : Attr, new()
        {
            if (split.Count < 2)
            {
                return null;
            }

            string[] templateRow = split.First();
            Dictionary<string, int> columnIndexing = [];

            for (int i = 0; i < templateRow.Length; i++)
            {
                string colHeader = templateRow[i].Trim().ToUpperInvariant();
                columnIndexing.TryAdd(colHeader, i);
            }

            int numCols = columnIndexing.Values.Max() + 1;
            PropertyInfo[] properties = new PropertyInfo[numCols];
            Regex[] propValidators = new Regex[numCols];
            TypeConverter[] propconverters = new TypeConverter[numCols];

            foreach (PropertyInfo p in typeof(T).GetProperties())
            {
                object[] attributes = p.GetCustomAttributes(inherit: true);

                foreach (object attribute in attributes)
                {
                    if (attribute is not Attr csvAttribute)
                    {
                        continue;
                    }

                    if (!columnIndexing.TryGetValue(csvAttribute.Name.ToUpperInvariant(), out int index))
                    {
                        if (!csvAttribute.ValidationRegex.IsMatch(string.Empty))
                        {
                            return null;
                        }
                        break;
                    }

                    properties[index] = p;
                    propValidators[index] = csvAttribute.ValidationRegex;
                    propconverters[index] = TypeDescriptor.GetConverter(p.PropertyType);
                }
            }

            List<T> objectList = [];

            for (int i = 1; i < split.Count; i++)
            {
                bool abortLine = false;
                string[] line = split.Skip(i).First();
                T obj = new();

                for (int col = 0; col < properties.Length; col++)
                {
                    string cur = col < line.Length ? line[col] : string.Empty;
                    PropertyInfo prop = properties[col];

                    if (prop == null)
                    {
                        continue;
                    }

                    bool valid = propValidators[col].IsMatch(cur);

                    if (!valid)
                    {
                        abortLine = true;
                        break;
                    }

                    object? value = propconverters[col].ConvertFromString(cur);
                    prop.SetValue(obj, value, null);
                }
                if (!abortLine)
                {
                    objectList.Add(obj);
                }
            }
            return objectList;
        }

        [Description("This Method splits the file into rows, default number or columns to process is 2 but this can be changed, requires a type T and attribute of type T as Attr," +
                     " this method will delete columns not specified in the attribute Attr's Name variable, also Attr is required to Implement IColumn")]
        public ICollection<string[]>? SplitFile<T, Attr>(string filePath, Encoding encoder, string varDelimiter, int numberOfColumnsToProcess = 2) where T : Attr, new() where Attr : IColumn
        {
            if (new FileInfo(filePath).Length != 0)
            {

                List<string[]> lines = [];
                string[] headers;
                using (StreamReader file = new(filePath))
                {
                    string? line = file.ReadLine();
                    if (line != null)
                    {
                        string[]? values = line.Split(varDelimiter, StringSplitOptions.TrimEntries);
                        if (varDelimiter == "\",\"")
                        {
                            values[0] = values[0].Replace("\"", string.Empty);
                            values[^1] = values[^1].Replace("\"", string.Empty);
                        }
                        headers = values;
                        while (line != null)
                        {
                            if (values.Length != headers.Length)
                            {
                                line = file.ReadLine();
                                throw new Exception("we may have a problem, or not?, idk what causes this tbh, maybe the entire csv is too big to read and it doesnt delete the entire columns before reprocessing," +
                                                  $" anyway Value row length = {values.Length}, Headers row length = {headers.Length}, please do check if youre are feeding this a malformed csv before blaming my library though :)");
                            }
                            else
                            {
                                lines.Add(values);
                                line = file.ReadLine();
                                if (line != null)
                                {
                                    values = line.Split(varDelimiter, StringSplitOptions.TrimEntries);
                                    if (varDelimiter == "\",\"")
                                    {
                                        values[0] = values[0].Replace("\"", string.Empty);
                                        values[^1] = values[^1].Replace("\"", string.Empty);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                if (headers.Length == numberOfColumnsToProcess)
                {
                    return lines;
                }
                else
                {
                    List<string> checkList = [];
                    foreach (PropertyInfo p in typeof(T).GetProperties())
                    {
                        Attr? attr = p.GetCustomAttribute<Attr>();
                        if (attr != null)
                        {
                            PropertyInfo? nameProp = attr.GetType().GetProperty("Name");
                            if (nameProp != null)
                            {
                                string? nameValue = nameProp.GetValue(attr)?.ToString();
                                if (nameValue != null)
                                {
                                    checkList.Add(nameValue);
                                }
                            }
                            else
                            {
                                throw new Exception($"{typeof(Attr).Name} Does not contain a Name variable, plz add ok :), ({typeof(T).Name} also needs to have variables with this attribute, did you even read my description??)");
                            }
                        }
                    }

                    IEnumerable<string> toRemove = headers.Except(checkList);

                    foreach (string remove in toRemove)
                    {
                        int index = headers.ToList().FindIndex(n => n == remove);
                        RemoveColumnByIndex(filePath, index, varDelimiter);
                    }

                    return SplitFile<T, Attr>(filePath, encoder, varDelimiter, numberOfColumnsToProcess);
                }
            }
            else
            {
                throw new Exception($"File \"{filePath}\" is empty");
            }
        }

        [Description("for columnNames, please include delimiters for example if your delimiter was [\",\"] please format as \"column1\",\"column2\" , if you do not supply a totalNumberOfColumns, it will go off the String columnNames ")]
        public void PrependColumnNames(string file, string columnNames, string varDelimiter, int? totalNumberOfColumns = null)
        {
            int columnsPresent = columnNames.Split(varDelimiter).Length;
            totalNumberOfColumns = totalNumberOfColumns ?? columnsPresent;
            string currContex = string.Empty;
            if (File.Exists(file))
            {
                currContex = File.ReadAllText(file);
            }
            if (totalNumberOfColumns == columnsPresent)
            {
                if (!currContex.StartsWith($"{columnNames}"))
                {
                    File.WriteAllText(file, $"{columnNames} {Environment.NewLine}{currContex}");
                }
            }
            if (totalNumberOfColumns > columnsPresent)
            {
                string emptyCol = string.Empty;
                for (int i = 0; i < totalNumberOfColumns - columnsPresent; i++)
                {
                    emptyCol += $"To Remove {varDelimiter}";
                }
                if (!currContex.StartsWith($"{emptyCol}{columnNames}"))
                {
                    File.WriteAllText(file, $"{emptyCol}{columnNames} {Environment.NewLine}{currContex}");
                }
            }
        }
        [Description("for columnNames, please include delimiters for example if your delimiter was [\",\"] please format as \"column1\",\"column2\", if you do not supply a totalNumberOfColumns, it will go off the String columnNames ")]
        public void AppendColumnNames(string file, string columnNames, string varDelimiter, int? totalNumberOfColumns = null)
        {

            int columnsPresent = columnNames.Split(varDelimiter).Length;
            totalNumberOfColumns = totalNumberOfColumns ?? columnsPresent;
            string currContex = string.Empty;
            if (File.Exists(file))
            {
                currContex = File.ReadAllText(file);
            }
            if (totalNumberOfColumns == columnsPresent)
            {
                if (!currContex.StartsWith($"{columnNames}"))
                {
                    File.WriteAllText(file, $"{columnNames}{Environment.NewLine}{currContex}");
                }
            }
            if (totalNumberOfColumns > columnsPresent)
            {
                string emptyCol = string.Empty;
                for (int i = 0; i < totalNumberOfColumns - columnsPresent; i++)
                {
                    emptyCol += $" {varDelimiter} To Remove";
                }
                if (!currContex.StartsWith($"{columnNames}"))
                {
                    File.WriteAllText(file, $"{columnNames}{emptyCol} {Environment.NewLine}{currContex}");
                }
            }
        }
        [Description("will work for most cases, could do with a rework, in otherwords make a backup file before running this, I am warning you!!")]
        public void RemoveColumnByIndex(string filePath, int index, string delimiter)
        {
            if (delimiter.Length == 3)
            {
                string[] dels = delimiter.Split();
                delimiter = dels[2] + dels[3];
            }

            List<string> lines = [];

            using (StreamReader reader = new(filePath))
            {
                string? line = reader.ReadLine();
                List<string> values = [];
                while (line != null)
                {
                    values.Clear();
                    string[] cols = line.Split(delimiter);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (i != index)
                        {
                            values.Add(cols[i]);
                        }
                    }
                    string newLine = string.Join(delimiter, values);
                    lines.Add(newLine);
                    line = reader.ReadLine();
                }
                reader.Dispose();
            }
            using (FileStream file = new(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                using (StreamWriter writer = new(file))
                {
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                    writer.Dispose();
                }
            }
        }
    }
}