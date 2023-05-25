using System.Collections.Generic;
using System;
using System.Data;
using System.Linq;

namespace DataAccess
{
    public static class DataMapper
    {
        public static List<T> ConvertToList<T>(DataTable dataTabel)
        {
            List<string> columnNames = dataTabel.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();
            var test = dataTabel.AsEnumerable().Select(row =>
            {
                T objT = Activator.CreateInstance<T>();
                foreach (System.Reflection.PropertyInfo propertie in typeof(T).GetProperties())
                    if (columnNames.Contains(propertie.Name.ToLower()))
                        propertie.SetValue(objT, row[propertie.Name]);

                return objT;
            }).ToList();
            return test;
        }
    }
}
