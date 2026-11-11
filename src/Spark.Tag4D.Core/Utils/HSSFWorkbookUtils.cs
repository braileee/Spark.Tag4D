using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Utils
{
    public class HSSFWorkbookUtils
    {
        public static HSSFWorkbook OpenExcelFile(string filePath, FileAccess fileAccessMode)
        {
            if (filePath.Equals(string.Empty))
            {
                return null;
            }
            HSSFWorkbook xlsWorkbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, fileAccessMode))
            {
                //открываем файл
                xlsWorkbook = new HSSFWorkbook(file);
            }
            return xlsWorkbook;
        }
    }
}
