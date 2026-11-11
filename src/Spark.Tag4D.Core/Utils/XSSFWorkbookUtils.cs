using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;

namespace Spark.Tag4D.Core.Utils
{
    public class XSSFWorkbookUtils
    {
        public static IWorkbook CreateExcelFile(string filePath, string defaultSheetName)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(defaultSheetName);

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(stream);
            }

            return workbook;
        }

        public static XSSFWorkbook OpenExcelFile(string filePath, FileAccess fileAccessMode)
        {
            if (filePath.Equals(string.Empty))
            {
                return null;
            }
            XSSFWorkbook xlsWorkbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, fileAccessMode))
            {
                xlsWorkbook = new XSSFWorkbook(file);
            }
            return xlsWorkbook;
        }

        public static XSSFWorkbook OpenExcelFileReadWrite(string filePath)
        {
            if (filePath.Equals(string.Empty))
            {
                return null;
            }
            XSSFWorkbook xlsWorkbook;
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                //открываем файл
                xlsWorkbook = new XSSFWorkbook(file);
            }
            return xlsWorkbook;
        }

        public static void WriteAndClose(IWorkbook workbook, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(stream);
                stream.Flush();
                stream.Close();
            }
        }
    }

}
