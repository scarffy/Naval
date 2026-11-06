using System;
using System.Collections.Generic;
using System.Globalization;

namespace Naval.Core
{
    /// <summary>A data file is missing something or holds a bad value. The message names the file, row and column.</summary>
    public sealed class DataException : Exception
    {
        public DataException(string message) : base(message) { }
    }

    /// <summary>A comma-separated file with a header row. Cells are trimmed; fields cannot contain commas.</summary>
    public sealed class CsvTable
    {
        readonly Dictionary<string, int> columns = new Dictionary<string, int>();

        public string FileName { get; private set; }
        public IReadOnlyList<string> Header { get; private set; }
        public IReadOnlyList<CsvRow> Rows { get; private set; }

        public CsvTable(string fileName, string text)
        {
            FileName = fileName;
            if (text == null) throw new DataException(fileName + ": no text to read");
            string[] lines = text.TrimStart('\uFEFF').Split('\n');
            var header = new List<string>();
            var rows = new List<CsvRow>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Trim().Length == 0) continue;
                string[] cells = line.Split(',');
                for (int c = 0; c < cells.Length; c++) cells[c] = cells[c].Trim();
                if (header.Count == 0)
                {
                    for (int c = 0; c < cells.Length; c++)
                    {
                        if (columns.ContainsKey(cells[c]))
                            throw new DataException(fileName + ": column '" + cells[c] + "' appears twice");
                        columns[cells[c]] = c;
                        header.Add(cells[c]);
                    }
                    continue;
                }
                if (cells.Length != header.Count)
                    throw new DataException(fileName + ", row " + (i + 1) + ": has " + cells.Length +
                                            " values but the header has " + header.Count);
                rows.Add(new CsvRow(this, i + 1, cells));
            }
            if (header.Count == 0) throw new DataException(fileName + ": the file is empty");
            Header = header;
            Rows = rows;
        }

        internal int ColumnIndex(string column)
        {
            int index;
            if (!columns.TryGetValue(column, out index))
                throw new DataException(FileName + ": missing column '" + column + "'");
            return index;
        }
    }

    /// <summary>One data row. The typed getters throw a <see cref="DataException"/> naming file, row and column.</summary>
    public sealed class CsvRow
    {
        readonly CsvTable table;
        readonly string[] cells;

        /// <summary>Line number in the file, counting the header as line 1.</summary>
        public int Line { get; private set; }

        internal CsvRow(CsvTable table, int line, string[] cells)
        {
            this.table = table;
            Line = line;
            this.cells = cells;
        }

        public DataException Error(string column, string message)
        {
            return new DataException(table.FileName + ", row " + Line + ", column '" + column + "': " + message);
        }

        /// <summary>The cell as written, empty if blank.</summary>
        public string Raw(string column)
        {
            return cells[table.ColumnIndex(column)];
        }

        public string Text(string column)
        {
            string value = Raw(column);
            if (value.Length == 0) throw Error(column, "value is missing");
            return value;
        }

        public int Int(string column, int min)
        {
            string value = Text(column);
            int result;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                throw Error(column, "'" + value + "' is not a whole number");
            if (result < min) throw Error(column, value + " is below the minimum " + min);
            return result;
        }

        public double Number(string column, double min, double max)
        {
            return Parse(column, Text(column), min, max);
        }

        /// <summary>A number, or null when the cell is blank.</summary>
        public double? OptionalNumber(string column, double min, double max)
        {
            string value = Raw(column);
            if (value.Length == 0) return null;
            return Parse(column, value, min, max);
        }

        public bool YesNo(string column)
        {
            string value = Text(column);
            if (value == "yes") return true;
            if (value == "no") return false;
            throw Error(column, "'" + value + "' must be yes or no");
        }

        double Parse(string column, string value, double min, double max)
        {
            double result;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                double.IsNaN(result) || double.IsInfinity(result))
                throw Error(column, "'" + value + "' is not a number");
            if (result < min || result > max)
                throw Error(column, value + " is outside the allowed range " + min + " to " + max);
            return result;
        }
    }
}
