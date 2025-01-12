using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TP.Data
{
    public abstract class TpDataObject
    {
        protected static int GetInt(IDataReader reader, string fieldName)
        {
            int fieldIndex = reader.GetOrdinal(fieldName);
            if (reader.IsDBNull(fieldIndex)) return 0;
            return reader.GetInt32(fieldIndex);
        }

        protected static string GetString(IDataReader reader, string fieldName)
        {
            return reader.GetString(reader.GetOrdinal(fieldName));
        }

        protected static DateTime GetDateTime(IDataReader reader, string fieldName)
        {
            int fieldIndex = reader.GetOrdinal(fieldName);
            if (reader.IsDBNull(fieldIndex)) return new DateTime(1899, 12, 30);
            return reader.GetDateTime(fieldIndex);
        }

        protected static bool GetBool(IDataReader reader, string fieldName)
        {
            int fieldIndex = reader.GetOrdinal(fieldName);
            if (reader.IsDBNull(fieldIndex)) return false;
            return reader.GetBoolean(fieldIndex);
        }

        protected static int GetInt(XmlReader reader, string attributeName)
        {
            return int.TryParse(reader.GetAttribute(attributeName), out int val) ? val : 0;
        }

        protected static string GetString(XmlReader reader, string attributeName)
        {
            return reader.GetAttribute(attributeName);
        }

        // https://stackoverflow.com/questions/21723697/regex-to-extract-date-time-from-given-string
        protected static DateTime GetDateTime(XmlReader reader, string fieldName)
        {
            Regex r = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}");
            System.Text.RegularExpressions.Match m = r.Match(GetString(reader, fieldName));
            if (m.Success)
            {
                return DateTime.ParseExact(m.Value, "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            }
            return new DateTime(1899, 12, 30);
        }
    }
}
