using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FRDB_SQLite;
using FRDB_SQLite;

namespace FRDB_SQLite
{
    public class FzDataTypeBLL
    {
        #region 1. Fields (none)

        #endregion

        #region 2. Properties (None)
        #endregion

        #region 3. Contructors (None)
        #endregion

        #region 4. Methods

        public static Boolean CheckDataType(Object value, FzDataTypeEntity dataType)
        {
            return FzDataTypeDAL.CheckDataType(value, dataType);
        }

        #endregion

        #region 5. Privates (none)
        #endregion
    }
}
