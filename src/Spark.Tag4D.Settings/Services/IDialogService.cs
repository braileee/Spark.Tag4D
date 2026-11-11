using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Spark.Tag4D.Settings.Services
{
    public interface IDialogService
    {
        MessageBoxResult ShowSaveChanges();

        void ShowError(string message);

        void ShowInformation(string message);
    }
}
