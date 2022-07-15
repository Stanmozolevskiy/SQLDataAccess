using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DataAccess
{
    public static class DataMapper
    {
        public static List<T> ConvertToList<T>(DataTable dataTabel)
        {
            List<string> columnNames = dataTabel.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();
            return dataTabel.AsEnumerable().Select(row =>
            {
                T objT = Activator.CreateInstance<T>();
                foreach (System.Reflection.PropertyInfo propertie in typeof(T).GetProperties())
                    if (columnNames.Contains(propertie.Name.ToLower()))
                        propertie.SetValue(objT, row[propertie.Name]);

                return objT;
            }).ToList();
        }
    }
}
