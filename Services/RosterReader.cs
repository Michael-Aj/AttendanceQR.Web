using AttendanceQR.Web.ViewModels;
using ClosedXML.Excel;
using System.Text;
using System.Linq; // <-- needed for Where/FirstOrDefault/Select

namespace AttendanceQR.Web.Services
{
    public static class RosterReader
    {
        private static string Canon(string? s) => (s ?? "").Trim().ToUpperInvariant();

        private static string CanonStudent(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var filtered = new string(s.Where(char.IsLetterOrDigit).ToArray());
            return filtered.ToUpperInvariant();
        }

        public static List<RosterRow> ReadRoster(Stream fileStream, string fileName)
        {
            if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return ReadXlsx(fileStream);
            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return ReadCsv(fileStream);   // line 21 - now implemented below

            // Common crash: users upload .xls (Excel 97-2003) -> ClosedXML cannot read it
            throw new InvalidOperationException("Unsupported file type. Please upload .xlsx (Excel) or .csv.");
        }

        private static List<RosterRow> ReadXlsx(Stream stream)
        {
            using var wb = new XLWorkbook(stream);

            // pick first non-empty worksheet; else clear message
            var ws = wb.Worksheets.FirstOrDefault(w => !(w.FirstCellUsed() is null && w.LastCellUsed() is null))
                     ?? wb.Worksheets.FirstOrDefault()
                     ?? throw new InvalidOperationException("The workbook has no worksheets.");

            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastCol == 0 || lastRow < 2)
                throw new InvalidOperationException("The sheet appears empty or has no data rows. Ensure row 1 has headers and row 2+ contain data.");

            // headers on row 1
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = ws.Cell(1, c).GetString()?.Trim();
                if (!string.IsNullOrEmpty(h)) headers[h.ToUpperInvariant()] = c;
            }

            int col(params string[] names)
            {
                foreach (var n in names)
                    if (headers.TryGetValue(n.ToUpperInvariant(), out var idx)) return idx;
                return -1;
            }

            var cSN = col("STUDENT NUMBER", "STUDENTID", "STUDENT_ID", "STUDENT NO", "ID");
            if (cSN == -1) throw new InvalidOperationException("Roster must include a 'Student Number' column.");

            var cFN = col("FIRST NAME", "FIRSTNAME", "GIVEN NAME");
            var cLN = col("LAST NAME", "LASTNAME", "SURNAME", "FAMILY NAME");
            var cPG = col("PROGRAMME", "QUALIFICATION", "COURSE", "PROGRAM");
            var cMC = col("MODULE CODE", "MODULE"); // optional

            string V(int r, int c) => c > 0 ? ws.Cell(r, c).GetFormattedString().Trim() : "";

            var rows = new List<RosterRow>();
            for (int r = 2; r <= lastRow; r++)
            {
                var student = CanonStudent(V(r, cSN));
                if (string.IsNullOrEmpty(student)) continue;

                rows.Add(new RosterRow
                {
                    StudentNumber = student,
                    FirstName = V(r, cFN),
                    LastName = V(r, cLN),
                    Programme = V(r, cPG),
                    ModuleCode = V(r, cMC).ToUpperInvariant()
                });
            }

            if (rows.Count == 0)
                throw new InvalidOperationException("No valid rows found. Check that 'Student Number' cells are filled.");

            return rows;
        }

        private static List<RosterRow> ReadCsv(Stream stream)
        {
            using var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = sr.ReadToEnd();
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("The CSV file is empty.");

            // Split lines
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                throw new InvalidOperationException("CSV must have a header row and at least one data row.");

            // Headers (upper-cased)
            var headers = lines[0].Split(',').Select(h => h.Trim().ToUpperInvariant()).ToArray();

            int idx(params string[] names)
            {
                foreach (var n in names.Select(x => x.Trim().ToUpperInvariant()))
                {
                    var j = Array.IndexOf(headers, n);
                    if (j >= 0) return j;
                }
                return -1;
            }

            var iSN = idx("STUDENT NUMBER", "STUDENTID", "STUDENT_ID", "STUDENT NO", "ID");
            if (iSN == -1) throw new InvalidOperationException("CSV must include a 'Student Number' column.");

            var iFN = idx("FIRST NAME", "FIRSTNAME", "GIVEN NAME");
            var iLN = idx("LAST NAME", "LASTNAME", "SURNAME", "FAMILY NAME");
            var iPG = idx("PROGRAMME", "QUALIFICATION", "COURSE", "PROGRAM");
            var iMC = idx("MODULE CODE", "MODULE"); // optional

            static string At(string[] cols, int j) => (j >= 0 && j < cols.Length) ? cols[j].Trim() : "";

            var rows = new List<RosterRow>();
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(','); // simple CSV; if you need quoted fields, use a CSV parser
                var student = CanonStudent(At(cols, iSN));
                if (string.IsNullOrEmpty(student)) continue;

                rows.Add(new RosterRow
                {
                    StudentNumber = student,
                    FirstName = At(cols, iFN),
                    LastName = At(cols, iLN),
                    Programme = At(cols, iPG),
                    ModuleCode = At(cols, iMC).ToUpperInvariant()
                });
            }

            if (rows.Count == 0)
                throw new InvalidOperationException("No valid rows found. Check that 'Student Number' cells are filled.");

            return rows;
        }
    }
}
