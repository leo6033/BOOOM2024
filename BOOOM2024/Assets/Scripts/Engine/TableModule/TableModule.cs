using System.Collections.Generic;
using Engine.Runtime;

namespace Engine.SettingModule
{
    public class TableModule : TSingleton<TableModule>
    {
        private static Dictionary<string, CsvReader> _csvReaders = new Dictionary<string, CsvReader>();

        public static CsvReader Get(string tableName)
        {
            if (!_csvReaders.TryGetValue(tableName, out var reader))
            {
                reader = new CsvReader();
                reader.ReadFile(tableName);
                _csvReaders[tableName] = reader;
            }

            return reader;
        }
    }
}