using Spark.Tag4D.Core.Models;
using Prism.Mvvm;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class ExpressionRuleViewModel : BindableBase
    {
        private string _sourcePropertyName = string.Empty;
        public string SourcePropertyName
        {
            get => _sourcePropertyName;
            set => SetProperty(ref _sourcePropertyName, value);
        }

        private string _function = string.Empty;
        public string Function
        {
            get => _function;
            set => SetProperty(ref _function, value);
        }

        private string _targetPropertyName = string.Empty;
        public string TargetPropertyName
        {
            get => _targetPropertyName;
            set => SetProperty(ref _targetPropertyName, value);
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public ExpressionRuleViewModel()
        {
        }

        public ExpressionRuleViewModel(ExpressionRule rule)
        {
            SourcePropertyName = rule.SourcePropertyName;
            Function = rule.Function;
            TargetPropertyName = rule.TargetPropertyName;
        }

        public ExpressionRule ToModel()
        {
            return new ExpressionRule
            {
                SourcePropertyName = SourcePropertyName,
                Function = Function,
                TargetPropertyName = TargetPropertyName
            };
        }
    }
}