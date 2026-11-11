namespace Spark.Tag4D.Microstation
{
    public sealed class Keyins
    {
        public static void LoadParameters(string unparsed)
        {
            App.Instance.LoadParameters();
        }

        public static void TagElements(string unparsed)
        {
            App.Instance.TagElements();
        }

        public static void ShowSettings(string unparsed)
        {
            App.Instance.ShowSettings();
        }
    }
}