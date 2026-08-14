using System;
using System.Data;
using Patient.DataAccess;
using Xunit;

namespace Patient.DataAccess.Tests
{
    public class SafeDataReaderTests
    {
        private class FakeDataRecord : IDataRecord
        {
            private readonly string[] _names;
            private readonly object?[] _values;

            public FakeDataRecord(string[] names, object?[] values)
            {
                _names = names;
                _values = values;
            }

            public int FieldCount => _names.Length;

            public bool GetBoolean(int i) => (bool)_values[i]!;
            public int GetInt32(int i) => (int)_values[i]!;
            public string GetString(int i) => (string)_values[i]!;
            public DateTime GetDateTime(int i) => (DateTime)_values[i]!;
            public object GetValue(int i) => _values[i]!;
            public string GetName(int i) => _names[i];
            public int GetOrdinal(string name)
            {
                for (int i = 0; i < _names.Length; i++) if (string.Equals(_names[i], name, StringComparison.OrdinalIgnoreCase)) return i;
                return -1;
            }
            public bool IsDBNull(int i) => _values[i] == null || _values[i] is DBNull;

            // Not used in tests - throw for clarity
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
            public char GetChar(int i) => throw new NotSupportedException();
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
            public IDataReader GetData(int i) => throw new NotSupportedException();
            public string GetDataTypeName(int i) => throw new NotSupportedException();
            public Type GetFieldType(int i) => _values[i]?.GetType() ?? typeof(object);
            public float GetFloat(int i) => throw new NotSupportedException();
            public double GetDouble(int i) => throw new NotSupportedException();
            public decimal GetDecimal(int i) => throw new NotSupportedException();
            public short GetInt16(int i) => throw new NotSupportedException();
            public long GetInt64(int i) => throw new NotSupportedException();
            public object this[string name] => GetValue(GetOrdinal(name));
            public object this[int i] => GetValue(i);
            public int GetValues(object[] values) => throw new NotSupportedException();
            public IEnumerator GetEnumerator() => throw new NotSupportedException();
        }

        [Fact]
        public void GetSafeBoolean_ReturnsValue_WhenColumnExists()
        {
            var names = new[] { "IsEmailVerified" };
            var values = new object?[] { true };
            var rec = new FakeDataRecord(names, values);

            Assert.True(UserDataAccess.GetSafeBoolean(rec, "IsEmailVerified"));
        }

        [Fact]
        public void GetSafeBoolean_ReturnsFalse_WhenMissingOrNull()
        {
            var names = Array.Empty<string>();
            var values = Array.Empty<object?>();
            var rec = new FakeDataRecord(names, values);

            Assert.False(UserDataAccess.GetSafeBoolean(rec, "IsEmailVerified"));
        }

        [Fact]
        public void GetSafeInt32_ReturnsDefault_WhenMissing()
        {
            var rec = new FakeDataRecord(Array.Empty<string>(), Array.Empty<object?>());
            Assert.Equal(0, UserDataAccess.GetSafeInt32(rec, "UserID"));
        }

        [Fact]
        public void GetSafeString_ReturnsNull_WhenMissing()
        {
            var rec = new FakeDataRecord(Array.Empty<string>(), Array.Empty<object?>());
            Assert.Null(UserDataAccess.GetSafeString(rec, "Email"));
        }

        [Fact]
        public void GetSafeDateTime_ReturnsDefault_WhenMissing()
        {
            var rec = new FakeDataRecord(Array.Empty<string>(), Array.Empty<object?>());
            var def = new DateTime(2000,1,1);
            Assert.Equal(def, UserDataAccess.GetSafeDateTime(rec, "DOB", def));
        }
    }
}
