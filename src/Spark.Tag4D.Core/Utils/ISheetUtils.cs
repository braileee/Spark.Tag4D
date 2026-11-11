using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Spark.Tag4D.Core.Utils.HSSFSheetUtils;

namespace Spark.Tag4D.Core.Utils
{
    public class ISheetUtils
    {
        public static void InsertRows(ref ISheet sheet1, int fromRowIndex, int rowCount)
        {
            try
            {
                if (rowCount == 0)
                    return;
                sheet1.ShiftRows(fromRowIndex + 1, sheet1.LastRowNum, rowCount);
                var rowSource = sheet1.GetRow(fromRowIndex);
                for (int rowIndex = fromRowIndex; rowIndex < fromRowIndex + rowCount; rowIndex++)
                {
                    //var rowSource = sheet1.GetRow(rowIndex + rowCount);
                    var rowInsert = sheet1.CreateRow(rowIndex + 1);
                    if (rowInsert == null || rowSource == null)
                    {
                        continue;
                    }
                    rowInsert.Height = rowSource.Height;
                    for (int colIndex = 0; colIndex < rowSource.LastCellNum; colIndex++)
                    {
                        var cellSource = rowSource.GetCell(colIndex);
                        var cellInsert = rowInsert.CreateCell(colIndex);
                        if (cellSource != null)
                            cellInsert.CellStyle = cellSource.CellStyle;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public static void CopySourceRowToAnother(XSSFWorkbook workbook, ISheet sheet1, int fromRowIndex, int rowCount)
        {
            for (int i = 0; i < rowCount; i++)
            {
                CopyRow(workbook, sheet1, fromRowIndex, fromRowIndex + i + 1);
            }
        }

        public static void CopySourceRowToAnotherWithoutCreation(XSSFWorkbook workbook, ISheet sheet1, int fromRowIndex, int rowCount)
        {
            //for (int rowIndex = fromRowIndex; rowIndex < fromRowIndex + rowCount; rowIndex++)
            //{
            //    CopyRow(workbook, sheet1, fromRowIndex, fromRowIndex + 1);
            //}
            for (int i = 0; i < rowCount; i++)
            {
                CopyRowWithoutCreate(workbook, sheet1, fromRowIndex, fromRowIndex + i + 1);
            }
        }


        public static void CopySourceRowToAnotherWithoutCreation(HSSFWorkbook workbook, ISheet sheet1, int fromRowIndex, int rowCount)
        {
            //for (int rowIndex = fromRowIndex; rowIndex < fromRowIndex + rowCount; rowIndex++)
            //{
            //    CopyRow(workbook, sheet1, fromRowIndex, fromRowIndex + 1);
            //}
            for (int i = 0; i < rowCount; i++)
            {
                CopyRowWithoutCreate(workbook, sheet1, fromRowIndex, fromRowIndex + i + 1);
            }
        }
    }
}
