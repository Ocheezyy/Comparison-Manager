using Syncfusion.WinForms.Controls;

namespace DeathVerificationFW
{
    public partial class IgnoreReasonDlg : SfForm
    {
        public IgnoreReasonDlg()
        {
            InitializeComponent();
        }

        public string BuildText()
        {
            var finalString = "";
            var cb1 = checkBox1.Checked;
            var cb2 = checkBox2.Checked;
            var cb3 = checkBox3.Checked;
            if (cb1 == true) { finalString += " Location"; }
            if (cb2 == true) { finalString += " Name"; }
            if (cb3 == true) { finalString += " Lexis Nexis"; }

            return finalString;
        }

        public string IgnoreText
        {
            get { return BuildText();  }
        }
    }
}
